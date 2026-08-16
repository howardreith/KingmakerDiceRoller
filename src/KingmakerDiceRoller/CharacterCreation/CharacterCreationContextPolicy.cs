using System;
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

            if (!isMain) return CharacterCreationContextDecision.Reject("Unit is not the main character.");
            if (!isPlayer) return CharacterCreationContextDecision.Reject("Unit is not player-faction.");
            if (isPet) return CharacterCreationContextDecision.Reject("Pets are excluded.");
            if (isEnemy) return CharacterCreationContextDecision.Reject("Enemies are excluded.");
            return CharacterCreationContextDecision.Accept(state, stateUnit, distribution);
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
