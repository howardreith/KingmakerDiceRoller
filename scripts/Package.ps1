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
# The packaged documents, hashed in the repository before staging; the archive must carry exactly these bytes.
$documents = [ordered]@{
    'Info.json' = 'Info.json'; 'LICENSE' = 'LICENSE'; 'THIRD-PARTY-NOTICES.md' = 'THIRD-PARTY-NOTICES.md'; 'README.md' = 'README.md'
    'licenses/UPSTREAM-WOTR-DICE-ROLLER-MIT.txt' = 'licenses\UPSTREAM-WOTR-DICE-ROLLER-MIT.txt'
}
$documentDigests = [ordered]@{}
foreach ($entry in $documents.Keys) { $documentDigests[$entry] = Get-Sha256 (Join-Path $root $documents[$entry]) }
# Staging, the new archive and its validation report live in a folder private to this invocation; the published package
# and its records are replaced only after the new archive passed validation and its documents match. A failure leaves the
# previous package and records exactly as they were.
$private = Join-Path $artifactsRoot ('.package-' + [Guid]::NewGuid().ToString('N'))
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
    foreach ($entry in $documents.Keys) { Copy-Item -LiteralPath (Join-Path $root $documents[$entry]) -Destination (Join-Path $stage $documents[$entry]) }
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
    # Publish.
    New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
    $zip = Join-Path $packageDir ([IO.Path]::GetFileName($newZip))
    if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
    [IO.File]::Move($newZip, $zip)
    if ((Get-Sha256 $zip) -cne $manifest.package_sha256) { throw 'The published package differs from the validated archive.' }
    $validation | Add-Member -NotePropertyName package -NotePropertyValue $zip -Force
    $validation | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $packageDir 'package-validation.json') -Encoding UTF8
    if ($Candidate) {
        Copy-Item -LiteralPath $stagedDll -Destination (Join-Path $packageDir 'KingmakerDiceRoller.dll')
        [IO.File]::WriteAllBytes((Join-Path $packageDir 'build-provenance.json'), $receiptBytes)
        Copy-Item -LiteralPath (Join-Path $artifactsRoot 'source-qualification.json') -Destination $packageDir
        foreach ($report in @('runtime-contracts.json','respec-contracts.json','native-ui-contracts.json')) {
            Copy-Item -LiteralPath (Join-Path $artifactsRoot ('contracts\' + $report)) -Destination $packageDir
        }
        if ((Get-Sha256 (Join-Path $packageDir 'KingmakerDiceRoller.dll')) -cne $manifest.dll_sha256) { throw 'Candidate DLL copy disagrees with the validated archive.' }
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $packageDir 'package-manifest.json') -Encoding UTF8
}
finally {
    if ([IO.Directory]::Exists($private)) { [IO.Directory]::Delete($private, $true) }
}
Write-Host "Package: $zip"
Write-Host "SHA-256: $($manifest.package_sha256)"
