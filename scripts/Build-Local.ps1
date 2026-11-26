[CmdletBinding()]
param([ValidateSet('Debug','Release')][string] $Configuration = 'Release')
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
Initialize-PinnedReferences | Out-Null
$loaderReferences = @(Get-PinnedReferenceIdentities)
# Fingerprint the material inputs before building: a change during or after the build invalidates the receipt.
$compilerReferences = @(Get-CompilerReferenceIdentities $Configuration)
$buildInputs = Get-BuildInputFingerprint $Configuration $compilerReferences
Assert-LocalBuildConfiguration   # again immediately before the production compile (also checked by the evaluation above)
& (Join-Path $PSScriptRoot 'Validate-Repository.ps1')
& (Join-Path $PSScriptRoot 'Test-SourceOracle.ps1')
& (Join-Path $PSScriptRoot 'Test-Domain.ps1') -Configuration $Configuration
& (Join-Path $PSScriptRoot 'Verify-KingmakerContracts.ps1')
$msbuild = Resolve-MSBuild
$project = Join-Path $root 'src\KingmakerDiceRoller\KingmakerDiceRoller.csproj'
# The production compile writes its own compiler inventory; it must equal the inventory evaluated before building,
# which the receipt records and the fingerprint covers.
$productionInventory = Join-Path ([IO.Path]::GetTempPath()) ('kdr-production-inputs-' + [Guid]::NewGuid().ToString('N') + '.txt')
try {
    & $msbuild $project /nologo /m /t:Rebuild /p:Configuration=$Configuration /p:Platform='AnyCPU' "/p:CompilerReferenceListPath=$productionInventory"
    if ($LASTEXITCODE -ne 0) { throw "Production build failed with exit code $LASTEXITCODE." }
    $produced = @(ConvertFrom-CompilerInputList $productionInventory)
}
finally { Remove-Item -LiteralPath $productionInventory -Force -ErrorAction SilentlyContinue }
$difference = Get-CompilerInventoryDifference $produced $compilerReferences
if ($difference) {
    throw "The production compile used different compiler inputs from the evaluated inventory ($difference); the build is not recorded."
}
$dll = Join-Path $root "artifacts\bin\$Configuration\KingmakerDiceRoller\KingmakerDiceRoller.dll"
Assert-FileExists $dll 'Built mod DLL'
& (Join-Path $PSScriptRoot 'Verify-RespecContracts.ps1') -DllPath $dll
& (Join-Path $PSScriptRoot 'Verify-NativeUiContracts.ps1') -DllPath $dll
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
    loader_references = $loaderReferences
    build_inputs_sha256 = $buildInputs
    compiler_references = $compilerReferences
    game_assembly_sha256 = $contracts.assembly_sha256
    game_assembly_mvid = $contracts.assembly_mvid
}
$path = Join-Path $root 'artifacts\build-provenance.json'
$provenance | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path -Encoding UTF8
Write-Host "Build qualification passed. DLL: $dll"
Write-Host "SHA-256: $($provenance.dll_sha256)"
