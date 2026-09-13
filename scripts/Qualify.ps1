[CmdletBinding()]
param(
    [switch] $Build,
    [switch] $Package,
    [switch] $Install,
    [switch] $Candidate,
    [ValidateSet('Debug','Release')][string] $Configuration = 'Release'
)
. (Join-Path $PSScriptRoot 'Common.ps1')
if ($Candidate -and $Install) { throw 'Candidate qualification never installs into the game.' }
if ($Candidate -and -not $Package) { throw '-Candidate requires -Package.' }
if ($Build) {
    & (Join-Path $PSScriptRoot 'Build-Local.ps1') -Configuration $Configuration
}
else {
    & (Join-Path $PSScriptRoot 'Validate-Repository.ps1')
    & (Join-Path $PSScriptRoot 'Test-SourceOracle.ps1')
}
if ($Package) { & (Join-Path $PSScriptRoot 'Package.ps1') -Configuration $Configuration -Candidate:$Candidate }
if ($Install) {
    if (-not $Package) { throw '-Install requires -Package in the same qualification run.' }
    & (Join-Path $PSScriptRoot 'Install.ps1')
}
Write-Host 'Qualification command completed. This does not replace the manual runtime smoke test.'
