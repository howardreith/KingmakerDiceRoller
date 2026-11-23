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
$stageRoot = Join-Path $artifactsRoot 'staging'
if ($Candidate) {
    # Admission uses the same typed receipt object that is exported below (read once), never a second read.
    if ($git.Dirty -or $loaderReceipt.dirty -or $loaderReceipt.commit -cne $git.Commit -or $loaderReceipt.configuration -cne $Configuration) {
        throw 'Candidate packaging requires a matching build from the exact clean current commit.'
    }
    $packageDir = Join-Path $artifactsRoot ('candidates\' + $git.Commit)
    if (Test-Path -LiteralPath $packageDir) { throw "Candidate output already exists; refusing to overwrite: $packageDir" }
    $stageRoot = Join-Path $packageDir 'staging'
}
# Check the resolved delete target and reject a redirected staging directory.
$stageRoot = [IO.Path]::GetFullPath($stageRoot)
if (-not $stageRoot.StartsWith($artifactsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Staging must stay inside repository artifacts.'
}
if (Test-Path -LiteralPath $stageRoot) {
    if ((Get-Item -LiteralPath $stageRoot).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Redirected staging directory is not allowed.' }
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}
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
foreach ($name in @('Info.json','LICENSE','THIRD-PARTY-NOTICES.md','README.md')) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $stage $name) }
Copy-Item -LiteralPath (Join-Path $root 'licenses\UPSTREAM-WOTR-DICE-ROLLER-MIT.txt') -Destination (Join-Path $stage 'licenses\UPSTREAM-WOTR-DICE-ROLLER-MIT.txt')
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
$zip = Join-Path $packageDir ("KingmakerDiceRoller-{0}.zip" -f $info.Version)
Invoke-RepositoryPython @((Join-Path $root 'tools\create_deterministic_zip.py'), $stageRoot, $zip)
# The archived DLL must be the exact DLL the receipt describes, read back from the produced ZIP.
$validationPath = Join-Path $packageDir 'package-validation.json'
& (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $zip -ReportPath $validationPath -ExpectedDllSha256 $loaderReceipt.dll_sha256
$validation = Get-Content -LiteralPath $validationPath -Raw | ConvertFrom-Json
$manifest = [ordered]@{
    candidate_only = [bool]$Candidate
    package = [IO.Path]::GetFileName($zip)
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
    entries = @(Get-ChildItem -LiteralPath $stage -File -Recurse | ForEach-Object { $_.FullName.Substring($stageRoot.Length + 1).Replace('\','/') } | Sort-Object)
}
if ($Candidate) {
    Copy-Item -LiteralPath $stagedDll -Destination (Join-Path $packageDir 'KingmakerDiceRoller.dll')
    [IO.File]::WriteAllBytes((Join-Path $packageDir 'build-provenance.json'), $receiptBytes)
    Copy-Item -LiteralPath (Join-Path $artifactsRoot 'source-qualification.json') -Destination $packageDir
    foreach ($report in @('runtime-contracts.json','respec-contracts.json','native-ui-contracts.json')) {
        Copy-Item -LiteralPath (Join-Path $artifactsRoot ('contracts\' + $report)) -Destination $packageDir
    }
}
# Final agreement: the archive, the staged DLL, the validation report and the receipt describe the same bytes.
if ((Get-Sha256 $zip) -cne $manifest.package_sha256 -or (Get-Sha256 $stagedDll) -cne $manifest.dll_sha256 -or
    $manifest.dll_sha256 -cne $loaderReceipt.dll_sha256) { throw 'Package records disagree; the package was not accepted.' }
if ($Candidate -and (Get-Sha256 (Join-Path $packageDir 'KingmakerDiceRoller.dll')) -cne $manifest.dll_sha256) { throw 'Candidate DLL copy disagrees with the validated archive.' }
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $packageDir 'package-manifest.json') -Encoding UTF8
Write-Host "Package: $zip"
Write-Host "SHA-256: $($manifest.package_sha256)"
