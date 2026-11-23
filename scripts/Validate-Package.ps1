[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string] $PackagePath,
    [string] $ReportPath,
    # When packaging, the DLL SHA-256 recorded by the build receipt: the archived DLL must be exactly that DLL.
    [string] $ExpectedDllSha256
)

. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
Assert-FileExists $PackagePath 'Package ZIP'
# An explicitly supplied expected digest must be well formed; only an omitted parameter skips the comparison.
if ($PSBoundParameters.ContainsKey('ExpectedDllSha256') -and $ExpectedDllSha256 -cnotmatch '^[0-9a-f]{64}$') {
    throw 'ExpectedDllSha256 must be a lowercase SHA-256 string.'
}
$PackagePath = [IO.Path]::GetFullPath($PackagePath)

$expectedInfoPath = Join-Path $root 'Info.json'
Assert-FileExists $expectedInfoPath 'Repository Info.json'
$expectedInfo = Get-Content -LiteralPath $expectedInfoPath -Raw | ConvertFrom-Json

Add-Type -AssemblyName System.IO.Compression.FileSystem
$allowed = @(
    'KingmakerDiceRoller/KingmakerDiceRoller.dll',
    'KingmakerDiceRoller/Info.json',
    'KingmakerDiceRoller/LICENSE',
    'KingmakerDiceRoller/THIRD-PARTY-NOTICES.md',
    'KingmakerDiceRoller/README.md',
    'KingmakerDiceRoller/licenses/UPSTREAM-WOTR-DICE-ROLLER-MIT.txt'
)
$required = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($path in $allowed) { [void]$required.Add($path) }
$observed = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$entryLengths = [ordered]@{}
$info = $null
$dllHeader = $null
$dllIdentity = $null
$dllSha256 = $null

$archive = [IO.Compression.ZipFile]::OpenRead($PackagePath)
try {
    # Directory records are allowed only as empty, safe records for the package's own folders; a directory record
    # carrying data, or any other path, is refused before files are counted.
    foreach ($directory in @($archive.Entries | Where-Object { -not $_.FullName -or $_.FullName.EndsWith('/') -or $_.FullName.EndsWith('\') })) {
        if ($directory.Length -ne 0 -or $directory.CompressedLength -gt 2 -or
            @('KingmakerDiceRoller/', 'KingmakerDiceRoller/licenses/') -cnotcontains $directory.FullName) {
            throw "Unexpected or data-bearing package directory record: '$($directory.FullName)'"
        }
    }
    $files = @($archive.Entries | Where-Object { $_.FullName -and -not $_.FullName.EndsWith('/') -and -not $_.FullName.EndsWith('\') })
    if ($files.Count -ne $allowed.Count) {
        throw "Package must contain exactly $($allowed.Count) files; observed $($files.Count)."
    }

    foreach ($entry in $files) {
        $name = $entry.FullName.Replace('\','/')
        $segments = @($name.Split('/'))
        if ([IO.Path]::IsPathRooted($name) -or $name.StartsWith('/') -or $name.Contains(':') -or
            $segments.Count -lt 2 -or $segments -contains '' -or $segments -contains '.' -or $segments -contains '..') {
            throw "Unsafe package entry: $name"
        }
        if (-not $observed.Add($name)) { throw "Duplicate package entry: $name" }
        if (-not $required.Contains($name)) { throw "Unexpected package entry: $name" }
        if ($entry.Length -le 0) { throw "Package entry is empty: $name" }
        $entryLengths[$name] = $entry.Length

        if ($name -eq 'KingmakerDiceRoller/Info.json') {
            $stream = $entry.Open()
            $reader = [IO.StreamReader]::new($stream, [Text.Encoding]::UTF8, $true)
            try { $info = $reader.ReadToEnd() | ConvertFrom-Json }
            finally { $reader.Dispose(); $stream.Dispose() }
        }
        elseif ($name -eq 'KingmakerDiceRoller/KingmakerDiceRoller.dll') {
            $stream = $entry.Open()
            try {
                $buffer = [IO.MemoryStream]::new()
                try {
                    $stream.CopyTo($buffer)
                    $dllBytes = $buffer.ToArray()
                    $dllHeader = @($dllBytes[0], $dllBytes[1])
                    $dllIdentity = Assert-PinnedAssemblyReferences $dllBytes
                    $dllSha256 = ([BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($dllBytes))).Replace('-', '').ToLowerInvariant()
                }
                finally { $buffer.Dispose() }
            }
            finally { $stream.Dispose() }
        }
        else {
            # Documentation and licence entries must be text: an assembly (or any binary) under an allowed text
            # name is refused, so the allow-list cannot carry a loader, Harmony or game DLL by renaming it.
            $stream = $entry.Open()
            $buffer = [IO.MemoryStream]::new()
            try { $stream.CopyTo($buffer); $textBytes = $buffer.ToArray() }
            finally { $buffer.Dispose(); $stream.Dispose() }
            if (($textBytes.Length -ge 2 -and $textBytes[0] -eq 0x4D -and $textBytes[1] -eq 0x5A) -or
                [Array]::IndexOf($textBytes, [byte]0) -ge 0) {
                throw "Package text entry contains binary content: $name"
            }
            try { [void][Text.UTF8Encoding]::new($false, $true).GetString($textBytes) }
            catch { throw "Package text entry is not valid UTF-8 text: $name" }
        }
    }

    foreach ($path in $allowed) {
        if (-not $observed.Contains($path)) { throw "Required package entry missing: $path" }
    }
}
finally {
    $archive.Dispose()
}

if ($null -eq $info) { throw 'Info.json could not be read from the package.' }
if (-not ($info.ManagerVersion -is [string]) -or $info.ManagerVersion -cne $expectedInfo.ManagerVersion -or $info.ManagerVersion -cne '0.31.1') {
    throw "Packaged ManagerVersion '$($info.ManagerVersion)' does not match the 0.31.1 baseline."
}
if ($ExpectedDllSha256 -and $dllSha256 -cne $ExpectedDllSha256) {
    throw "Archived DLL SHA-256 $dllSha256 does not match the build receipt's $ExpectedDllSha256."
}
if ($info.Id -ne 'KingmakerDiceRoller') { throw "Unexpected packaged UMM ID: $($info.Id)" }
if ($info.Version -ne $expectedInfo.Version) {
    throw "Packaged version '$($info.Version)' does not match repository version '$($expectedInfo.Version)'."
}
if ($info.AssemblyName -ne 'KingmakerDiceRoller.dll') { throw "Unexpected packaged assembly name: $($info.AssemblyName)" }
if ($info.EntryMethod -ne 'KingmakerDiceRoller.Main.Load') { throw "Unexpected packaged entry method: $($info.EntryMethod)" }
# Public branding: the displayed name and author change; the identity checks above stay fixed.
$publicDisplayName = "Mr. Bard's Kingmaker Roll For Stats UI"
$publicAuthor = 'Mr. Bard'
if ($info.DisplayName -ne $expectedInfo.DisplayName -or $info.DisplayName -ne $publicDisplayName) {
    throw "Unexpected packaged display name: $($info.DisplayName)"
}
if ($info.Author -ne $expectedInfo.Author -or $info.Author -ne $publicAuthor) {
    throw "Unexpected packaged author: $($info.Author)"
}
if ($dllHeader.Count -ne 2 -or $dllHeader[0] -ne 0x4D -or $dllHeader[1] -ne 0x5A) {
    throw 'Packaged DLL does not have a PE MZ header.'
}

$result = [ordered]@{
    status = 'package-validated'
    package = $PackagePath
    package_sha256 = Get-Sha256 $PackagePath
    version = $info.Version
    manager_version = $info.ManagerVersion
    dll_sha256 = $dllSha256
    dll_mvid = $dllIdentity.Mvid
    dll_loader_references = @($dllIdentity.LoaderReferences)
    entries = @($entryLengths.Keys)
    entry_lengths = $entryLengths
}
if ($ReportPath) {
    $resolvedReport = if ([IO.Path]::IsPathRooted($ReportPath)) { $ReportPath } else { Join-Path $root $ReportPath }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $resolvedReport) | Out-Null
    $result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resolvedReport -Encoding UTF8
}
Write-Host "Package validation passed: $PackagePath"
Write-Host "Package SHA-256: $($result.package_sha256)"
Write-Host "Packaged DLL SHA-256: $dllSha256; MVID: $($dllIdentity.Mvid); loader references: $((@($dllIdentity.LoaderReferences) | ForEach-Object { $_.name + ' ' + $_.version }) -join ', ')"
