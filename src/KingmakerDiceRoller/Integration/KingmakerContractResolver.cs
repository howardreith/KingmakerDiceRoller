using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KingmakerDiceRoller.Integration
{
    public sealed class KingmakerContractResolver
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public KingmakerContracts Resolve()
        {
            Assembly gameAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => string.Equals(assembly.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));
            if (gameAssembly == null)
            {
                throw new ContractResolutionException("Assembly-CSharp is not loaded.");
            }

            var evidence = new List<string>();
            Type levelUpStateType = RequireType(gameAssembly, "Kingmaker.UnitLogic.Class.LevelUp.LevelUpState");
            Type unitDescriptorType = RequireType(gameAssembly, "Kingmaker.UnitLogic.UnitDescriptor");
            Type statsDistributionType = RequireType(gameAssembly, "Kingmaker.UnitLogic.Class.LevelUp.StatsDistribution");
            Type statTypeType = RequireType(gameAssembly, "Kingmaker.EntitySystem.Stats.StatType");
            Type statTypeHelperType = RequireType(gameAssembly, "Kingmaker.EntitySystem.Stats.StatTypeHelper");
            Type charBuildModeType = levelUpStateType.GetNestedType("CharBuildMode", BindingFlags.Public | BindingFlags.NonPublic);
            if (charBuildModeType == null || !charBuildModeType.IsEnum)
            {
                throw new ContractResolutionException("LevelUpState.CharBuildMode enum was not found.");
            }

            ConstructorInfo constructor = levelUpStateType.GetConstructor(
                InstanceFlags,
                null,
                new[] { unitDescriptorType, charBuildModeType },
                null);
            if (constructor == null)
            {
                throw new ContractResolutionException("Exact LevelUpState(UnitDescriptor, CharBuildMode) constructor was not found.");
            }
            evidence.Add(Describe(constructor));

            MethodInfo start = statsDistributionType.GetMethod(
                "Start",
                InstanceFlags,
                null,
                new[] { typeof(int) },
                null);
            if (start == null || start.ReturnType != typeof(void))
            {
                throw new ContractResolutionException("Exact StatsDistribution.Start(int) method was not found.");
            }
            evidence.Add(Describe(start));

            MethodInfo isComplete = statsDistributionType.GetMethod(
                "IsComplete",
                InstanceFlags,
                null,
                Type.EmptyTypes,
                null);
            if (isComplete == null || isComplete.ReturnType != typeof(bool))
            {
                throw new ContractResolutionException("Exact bool StatsDistribution.IsComplete() method was not found.");
            }
            evidence.Add(Describe(isComplete));

            MemberInfo stateUnit = ReflectionAccess.RequireInstanceMember(levelUpStateType, "Unit");
            MemberInfo stateDistribution = ReflectionAccess.RequireInstanceMember(levelUpStateType, "StatsDistribution");
            MemberInfo isFirstLevel = ReflectionAccess.RequireInstanceMember(levelUpStateType, "IsFirstLevel");
            if (!unitDescriptorType.IsAssignableFrom(ReflectionAccess.GetMemberType(stateUnit)))
            {
                throw new ContractResolutionException("LevelUpState.Unit is not a UnitDescriptor contract.");
            }
            if (!statsDistributionType.IsAssignableFrom(ReflectionAccess.GetMemberType(stateDistribution)))
            {
                throw new ContractResolutionException("LevelUpState.StatsDistribution has an unexpected type.");
            }
            if (ReflectionAccess.GetMemberType(isFirstLevel) != typeof(bool))
            {
                throw new ContractResolutionException("LevelUpState.IsFirstLevel is not Boolean.");
            }

            MemberInfo unitStats = ReflectionAccess.RequireInstanceMember(unitDescriptorType, "Stats");
            Type unitStatsType = ReflectionAccess.GetMemberType(unitStats);
            MethodInfo[] getStatCandidates = unitStatsType.GetMethods(InstanceFlags)
                .Where(method => string.Equals(method.Name, "GetStat", StringComparison.Ordinal))
                .Where(method => !method.IsGenericMethod)
                .Where(method =>
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == statTypeType;
                })
                .ToArray();
            if (getStatCandidates.Length != 1)
            {
                throw new ContractResolutionException(
                    "Expected exactly one non-generic " + unitStatsType.FullName +
                    ".GetStat(StatType), found " + getStatCandidates.Length + ".");
            }
            MethodInfo getStat = getStatCandidates[0];
            MemberInfo baseValue = ReflectionAccess.RequireInstanceMember(getStat.ReturnType, "BaseValue");
            if (ReflectionAccess.GetMemberType(baseValue) != typeof(int))
            {
                throw new ContractResolutionException("The ability stat BaseValue member is not Int32.");
            }

            MemberInfo statValues = ReflectionAccess.RequireInstanceMember(statsDistributionType, "StatValues");
            if (!typeof(IDictionary).IsAssignableFrom(ReflectionAccess.GetMemberType(statValues)))
            {
                throw new ContractResolutionException("StatsDistribution.StatValues is not an IDictionary-compatible contract.");
            }

            MemberInfo available = ReflectionAccess.RequireInstanceMember(statsDistributionType, "Available");
            MemberInfo points = ReflectionAccess.RequireInstanceMember(statsDistributionType, "Points");
            MemberInfo totalPoints = ReflectionAccess.RequireInstanceMember(statsDistributionType, "TotalPoints");
            if (ReflectionAccess.GetMemberType(available) != typeof(bool) ||
                ReflectionAccess.GetMemberType(points) != typeof(int) ||
                ReflectionAccess.GetMemberType(totalPoints) != typeof(int) ||
                !ReflectionAccess.CanWrite(available) ||
                !ReflectionAccess.CanWrite(points) ||
                !ReflectionAccess.CanWrite(totalPoints))
            {
                throw new ContractResolutionException(
                    "StatsDistribution allocator state members must have exact writable Boolean/Int32 contracts.");
            }

            MemberInfo attributes = ReflectionAccess.RequireStaticMember(statTypeHelperType, "Attributes");
            Array attributeArray = ReflectionAccess.Read(attributes, null) as Array;
            if (attributeArray == null || attributeArray.Length != 6)
            {
                throw new ContractResolutionException("StatTypeHelper.Attributes must contain exactly six entries.");
            }
            var abilityKeys = new object[6];
            string[] expectedNames = { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };
            for (int index = 0; index < abilityKeys.Length; index++)
            {
                abilityKeys[index] = attributeArray.GetValue(index);
                if (!string.Equals(abilityKeys[index].ToString(), expectedNames[index], StringComparison.Ordinal))
                {
                    throw new ContractResolutionException("Unexpected ability order at index " + index + ": " + abilityKeys[index] + ".");
                }
            }

            Type gameType = RequireType(gameAssembly, "Kingmaker.Game");
            Type playerType = RequireType(gameAssembly, "Kingmaker.Player");
            Type unitReferenceType = RequireType(gameAssembly, "Kingmaker.EntitySystem.Entities.UnitReference");
            Type unitEntityDataType = RequireType(gameAssembly, "Kingmaker.EntitySystem.Entities.UnitEntityData");
            MemberInfo gameInstance = ReflectionAccess.RequireStaticMember(gameType, "Instance");
            MemberInfo gameUi = ReflectionAccess.RequireInstanceMember(gameType, "UI");
            MemberInfo gamePlayer = ReflectionAccess.RequireInstanceMember(gameType, "Player");
            if (!playerType.IsAssignableFrom(ReflectionAccess.GetMemberType(gamePlayer)))
            {
                throw new ContractResolutionException("Game.Player is not a Kingmaker.Player contract.");
            }
            MemberInfo playerMainCharacter = ReflectionAccess.RequireInstanceMember(playerType, "MainCharacter");
            if (ReflectionAccess.GetMemberType(playerMainCharacter) != unitReferenceType)
            {
                throw new ContractResolutionException("Player.MainCharacter is not a UnitReference contract.");
            }
            MemberInfo unitReferenceValue = ReflectionAccess.RequireInstanceMember(unitReferenceType, "Value");
            if (!unitEntityDataType.IsAssignableFrom(ReflectionAccess.GetMemberType(unitReferenceValue)))
            {
                throw new ContractResolutionException("UnitReference.Value is not a UnitEntityData contract.");
            }
            MemberInfo unitEntityDescriptor = ReflectionAccess.RequireInstanceMember(unitEntityDataType, "Descriptor");
            if (!unitDescriptorType.IsAssignableFrom(ReflectionAccess.GetMemberType(unitEntityDescriptor)))
            {
                throw new ContractResolutionException("UnitEntityData.Descriptor is not a UnitDescriptor contract.");
            }
            Type uiType = ReflectionAccess.GetMemberType(gameUi);
            MemberInfo characterBuildController = ReflectionAccess.RequireInstanceMember(uiType, "CharacterBuildController");
            Type characterBuildControllerType = ReflectionAccess.GetMemberType(characterBuildController);
            MemberInfo levelUpController = ReflectionAccess.RequireInstanceMember(characterBuildControllerType, "LevelUpController");
            Type controllerType = ReflectionAccess.GetMemberType(levelUpController);
            MemberInfo controllerState = ReflectionAccess.RequireInstanceMember(controllerType, "State");
            if (!levelUpStateType.IsAssignableFrom(ReflectionAccess.GetMemberType(controllerState)))
            {
                throw new ContractResolutionException("LevelUpController.State is not a LevelUpState contract.");
            }
            MemberInfo controllerUnit = ReflectionAccess.RequireInstanceMember(controllerType, "Unit");
            MemberInfo controllerPreview = ReflectionAccess.RequireInstanceMember(controllerType, "Preview");
            if (!unitDescriptorType.IsAssignableFrom(ReflectionAccess.GetMemberType(controllerUnit)) ||
                !unitDescriptorType.IsAssignableFrom(ReflectionAccess.GetMemberType(controllerPreview)))
            {
                throw new ContractResolutionException("LevelUpController.Unit or Preview is not a UnitDescriptor contract.");
            }
            FieldInfo recalculate = controllerType.GetField("m_RecalculatePreview", InstanceFlags);
            MethodInfo updatePreview = controllerType.GetMethod("UpdatePreview", InstanceFlags, null, Type.EmptyTypes, null);
            if (recalculate == null || recalculate.FieldType != typeof(bool) || updatePreview == null || updatePreview.ReturnType != typeof(void))
            {
                throw new ContractResolutionException("Exact LevelUpController preview refresh contract was not found.");
            }

            Type phaseKindType = RequireType(gameAssembly, "Kingmaker.UI.LevelUp.Phase.CharBPhase+Type");
            Type skillsPhaseType = RequireType(gameAssembly, "Kingmaker.UI.LevelUp.Phase.CharBPhaseSkills");
            Type abilityAllocatorType = RequireType(gameAssembly, "Kingmaker.UI.LevelUp.CharBAbilityScoresAllocator");
            MemberInfo currentPhase = ReflectionAccess.RequireInstanceMember(characterBuildControllerType, "CurrentPhase");
            Type currentPhaseType = ReflectionAccess.GetMemberType(currentPhase);
            if (Nullable.GetUnderlyingType(currentPhaseType) != phaseKindType)
            {
                throw new ContractResolutionException("CharacterBuildController.CurrentPhase is not Nullable<CharBPhase.Type>.");
            }
            object skillsPhaseValue;
            try
            {
                skillsPhaseValue = Enum.Parse(phaseKindType, "Skills", false);
            }
            catch (Exception exception)
            {
                throw new ContractResolutionException("CharBPhase.Type.Skills was not found: " + exception.Message);
            }
            MemberInfo skillsPhase = ReflectionAccess.RequireInstanceMember(characterBuildControllerType, "Skills");
            if (ReflectionAccess.GetMemberType(skillsPhase) != skillsPhaseType)
            {
                throw new ContractResolutionException("CharacterBuildController.Skills has an unexpected type.");
            }
            MemberInfo abilityAllocator = ReflectionAccess.RequireInstanceMember(skillsPhaseType, "AbilityScoresAllocator");
            if (ReflectionAccess.GetMemberType(abilityAllocator) != abilityAllocatorType)
            {
                throw new ContractResolutionException("CharBPhaseSkills.AbilityScoresAllocator has an unexpected type.");
            }
            MethodInfo fillAbilityData = abilityAllocatorType.GetMethod(
                "FillData",
                InstanceFlags,
                null,
                Type.EmptyTypes,
                null);
            if (fillAbilityData == null || fillAbilityData.ReturnType != typeof(void))
            {
                throw new ContractResolutionException("Exact CharBAbilityScoresAllocator.FillData() method was not found.");
            }
            MemberInfo unitEntity = ReflectionAccess.RequireInstanceMember(unitDescriptorType, "Unit");
            if (!unitEntityDataType.IsAssignableFrom(ReflectionAccess.GetMemberType(unitEntity)))
            {
                throw new ContractResolutionException("UnitDescriptor.Unit is not a UnitEntityData contract.");
            }
            FieldInfo allocatorSourceEntity = abilityAllocatorType.GetField("m_Unit", InstanceFlags);
            FieldInfo allocatorPreviewEntity = abilityAllocatorType.GetField("m_PreviewUnit", InstanceFlags);
            if (allocatorSourceEntity == null || allocatorSourceEntity.FieldType != unitEntityDataType ||
                allocatorPreviewEntity == null || allocatorPreviewEntity.FieldType != unitEntityDataType)
            {
                throw new ContractResolutionException(
                    "Exact CharBAbilityScoresAllocator source/preview binding fields were not found.");
            }

            evidence.Add("Assembly=" + gameAssembly.FullName);
            evidence.Add("MVID=" + gameAssembly.ManifestModule.ModuleVersionId.ToString("D"));
            evidence.Add("Abilities=" + string.Join(",", abilityKeys.Select(value => value.ToString()).ToArray()));
            evidence.Add("ControllerPath=Game.Instance.UI.CharacterBuildController.LevelUpController");
            evidence.Add("Lifecycle=" + controllerType.FullName + ".State");
            evidence.Add("MainCharacterPath=Game.Instance.Player.MainCharacter.Value.Descriptor");
            evidence.Add("ControllerIdentity=" + controllerType.FullName + ".Unit + Preview");
            evidence.Add("Preview=" + controllerType.FullName + ".m_RecalculatePreview + UpdatePreview()");
            evidence.Add("AllocatorState=" + statsDistributionType.FullName + ".Available + Points + TotalPoints (writable)");
            evidence.Add(
                "AbilityPresentation=Game.Instance.UI.CharacterBuildController.CurrentPhase(Skills) -> " +
                skillsPhaseType.FullName + ".AbilityScoresAllocator -> " +
                abilityAllocatorType.FullName + ".FillData()");
            evidence.Add(
                "AbilityPresentationBinding=" + abilityAllocatorType.FullName + ".m_Unit + m_PreviewUnit");

            return new KingmakerContracts(
                gameAssembly,
                levelUpStateType,
                unitDescriptorType,
                charBuildModeType,
                statsDistributionType,
                statTypeType,
                constructor,
                start,
                isComplete,
                stateUnit,
                stateDistribution,
                isFirstLevel,
                unitStats,
                getStat,
                baseValue,
                statValues,
                available,
                points,
                totalPoints,
                abilityKeys,
                gameInstance,
                gameUi,
                gamePlayer,
                playerMainCharacter,
                characterBuildController,
                levelUpController,
                controllerState,
                controllerUnit,
                controllerPreview,
                recalculate,
                updatePreview,
                currentPhase,
                skillsPhaseValue,
                skillsPhase,
                abilityAllocator,
                fillAbilityData,
                unitEntity,
                allocatorSourceEntity,
                allocatorPreviewEntity,
                evidence);
        }

        private static Type RequireType(Assembly assembly, string fullName)
        {
            Type type = assembly.GetType(fullName, false);
            if (type == null) throw new ContractResolutionException("Required type " + fullName + " was not found.");
            return type;
        }

        private static string Describe(MethodBase method)
        {
            return method.DeclaringType.FullName + "." + method.Name + "(" +
                string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.FullName).ToArray()) + ")";
        }
    }
}
