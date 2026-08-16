[CmdletBinding()]
param()
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$command = Resolve-PythonCommand
$exe = $command[0]
$prefix = @()
if ($command.Count -gt 1) { $prefix = $command[1..($command.Count - 1)] }
Push-Location $root
try {
    & $exe @prefix -m unittest discover -s tests/python -p 'test_*.py' -v
    if ($LASTEXITCODE -ne 0) { throw "Source behavior oracle failed with exit code $LASTEXITCODE." }
}
finally { Pop-Location }
