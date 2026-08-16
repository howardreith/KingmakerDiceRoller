[CmdletBinding()]
param([ValidateSet('Debug','Release')][string] $Configuration = 'Release')
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$dll = Join-Path $root "artifacts\bin\$Configuration\KingmakerDiceRoller\KingmakerDiceRoller.dll"
Assert-FileExists $dll 'Built mod DLL'
$info = Get-Content -LiteralPath (Join-Path $root 'Info.json') -Raw | ConvertFrom-Json
$stageRoot = Join-Path $root 'artifacts\staging'
$stage = Join-Path $stageRoot 'KingmakerDiceRoller'
if (Test-Path -LiteralPath $stageRoot) { Remove-Item -LiteralPath $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Join-Path $stage 'licenses') | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $stage 'KingmakerDiceRoller.dll')
foreach ($name in @('Info.json','LICENSE','THIRD-PARTY-NOTICES.md','README.md')) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $stage $name) }
Copy-Item -LiteralPath (Join-Path $root 'licenses\UPSTREAM-WOTR-DICE-ROLLER-MIT.txt') -Destination (Join-Path $stage 'licenses\UPSTREAM-WOTR-DICE-ROLLER-MIT.txt')
$packageDir = Join-Path $root 'artifacts\packages'
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
$zip = Join-Path $packageDir ("KingmakerDiceRoller-{0}.zip" -f $info.Version)
Invoke-RepositoryPython @((Join-Path $root 'tools\create_deterministic_zip.py'), $stageRoot, $zip)
$manifest = [ordered]@{
    package = [IO.Path]::GetFileName($zip)
    package_sha256 = Get-Sha256 $zip
    dll_sha256 = Get-Sha256 $dll
    version = $info.Version
    entries = @(Get-ChildItem -LiteralPath $stage -File -Recurse | ForEach-Object { $_.FullName.Substring($stageRoot.Length + 1).Replace('\','/') } | Sort-Object)
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $packageDir 'package-manifest.json') -Encoding UTF8
Write-Host "Package: $zip"
Write-Host "SHA-256: $($manifest.package_sha256)"
