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

            object stateUnit = ReflectionAccess.Read(contracts.LevelUpStateUnitMember, state);
            if (!ReferenceEquals(stateUnit, constructorUnit)) return CharacterCreationContextDecision.Reject("Constructor unit does not match LevelUpState.Unit.");
            object distribution = ReflectionAccess.Read(contracts.LevelUpStateDistributionMember, state);
            if (distribution == null || !contracts.StatsDistributionType.IsInstanceOfType(distribution)) return CharacterCreationContextDecision.Reject("StatsDistribution is unavailable.");

            object firstLevelValue = ReflectionAccess.Read(contracts.LevelUpStateIsFirstLevelMember, state);
            if (!(firstLevelValue is bool) || !(bool)firstLevelValue) return CharacterCreationContextDecision.Reject("LevelUpState is not first-level character creation.");

            bool isMain;
            bool isPlayer;
            bool isPet;
            bool isEnemy;
            string matched;
            if (!ReflectionAccess.TryReadBoolean(stateUnit, MainCharacterPaths, out isMain, out matched)) return CharacterCreationContextDecision.Reject("Main-character identity contract is unavailable.");
            if (!ReflectionAccess.TryReadBoolean(stateUnit, PlayerFactionPaths, out isPlayer, out matched)) return CharacterCreationContextDecision.Reject("Player-faction identity contract is unavailable.");
            if (!ReflectionAccess.TryReadBoolean(stateUnit, PetPaths, out isPet, out matched)) return CharacterCreationContextDecision.Reject("Pet identity contract is unavailable.");
            if (!ReflectionAccess.TryReadBoolean(stateUnit, EnemyPaths, out isEnemy, out matched)) return CharacterCreationContextDecision.Reject("Enemy identity contract is unavailable.");

            if (isPet) return CharacterCreationContextDecision.Reject("Pets are excluded.");
            if (isEnemy) return CharacterCreationContextDecision.Reject("Enemies are excluded.");

            string ownershipDetail;
            if (!TryIsOwnedByActiveController(state, stateUnit, contracts, out ownershipDetail))
            {
                return CharacterCreationContextDecision.Reject(
                    "Candidate is not owned by the active character-build controller. " +
                    ownershipDetail + " Flags: main=" + isMain + ", player=" + isPlayer + ".");
            }

            bool hasEstablishedMainCharacter;
            string mainCharacterDetail;
            if (!TryHasEstablishedMainCharacter(contracts, out hasEstablishedMainCharacter, out mainCharacterDetail))
            {
                return CharacterCreationContextDecision.Reject(
                    "Main-character boundary could not be resolved. " + mainCharacterDetail);
            }
            if (hasEstablishedMainCharacter)
            {
                return CharacterCreationContextDecision.Reject(
                    "An established main character already exists; only new-game character creation is supported. " +
                    mainCharacterDetail);
            }

            return CharacterCreationContextDecision.Accept(state, stateUnit, distribution);
        }

        private static bool TryIsOwnedByActiveController(
            object state,
            object stateUnit,
            KingmakerContracts contracts,
            out string detail)
        {
            detail = "Controller observation was unavailable.";
            object controller;
            if (!contracts.TryGetLevelUpController(out controller)) return false;
            if (controller == null)
            {
                detail = "LevelUpController is null.";
                return false;
            }

            bool stateOwned = false;
            object currentState;
            if (contracts.TryGetCurrentLevelUpState(out currentState))
            {
                stateOwned = ReferenceEquals(currentState, state);
            }

            bool unitOwned = TryControllerUnitMatch(controller, "Unit", stateUnit, contracts);
            bool previewOwned = TryControllerUnitMatch(controller, "Preview", stateUnit, contracts);
            detail = "stateOwned=" + stateOwned + ", unitOwned=" + unitOwned + ", previewOwned=" + previewOwned + ".";
            return stateOwned || unitOwned || previewOwned;
        }

        private static bool TryControllerUnitMatch(
            object controller,
            string memberName,
            object stateUnit,
            KingmakerContracts contracts)
        {
            try
            {
                MemberInfo member = ReflectionAccess.FindInstanceMember(controller.GetType(), memberName);
                if (member == null) return false;
                object candidate = ReflectionAccess.Read(member, controller);
                object descriptor;
                if (!TryResolveUnitDescriptor(candidate, contracts, 0, out descriptor)) return false;
                return ReferenceEquals(descriptor, stateUnit);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryHasEstablishedMainCharacter(
            KingmakerContracts contracts,
            out bool hasEstablishedMainCharacter,
            out string detail)
        {
            hasEstablishedMainCharacter = false;
            detail = "Main character is absent.";
            try
            {
                object game = ReflectionAccess.Read(contracts.GameInstanceMember, null);
                if (game == null)
                {
                    detail = "Game.Instance is null.";
                    return false;
                }

                MemberInfo playerMember = ReflectionAccess.FindInstanceMember(game.GetType(), "Player");
                if (playerMember == null)
                {
                    detail = "Game.Player member is unavailable.";
                    return false;
                }

                object player = ReflectionAccess.Read(playerMember, game);
                if (player == null)
                {
                    detail = "Game.Player is null; no established campaign character exists.";
                    return true;
                }

                MemberInfo mainCharacterMember = ReflectionAccess.FindInstanceMember(player.GetType(), "MainCharacter");
                if (mainCharacterMember == null)
                {
                    detail = "Player.MainCharacter member is unavailable.";
                    return false;
                }

                object mainCharacter = ReflectionAccess.Read(mainCharacterMember, player);
                object mainDescriptor;
                if (!TryResolveUnitDescriptor(mainCharacter, contracts, 0, out mainDescriptor))
                {
                    detail = "Player.MainCharacter could not be normalized to a UnitDescriptor.";
                    return false;
                }

                hasEstablishedMainCharacter = mainDescriptor != null;
                detail = hasEstablishedMainCharacter
                    ? "Player.MainCharacter resolves to an existing UnitDescriptor."
                    : "Player.MainCharacter has no live value.";
                return true;
            }
            catch (Exception exception)
            {
                detail = "Main-character observation failed with " + exception.GetType().Name + ".";
                return false;
            }
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
    }
}
