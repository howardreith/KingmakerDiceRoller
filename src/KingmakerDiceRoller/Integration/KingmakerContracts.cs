using System;
using System.Collections.Generic;
using System.Reflection;

namespace KingmakerDiceRoller.Integration
{
    public sealed class KingmakerContracts
    {
        internal KingmakerContracts(
            Assembly gameAssembly,
            Type levelUpStateType,
            Type unitDescriptorType,
            Type charBuildModeType,
            Type statsDistributionType,
            Type statTypeType,
            ConstructorInfo levelUpStateConstructor,
            MethodInfo distributionStartMethod,
            MethodInfo distributionIsCompleteMethod,
            MemberInfo levelUpStateUnitMember,
            MemberInfo levelUpStateDistributionMember,
            MemberInfo levelUpStateIsFirstLevelMember,
            MemberInfo unitStatsMember,
            MethodInfo unitStatsGetStatMethod,
            MemberInfo statBaseValueMember,
            MemberInfo distributionStatValuesMember,
            MemberInfo distributionTotalPointsMember,
            object[] abilityStatKeys,
            MemberInfo gameInstanceMember,
            MemberInfo gameUiMember,
            MemberInfo uiCharacterBuildControllerMember,
            MemberInfo characterBuildLevelUpControllerMember,
            MemberInfo levelUpControllerStateMember,
            FieldInfo previewRecalculateField,
            MethodInfo previewUpdateMethod,
            IReadOnlyList<string> evidence)
        {
            GameAssembly = gameAssembly;
            LevelUpStateType = levelUpStateType;
            UnitDescriptorType = unitDescriptorType;
            CharBuildModeType = charBuildModeType;
            StatsDistributionType = statsDistributionType;
            StatTypeType = statTypeType;
            LevelUpStateConstructor = levelUpStateConstructor;
            DistributionStartMethod = distributionStartMethod;
            DistributionIsCompleteMethod = distributionIsCompleteMethod;
            LevelUpStateUnitMember = levelUpStateUnitMember;
            LevelUpStateDistributionMember = levelUpStateDistributionMember;
            LevelUpStateIsFirstLevelMember = levelUpStateIsFirstLevelMember;
            UnitStatsMember = unitStatsMember;
            UnitStatsGetStatMethod = unitStatsGetStatMethod;
            StatBaseValueMember = statBaseValueMember;
            DistributionStatValuesMember = distributionStatValuesMember;
            DistributionTotalPointsMember = distributionTotalPointsMember;
            AbilityStatKeys = abilityStatKeys;
            GameInstanceMember = gameInstanceMember;
            GameUiMember = gameUiMember;
            UiCharacterBuildControllerMember = uiCharacterBuildControllerMember;
            CharacterBuildLevelUpControllerMember = characterBuildLevelUpControllerMember;
            LevelUpControllerStateMember = levelUpControllerStateMember;
            PreviewRecalculateField = previewRecalculateField;
            PreviewUpdateMethod = previewUpdateMethod;
            Evidence = evidence;
        }

        public Assembly GameAssembly { get; }
        public Type LevelUpStateType { get; }
        public Type UnitDescriptorType { get; }
        public Type CharBuildModeType { get; }
        public Type StatsDistributionType { get; }
        public Type StatTypeType { get; }
        public ConstructorInfo LevelUpStateConstructor { get; }
        public MethodInfo DistributionStartMethod { get; }
        public MethodInfo DistributionIsCompleteMethod { get; }
        public MemberInfo LevelUpStateUnitMember { get; }
        public MemberInfo LevelUpStateDistributionMember { get; }
        public MemberInfo LevelUpStateIsFirstLevelMember { get; }
        public MemberInfo UnitStatsMember { get; }
        public MethodInfo UnitStatsGetStatMethod { get; }
        public MemberInfo StatBaseValueMember { get; }
        public MemberInfo DistributionStatValuesMember { get; }
        public MemberInfo DistributionTotalPointsMember { get; }
        public object[] AbilityStatKeys { get; }
        public MemberInfo GameInstanceMember { get; }
        public MemberInfo GameUiMember { get; }
        public MemberInfo UiCharacterBuildControllerMember { get; }
        public MemberInfo CharacterBuildLevelUpControllerMember { get; }
        public MemberInfo LevelUpControllerStateMember { get; }
        public FieldInfo PreviewRecalculateField { get; }
        public MethodInfo PreviewUpdateMethod { get; }
        public IReadOnlyList<string> Evidence { get; }

        public bool TryGetLevelUpController(out object controller)
        {
            controller = null;
            try
            {
                object game = ReflectionAccess.Read(GameInstanceMember, null);
                if (game == null) return true;
                object ui = ReflectionAccess.Read(GameUiMember, game);
                if (ui == null) return true;
                object characterBuildController = ReflectionAccess.Read(UiCharacterBuildControllerMember, ui);
                if (characterBuildController == null) return true;
                controller = ReflectionAccess.Read(CharacterBuildLevelUpControllerMember, characterBuildController);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryGetCurrentLevelUpState(out object state)
        {
            state = null;
            object controller;
            if (!TryGetLevelUpController(out controller)) return false;
            if (controller == null) return true;
            try
            {
                object candidate = ReflectionAccess.Read(LevelUpControllerStateMember, controller);
                if (candidate != null && !LevelUpStateType.IsInstanceOfType(candidate)) return false;
                state = candidate;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string AssemblyIdentity => GameAssembly.FullName;
        public Guid AssemblyMvid => GameAssembly.ManifestModule.ModuleVersionId;
    }
}
