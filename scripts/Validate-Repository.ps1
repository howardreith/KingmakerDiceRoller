[CmdletBinding()]
param([string] $ReportPath = 'artifacts/source-qualification.json')
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
& (Join-Path $PSScriptRoot 'Test-ReleaseQualificationGate.ps1')
Invoke-RepositoryPython @((Join-Path $root 'tools\validate_repository.py'), '--report', $ReportPath)
