[CmdletBinding()]
param([string] $ReportPath = 'artifacts/source-qualification.json')
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
Invoke-RepositoryPython @((Join-Path $root 'tools\validate_repository.py'), '--report', $ReportPath)
