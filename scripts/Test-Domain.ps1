[CmdletBinding()]
param([ValidateSet('Debug','Release')][string] $Configuration = 'Release')
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$msbuild = Resolve-MSBuild
$project = Join-Path $root 'tests\KingmakerDiceRoller.DomainTests\KingmakerDiceRoller.DomainTests.csproj'
& $msbuild $project /nologo /m /t:Rebuild /p:Configuration=$Configuration /p:Platform='AnyCPU'
if ($LASTEXITCODE -ne 0) { throw "Domain test build failed with exit code $LASTEXITCODE." }
$runner = Join-Path $root "artifacts\tests\$Configuration\KingmakerDiceRoller.DomainTests.exe"
Assert-FileExists $runner 'Domain test runner'
& $runner
if ($LASTEXITCODE -ne 0) { throw "Domain tests failed with exit code $LASTEXITCODE." }
