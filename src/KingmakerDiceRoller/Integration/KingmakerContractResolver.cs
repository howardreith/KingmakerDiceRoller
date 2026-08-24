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
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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
            Type unitHelperType = RequireType(gameAssembly, "Kingmaker.UnitLogic.UnitHelper");
            Type statsDistributionType = RequireType(gameAssembly, "Kingmaker.UnitLogic.Class.LevelUp.StatsDistribution");
            Type statTypeType = RequireType(gameAssembly, "Kingmaker.EntitySystem.Stats.StatType");
            Type statTypeHelperType = RequireType(gameAssembly, "Kingmaker.EntitySystem.Stats.StatTypeHelper");
            Type charBuildModeType = levelUpStateType.GetNestedType("CharBuildMode", BindingFlags.Public | BindingFlags.NonPublic);
            if (charBuildModeType == null || !charBuildModeType.IsEnum)
            {
                throw new ContractResolutionException("LevelUpState.CharBuildMode enum was not found.");
            }
            object charGenMode;
            try
            {
                charGenMode = Enum.Parse(charBuildModeType, "CharGen", false);
            }
            catch (Exception exception)
            {
                throw new ContractResolutionException("LevelUpState.CharBuildMode.CharGen was not found: " + exception.Message);
            }
            if (Convert.ToInt32(charGenMode) != 1)
            {
                throw new ContractResolutionException("LevelUpState.CharBuildMode.CharGen is not exact value 1.");
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
            MemberInfo isEmployee = ReflectionAccess.RequireInstanceMember(levelUpStateType, "IsEmployee");
            MemberInfo stateMode = ReflectionAccess.RequireInstanceMember(levelUpStateType, "Mode");
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
            if (ReflectionAccess.GetMemberType(isEmployee) != typeof(bool))
            {
                throw new ContractResolutionException("LevelUpState.IsEmployee is not Boolean.");
            }
            if (ReflectionAccess.GetMemberType(stateMode) != charBuildModeType)
            {
                throw new ContractResolutionException("LevelUpState.Mode is not the exact CharBuildMode enum.");
            }
            MethodInfo isCustomCompanion = unitHelperType.GetMethod(
                "IsCustomCompanion",
                StaticFlags,
                null,
                new[] { unitDescriptorType },
                null);
            if (isCustomCompanion == null || isCustomCompanion.ReturnType != typeof(bool))
            {
                throw new ContractResolutionException(
                    "Exact bool UnitHelper.IsCustomCompanion(UnitDescriptor) method was not found.");
            }
            var employeeProperty = isEmployee as PropertyInfo;
            MethodInfo employeeGetter = employeeProperty == null
                ? null
                : employeeProperty.GetGetMethod(true);
            if (employeeGetter == null || employeeGetter.IsStatic ||
                !MethodBodyContainsToken(employeeGetter, isCustomCompanion.MetadataToken))
            {
                throw new ContractResolutionException(
                    "LevelUpState.IsEmployee does not call the exact UnitHelper.IsCustomCompanion(UnitDescriptor) discriminator.");
            }
            evidence.Add(Describe(isCustomCompanion));

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

            Type levelUpActionType = RequireType(
                gameAssembly,
                "Kingmaker.UnitLogic.Class.LevelUp.Actions.ILevelUpAction");
            MethodInfo applyLevelup = controllerType.GetMethod(
                "ApplyLevelup",
                InstanceFlags,
                null,
                new[] { unitDescriptorType },
                null);
            MethodInfo commit = controllerType.GetMethod(
                "Commit",
                InstanceFlags,
                null,
                Type.EmptyTypes,
                null);
            MethodInfo setupNewCharacter = controllerType.GetMethod(
                "SetupNewCharacher",
                InstanceFlags,
                null,
                Type.EmptyTypes,
                null);
            MethodInfo applyAction = levelUpActionType.GetMethod(
                "Apply",
                InstanceFlags,
                null,
                new[] { levelUpStateType, unitDescriptorType },
                null);
            FieldInfo onSuccess = controllerType.GetField("m_OnSuccess", InstanceFlags);
            if (applyLevelup == null || applyLevelup.IsStatic || applyLevelup.IsPublic ||
                !applyLevelup.ReturnType.IsGenericType ||
                applyLevelup.ReturnType.GetGenericTypeDefinition() != typeof(List<>) ||
                applyLevelup.ReturnType.GetGenericArguments()[0] != levelUpActionType ||
                commit == null || commit.IsStatic || !commit.IsPublic ||
                commit.ReturnType != typeof(void) ||
                setupNewCharacter == null || setupNewCharacter.IsStatic ||
                setupNewCharacter.IsPublic || setupNewCharacter.ReturnType != typeof(void) ||
                applyAction == null || applyAction.IsStatic ||
                applyAction.ReturnType != typeof(void) ||
                onSuccess == null || onSuccess.IsStatic || onSuccess.FieldType != typeof(Action))
            {
                throw new ContractResolutionException(
                    "Exact LevelUpController authoritative finalization methods were not found.");
            }
            int commitUnitOffset = FindTokenOffset(commit, controllerUnit.MetadataToken);
            int commitApplyOffset = FindTokenOffset(commit, applyLevelup.MetadataToken);
            int commitSetupOffset = FindTokenOffset(commit, setupNewCharacter.MetadataToken);
            int commitSuccessOffset = FindTokenOffset(commit, onSuccess.MetadataToken);
            if (commitUnitOffset < 0 || commitApplyOffset <= commitUnitOffset ||
                commitSetupOffset <= commitApplyOffset || commitSuccessOffset <= commitSetupOffset ||
                FindTokenOffset(applyLevelup, constructor.MetadataToken) < 0 ||
                FindTokenOffset(applyLevelup, applyAction.MetadataToken) < 0 ||
                FindTokenOffset(updatePreview, applyLevelup.MetadataToken) < 0)
            {
                throw new ContractResolutionException(
                    "LevelUpController.Commit no longer applies LevelUpActions to Unit before first-level setup and the success callback.");
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
            FieldInfo allocatorStatEntries = abilityAllocatorType.GetField("m_StatEntries", InstanceFlags);
            if (allocatorStatEntries == null || !allocatorStatEntries.FieldType.IsGenericType ||
                allocatorStatEntries.FieldType.GetGenericTypeDefinition() != typeof(List<>))
            {
                throw new ContractResolutionException("Exact CharBAbilityScoresAllocator.m_StatEntries list was not found.");
            }
            Type scoreEntryType = allocatorStatEntries.FieldType.GetGenericArguments()[0];
            if (!string.Equals(scoreEntryType.FullName, "Kingmaker.UI.LevelUp.CharBScoresEntry", StringComparison.Ordinal))
            {
                throw new ContractResolutionException("Ability allocator entries are not exact CharBScoresEntry instances.");
            }
            FieldInfo scoreEntryUpButton = scoreEntryType.GetField("UpButton", InstanceFlags);
            FieldInfo scoreEntryDownButton = scoreEntryType.GetField("DownButton", InstanceFlags);
            if (scoreEntryUpButton == null || scoreEntryDownButton == null ||
                scoreEntryUpButton.FieldType != scoreEntryDownButton.FieldType ||
                !string.Equals(scoreEntryUpButton.FieldType.FullName, "UnityEngine.UI.Button", StringComparison.Ordinal))
            {
                throw new ContractResolutionException("Exact CharBScoresEntry UpButton/DownButton fields were not found.");
            }
            PropertyInfo interactable = scoreEntryUpButton.FieldType.GetProperty("interactable", InstanceFlags);
            if (interactable == null || interactable.PropertyType != typeof(bool) ||
                interactable.GetGetMethod(true) == null || interactable.GetSetMethod(true) == null)
            {
                throw new ContractResolutionException("Unity Selectable.interactable is not an exact writable Boolean property.");
            }
            FieldInfo allocatorMainLabel = abilityAllocatorType.GetField("m_MainLabel", InstanceFlags);
            FieldInfo allocatorFrame = abilityAllocatorType.GetField("m_Frame", InstanceFlags);
            FieldInfo allocatorRaceBonusContainer = abilityAllocatorType.GetField("m_RaceBonusContainer", InstanceFlags);
            if (allocatorMainLabel == null ||
                !string.Equals(allocatorMainLabel.FieldType.FullName, "TMPro.TextMeshProUGUI", StringComparison.Ordinal) ||
                allocatorFrame == null ||
                !string.Equals(allocatorFrame.FieldType.FullName, "UnityEngine.UI.Image", StringComparison.Ordinal) ||
                allocatorRaceBonusContainer == null ||
                !string.Equals(allocatorRaceBonusContainer.FieldType.FullName, "UnityEngine.GameObject", StringComparison.Ordinal))
            {
                throw new ContractResolutionException("Native allocator label/frame/racial-bonus UI anchors were not found.");
            }

            evidence.Add("Assembly=" + gameAssembly.FullName);
            evidence.Add("MVID=" + gameAssembly.ManifestModule.ModuleVersionId.ToString("D"));
            evidence.Add("Abilities=" + string.Join(",", abilityKeys.Select(value => value.ToString()).ToArray()));
            evidence.Add("ControllerPath=Game.Instance.UI.CharacterBuildController.LevelUpController");
            evidence.Add("Lifecycle=" + controllerType.FullName + ".State");
            evidence.Add("MainCharacterPath=Game.Instance.Player.MainCharacter.Value.Descriptor");
            evidence.Add(
                "MercenaryDiscriminator=" + levelUpStateType.FullName +
                ".IsEmployee + " + unitHelperType.FullName + ".IsCustomCompanion(UnitDescriptor)");
            evidence.Add("ControllerIdentity=" + controllerType.FullName + ".Unit + Preview");
            evidence.Add("Preview=" + controllerType.FullName + ".m_RecalculatePreview + UpdatePreview()");
            evidence.Add(
                "MercenaryFinalization=" + controllerType.FullName +
                ".Commit -> ApplyLevelup(Unit) -> SetupNewCharacher -> m_OnSuccess");
            evidence.Add(
                "AuthoritativeAssignmentSeam=postfix " + controllerType.FullName +
                ".ApplyLevelup(Unit), before first-level setup/success callback; final verification=postfix Commit()");
            evidence.Add("AllocatorState=" + statsDistributionType.FullName + ".Available + Points + TotalPoints (writable)");
            evidence.Add(
                "AbilityPresentation=Game.Instance.UI.CharacterBuildController.CurrentPhase(Skills) -> " +
                skillsPhaseType.FullName + ".AbilityScoresAllocator -> " +
                abilityAllocatorType.FullName + ".FillData()");
            evidence.Add(
                "AbilityPresentationBinding=" + abilityAllocatorType.FullName + ".m_Unit + m_PreviewUnit");
            evidence.Add(
                "AbilityControls=" + abilityAllocatorType.FullName + ".m_StatEntries -> " +
                scoreEntryType.FullName + ".UpButton + DownButton -> UnityEngine.UI.Selectable.interactable");
            evidence.Add(
                "AbilityPanelStyles=" + abilityAllocatorType.FullName + ".m_MainLabel + m_Frame");
            evidence.Add(
                "AbilityPanelAccessAnchor=" + abilityAllocatorType.FullName + ".m_RaceBonusContainer");

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
                isEmployee,
                stateMode,
                isCustomCompanion,
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
                applyLevelup,
                commit,
                currentPhase,
                skillsPhaseValue,
                skillsPhase,
                abilityAllocator,
                fillAbilityData,
                unitEntity,
                allocatorSourceEntity,
                allocatorPreviewEntity,
                allocatorStatEntries,
                scoreEntryUpButton,
                scoreEntryDownButton,
                interactable,
                allocatorMainLabel,
                allocatorFrame,
                allocatorRaceBonusContainer,
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

        private static bool MethodBodyContainsToken(MethodInfo method, int metadataToken)
        {
            return FindTokenOffset(method, metadataToken) >= 0;
        }

        private static int FindTokenOffset(MethodBase method, int metadataToken)
        {
            MethodBody body = method.GetMethodBody();
            if (body == null) return -1;
            byte[] bytes = body.GetILAsByteArray();
            byte[] token = BitConverter.GetBytes(metadataToken);
            for (int offset = 0; offset <= bytes.Length - token.Length; offset++)
            {
                bool matches = true;
                for (int index = 0; index < token.Length; index++)
                {
                    if (bytes[offset + index] == token[index]) continue;
                    matches = false;
                    break;
                }
                if (matches) return offset;
            }
            return -1;
        }
    }
}
