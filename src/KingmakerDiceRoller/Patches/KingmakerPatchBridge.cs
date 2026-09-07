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
        private static NativeRespecEntryService respec;
        public static void ConfigureRespec(NativeRespecEntryService service) { respec = service; }

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
            respec?.EndSelection();
            respec = null;
            coordinator = null;
            panel = null;
            logger = null;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void RespecSelectorConfirming(object __instance)
        {
            try { respec?.BeginSelection(__instance); }
            catch (Exception exception) { logger?.Exception("Respec selector prefix", exception); }
        }
        public static void RespecSelectorConfirmed()
        {
            try { respec?.EndSelection(); }
            catch (Exception exception) { logger?.Exception("Respec selector postfix", exception); }
        }
        public static void RespecBuildStarted(object __0, object __2, object __3)
        {
            try { respec?.BuildStarted(__0, __2, __3); }
            catch (Exception exception) { logger?.Exception("Respec launch postfix", exception); }
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void RespecCommitStarting(object __instance)
        {
            try { respec?.BeginCommit(__instance); }
            catch (Exception exception) { logger?.Exception("Respec commit prefix", exception); }
        }
        public static void RespecCopyCompleted(object __instance)
        {
            try { coordinator?.OnRespecCopyCompleted(__instance); }
            catch (Exception exception) { logger?.Exception("Respec native copy postfix", exception); }
        }
        public static void RespecBuildCanceling(object __instance)
        {
            try { respec?.Cancel(__instance); }
            catch (Exception exception) { logger?.Exception("Respec cancel prefix", exception); }
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
