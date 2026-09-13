[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string] $Configuration = 'Release',
    [switch] $Candidate
)
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$dll = Join-Path $root "artifacts\bin\$Configuration\KingmakerDiceRoller\KingmakerDiceRoller.dll"
Assert-FileExists $dll 'Built mod DLL'
$info = Get-Content -LiteralPath (Join-Path $root 'Info.json') -Raw | ConvertFrom-Json
$git = Get-GitMetadata
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $root 'artifacts'))
$packageDir = Join-Path $artifactsRoot 'packages'
$stageRoot = Join-Path $artifactsRoot 'staging'
if ($Candidate) {
    $provenancePath = Join-Path $artifactsRoot 'build-provenance.json'
    Assert-FileExists $provenancePath 'Build provenance'
    $provenance = Get-Content -LiteralPath $provenancePath -Raw | ConvertFrom-Json
    if ($git.Dirty -or $provenance.dirty -or $provenance.commit -ne $git.Commit -or
        $provenance.configuration -ne $Configuration -or $provenance.dll_sha256 -ne (Get-Sha256 $dll)) {
        throw 'Candidate packaging requires a matching build from the exact clean current commit.'
    }
    $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString()
    $fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($dll)
    if ($assemblyVersion -ne ($info.Version + '.0') -or $fileVersion.FileVersion -ne ($info.Version + '.0') -or
        $fileVersion.ProductVersion -ne $info.Version) { throw 'Candidate assembly and Info.json versions disagree.' }
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
Copy-Item -LiteralPath $dll -Destination (Join-Path $stage 'KingmakerDiceRoller.dll')
foreach ($name in @('Info.json','LICENSE','THIRD-PARTY-NOTICES.md','README.md')) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $stage $name) }
Copy-Item -LiteralPath (Join-Path $root 'licenses\UPSTREAM-WOTR-DICE-ROLLER-MIT.txt') -Destination (Join-Path $stage 'licenses\UPSTREAM-WOTR-DICE-ROLLER-MIT.txt')
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
$zip = Join-Path $packageDir ("KingmakerDiceRoller-{0}.zip" -f $info.Version)
Invoke-RepositoryPython @((Join-Path $root 'tools\create_deterministic_zip.py'), $stageRoot, $zip)
& (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $zip -ReportPath (Join-Path $packageDir 'package-validation.json')
$manifest = [ordered]@{
    candidate_only = [bool]$Candidate
    package = [IO.Path]::GetFileName($zip)
    package_sha256 = Get-Sha256 $zip
    dll_sha256 = Get-Sha256 $dll
    version = $info.Version
    source_branch = $git.Branch
    source_commit = $git.Commit
    source_dirty = $git.Dirty
    entries = @(Get-ChildItem -LiteralPath $stage -File -Recurse | ForEach-Object { $_.FullName.Substring($stageRoot.Length + 1).Replace('\','/') } | Sort-Object)
}
if ($Candidate) {
    Copy-Item -LiteralPath $dll -Destination (Join-Path $packageDir 'KingmakerDiceRoller.dll')
    Copy-Item -LiteralPath $provenancePath -Destination (Join-Path $packageDir 'build-provenance.json')
    Copy-Item -LiteralPath (Join-Path $artifactsRoot 'source-qualification.json') -Destination $packageDir
    foreach ($report in @('runtime-contracts.json','respec-contracts.json','native-ui-contracts.json')) {
        Copy-Item -LiteralPath (Join-Path $artifactsRoot ('contracts\' + $report)) -Destination $packageDir
    }
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $packageDir 'package-manifest.json') -Encoding UTF8
Write-Host "Package: $zip"
Write-Host "SHA-256: $($manifest.package_sha256)"
