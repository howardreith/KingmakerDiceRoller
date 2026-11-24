[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string] $Configuration = 'Release',
    [switch] $Candidate
)
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$dll = Join-Path $root "artifacts\bin\$Configuration\KingmakerDiceRoller\KingmakerDiceRoller.dll"
Assert-FileExists $dll 'Built mod DLL'
$loaderReceiptPath = Join-Path $root 'artifacts\build-provenance.json'
Assert-FileExists $loaderReceiptPath 'Build receipt with pinned loader references'
# Read the receipt once; every later record is produced from these bytes, the staged DLL and the validated archive,
# never by rereading mutable build output.
$receiptBytes = [IO.File]::ReadAllBytes($loaderReceiptPath)
$loaderReceipt = [Text.Encoding]::UTF8.GetString($receiptBytes).TrimStart([char]0xFEFF) | ConvertFrom-Json
Assert-PinnedBuildReceipt $loaderReceipt $dll $Configuration
$info = Get-Content -LiteralPath (Join-Path $root 'Info.json') -Raw | ConvertFrom-Json
$git = Get-GitMetadata
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $root 'artifacts'))
$packageDir = Join-Path $artifactsRoot 'packages'
if ($Candidate) {
    # Admission uses the same typed receipt object that is exported below (read once), never a second read.
    if ($git.Dirty -or $loaderReceipt.dirty -or $loaderReceipt.commit -cne $git.Commit -or $loaderReceipt.configuration -cne $Configuration) {
        throw 'Candidate packaging requires a matching build from the exact clean current commit.'
    }
    $packageDir = Join-Path $artifactsRoot ('candidates\' + $git.Commit)
    if (Test-Path -LiteralPath $packageDir) { throw "Candidate output already exists; refusing to overwrite: $packageDir" }
}
# The packaged documents are read once; staging writes those bytes and the archive must carry exactly them. A
# candidate's documents must be the admitted commit's files as Git checks them out, so an edit after admission is refused.
$documents = [ordered]@{
    'Info.json' = 'Info.json'; 'LICENSE' = 'LICENSE'; 'THIRD-PARTY-NOTICES.md' = 'THIRD-PARTY-NOTICES.md'; 'README.md' = 'README.md'
    'licenses/UPSTREAM-WOTR-DICE-ROLLER-MIT.txt' = 'licenses\UPSTREAM-WOTR-DICE-ROLLER-MIT.txt'
}
$documentDigests = [ordered]@{}
$documentBytes = [ordered]@{}
foreach ($entry in $documents.Keys) {
    $bytes = [IO.File]::ReadAllBytes((Join-Path $root $documents[$entry]))
    if ($Candidate) {
        $committed = Get-GitCheckoutBytes $git.Commit $documents[$entry]
        if (-not [Linq.Enumerable]::SequenceEqual([byte[]]$bytes, [byte[]]$committed)) { throw "The candidate's $entry differs from the admitted commit $($git.Commit); the package was not accepted." }
    }
    $documentBytes[$entry] = $bytes
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $documentDigests[$entry] = ([BitConverter]::ToString($hasher.ComputeHash([byte[]]$bytes))).Replace('-', '').ToLowerInvariant() } finally { $hasher.Dispose() }
}
# R5-F5: a candidate's qualification reports are read once and must describe this build: the packaged DLL, the game
# assembly the receipt's compile referenced, and the admitted Info.json.
$candidateReports = [ordered]@{}
if ($Candidate) {
    $gameReference = @($loaderReceipt.compiler_references | Where-Object { $_.kind -ceq 'reference' -and ($_.name -ceq 'Assembly-CSharp.dll' -or $_.name.EndsWith('/Assembly-CSharp.dll', [StringComparison]::Ordinal)) })
    if ($gameReference.Count -ne 1) { throw 'The build receipt does not name exactly one Assembly-CSharp.dll compile reference.' }
    $gameSha = [string]$gameReference[0].sha256
    $reportSpecs = [ordered]@{
        'source-qualification.json' = @{ Path = 'source-qualification.json'; Status = @('passed'); Dll = $null; Game = $null; Info = 'info_sha256' }
        'runtime-contracts.json' = @{ Path = 'contracts\runtime-contracts.json'; Status = @('passed'); Dll = $null; Game = 'assembly_sha256'; Info = $null }
        'respec-contracts.json' = @{ Path = 'contracts\respec-contracts.json'; Status = @('passed'); Dll = 'dll_sha256'; Game = 'game_sha256'; Info = $null }
        'native-ui-contracts.json' = @{ Path = 'contracts\native-ui-contracts.json'; Status = @('source-contract-qualified'); Dll = 'dll_sha256'; Game = 'game_assembly_sha256'; Info = $null }
    }
    foreach ($name in $reportSpecs.Keys) {
        $spec = $reportSpecs[$name]
        $bytes = [IO.File]::ReadAllBytes((Join-Path $artifactsRoot $spec.Path))
        $report = [Text.Encoding]::UTF8.GetString($bytes).TrimStart([char]0xFEFF) | ConvertFrom-Json
        $checks = @(@('status', $spec.Status), @($spec.Dll, @([string]$loaderReceipt.dll_sha256)), @($spec.Game, @($gameSha)), @($spec.Info, @([string]$documentDigests['Info.json'])))
        foreach ($check in $checks) {
            if (-not $check[0]) { continue }
            $value = if ($report.PSObject.Properties[$check[0]]) { $report.($check[0]) } else { $null }
            if (-not ($value -is [string]) -or @($check[1] | Where-Object { [string]::Equals($_, $value, [StringComparison]::Ordinal) }).Count -ne 1) {
                throw "The qualification report $name does not describe this build ($($check[0]) is '$value'); the candidate was not published."
            }
        }
        $candidateReports[$name] = $bytes
    }
}
# Staging, the new archive and its validation report live in a folder private to this invocation; the published package
# and its records are replaced only after the new archive passed validation and its documents match. A failure leaves the
# previous package and records exactly as they were.
$private = Join-Path $artifactsRoot ('.package-' + [Guid]::NewGuid().ToString('N'))
$keepPrivate = $false
try {
    $stageRoot = Join-Path $private 'staging'
    $stage = Join-Path $stageRoot 'KingmakerDiceRoller'
    New-Item -ItemType Directory -Force -Path (Join-Path $stage 'licenses') | Out-Null
    $stagedDll = Join-Path $stage 'KingmakerDiceRoller.dll'
    Copy-Item -LiteralPath $dll -Destination $stagedDll
    $stagedDllSha = Get-Sha256 $stagedDll
    if ($stagedDllSha -cne $loaderReceipt.dll_sha256) { throw 'The build output changed before staging; rebuild first.' }
    if ($Candidate) {
        $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($stagedDll).Version.ToString()
        $fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($stagedDll)
        if ($assemblyVersion -ne ($info.Version + '.0') -or $fileVersion.FileVersion -ne ($info.Version + '.0') -or
            $fileVersion.ProductVersion -ne $info.Version) { throw 'Candidate assembly and Info.json versions disagree.' }
    }
    foreach ($entry in $documents.Keys) { [IO.File]::WriteAllBytes((Join-Path $stage $documents[$entry]), [byte[]]$documentBytes[$entry]) }
    $newZip = Join-Path $private ("KingmakerDiceRoller-{0}.zip" -f $info.Version)
    Invoke-RepositoryPython @((Join-Path $root 'tools\create_deterministic_zip.py'), $stageRoot, $newZip)
    # The archived DLL must be the exact DLL the receipt describes, read back from the new archive.
    $newValidation = Join-Path $private 'package-validation.json'
    & (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $newZip -ReportPath $newValidation -ExpectedDllSha256 $loaderReceipt.dll_sha256
    $validation = Get-Content -LiteralPath $newValidation -Raw | ConvertFrom-Json
    # The documents read back from the validated archive must be the repository files hashed before staging.
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($newZip)
    try {
        foreach ($entry in $documents.Keys) {
            $zipEntry = $archive.GetEntry('KingmakerDiceRoller/' + $entry)
            if ($null -eq $zipEntry) { throw "The new archive has no $entry." }
            $stream = $zipEntry.Open(); $hasher = [Security.Cryptography.SHA256]::Create()
            try { $archived = ([BitConverter]::ToString($hasher.ComputeHash($stream))).Replace('-', '').ToLowerInvariant() } finally { $hasher.Dispose(); $stream.Dispose() }
            if ($archived -cne $documentDigests[$entry]) { throw "Package files changed during staging: the archived $entry differs from the verified repository file. The package was not accepted." }
        }
    }
    finally { $archive.Dispose() }
    $manifest = [ordered]@{
        candidate_only = [bool]$Candidate
        package = [IO.Path]::GetFileName($newZip)
        package_sha256 = [string]$validation.package_sha256
        dll_sha256 = [string]$validation.dll_sha256
        dll_mvid = [string]$validation.dll_mvid
        version = $info.Version
        # The build that produced the DLL (from its receipt) and the checkout that packaged it are recorded separately.
        producing_commit = $loaderReceipt.commit
        producing_dirty = $loaderReceipt.dirty
        producing_configuration = $loaderReceipt.configuration
        build_inputs_sha256 = $loaderReceipt.build_inputs_sha256
        receipt_sha256 = ([BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($receiptBytes))).Replace('-', '').ToLowerInvariant()
        packaging_branch = $git.Branch
        packaging_commit = $git.Commit
        packaging_dirty = $git.Dirty
        document_sha256 = $documentDigests
        entries = @(Get-ChildItem -LiteralPath $stage -File -Recurse | ForEach-Object { $_.FullName.Substring($stageRoot.Length + 1).Replace('\','/') } | Sort-Object)
    }
    # Final agreement: the archive, the staged DLL, the validation report and the receipt describe the same bytes.
    if ((Get-Sha256 $newZip) -cne $manifest.package_sha256 -or (Get-Sha256 $stagedDll) -cne $manifest.dll_sha256 -or
        $manifest.dll_sha256 -cne $loaderReceipt.dll_sha256) { throw 'Package records disagree; the package was not accepted.' }
    $validationFinal = Join-Path $private 'package-validation.final.json'
    $manifestFinal = Join-Path $private 'package-manifest.final.json'
    $zipName = [IO.Path]::GetFileName($newZip)
    if ($Candidate) {
        # The whole candidate is prepared in private storage and published once, with a move that refuses an existing
        # destination: a failure leaves no commit directory, and a competing same-commit publisher's files are kept.
        $candidateStage = Join-Path $private 'candidate'
        New-Item -ItemType Directory -Path $candidateStage | Out-Null
        $zip = Join-Path $packageDir $zipName
        $validation | Add-Member -NotePropertyName package -NotePropertyValue $zip -Force
        $validation | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $candidateStage 'package-validation.json') -Encoding UTF8
        [IO.File]::Move($newZip, (Join-Path $candidateStage $zipName))
        Copy-Item -LiteralPath $stagedDll -Destination (Join-Path $candidateStage 'KingmakerDiceRoller.dll')
        [IO.File]::WriteAllBytes((Join-Path $candidateStage 'build-provenance.json'), $receiptBytes)
        foreach ($name in $candidateReports.Keys) { [IO.File]::WriteAllBytes((Join-Path $candidateStage $name), [byte[]]$candidateReports[$name]) }
        $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $candidateStage 'package-manifest.json') -Encoding UTF8
        if ((Get-Sha256 (Join-Path $candidateStage $zipName)) -cne $manifest.package_sha256 -or (Get-Sha256 (Join-Path $candidateStage 'KingmakerDiceRoller.dll')) -cne $manifest.dll_sha256) {
            throw 'The prepared candidate disagrees with the validated archive; nothing was published.'
        }
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $packageDir) | Out-Null
        try { [IO.Directory]::Move($candidateStage, $packageDir) }
        catch { throw "Candidate output already exists or cannot be published; nothing was overwritten: $packageDir ($($_.Exception.Message))" }
    }
    else {
        # Ordinary publication of the set {ZIP, validation report, manifest}. One publisher at a time per destination (an
        # exclusive lock file removed when its handle closes); both records are prepared before any public file changes;
        # backup acquisition, placement and rollback are one recovery scope that tracks exactly the files this invocation
        # moved and placed. If restoration itself fails, the backups are kept (not deleted) and named.
        New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
        $zip = Join-Path $packageDir $zipName
        $validation | Add-Member -NotePropertyName package -NotePropertyValue $zip -Force
        $validation | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $validationFinal -Encoding UTF8
        $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestFinal -Encoding UTF8
        $publishLock = $null
        $deadline = [DateTime]::UtcNow.AddSeconds(300)
        while ($null -eq $publishLock) {
            try { $publishLock = [IO.FileStream]::new((Join-Path $packageDir '.publish.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None, 1, [IO.FileOptions]::DeleteOnClose) }
            catch [IO.IOException] {
                if ([DateTime]::UtcNow -gt $deadline) { throw "Another package publication holds $packageDir; nothing was changed." }
                Start-Sleep -Milliseconds 200
            }
        }
        try {
            $previous = Join-Path $private 'previous'; New-Item -ItemType Directory -Path $previous | Out-Null
            $pairs = @(@($newZip, $zip), @($validationFinal, (Join-Path $packageDir 'package-validation.json')), @($manifestFinal, (Join-Path $packageDir 'package-manifest.json')))
            $moved = New-Object System.Collections.Generic.List[string]
            $placed = New-Object System.Collections.Generic.List[string]
            try {
                foreach ($pair in $pairs) {
                    $public = $pair[1]
                    if (Test-Path -LiteralPath $public) { [IO.File]::Move($public, (Join-Path $previous ([IO.Path]::GetFileName($public)))); $moved.Add($public) }
                }
                foreach ($pair in $pairs) {
                    [IO.File]::Move($pair[0], $pair[1]); $placed.Add($pair[1])
                    if ($pair[1] -ceq $zip -and (Get-Sha256 $zip) -cne $manifest.package_sha256) { throw 'The published package differs from the validated archive.' }
                }
            }
            catch {
                $failure = $_
                $unrestored = @()
                for ($i = $placed.Count - 1; $i -ge 0; $i--) { try { [IO.File]::Delete($placed[$i]) } catch { $unrestored += $placed[$i] } }
                for ($i = $moved.Count - 1; $i -ge 0; $i--) {
                    try { [IO.File]::Move((Join-Path $previous ([IO.Path]::GetFileName($moved[$i]))), $moved[$i]) } catch { $unrestored += $moved[$i] }
                }
                if ($unrestored.Count -gt 0) {
                    $keepPrivate = $true
                    throw "Publication failed ($($failure.Exception.Message)) and the previous package set could not be fully restored ($($unrestored -join ', ')); the previous files are kept in $previous."
                }
                throw $failure
            }
        }
        finally { $publishLock.Dispose() }
    }
}
finally {
    if (-not $keepPrivate -and [IO.Directory]::Exists($private)) { [IO.Directory]::Delete($private, $true) }
}
Write-Host "Package: $zip"
Write-Host "SHA-256: $($manifest.package_sha256)"
