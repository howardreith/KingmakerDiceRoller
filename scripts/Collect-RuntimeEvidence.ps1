[CmdletBinding()]
param([string] $Destination)
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$config = Get-KingmakerConfiguration
if (-not $Destination) { $Destination = Join-Path $root ('artifacts\runtime-evidence\' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss')) }
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$candidates = @(
    (Join-Path $config.InstallDir 'Player.log'),
    (Join-Path $config.InstallDir 'output_log.txt'),
    (Join-Path $config.InstallDir 'UnityModManager\Logs\UnityModManager.log'),
    (Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Player.log')
)
foreach ($candidate in $candidates) {
    if (Test-Path -LiteralPath $candidate -PathType Leaf) { Copy-Item -LiteralPath $candidate -Destination $Destination }
}
$metadata = [ordered]@{
    collected_at_utc = [DateTime]::UtcNow.ToString('o')
    game_install = $config.InstallDir
    git = Get-GitMetadata
    note = 'Attach screenshots and save/reload observations manually; do not commit this directory.'
}
$metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Destination 'evidence-metadata.json') -Encoding UTF8
Write-Host "Runtime evidence collected at $Destination"
