[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string] $PackagePath,
    [string] $ReportPath
)

. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
Assert-FileExists $PackagePath 'Package ZIP'
$PackagePath = [IO.Path]::GetFullPath($PackagePath)

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

$archive = [IO.Compression.ZipFile]::OpenRead($PackagePath)
try {
    $files = @($archive.Entries | Where-Object { $_.FullName -and -not $_.FullName.EndsWith('/') })
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
                $first = $stream.ReadByte()
                $second = $stream.ReadByte()
                $dllHeader = @($first, $second)
            }
            finally { $stream.Dispose() }
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
if ($info.Id -ne 'KingmakerDiceRoller') { throw "Unexpected packaged UMM ID: $($info.Id)" }
if ($info.Version -ne '0.1.0-alpha.2') { throw "Unexpected packaged version: $($info.Version)" }
if ($info.AssemblyName -ne 'KingmakerDiceRoller.dll') { throw "Unexpected packaged assembly name: $($info.AssemblyName)" }
if ($info.EntryMethod -ne 'KingmakerDiceRoller.Main.Load') { throw "Unexpected packaged entry method: $($info.EntryMethod)" }
if ($dllHeader.Count -ne 2 -or $dllHeader[0] -ne 0x4D -or $dllHeader[1] -ne 0x5A) {
    throw 'Packaged DLL does not have a PE MZ header.'
}

$result = [ordered]@{
    status = 'package-validated'
    package = $PackagePath
    package_sha256 = Get-Sha256 $PackagePath
    version = $info.Version
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
