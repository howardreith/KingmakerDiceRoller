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
$allFlags = [Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
$nestedFlags = [Reflection.BindingFlags]'Public,NonPublic'

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
function Member-IsWritable([Reflection.MemberInfo] $Member) {
    if ($Member -is [Reflection.PropertyInfo]) { return $null -ne $Member.GetSetMethod($true) }
    if ($Member -is [Reflection.FieldInfo]) { return -not $Member.IsInitOnly -and -not $Member.IsLiteral }
    return $false
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
function Test-ByteSequence([byte[]] $Bytes, [byte[]] $Sequence) {
    return (Find-ByteSequenceOffset $Bytes $Sequence) -ge 0
}
function Find-ByteSequenceOffset([byte[]] $Bytes, [byte[]] $Sequence) {
    if (-not $Bytes -or -not $Sequence -or $Sequence.Length -gt $Bytes.Length) { return -1 }
    for ($offset = 0; $offset -le $Bytes.Length - $Sequence.Length; $offset++) {
        $matches = $true
        for ($index = 0; $index -lt $Sequence.Length; $index++) {
            if ($Bytes[$offset + $index] -ne $Sequence[$index]) { $matches = $false; break }
        }
        if ($matches) { return $offset }
    }
    return -1
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
    $unitHelper = Require-Type $assembly 'Kingmaker.UnitLogic.UnitHelper'
    $unitEntity = Require-Type $assembly 'Kingmaker.EntitySystem.Entities.UnitEntityData'
    $unitReference = Require-Type $assembly 'Kingmaker.EntitySystem.Entities.UnitReference'
    $playerType = Require-Type $assembly 'Kingmaker.Player'
    $distribution = Require-Type $assembly 'Kingmaker.UnitLogic.Class.LevelUp.StatsDistribution'
    $statType = Require-Type $assembly 'Kingmaker.EntitySystem.Stats.StatType'
    $statHelper = Require-Type $assembly 'Kingmaker.EntitySystem.Stats.StatTypeHelper'
    $mode = $state.GetNestedType('CharBuildMode', [Reflection.BindingFlags]'Public,NonPublic')
    if (-not $mode -or -not $mode.IsEnum) { throw 'LevelUpState.CharBuildMode enum was not found.' }
    $charGenMode = [Enum]::Parse($mode, 'CharGen', $false)
    if ([int] $charGenMode -ne 1) { throw 'LevelUpState.CharBuildMode.CharGen is not exact value 1.' }
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
    $isEmployee = Require-Member $state 'IsEmployee'
    if ((Member-Type $isEmployee) -ne [bool]) { throw 'IsEmployee is not Boolean.' }
    $stateMode = Require-Member $state 'Mode'
    if ((Member-Type $stateMode) -ne $mode) { throw 'LevelUpState.Mode is not the exact CharBuildMode enum.' }
    $isCustomCompanion = $unitHelper.GetMethod(
        'IsCustomCompanion',
        $staticFlags,
        $null,
        [Type[]]@($unit),
        $null)
    if (-not $isCustomCompanion -or $isCustomCompanion.ReturnType -ne [bool]) {
        throw 'Exact bool UnitHelper.IsCustomCompanion(UnitDescriptor) was not found.'
    }
    $employeeGetter = if ($isEmployee -is [Reflection.PropertyInfo]) { $isEmployee.GetGetMethod($true) } else { $null }
    if (-not $employeeGetter -or $employeeGetter.IsStatic -or
        -not (Test-ByteSequence ($employeeGetter.GetMethodBody().GetILAsByteArray()) ([BitConverter]::GetBytes($isCustomCompanion.MetadataToken)))) {
        throw 'LevelUpState.IsEmployee does not call the exact UnitHelper.IsCustomCompanion(UnitDescriptor) discriminator.'
    }
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
    $available = Require-Member $distribution 'Available'
    $points = Require-Member $distribution 'Points'
    $totalPoints = Require-Member $distribution 'TotalPoints'
    if ((Member-Type $available) -ne [bool] -or
        (Member-Type $points) -ne [int] -or
        (Member-Type $totalPoints) -ne [int] -or
        -not (Member-IsWritable $available) -or
        -not (Member-IsWritable $points) -or
        -not (Member-IsWritable $totalPoints)) {
        throw 'StatsDistribution allocator state members must have exact writable Boolean/Int32 contracts.'
    }
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

    $levelUpAction = Require-Type $assembly 'Kingmaker.UnitLogic.Class.LevelUp.Actions.ILevelUpAction'
    $applyLevelup = $controller.GetMethod('ApplyLevelup',$flags,$null,[Type[]]@($unit),$null)
    $commit = $controller.GetMethod('Commit',$flags,$null,[Type[]]@(),$null)
    $setupNewCharacter = $controller.GetMethod('SetupNewCharacher',$flags,$null,[Type[]]@(),$null)
    $applyAction = $levelUpAction.GetMethod('Apply',$flags,$null,[Type[]]@($state,$unit),$null)
    $onSuccess = $controller.GetField('m_OnSuccess',$flags)
    if (-not $applyLevelup -or $applyLevelup.IsStatic -or $applyLevelup.IsPublic -or
        -not $applyLevelup.ReturnType.IsGenericType -or
        $applyLevelup.ReturnType.GetGenericTypeDefinition().FullName -ne 'System.Collections.Generic.List`1' -or
        $applyLevelup.ReturnType.GetGenericArguments()[0] -ne $levelUpAction -or
        -not $commit -or $commit.IsStatic -or -not $commit.IsPublic -or $commit.ReturnType -ne [void] -or
        -not $setupNewCharacter -or $setupNewCharacter.IsStatic -or $setupNewCharacter.IsPublic -or
        $setupNewCharacter.ReturnType -ne [void] -or
        -not $applyAction -or $applyAction.IsStatic -or $applyAction.ReturnType -ne [void] -or
        -not $onSuccess -or $onSuccess.IsStatic -or $onSuccess.FieldType -ne [Action]) {
        throw 'Exact LevelUpController authoritative finalization methods were not found.'
    }
    $commitBytes = $commit.GetMethodBody().GetILAsByteArray()
    $commitUnitOffset = Find-ByteSequenceOffset $commitBytes ([BitConverter]::GetBytes($controllerUnit.MetadataToken))
    $commitApplyOffset = Find-ByteSequenceOffset $commitBytes ([BitConverter]::GetBytes($applyLevelup.MetadataToken))
    $commitSetupOffset = Find-ByteSequenceOffset $commitBytes ([BitConverter]::GetBytes($setupNewCharacter.MetadataToken))
    $commitSuccessOffset = Find-ByteSequenceOffset $commitBytes ([BitConverter]::GetBytes($onSuccess.MetadataToken))
    if ($commitUnitOffset -lt 0 -or $commitApplyOffset -le $commitUnitOffset -or
        $commitSetupOffset -le $commitApplyOffset -or $commitSuccessOffset -le $commitSetupOffset) {
        throw 'LevelUpController.Commit no longer applies to Unit before first-level setup and the success callback.'
    }
    $applyBytes = $applyLevelup.GetMethodBody().GetILAsByteArray()
    if ((Find-ByteSequenceOffset $applyBytes ([BitConverter]::GetBytes($constructor.MetadataToken))) -lt 0 -or
        (Find-ByteSequenceOffset $applyBytes ([BitConverter]::GetBytes($applyAction.MetadataToken))) -lt 0) {
        throw 'ApplyLevelup no longer constructs a fresh LevelUpState and replays ILevelUpAction.Apply.'
    }
    $updateBytes = $update.GetMethodBody().GetILAsByteArray()
    if ((Find-ByteSequenceOffset $updateBytes ([BitConverter]::GetBytes($applyLevelup.MetadataToken))) -lt 0) {
        throw 'UpdatePreview no longer uses the same ApplyLevelup replay path as finalization.'
    }

    $createCompanionAction = Require-Type $assembly 'Kingmaker.Designers.EventConditionActionSystem.Actions.CreateCustomCompanion'
    $levelUpInitiateHandler = Require-Type $assembly 'Kingmaker.PubSubSystem.ILevelUpInitiateUIHandler'
    $runCreateCompanion = $createCompanionAction.GetMethod('RunAction',$flags,$null,[Type[]]@(),$null)
    $playerCreateCandidates = @($playerType.GetMethods($allFlags) | Where-Object {
        if ($_.Name -ne 'CreateCustomCompanion') { return $false }
        $parameters = $_.GetParameters()
        return $parameters.Length -eq 3 -and
            $parameters[0].ParameterType -eq [Action] -and
            $parameters[1].ParameterType.IsGenericType -and
            $parameters[1].ParameterType.GetGenericTypeDefinition().FullName -eq 'System.Nullable`1' -and
            $parameters[1].ParameterType.GetGenericArguments()[0] -eq [int] -and
            $parameters[2].ParameterType -eq [bool]
    })
    if (-not $runCreateCompanion -or $runCreateCompanion.ReturnType -ne [void] -or
        $playerCreateCandidates.Count -ne 1) {
        throw 'Exact CreateCustomCompanion.RunAction -> Player.CreateCustomCompanion contract was not found.'
    }
    $playerCreateCompanion = $playerCreateCandidates[0]
    $actionClosureType = $createCompanionAction.GetNestedType('<>c__DisplayClass5_0',$nestedFlags)
    $playerClosureType = $playerType.GetNestedType('<>c__DisplayClass168_0',$nestedFlags)
    if (-not $actionClosureType -or -not $playerClosureType) {
        throw 'Expected mercenary success/start callback closure types were not found.'
    }
    $actionSuccessCallback = $actionClosureType.GetMethod('<RunAction>b__0',$flags,$null,[Type[]]@(),$null)
    $playerStartCallback = $playerClosureType.GetMethod(
        '<CreateCustomCompanion>b__0',
        $flags,
        $null,
        [Type[]]@($levelUpInitiateHandler),
        $null)
    $handleLevelUpStart = $levelUpInitiateHandler.GetMethod('HandleLevelUpStart',$flags)
    $characterBuildHandleStart = $characterBuild.GetMethod('HandleLevelUpStart',$flags)
    if (-not $actionSuccessCallback -or $actionSuccessCallback.ReturnType -ne [void] -or
        -not $playerStartCallback -or $playerStartCallback.ReturnType -ne [void] -or
        -not $handleLevelUpStart -or -not $characterBuildHandleStart) {
        throw 'Mercenary creation callback or HandleLevelUpStart contract was not found.'
    }
    $handleParameters = $handleLevelUpStart.GetParameters()
    if ($handleParameters.Length -ne 4 -or $handleParameters[0].ParameterType -ne $unit -or
        $handleParameters[1].ParameterType.FullName -ne 'Newtonsoft.Json.Linq.JToken' -or
        $handleParameters[2].ParameterType -ne [Action] -or $handleParameters[3].ParameterType -ne $mode) {
        throw 'ILevelUpInitiateUIHandler.HandleLevelUpStart has an unexpected signature.'
    }
    $levelUpStart = $controller.GetMethod(
        'Start',
        $staticFlags,
        $null,
        [Type[]]@($unit,[bool],$handleParameters[1].ParameterType,[Action],$mode),
        $null)
    if (-not $levelUpStart -or -not $levelUpStart.IsStatic -or $levelUpStart.ReturnType -ne $controller) {
        throw 'Exact LevelUpController.Start mercenary entry contract was not found.'
    }
    $runBytes = $runCreateCompanion.GetMethodBody().GetILAsByteArray()
    $runCallbackOffset = Find-ByteSequenceOffset $runBytes ([BitConverter]::GetBytes($actionSuccessCallback.MetadataToken))
    $runCreateOffset = Find-ByteSequenceOffset $runBytes ([BitConverter]::GetBytes($playerCreateCompanion.MetadataToken))
    $playerCreateBytes = $playerCreateCompanion.GetMethodBody().GetILAsByteArray()
    $playerCallbackBytes = $playerStartCallback.GetMethodBody().GetILAsByteArray()
    $characterBuildStartBytes = $characterBuildHandleStart.GetMethodBody().GetILAsByteArray()
    if ($runCallbackOffset -lt 0 -or $runCreateOffset -le $runCallbackOffset -or
        (Find-ByteSequenceOffset $playerCreateBytes ([BitConverter]::GetBytes($playerStartCallback.MetadataToken))) -lt 0 -or
        (Find-ByteSequenceOffset $playerCallbackBytes ([BitConverter]::GetBytes($handleLevelUpStart.MetadataToken))) -lt 0 -or
        (Find-ByteSequenceOffset $characterBuildStartBytes ([BitConverter]::GetBytes($levelUpStart.MetadataToken))) -lt 0) {
        throw 'The native mercenary entry lifecycle no longer reaches LevelUpController.Start through the expected callbacks.'
    }
    $onCreate = $createCompanionAction.GetField('OnCreate',$flags)
    if (-not $onCreate -or
        (Find-ByteSequenceOffset ($actionSuccessCallback.GetMethodBody().GetILAsByteArray()) ([BitConverter]::GetBytes($onCreate.MetadataToken))) -lt 0) {
        throw 'The supplied mercenary success callback no longer invokes CreateCustomCompanion.OnCreate.'
    }
    $remoteCompanions = Require-Member $playerType 'RemoteCompanions'
    $crossSceneState = $playerType.GetField('CrossSceneState',$flags)
    $gamePlayerReader = if ($gamePlayer -is [Reflection.PropertyInfo]) { $gamePlayer.GetGetMethod($true) } else { $gamePlayer }
    $remoteCompanionsReader = if ($remoteCompanions -is [Reflection.PropertyInfo]) { $remoteCompanions.GetGetMethod($true) } else { $remoteCompanions }
    $setupBytes = $setupNewCharacter.GetMethodBody().GetILAsByteArray()
    if (-not $crossSceneState -or
        (Find-ByteSequenceOffset $setupBytes ([BitConverter]::GetBytes($controllerUnit.MetadataToken))) -lt 0 -or
        (Find-ByteSequenceOffset $setupBytes ([BitConverter]::GetBytes($gamePlayerReader.MetadataToken))) -lt 0 -or
        (Find-ByteSequenceOffset $setupBytes ([BitConverter]::GetBytes($crossSceneState.MetadataToken))) -lt 0 -or
        (Find-ByteSequenceOffset $setupBytes ([BitConverter]::GetBytes($remoteCompanionsReader.MetadataToken))) -lt 0) {
        throw 'SetupNewCharacher no longer inserts LevelUpController.Unit through native player companion ownership.'
    }
    $phaseKind = Require-Type $assembly 'Kingmaker.UI.LevelUp.Phase.CharBPhase+Type'
    $skillsPhase = Require-Type $assembly 'Kingmaker.UI.LevelUp.Phase.CharBPhaseSkills'
    $abilityAllocator = Require-Type $assembly 'Kingmaker.UI.LevelUp.CharBAbilityScoresAllocator'
    $currentPhase = Require-Member $characterBuild 'CurrentPhase'
    if ([Nullable]::GetUnderlyingType((Member-Type $currentPhase)) -ne $phaseKind) {
        throw 'CharacterBuildController.CurrentPhase is not Nullable<CharBPhase.Type>.'
    }
    $skillsValue = [Enum]::Parse($phaseKind, 'Skills', $false)
    if ($skillsValue.ToString() -ne 'Skills') { throw 'CharBPhase.Type.Skills was not found.' }
    $skillsMember = Require-Member $characterBuild 'Skills'
    if ((Member-Type $skillsMember) -ne $skillsPhase) { throw 'CharacterBuildController.Skills has an unexpected type.' }
    $abilityAllocatorMember = Require-Member $skillsPhase 'AbilityScoresAllocator'
    if ((Member-Type $abilityAllocatorMember) -ne $abilityAllocator) { throw 'CharBPhaseSkills.AbilityScoresAllocator has an unexpected type.' }
    $fillAbilityData = $abilityAllocator.GetMethod('FillData',$flags,$null,[Type[]]@(),$null)
    if (-not $fillAbilityData -or $fillAbilityData.ReturnType -ne [void]) { throw 'Exact CharBAbilityScoresAllocator.FillData() was not found.' }
    $unitEntityMember = Require-Member $unit 'Unit'
    if (-not $unitEntity.IsAssignableFrom((Member-Type $unitEntityMember))) { throw 'UnitDescriptor.Unit is not a UnitEntityData contract.' }
    $allocatorSource = $abilityAllocator.GetField('m_Unit',$flags)
    $allocatorPreview = $abilityAllocator.GetField('m_PreviewUnit',$flags)
    if (-not $allocatorSource -or $allocatorSource.FieldType -ne $unitEntity -or
        -not $allocatorPreview -or $allocatorPreview.FieldType -ne $unitEntity) {
        throw 'Exact CharBAbilityScoresAllocator source/preview binding fields were not found.'
    }
    $allocatorEntries = $abilityAllocator.GetField('m_StatEntries',$flags)
    if (-not $allocatorEntries -or -not $allocatorEntries.FieldType.IsGenericType -or
        $allocatorEntries.FieldType.GetGenericTypeDefinition().FullName -ne 'System.Collections.Generic.List`1') {
        throw 'Exact CharBAbilityScoresAllocator.m_StatEntries list was not found.'
    }
    $scoreEntry = $allocatorEntries.FieldType.GetGenericArguments()[0]
    if ($scoreEntry.FullName -ne 'Kingmaker.UI.LevelUp.CharBScoresEntry') {
        throw 'Ability allocator score rows are not exact CharBScoresEntry instances.'
    }
    $upButton = $scoreEntry.GetField('UpButton',$flags)
    $downButton = $scoreEntry.GetField('DownButton',$flags)
    if (-not $upButton -or -not $downButton -or
        $upButton.IsStatic -or $downButton.IsStatic -or
        $upButton.FieldType -ne $downButton.FieldType -or
        $upButton.FieldType.FullName -ne 'UnityEngine.UI.Button') {
        throw 'Exact CharBScoresEntry UpButton/DownButton instance fields were not found.'
    }
    $interactable = $upButton.FieldType.GetProperty('interactable',$flags)
    if (-not $interactable -or $interactable.PropertyType -ne [bool] -or
        -not $interactable.GetGetMethod($true) -or -not $interactable.GetSetMethod($true)) {
        throw 'UnityEngine.UI.Selectable.interactable is not an exact writable Boolean contract.'
    }
    $mainLabel = $abilityAllocator.GetField('m_MainLabel',$flags)
    $frame = $abilityAllocator.GetField('m_Frame',$flags)
    $raceBonusContainer = $abilityAllocator.GetField('m_RaceBonusContainer',$flags)
    if (-not $mainLabel -or $mainLabel.IsStatic -or $mainLabel.FieldType.FullName -ne 'TMPro.TextMeshProUGUI' -or
        -not $frame -or $frame.IsStatic -or $frame.FieldType.FullName -ne 'UnityEngine.UI.Image' -or
        -not $raceBonusContainer -or $raceBonusContainer.IsStatic -or $raceBonusContainer.FieldType.FullName -ne 'UnityEngine.GameObject') {
        throw 'Native ability allocator text/frame/racial-bonus UI anchors were not found.'
    }

    $report = [ordered]@{
        status = 'passed'
        target = 'Pathfinder: Kingmaker 2.1.7b'
        assembly_path = $assemblyPath
        assembly_sha256 = Get-Sha256 $assemblyPath
        assembly_identity = $assembly.FullName
        assembly_mvid = $assembly.ManifestModule.ModuleVersionId.ToString('D')
        signatures = @(
            $constructor.ToString(), $start.ToString(), $complete.ToString(),
            "$($state.FullName).IsEmployee",
            "$($unitHelper.FullName).IsCustomCompanion($($unit.FullName))",
            "Game.Instance.UI.CharacterBuildController.LevelUpController -> $($controller.FullName)",
            "$($controller.FullName).State", "$($controller.FullName).Unit", "$($controller.FullName).Preview",
            'Game.Instance.Player.MainCharacter.Value.Descriptor',
            "$($controller.FullName).m_RecalculatePreview", "$($controller.FullName).UpdatePreview()",
            "$($createCompanionAction.FullName).RunAction()",
            "$($playerType.FullName).CreateCustomCompanion(Action, Nullable<Int32>, Boolean)",
            "$($levelUpInitiateHandler.FullName).HandleLevelUpStart(UnitDescriptor, JToken, Action, CharBuildMode)",
            "$($controller.FullName).Start(UnitDescriptor, Boolean, JToken, Action, CharBuildMode)",
            "$($controller.FullName).ApplyLevelup(UnitDescriptor)",
            "$($controller.FullName).Commit()", "$($controller.FullName).SetupNewCharacher()",
            "$($distribution.FullName).Available", "$($distribution.FullName).Points", "$($distribution.FullName).TotalPoints",
            "$($characterBuild.FullName).CurrentPhase == $($phaseKind.FullName).Skills",
            "$($characterBuild.FullName).Skills -> $($skillsPhase.FullName).AbilityScoresAllocator",
            "$($abilityAllocator.FullName).FillData()",
            "$($abilityAllocator.FullName).m_Unit", "$($abilityAllocator.FullName).m_PreviewUnit",
            "$($abilityAllocator.FullName).m_StatEntries -> $($scoreEntry.FullName)",
            "$($scoreEntry.FullName).UpButton", "$($scoreEntry.FullName).DownButton",
            "$($upButton.FieldType.FullName).interactable",
            "$($abilityAllocator.FullName).m_MainLabel", "$($abilityAllocator.FullName).m_Frame",
            "$($abilityAllocator.FullName).m_RaceBonusContainer"
        )
        abilities = $abilityNames
        context_paths = [ordered]@{ main_character=$mainPath; player_faction=$playerPath; pet=$petPath; enemy=$enemyPath }
        identity_paths = [ordered]@{
            player_main_character = 'Game.Instance.Player.MainCharacter.Value.Descriptor'
            controller_unit = 'Game.Instance.UI.CharacterBuildController.LevelUpController.Unit'
            controller_preview = 'Game.Instance.UI.CharacterBuildController.LevelUpController.Preview'
        }
        mercenary_discriminator = [ordered]@{
            state = 'Kingmaker.UnitLogic.Class.LevelUp.LevelUpState.IsEmployee'
            stable_owner = 'Kingmaker.UnitLogic.UnitHelper.IsCustomCompanion(LevelUpController.Unit)'
            observed_mode = 'CharGen'
        }
        mercenary_entry_lifecycle = [ordered]@{
            action = 'CreateCustomCompanion.RunAction'
            player_factory = 'Player.CreateCustomCompanion(successCallback, xp, importable)'
            start_callback = 'ILevelUpInitiateUIHandler.HandleLevelUpStart(newCompanion.Descriptor, null, successCallback, CharGen)'
            controller_start = 'CharacterBuildController.HandleLevelUpStart -> LevelUpController.Start'
        }
        mercenary_finalization = [ordered]@{
            native_order = 'LevelUpController.Commit -> ApplyLevelup(LevelUpController.Unit) -> SetupNewCharacher -> m_OnSuccess'
            replay = 'ApplyLevelup constructs a fresh LevelUpState and replays ILevelUpAction.Apply against the supplied descriptor'
            preview = 'UpdatePreview also invokes ApplyLevelup, but on the transient Preview descriptor'
            authoritative_assignment_seam = 'postfix LevelUpController.ApplyLevelup(Unit), before SetupNewCharacher and success callback'
            verification_seam = 'postfix LevelUpController.Commit, after success callback'
            final_descriptor = 'LevelUpController.Unit inserted through Player.CrossSceneState and Player.RemoteCompanions'
        }
        allocator_state_writable = $true
        ability_presentation = [ordered]@{
            active_phase = 'Game.Instance.UI.CharacterBuildController.CurrentPhase == Skills'
            phase = 'Game.Instance.UI.CharacterBuildController.Skills'
            allocator = 'CharBPhaseSkills.AbilityScoresAllocator'
            refresh = 'Kingmaker.UI.LevelUp.CharBAbilityScoresAllocator.FillData()'
            bindings = @('m_Unit','m_PreviewUnit')
            score_rows = 'm_StatEntries -> CharBScoresEntry'
            controls = @('UpButton.interactable','DownButton.interactable')
            style_anchors = @('m_MainLabel: TMPro.TextMeshProUGUI','m_Frame: UnityEngine.UI.Image')
            preferred_access_tab_geometry = 'm_RaceBonusContainer: UnityEngine.GameObject'
            fallback_access_tab_geometry = @('m_Frame: UnityEngine.UI.Image','ability allocator RectTransform','ability phase root RectTransform')
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
