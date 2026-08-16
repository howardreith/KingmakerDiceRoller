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
    $getStat = $statsType.GetMethod('GetStat',$flags,$null,[Type[]]@($statType),$null)
    if (-not $getStat) { throw 'Unit stats GetStat(StatType) was not found.' }
    $baseValue = Require-Member $getStat.ReturnType 'BaseValue'
    if ((Member-Type $baseValue) -ne [int]) { throw 'BaseValue is not Int32.' }
    $statValues = Require-Member $distribution 'StatValues'
    if (-not [Collections.IDictionary].IsAssignableFrom((Member-Type $statValues))) { throw 'StatValues is not IDictionary-compatible.' }
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
    $controllerMember = Require-Member $game 'LevelUpController'
    $controller = Member-Type $controllerMember
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
            "$($controller.FullName).m_RecalculatePreview", "$($controller.FullName).UpdatePreview()"
        )
        abilities = $abilityNames
        context_paths = [ordered]@{ main_character=$mainPath; player_faction=$playerPath; pet=$petPath; enemy=$enemyPath }
    }
    $target = if ([IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $root $OutputPath }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $target -Encoding UTF8
    Write-Host "Kingmaker contract verification passed: $target"
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}
