[CmdletBinding()]
param([string] $DllPath = 'artifacts/bin/Release/KingmakerDiceRoller/KingmakerDiceRoller.dll',
      [string] $OutputPath = 'artifacts/contracts/respec-contracts.json')
. (Join-Path $PSScriptRoot 'Common.ps1')
$config = Get-KingmakerConfiguration
$root = Get-RepositoryRoot
$gamePath = Join-Path $config.ManagedDir 'Assembly-CSharp.dll'
$modPath = if ([IO.Path]::IsPathRooted($DllPath)) { $DllPath } else { Join-Path $root $DllPath }
$resolver = [ResolveEventHandler]{
    param($sender, $eventArgs)
    $name = New-Object Reflection.AssemblyName($eventArgs.Name)
    foreach ($directory in @($config.ManagedDir, $config.UnityModManagerDir)) {
        $candidate = Join-Path $directory ($name.Name + '.dll')
        if (Test-Path -LiteralPath $candidate) { return [Reflection.Assembly]::LoadFrom($candidate) }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
$module = $null
try {
    [Reflection.Assembly]::LoadFrom($gamePath) | Out-Null
    [Reflection.Assembly]::LoadFrom($modPath) | Out-Null
    [Reflection.Assembly]::LoadFrom((Join-Path $config.UnityModManagerDir '0Harmony12.dll')) | Out-Null
    [Reflection.Assembly]::LoadFrom((Join-Path $config.UnityModManagerDir 'dnlib.dll')) | Out-Null
    $base = (New-Object KingmakerDiceRoller.Integration.KingmakerContractResolver).Resolve()
    $native = [KingmakerDiceRoller.Integration.NativeRespecContracts]::Resolve($base)
    $module = [dnlib.DotNet.ModuleDefMD]::Load($gamePath)
    $checks = New-Object 'System.Collections.Generic.List[string]'
    function Require-Order([string] $TypeName, [string] $MethodName, [string[]] $Patterns) {
        $type = $module.GetTypes() | Where-Object { [string]$_.FullName -ceq $TypeName }
        $methods = @($type.Methods | Where-Object { [string]$_.Name -ceq $MethodName })
        if ($methods.Count -ne 1 -or -not $methods[0].HasBody) { throw "Ambiguous/missing inspected method: $TypeName::$MethodName" }
        $instructions = @($methods[0].Body.Instructions)
        $cursor = 0
        foreach ($pattern in $Patterns) {
            $found = $false
            for (; $cursor -lt $instructions.Count; $cursor++) {
                # Branch operands can print their destination instruction; inspect calls/fields only.
                if ([string]$instructions[$cursor].OpCode -in @('call','callvirt','newobj','ldfld','stfld','ldftn','ldsfld','stsfld')) {
                    if ([string]$instructions[$cursor].Operand -like $pattern) { $found = $true; $cursor++; break }
                }
            }
            if (-not $found) { throw "Inspected IL order changed: $TypeName::$MethodName -> $pattern" }
        }
        $checks.Add("$TypeName::$MethodName ordered contract")
    }
    Require-Order 'Kingmaker.UI.CharSelect.CharSelectWindow' 'OnButtonOk' @('*::get_CurrentCharacter()*','*::CloseWindow()*','*Kingmaker.Player::RespecCompanion(*)')
    Require-Order 'Kingmaker.Player' 'RespecCompanion' @('*Kingmaker.Player/<>c__DisplayClass171_0::unit','*::CreateUnitVacuum(*)','*Kingmaker.Player/<>c__DisplayClass171_0::newUnit','*::<RespecCompanion>b__0()*','*::<RespecCompanion>b__1(*)')
    Require-Order 'Kingmaker.Player/<>c__DisplayClass171_0' '<RespecCompanion>b__1' @('*::newUnit','*::get_Descriptor()*','*::onSuccess','*::HandleLevelUpStart(*)')
    Require-Order 'Kingmaker.Player/<>c__DisplayClass171_0' '<RespecCompanion>b__0' @('*::newUnit','*::PreSave()*','*::PrepareRespec()*','*Newtonsoft.Json.Linq.JObject::FromObject(*)','*::unit','*Newtonsoft.Json.JsonConvert::PopulateObject(*)','*::successCallback','*System.Action::Invoke()*','*::<RespecCompanion>b__8(*)')
    Require-Order 'Kingmaker.UnitLogic.Class.LevelUp.LevelUpController' 'Commit' @('*LevelUpController::Preview','*::Dispose()*','*LevelUpController::Unit','*::ApplyLevelup(*)','*::SetupNewCharacher()*','*::m_OnSuccess','*System.Action::Invoke()*')
    Require-Order 'Kingmaker.UnitLogic.Class.LevelUp.LevelUpController' 'ApplyLevelup' @('*Kingmaker.UnitLogic.Class.LevelUp.LevelUpState::.ctor(*)','*ILevelUpAction::Check(*)','*ILevelUpAction::Apply(*)','*::ReapplyFeaturesOnLevelUp()*')
    Require-Order 'Kingmaker.UI.LevelUp.CharacterBuildController' 'HandleUnitChangedAfterRespec' @('*::get_Descriptor()*','*CharacterBuildController::Unit')
    Require-Order 'Kingmaker.UI.LevelUp.CharacterBuildController' 'Commit' @('*LevelUpController::Commit()*','*LevelUpController::CanLevelUp(*)','*::<Commit>b__93_1(*)')
    Require-Order 'Kingmaker.UnitLogic.Class.LevelUp.Actions.SelectRaceStat' 'Apply' @('*::CanSelectRaceStat','*::SelectedRaceStat','*::AddModifier(*)')
    Require-Order 'Kingmaker.UnitLogic.Class.LevelUp.Actions.SpendAttributePoint' 'Apply' @('*::AttributePoints','*::get_BaseValue()*','*::set_BaseValue(*)')
    $source = Get-Content -LiteralPath (Join-Path $root 'tests/KingmakerDiceRoller.ContractTests/InvocationScopeChecks.cs') -Raw
    Add-Type -TypeDefinition $source -ReferencedAssemblies @($modPath, (Join-Path $config.UnityModManagerDir '0Harmony12.dll'), (Join-Path $config.UnityModManagerDir '0Harmony.dll'))
    $scopeChecks = [KingmakerDiceRoller.ContractTests.InvocationScopeChecks]::Run()
    $checks.Add("Harmony12 synchronous invocation scope: $scopeChecks/4 (return/exception with Harmony12 alone and co-installed HarmonyLib)")
    $eddicPath = Join-Path $config.ModsDir 'EddicKingmakerRespec/EddicKingmakerRespec.dll'
    $eddic = $null
    if (Test-Path -LiteralPath $eddicPath) {
        $eddicModule = [dnlib.DotNet.ModuleDefMD]::Load($eddicPath)
        try {
            if ([string]$eddicModule.Mvid -ne 'be15b46a-80b0-417c-a62d-b3f6aea24c55' -or
                (Get-Sha256 $eddicPath) -ne '385c1ed87b5acb70092875ce28527cefb5698c64b61d3f706a27dc37161fc4d6') { throw 'Eddic binary differs from the inspected adapter contract.' }
            $patches = @($eddicModule.GetTypes() | Where-Object { $_.CustomAttributes | Where-Object { [string]$_.TypeFullName -eq 'HarmonyLib.HarmonyPatch' } } | ForEach-Object { [string]$_.FullName })
            $eddic = [ordered]@{ id='EddicKingmakerRespec'; version='1.0'; mvid=[string]$eddicModule.Mvid; sha256=Get-Sha256 $eddicPath; declared_patch_types=$patches; runtime_enabled='NOT OBSERVED (game not running)' }
        } finally { $eddicModule.Dispose() }
    }
    $report = [ordered]@{ status='passed'; game_sha256=Get-Sha256 $gamePath; game_mvid=$base.GameAssembly.ManifestModule.ModuleVersionId.ToString(); dll_sha256=Get-Sha256 $modPath; checks=@($checks); eddic=$eddic; newman55='NOT RUN: exact Respecialization DLL unavailable'; live_ui='NOT RUN'; persistence='NOT RUN' }
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $root $OutputPath) -Encoding UTF8
    Write-Host "Respec installed-assembly contracts passed: $($checks.Count) checks. Live UI/save qualification NOT RUN."
}
finally {
    if ($module) { $module.Dispose() }
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}
