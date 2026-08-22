using System;
using System.Reflection;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class CharacterCreationContextPolicy
    {
        private static readonly string[] MainCharacterPaths = { "IsMainCharacter", "Unit.IsMainCharacter" };
        private static readonly string[] PlayerFactionPaths = { "IsPlayerFaction", "Unit.IsPlayerFaction" };
        private static readonly string[] PetPaths = { "IsPet", "Unit.IsPet" };
        private static readonly string[] EnemyPaths = { "IsPlayersEnemy", "Unit.IsPlayersEnemy" };

        public CharacterCreationContextDecision Evaluate(object state, object constructorUnit, object mode, KingmakerContracts contracts)
        {
            if (state == null || constructorUnit == null || mode == null || contracts == null)
            {
                return CharacterCreationContextDecision.Reject("Missing constructor context.");
            }
            if (!contracts.LevelUpStateType.IsInstanceOfType(state)) return CharacterCreationContextDecision.Reject("Unexpected LevelUpState runtime type.");
            if (!contracts.UnitDescriptorType.IsInstanceOfType(constructorUnit)) return CharacterCreationContextDecision.Reject("Unexpected UnitDescriptor runtime type.");
            if (!contracts.CharBuildModeType.IsInstanceOfType(mode)) return CharacterCreationContextDecision.Reject("Unexpected CharBuildMode runtime type.");

            string modeName = mode.ToString();
            if (string.Equals(modeName, "PreGen", StringComparison.Ordinal))
            {
                return CharacterCreationContextDecision.Reject("Pre-generated character creation is excluded.");
            }
            if (string.Equals(modeName, "Respec", StringComparison.Ordinal))
            {
                return CharacterCreationContextDecision.Reject("Respecialization is excluded.");
            }
            if (!string.Equals(modeName, "CharGen", StringComparison.Ordinal) &&
                !string.Equals(modeName, "LevelUp", StringComparison.Ordinal))
            {
                return CharacterCreationContextDecision.Reject("Unsupported character-build mode " + DescribeMode(mode) + ".");
            }

            object stateUnit;
            object distribution;
            bool isFirstLevel;
            try
            {
                stateUnit = ReflectionAccess.Read(contracts.LevelUpStateUnitMember, state);
                if (!ReferenceEquals(stateUnit, constructorUnit)) return CharacterCreationContextDecision.Reject("Constructor unit does not match LevelUpState.Unit.");
                distribution = ReflectionAccess.Read(contracts.LevelUpStateDistributionMember, state);
                if (distribution == null || !contracts.StatsDistributionType.IsInstanceOfType(distribution)) return CharacterCreationContextDecision.Reject("StatsDistribution is unavailable.");

                object firstLevelValue = ReflectionAccess.Read(contracts.LevelUpStateIsFirstLevelMember, state);
                isFirstLevel = firstLevelValue is bool && (bool)firstLevelValue;
            }
            catch (Exception exception)
            {
                return CharacterCreationContextDecision.Reject(
                    "LevelUpState context observation failed with " + exception.GetType().Name + ".");
            }

            if (!isFirstLevel)
            {
                return CharacterCreationContextDecision.Reject(
                    "LevelUpState is not first-level character creation. Facts: mode=" + modeName + ", isFirstLevel=false.");
            }

            bool isMain;
            bool isPlayer;
            bool isPet;
            bool isEnemy;
            string matched;
            try
            {
                if (!ReflectionAccess.TryReadBoolean(stateUnit, MainCharacterPaths, out isMain, out matched)) return CharacterCreationContextDecision.Reject("Main-character identity contract is unavailable.");
                if (!ReflectionAccess.TryReadBoolean(stateUnit, PlayerFactionPaths, out isPlayer, out matched)) return CharacterCreationContextDecision.Reject("Player-faction identity contract is unavailable.");
                if (!ReflectionAccess.TryReadBoolean(stateUnit, PetPaths, out isPet, out matched)) return CharacterCreationContextDecision.Reject("Pet identity contract is unavailable.");
                if (!ReflectionAccess.TryReadBoolean(stateUnit, EnemyPaths, out isEnemy, out matched)) return CharacterCreationContextDecision.Reject("Enemy identity contract is unavailable.");
            }
            catch (Exception exception)
            {
                return CharacterCreationContextDecision.Reject(
                    "Candidate identity observation failed with " + exception.GetType().Name + ".");
            }

            string candidateFacts = BuildCandidateFacts(modeName, isFirstLevel, isMain, isPlayer);
            if (isPet) return CharacterCreationContextDecision.Reject("Pets are excluded. " + candidateFacts);
            if (isEnemy) return CharacterCreationContextDecision.Reject("Enemies are excluded. " + candidateFacts);

            ControllerOwnershipObservation ownership = ObserveActiveController(state, stateUnit, contracts);
            if (!ownership.IsOwned)
            {
                return CharacterCreationContextDecision.Reject(
                    "Candidate is not owned by the active character-build controller. " +
                    ownership.Detail + " " +
                    BuildFacts(modeName, isFirstLevel, isMain, isPlayer, ownership, false, false, null, null));
            }

            object mainDescriptor;
            string mainCharacterDetail;
            bool mainCharacterResolved = TryGetMainCharacterDescriptor(
                contracts,
                out mainDescriptor,
                out mainCharacterDetail);
            MainCharacterIdentityRelation relation = MainCharacterIdentityClassifier.Classify(
                mainCharacterResolved,
                mainDescriptor,
                stateUnit,
                ownership.UnitResolved,
                ownership.UnitDescriptor);
            string facts = BuildFacts(
                modeName,
                isFirstLevel,
                isMain,
                isPlayer,
                ownership,
                true,
                mainCharacterResolved,
                mainDescriptor,
                relation);

            if (relation == MainCharacterIdentityRelation.Unresolved)
            {
                return CharacterCreationContextDecision.Reject(
                    "Main-character identity could not be resolved; the candidate fails closed. " +
                    mainCharacterDetail + " " + facts,
                    relation);
            }

            if (relation == MainCharacterIdentityRelation.DifferentFromCandidate)
            {
                return CharacterCreationContextDecision.Reject(
                    "Rejected controller-owned first-level candidate because Player.MainCharacter resolves to a different UnitDescriptor. " +
                    mainCharacterDetail + " " + facts,
                    relation);
            }

            string acceptance;
            if (relation == MainCharacterIdentityRelation.Absent)
            {
                acceptance = "Accepted controller-owned first-level new-game preview; Player.MainCharacter has no live descriptor.";
            }
            else if (relation == MainCharacterIdentityRelation.SameAsCandidate)
            {
                acceptance = "Accepted controller-owned first-level new-game preview; Player.MainCharacter matches the candidate descriptor.";
            }
            else
            {
                acceptance = "Accepted controller-owned first-level new-game preview; Player.MainCharacter matches the controller source for the owned preview descriptor.";
            }

            return CharacterCreationContextDecision.Accept(
                state,
                stateUnit,
                distribution,
                acceptance + " " + facts,
                relation);
        }

        private static ControllerOwnershipObservation ObserveActiveController(
            object state,
            object stateUnit,
            KingmakerContracts contracts)
        {
            var observation = new ControllerOwnershipObservation { CandidateDescriptor = stateUnit };
            object controller;
            if (!contracts.TryGetLevelUpController(out controller))
            {
                observation.Detail = "Controller observation failed.";
                return observation;
            }
            if (controller == null)
            {
                observation.Detail = "LevelUpController is null.";
                return observation;
            }

            object currentState;
            observation.StateResolved = contracts.TryGetCurrentLevelUpState(out currentState);
            if (observation.StateResolved)
            {
                observation.StateMatches = ReferenceEquals(currentState, state);
            }

            observation.UnitResolved = TryControllerUnitDescriptor(
                controller,
                contracts.LevelUpControllerUnitMember,
                contracts,
                out observation.UnitDescriptor);
            observation.UnitMatches = observation.UnitResolved && ReferenceEquals(observation.UnitDescriptor, stateUnit);
            observation.PreviewResolved = TryControllerUnitDescriptor(
                controller,
                contracts.LevelUpControllerPreviewMember,
                contracts,
                out observation.PreviewDescriptor);
            observation.PreviewMatches = observation.PreviewResolved && ReferenceEquals(observation.PreviewDescriptor, stateUnit);
            observation.Detail =
                "stateOwned=" + observation.StateMatches +
                ", unitOwned=" + observation.UnitMatches +
                ", previewOwned=" + observation.PreviewMatches + ".";
            return observation;
        }

        private static bool TryControllerUnitDescriptor(
            object controller,
            MemberInfo member,
            KingmakerContracts contracts,
            out object descriptor)
        {
            descriptor = null;
            try
            {
                if (member == null) return false;
                object candidate = ReflectionAccess.Read(member, controller);
                return TryResolveUnitDescriptor(candidate, contracts, 0, out descriptor);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetMainCharacterDescriptor(
            KingmakerContracts contracts,
            out object mainDescriptor,
            out string detail)
        {
            mainDescriptor = null;
            detail = "Main-character observation was unavailable.";
            try
            {
                object game = ReflectionAccess.Read(contracts.GameInstanceMember, null);
                if (game == null)
                {
                    detail = "Game.Instance is null.";
                    return false;
                }

                object player = ReflectionAccess.Read(contracts.GamePlayerMember, game);
                if (player == null)
                {
                    detail = "Game.Player is null.";
                    return false;
                }

                object mainCharacter = ReflectionAccess.Read(contracts.PlayerMainCharacterMember, player);
                if (!TryResolveUnitDescriptor(mainCharacter, contracts, 0, out mainDescriptor))
                {
                    detail = "Player.MainCharacter could not be normalized to a UnitDescriptor.";
                    return false;
                }

                detail = mainDescriptor != null
                    ? "Player.MainCharacter resolves to a live UnitDescriptor."
                    : "Player.MainCharacter has no live value.";
                return true;
            }
            catch (Exception exception)
            {
                detail = "Main-character observation failed with " + exception.GetType().Name + ".";
                return false;
            }
        }

        private static string BuildCandidateFacts(
            string modeName,
            bool isFirstLevel,
            bool isMain,
            bool isPlayer)
        {
            return "Facts: mode=" + modeName +
                ", isFirstLevel=" + BooleanText(isFirstLevel) +
                ", candidateMainFlag=" + BooleanText(isMain) +
                ", candidatePlayerFlag=" + BooleanText(isPlayer) + ".";
        }

        private static string BuildFacts(
            string modeName,
            bool isFirstLevel,
            bool isMain,
            bool isPlayer,
            ControllerOwnershipObservation ownership,
            bool mainObservationAttempted,
            bool mainCharacterResolved,
            object mainDescriptor,
            MainCharacterIdentityRelation? relation)
        {
            string present = !mainObservationAttempted
                ? "notEvaluated"
                : mainCharacterResolved
                    ? BooleanText(mainDescriptor != null)
                    : "unresolved";
            bool mainMatchesCandidate = mainCharacterResolved &&
                mainDescriptor != null &&
                ReferenceEquals(mainDescriptor, ownership.CandidateDescriptor);
            bool mainMatchesControllerUnit = mainCharacterResolved &&
                mainDescriptor != null &&
                ownership.UnitResolved &&
                ReferenceEquals(mainDescriptor, ownership.UnitDescriptor);
            return "Facts: mode=" + modeName +
                ", isFirstLevel=" + BooleanText(isFirstLevel) +
                ", candidateMainFlag=" + BooleanText(isMain) +
                ", candidatePlayerFlag=" + BooleanText(isPlayer) +
                ", controllerStateMatches=" + BooleanText(ownership.StateMatches) +
                ", controllerUnitMatches=" + BooleanText(ownership.UnitMatches) +
                ", controllerPreviewMatches=" + BooleanText(ownership.PreviewMatches) +
                ", mainCharacterPresent=" + present +
                ", mainMatchesCandidate=" + BooleanText(mainMatchesCandidate) +
                ", mainMatchesControllerUnit=" + BooleanText(mainMatchesControllerUnit) +
                ", mainRelation=" + (relation.HasValue ? relation.Value.ToString() : "notEvaluated") + ".";
        }

        private static string BooleanText(bool value)
        {
            return value ? "true" : "false";
        }

        private static bool TryResolveUnitDescriptor(
            object candidate,
            KingmakerContracts contracts,
            int depth,
            out object descriptor)
        {
            descriptor = null;
            if (candidate == null) return true;
            if (contracts.UnitDescriptorType.IsInstanceOfType(candidate))
            {
                descriptor = candidate;
                return true;
            }
            if (depth >= 4) return false;

            string[] memberNames = { "Value", "Descriptor", "Unit" };
            for (int index = 0; index < memberNames.Length; index++)
            {
                MemberInfo member = ReflectionAccess.FindInstanceMember(candidate.GetType(), memberNames[index]);
                if (member == null) continue;

                object nested;
                try
                {
                    nested = ReflectionAccess.Read(member, candidate);
                }
                catch
                {
                    continue;
                }

                if (ReferenceEquals(nested, candidate)) continue;
                if (TryResolveUnitDescriptor(nested, contracts, depth + 1, out descriptor)) return true;
            }

            return false;
        }

        private static string DescribeMode(object mode)
        {
            try
            {
                return "'" + mode + "' (" + Convert.ToInt32(mode) + ")";
            }
            catch
            {
                return "'" + mode + "'";
            }
        }

        private sealed class ControllerOwnershipObservation
        {
            internal bool StateResolved;
            internal bool StateMatches;
            internal bool UnitResolved;
            internal object UnitDescriptor;
            internal bool UnitMatches;
            internal bool PreviewResolved;
            internal object PreviewDescriptor;
            internal bool PreviewMatches;
            internal object CandidateDescriptor;
            internal string Detail = "Controller observation was unavailable.";

            internal bool IsOwned => StateMatches || UnitMatches || PreviewMatches;
        }
    }
}
