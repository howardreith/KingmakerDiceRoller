[CmdletBinding()]
param(
    [switch] $Build,
    [switch] $Package,
    [switch] $Install,
    [ValidateSet('Debug','Release')][string] $Configuration = 'Release'
)
. (Join-Path $PSScriptRoot 'Common.ps1')
if ($Build) {
    & (Join-Path $PSScriptRoot 'Build-Local.ps1') -Configuration $Configuration
}
else {
    & (Join-Path $PSScriptRoot 'Validate-Repository.ps1')
    & (Join-Path $PSScriptRoot 'Test-SourceOracle.ps1')
}
if ($Package) { & (Join-Path $PSScriptRoot 'Package.ps1') -Configuration $Configuration }
if ($Install) {
    if (-not $Package) { throw '-Install requires -Package in the same qualification run.' }
    & (Join-Path $PSScriptRoot 'Install.ps1')
}
Write-Host 'Qualification command completed. This does not replace the manual runtime smoke test.'
