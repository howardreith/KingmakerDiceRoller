[CmdletBinding(SupportsShouldProcess=$true)]
param()
. (Join-Path $PSScriptRoot 'Common.ps1')
$config = Get-KingmakerConfiguration
$target = Join-Path $config.ModsDir 'KingmakerDiceRoller'
if (-not (Test-Path -LiteralPath $target -PathType Container)) { Write-Host 'Kingmaker Dice Roller is not installed.'; return }
if ($PSCmdlet.ShouldProcess($target, 'Back up and remove Kingmaker Dice Roller')) {
    $stamp = [DateTime]::Now.ToString('yyyyMMdd-HHmmss')
    $backup = Join-Path $config.InstallDir "ModBackups\KingmakerDiceRoller\uninstall-$stamp"
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backup) | Out-Null
    Move-Item -LiteralPath $target -Destination $backup
    Write-Host "Removed only KingmakerDiceRoller; backup: $backup"
}
