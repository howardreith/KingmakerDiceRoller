[CmdletBinding()]
param([ValidateSet('Debug','Release')][string] $Configuration = 'Release')
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
& (Join-Path $PSScriptRoot 'Validate-Repository.ps1')
& (Join-Path $PSScriptRoot 'Test-SourceOracle.ps1')
& (Join-Path $PSScriptRoot 'Test-Domain.ps1') -Configuration $Configuration
& (Join-Path $PSScriptRoot 'Verify-KingmakerContracts.ps1')
$msbuild = Resolve-MSBuild
$project = Join-Path $root 'src\KingmakerDiceRoller\KingmakerDiceRoller.csproj'
& $msbuild $project /nologo /m /t:Rebuild /p:Configuration=$Configuration /p:Platform='AnyCPU'
if ($LASTEXITCODE -ne 0) { throw "Production build failed with exit code $LASTEXITCODE." }
$dll = Join-Path $root "artifacts\bin\$Configuration\KingmakerDiceRoller\KingmakerDiceRoller.dll"
Assert-FileExists $dll 'Built mod DLL'
$git = Get-GitMetadata
$contracts = Get-Content -LiteralPath (Join-Path $root 'artifacts\contracts\runtime-contracts.json') -Raw | ConvertFrom-Json
$provenance = [ordered]@{
    status = 'build-qualified'
    built_at_utc = [DateTime]::UtcNow.ToString('o')
    configuration = $Configuration
    branch = $git.Branch
    commit = $git.Commit
    dirty = $git.Dirty
    dll_sha256 = Get-Sha256 $dll
    game_assembly_sha256 = $contracts.assembly_sha256
    game_assembly_mvid = $contracts.assembly_mvid
}
$path = Join-Path $root 'artifacts\build-provenance.json'
$provenance | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path -Encoding UTF8
Write-Host "Build qualification passed. DLL: $dll"
Write-Host "SHA-256: $($provenance.dll_sha256)"
