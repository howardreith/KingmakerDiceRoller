[CmdletBinding()]
param()
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$command = Resolve-PythonCommand
Push-Location $root
try {
    $prefix = @($command.Prefix)
    & $command.Executable @prefix -m unittest discover -s tests/python -p 'test_*.py' -v
    if ($LASTEXITCODE -ne 0) { throw "Source behavior oracle failed with exit code $LASTEXITCODE." }
}
finally { Pop-Location }
