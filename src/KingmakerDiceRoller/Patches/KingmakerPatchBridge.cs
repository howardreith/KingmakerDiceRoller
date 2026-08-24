using System;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Logging;
using KingmakerDiceRoller.UI;

namespace KingmakerDiceRoller.Patches
{
    public static class KingmakerPatchBridge
    {
        private static CharacterCreationCoordinator coordinator;
        private static IModLogger logger;
        private static NativeRollPanelHost panel;

        public static void Configure(
            CharacterCreationCoordinator value,
            NativeRollPanelHost panelHost,
            IModLogger modLogger)
        {
            coordinator = value;
            panel = panelHost;
            logger = modLogger;
        }

        public static void Clear()
        {
            coordinator = null;
            panel = null;
            logger = null;
        }

        public static void LevelUpStateConstructed(object __instance, object __0, object __1)
        {
            try { coordinator?.OnLevelUpStateConstructed(__instance, __0, __1); }
            catch (Exception exception) { logger?.Exception("LevelUpState constructor postfix", exception); }
        }

        public static void StatsDistributionStarted(object __instance, int __0)
        {
            try { coordinator?.OnDistributionStarted(__instance, __0); }
            catch (Exception exception) { logger?.Exception("StatsDistribution.Start postfix", exception); }
        }

        public static void StatsDistributionIsComplete(object __instance, ref bool __result)
        {
            try { coordinator?.OnDistributionIsComplete(__instance, ref __result); }
            catch (Exception exception) { logger?.Exception("StatsDistribution.IsComplete postfix", exception); }
        }

        public static void AbilityAllocatorFilled(object __instance)
        {
            try { panel?.OnAbilityAllocatorFilled(__instance); }
            catch (Exception exception) { logger?.Exception("Ability allocator FillData postfix", exception); }
        }

        public static void LevelUpAppliedToAuthoritativeUnit(object __instance, object __0)
        {
            try { coordinator?.OnLevelUpAppliedToAuthoritativeUnit(__instance, __0); }
            catch (Exception exception) { logger?.Exception("LevelUpController.ApplyLevelup postfix", exception); }
        }

        public static void LevelUpCommitCompleted(object __instance)
        {
            try { coordinator?.OnLevelUpCommitCompleted(__instance); }
            catch (Exception exception) { logger?.Exception("LevelUpController.Commit postfix", exception); }
        }
    }
}
