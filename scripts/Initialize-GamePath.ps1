[CmdletBinding()]
param([string] $KingmakerInstallDir)
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$target = Join-Path $root 'GamePath.props'
if (Test-Path -LiteralPath $target) { throw "GamePath.props already exists: $target" }
Copy-Item -LiteralPath (Join-Path $root 'GamePath.props.example') -Destination $target
if ($KingmakerInstallDir) {
    [xml] $xml = Get-Content -LiteralPath $target -Raw
    $group = @($xml.Project.PropertyGroup) | Where-Object { $_.KingmakerInstallDir } | Select-Object -First 1
    $group.KingmakerInstallDir = $KingmakerInstallDir
    $xml.Save($target)
}
Write-Host "Created $target"
Write-Host 'Review UnityModManagerDir before building.'
