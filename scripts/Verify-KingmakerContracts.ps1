[CmdletBinding()]
param(
    [string] $GamePathProps,
    [string] $OutputPath = 'artifacts/contracts/runtime-contracts.json'
)
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
if (-not $GamePathProps) { $GamePathProps = Join-Path $root 'GamePath.props' }
$config = Get-KingmakerConfiguration $GamePathProps
$assemblyPath = Join-Path $config.ManagedDir 'Assembly-CSharp.dll'
$flags = [Reflection.BindingFlags]'Public,NonPublic,Instance'
$staticFlags = [Reflection.BindingFlags]'Public,NonPublic,Static'

function Require-Type([Reflection.Assembly] $Assembly, [string] $Name) {
    $type = $Assembly.GetType($Name, $false)
    if (-not $type) { throw "Required type was not found: $Name" }
    return $type
}
function Find-Member([Type] $Type, [string] $Name, [bool] $Static = $false) {
    $binding = if ($Static) { $staticFlags } else { $flags }
    $property = $Type.GetProperty($Name, $binding)
    if ($property) { return $property }
    return $Type.GetField($Name, $binding)
}
function Require-Member([Type] $Type, [string] $Name, [bool] $Static = $false) {
    $member = Find-Member $Type $Name $Static
    if (-not $member) { throw "Required member was not found: $($Type.FullName).$Name" }
    return $member
}
function Member-Type([Reflection.MemberInfo] $Member) {
    if ($Member -is [Reflection.PropertyInfo]) { return $Member.PropertyType }
    if ($Member -is [Reflection.FieldInfo]) { return $Member.FieldType }
    throw "Unsupported member type: $($Member.MemberType)"
}
function Test-BooleanPath([Type] $Type, [string[]] $Paths) {
    foreach ($path in $Paths) {
        $current = $Type
        $ok = $true
        foreach ($segment in $path.Split('.')) {
            $member = Find-Member $current $segment
            if (-not $member) { $ok = $false; break }
            $current = Member-Type $member
        }
        if ($ok -and $current -eq [bool]) { return $path }
    }
    return $null
}

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
try {
    $assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
    $state = Require-Type $assembly 'Kingmaker.UnitLogic.Class.LevelUp.LevelUpState'
    $unit = Require-Type $assembly 'Kingmaker.UnitLogic.UnitDescriptor'
    $unitEntity = Require-Type $assembly 'Kingmaker.EntitySystem.Entities.UnitEntityData'
    $unitReference = Require-Type $assembly 'Kingmaker.EntitySystem.Entities.UnitReference'
    $playerType = Require-Type $assembly 'Kingmaker.Player'
    $distribution = Require-Type $assembly 'Kingmaker.UnitLogic.Class.LevelUp.StatsDistribution'
    $statType = Require-Type $assembly 'Kingmaker.EntitySystem.Stats.StatType'
    $statHelper = Require-Type $assembly 'Kingmaker.EntitySystem.Stats.StatTypeHelper'
    $mode = $state.GetNestedType('CharBuildMode', [Reflection.BindingFlags]'Public,NonPublic')
    if (-not $mode -or -not $mode.IsEnum) { throw 'LevelUpState.CharBuildMode enum was not found.' }
    $constructor = $state.GetConstructor($flags, $null, [Type[]]@($unit,$mode), $null)
    if (-not $constructor) { throw 'Exact LevelUpState(UnitDescriptor, CharBuildMode) constructor was not found.' }
    $start = $distribution.GetMethod('Start',$flags,$null,[Type[]]@([int]),$null)
    if (-not $start -or $start.ReturnType -ne [void]) { throw 'Exact StatsDistribution.Start(int) was not found.' }
    $complete = $distribution.GetMethod('IsComplete',$flags,$null,[Type[]]@(),$null)
    if (-not $complete -or $complete.ReturnType -ne [bool]) { throw 'Exact bool StatsDistribution.IsComplete() was not found.' }
    $stateUnit = Require-Member $state 'Unit'
    $stateDistribution = Require-Member $state 'StatsDistribution'
    $firstLevel = Require-Member $state 'IsFirstLevel'
    if ((Member-Type $firstLevel) -ne [bool]) { throw 'IsFirstLevel is not Boolean.' }
    $unitStats = Require-Member $unit 'Stats'
    $statsType = Member-Type $unitStats
    $getStatCandidates = @($statsType.GetMethods($flags) | Where-Object { $_.Name -eq 'GetStat' -and -not $_.IsGenericMethod -and $_.GetParameters().Length -eq 1 -and $_.GetParameters()[0].ParameterType -eq $statType })
    if ($getStatCandidates.Count -ne 1) { throw "Expected exactly one non-generic CharacterStats.GetStat(StatType), found $($getStatCandidates.Count)." }
    $getStat = $getStatCandidates[0]
    $baseValue = Require-Member $getStat.ReturnType 'BaseValue'
    if ((Member-Type $baseValue) -ne [int]) { throw 'BaseValue is not Int32.' }
    $statValues = Require-Member $distribution 'StatValues'
    $dictionaryType = [System.Collections.IDictionary]
    if (-not $dictionaryType.IsAssignableFrom((Member-Type $statValues))) { throw 'StatValues is not IDictionary-compatible.' }
    $attributes = Require-Member $statHelper 'Attributes' $true
    $attributeValues = if ($attributes -is [Reflection.PropertyInfo]) { $attributes.GetValue($null,$null) } else { $attributes.GetValue($null) }
    if (-not $attributeValues -or $attributeValues.Length -ne 6) { throw 'StatTypeHelper.Attributes does not contain six values.' }
    $abilityNames = @($attributeValues | ForEach-Object { $_.ToString() })
    $expected = @('Strength','Dexterity','Constitution','Intelligence','Wisdom','Charisma')
    if (($abilityNames -join ',') -ne ($expected -join ',')) { throw "Unexpected ability order: $($abilityNames -join ',')" }
    $mainPath = Test-BooleanPath $unit @('IsMainCharacter','Unit.IsMainCharacter')
    $playerPath = Test-BooleanPath $unit @('IsPlayerFaction','Unit.IsPlayerFaction')
    $petPath = Test-BooleanPath $unit @('IsPet','Unit.IsPet')
    $enemyPath = Test-BooleanPath $unit @('IsPlayersEnemy','Unit.IsPlayersEnemy')
    if (-not $mainPath -or -not $playerPath -or -not $petPath -or -not $enemyPath) { throw 'One or more fail-closed unit identity paths are unavailable.' }
    $game = Require-Type $assembly 'Kingmaker.Game'
    $gameInstance = Require-Member $game 'Instance' $true
    $gameUi = Require-Member $game 'UI'
    $gamePlayer = Require-Member $game 'Player'
    if (-not $playerType.IsAssignableFrom((Member-Type $gamePlayer))) { throw 'Game.Player is not a Kingmaker.Player contract.' }
    $playerMainCharacter = Require-Member $playerType 'MainCharacter'
    if ((Member-Type $playerMainCharacter) -ne $unitReference) { throw 'Player.MainCharacter is not a UnitReference contract.' }
    $unitReferenceValue = Require-Member $unitReference 'Value'
    if (-not $unitEntity.IsAssignableFrom((Member-Type $unitReferenceValue))) { throw 'UnitReference.Value is not a UnitEntityData contract.' }
    $unitEntityDescriptor = Require-Member $unitEntity 'Descriptor'
    if (-not $unit.IsAssignableFrom((Member-Type $unitEntityDescriptor))) { throw 'UnitEntityData.Descriptor is not a UnitDescriptor contract.' }
    $ui = Member-Type $gameUi
    $characterBuildController = Require-Member $ui 'CharacterBuildController'
    $characterBuild = Member-Type $characterBuildController
    $controllerMember = Require-Member $characterBuild 'LevelUpController'
    $controller = Member-Type $controllerMember
    $controllerState = Require-Member $controller 'State'
    if (-not $state.IsAssignableFrom((Member-Type $controllerState))) { throw 'LevelUpController.State is not a LevelUpState contract.' }
    $controllerUnit = Require-Member $controller 'Unit'
    $controllerPreview = Require-Member $controller 'Preview'
    if (-not $unit.IsAssignableFrom((Member-Type $controllerUnit)) -or
        -not $unit.IsAssignableFrom((Member-Type $controllerPreview))) {
        throw 'LevelUpController.Unit or Preview is not a UnitDescriptor contract.'
    }
    $recalculate = $controller.GetField('m_RecalculatePreview',$flags)
    $update = $controller.GetMethod('UpdatePreview',$flags,$null,[Type[]]@(),$null)
    if (-not $recalculate -or $recalculate.FieldType -ne [bool] -or -not $update -or $update.ReturnType -ne [void]) { throw 'Exact preview refresh contract is unavailable.' }

    $report = [ordered]@{
        status = 'passed'
        target = 'Pathfinder: Kingmaker 2.1.7b'
        assembly_path = $assemblyPath
        assembly_sha256 = Get-Sha256 $assemblyPath
        assembly_identity = $assembly.FullName
        assembly_mvid = $assembly.ManifestModule.ModuleVersionId.ToString('D')
        signatures = @(
            $constructor.ToString(), $start.ToString(), $complete.ToString(),
            "Game.Instance.UI.CharacterBuildController.LevelUpController -> $($controller.FullName)",
            "$($controller.FullName).State", "$($controller.FullName).Unit", "$($controller.FullName).Preview",
            'Game.Instance.Player.MainCharacter.Value.Descriptor',
            "$($controller.FullName).m_RecalculatePreview", "$($controller.FullName).UpdatePreview()"
        )
        abilities = $abilityNames
        context_paths = [ordered]@{ main_character=$mainPath; player_faction=$playerPath; pet=$petPath; enemy=$enemyPath }
        identity_paths = [ordered]@{
            player_main_character = 'Game.Instance.Player.MainCharacter.Value.Descriptor'
            controller_unit = 'Game.Instance.UI.CharacterBuildController.LevelUpController.Unit'
            controller_preview = 'Game.Instance.UI.CharacterBuildController.LevelUpController.Preview'
        }
    }
    $target = if ([IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $root $OutputPath }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $target -Encoding UTF8
    Write-Host "Kingmaker contract verification passed: $target"
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}
