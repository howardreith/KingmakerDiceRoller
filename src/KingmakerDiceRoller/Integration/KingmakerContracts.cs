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
            MemberInfo distributionAvailableMember,
            MemberInfo distributionPointsMember,
            MemberInfo distributionTotalPointsMember,
            object[] abilityStatKeys,
            MemberInfo gameInstanceMember,
            MemberInfo gameUiMember,
            MemberInfo gamePlayerMember,
            MemberInfo playerMainCharacterMember,
            MemberInfo uiCharacterBuildControllerMember,
            MemberInfo characterBuildLevelUpControllerMember,
            MemberInfo levelUpControllerStateMember,
            MemberInfo levelUpControllerUnitMember,
            MemberInfo levelUpControllerPreviewMember,
            FieldInfo previewRecalculateField,
            MethodInfo previewUpdateMethod,
            MemberInfo characterBuildCurrentPhaseMember,
            object skillsPhaseValue,
            MemberInfo characterBuildSkillsPhaseMember,
            MemberInfo abilityPhaseAllocatorMember,
            MethodInfo abilityAllocatorFillDataMethod,
            MemberInfo unitDescriptorEntityMember,
            FieldInfo abilityAllocatorSourceEntityField,
            FieldInfo abilityAllocatorPreviewEntityField,
            FieldInfo abilityAllocatorStatEntriesField,
            FieldInfo scoreEntryUpButtonField,
            FieldInfo scoreEntryDownButtonField,
            PropertyInfo selectableInteractableProperty,
            FieldInfo abilityAllocatorMainLabelField,
            FieldInfo abilityAllocatorFrameField,
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
            DistributionAvailableMember = distributionAvailableMember;
            DistributionPointsMember = distributionPointsMember;
            DistributionTotalPointsMember = distributionTotalPointsMember;
            AbilityStatKeys = abilityStatKeys;
            GameInstanceMember = gameInstanceMember;
            GameUiMember = gameUiMember;
            GamePlayerMember = gamePlayerMember;
            PlayerMainCharacterMember = playerMainCharacterMember;
            UiCharacterBuildControllerMember = uiCharacterBuildControllerMember;
            CharacterBuildLevelUpControllerMember = characterBuildLevelUpControllerMember;
            LevelUpControllerStateMember = levelUpControllerStateMember;
            LevelUpControllerUnitMember = levelUpControllerUnitMember;
            LevelUpControllerPreviewMember = levelUpControllerPreviewMember;
            PreviewRecalculateField = previewRecalculateField;
            PreviewUpdateMethod = previewUpdateMethod;
            CharacterBuildCurrentPhaseMember = characterBuildCurrentPhaseMember;
            SkillsPhaseValue = skillsPhaseValue;
            CharacterBuildSkillsPhaseMember = characterBuildSkillsPhaseMember;
            AbilityPhaseAllocatorMember = abilityPhaseAllocatorMember;
            AbilityAllocatorFillDataMethod = abilityAllocatorFillDataMethod;
            UnitDescriptorEntityMember = unitDescriptorEntityMember;
            AbilityAllocatorSourceEntityField = abilityAllocatorSourceEntityField;
            AbilityAllocatorPreviewEntityField = abilityAllocatorPreviewEntityField;
            AbilityAllocatorStatEntriesField = abilityAllocatorStatEntriesField;
            ScoreEntryUpButtonField = scoreEntryUpButtonField;
            ScoreEntryDownButtonField = scoreEntryDownButtonField;
            SelectableInteractableProperty = selectableInteractableProperty;
            AbilityAllocatorMainLabelField = abilityAllocatorMainLabelField;
            AbilityAllocatorFrameField = abilityAllocatorFrameField;
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
        public MemberInfo DistributionAvailableMember { get; }
        public MemberInfo DistributionPointsMember { get; }
        public MemberInfo DistributionTotalPointsMember { get; }
        public object[] AbilityStatKeys { get; }
        public MemberInfo GameInstanceMember { get; }
        public MemberInfo GameUiMember { get; }
        public MemberInfo GamePlayerMember { get; }
        public MemberInfo PlayerMainCharacterMember { get; }
        public MemberInfo UiCharacterBuildControllerMember { get; }
        public MemberInfo CharacterBuildLevelUpControllerMember { get; }
        public MemberInfo LevelUpControllerStateMember { get; }
        public MemberInfo LevelUpControllerUnitMember { get; }
        public MemberInfo LevelUpControllerPreviewMember { get; }
        public FieldInfo PreviewRecalculateField { get; }
        public MethodInfo PreviewUpdateMethod { get; }
        public MemberInfo CharacterBuildCurrentPhaseMember { get; }
        public object SkillsPhaseValue { get; }
        public MemberInfo CharacterBuildSkillsPhaseMember { get; }
        public MemberInfo AbilityPhaseAllocatorMember { get; }
        public MethodInfo AbilityAllocatorFillDataMethod { get; }
        public MemberInfo UnitDescriptorEntityMember { get; }
        public FieldInfo AbilityAllocatorSourceEntityField { get; }
        public FieldInfo AbilityAllocatorPreviewEntityField { get; }
        public FieldInfo AbilityAllocatorStatEntriesField { get; }
        public FieldInfo ScoreEntryUpButtonField { get; }
        public FieldInfo ScoreEntryDownButtonField { get; }
        public PropertyInfo SelectableInteractableProperty { get; }
        public FieldInfo AbilityAllocatorMainLabelField { get; }
        public FieldInfo AbilityAllocatorFrameField { get; }
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

        public bool TryGetLevelUpControllerContext(
            out object controller,
            out object sourceUnit,
            out object state,
            out object preview)
        {
            controller = null;
            sourceUnit = null;
            state = null;
            preview = null;
            if (!TryGetLevelUpController(out controller)) return false;
            if (controller == null) return true;

            try
            {
                sourceUnit = ReflectionAccess.Read(LevelUpControllerUnitMember, controller);
                state = ReflectionAccess.Read(LevelUpControllerStateMember, controller);
                preview = ReflectionAccess.Read(LevelUpControllerPreviewMember, controller);
                if (sourceUnit != null && !UnitDescriptorType.IsInstanceOfType(sourceUnit)) return false;
                if (state != null && !LevelUpStateType.IsInstanceOfType(state)) return false;
                if (preview != null && !UnitDescriptorType.IsInstanceOfType(preview)) return false;
                return true;
            }
            catch
            {
                controller = null;
                sourceUnit = null;
                state = null;
                preview = null;
                return false;
            }
        }

        public bool TryGetAbilityPhasePresentationContext(
            out object characterBuildController,
            out bool abilityPhaseActive,
            out object abilityPhase,
            out object allocator)
        {
            characterBuildController = null;
            abilityPhaseActive = false;
            abilityPhase = null;
            allocator = null;
            try
            {
                object game = ReflectionAccess.Read(GameInstanceMember, null);
                if (game == null) return true;
                object ui = ReflectionAccess.Read(GameUiMember, game);
                if (ui == null) return true;
                characterBuildController = ReflectionAccess.Read(UiCharacterBuildControllerMember, ui);
                if (characterBuildController == null) return true;

                object currentPhase = ReflectionAccess.Read(
                    CharacterBuildCurrentPhaseMember,
                    characterBuildController);
                abilityPhaseActive = currentPhase != null && Equals(currentPhase, SkillsPhaseValue);
                abilityPhase = ReflectionAccess.Read(
                    CharacterBuildSkillsPhaseMember,
                    characterBuildController);
                if (abilityPhase == null) return true;
                allocator = ReflectionAccess.Read(AbilityPhaseAllocatorMember, abilityPhase);
                if (allocator != null &&
                    !AbilityAllocatorFillDataMethod.DeclaringType.IsInstanceOfType(allocator))
                {
                    characterBuildController = null;
                    abilityPhaseActive = false;
                    abilityPhase = null;
                    allocator = null;
                    return false;
                }
                return true;
            }
            catch
            {
                characterBuildController = null;
                abilityPhaseActive = false;
                abilityPhase = null;
                allocator = null;
                return false;
            }
        }

        public bool TryGetDescriptorEntity(object descriptor, out object entity)
        {
            entity = null;
            if (descriptor == null || !UnitDescriptorType.IsInstanceOfType(descriptor)) return false;
            try
            {
                entity = ReflectionAccess.Read(UnitDescriptorEntityMember, descriptor);
                return entity != null;
            }
            catch
            {
                entity = null;
                return false;
            }
        }

        public string AssemblyIdentity => GameAssembly.FullName;
        public Guid AssemblyMvid => GameAssembly.ManifestModule.ModuleVersionId;
    }
}
