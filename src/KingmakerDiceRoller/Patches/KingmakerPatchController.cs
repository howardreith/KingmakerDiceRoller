using System;
using System.Reflection;
using Harmony12;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;
using KingmakerDiceRoller.UI;

namespace KingmakerDiceRoller.Patches
{
    public sealed class KingmakerPatchController
    {
        private const string HarmonyId = "howardreith.kingmakerdiceroller";
        private readonly IModLogger logger;
        private HarmonyInstance harmony;
        private HarmonyInstance respecHarmony;

        public KingmakerPatchController(IModLogger logger)
        {
            this.logger = logger;
        }

        public bool IsInstalled => harmony != null;

        public void Install(
            KingmakerContracts contracts,
            CharacterCreationCoordinator coordinator,
            NativeRollPanelHost panel)
        {
            if (IsInstalled) return;
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));
            if (panel == null) throw new ArgumentNullException(nameof(panel));

            HarmonyInstance candidate = HarmonyInstance.Create(HarmonyId);
            KingmakerPatchBridge.Configure(coordinator, panel, logger);
            try
            {
                PatchPostfix(candidate, contracts.LevelUpStateConstructor, nameof(KingmakerPatchBridge.LevelUpStateConstructed));
                PatchPostfix(candidate, contracts.DistributionStartMethod, nameof(KingmakerPatchBridge.StatsDistributionStarted));
                PatchPostfix(candidate, contracts.DistributionIsCompleteMethod, nameof(KingmakerPatchBridge.StatsDistributionIsComplete));
                PatchPostfix(candidate, contracts.AbilityAllocatorFillDataMethod, nameof(KingmakerPatchBridge.AbilityAllocatorFilled));
                PatchPostfix(candidate, contracts.LevelUpApplyLevelupMethod, nameof(KingmakerPatchBridge.LevelUpAppliedToAuthoritativeUnit));
                PatchPostfix(candidate, contracts.LevelUpCommitMethod, nameof(KingmakerPatchBridge.LevelUpCommitCompleted));
                PatchPrefix(candidate, contracts.CharacterBuildSetPhaseMethod, nameof(KingmakerPatchBridge.CharacterBuildSetPhaseStarting));
                harmony = candidate;
                InstallOptionalRespec(contracts, coordinator);
            }
            catch
            {
                try { candidate.UnpatchAll(HarmonyId); }
                finally { KingmakerPatchBridge.Clear(); }
                throw;
            }
        }

        private void InstallOptionalRespec(KingmakerContracts contracts, CharacterCreationCoordinator coordinator)
        {
            HarmonyInstance optional = HarmonyInstance.Create(HarmonyId + ".respec");
            try
            {
                NativeRespecContracts resolved = NativeRespecContracts.Resolve(contracts);
                KingmakerPatchBridge.ConfigureRespec(new NativeRespecEntryService(resolved, coordinator));
                PatchOptional(optional, resolved.SelectorConfirm, nameof(KingmakerPatchBridge.RespecSelectorConfirming), nameof(KingmakerPatchBridge.RespecSelectorConfirmed));
                PatchOptional(optional, resolved.BuildStarted, null, nameof(KingmakerPatchBridge.RespecBuildStarted));
                PatchOptional(optional, contracts.LevelUpCommitMethod, nameof(KingmakerPatchBridge.RespecCommitStarting), null);
                PatchOptional(optional, resolved.Cancel, nameof(KingmakerPatchBridge.RespecBuildCanceling), null);
                PatchOptional(optional, resolved.copyCallback, null, nameof(KingmakerPatchBridge.RespecCopyCompleted));
                respecHarmony = optional;
                logger.Info("Native/Eddic respec selector and original-recipient contracts resolved; live qualification remains separate.");
            }
            catch (Exception exception)
            {
                optional.UnpatchAll(HarmonyId + ".respec");
                KingmakerPatchBridge.ConfigureRespec(null);
                logger.Exception("Optional respec adapter disabled; creation remains available", exception);
            }
        }

        private static void PatchOptional(HarmonyInstance instance, MethodBase original, string prefix, string postfix)
        {
            // No provider ordering constraint: observe completed native launch/replay and preserve other patches.
            instance.Patch(original, Bridge(prefix), Bridge(postfix));
        }
        private static HarmonyMethod Bridge(string name)
        {
            return name == null ? null : new HarmonyMethod(typeof(KingmakerPatchBridge).GetMethod(name));
        }

        public void Uninstall()
        {
            HarmonyInstance installed = harmony;
            harmony = null;
            try
            {
                respecHarmony?.UnpatchAll(HarmonyId + ".respec");
                respecHarmony = null;
                installed?.UnpatchAll(HarmonyId);
            }
            finally
            {
                KingmakerPatchBridge.Clear();
            }
        }

        private static void PatchPrefix(HarmonyInstance instance, MethodBase original, string bridgeMethodName)
        {
            MethodInfo bridge = typeof(KingmakerPatchBridge).GetMethod(bridgeMethodName, BindingFlags.Public | BindingFlags.Static);
            if (bridge == null) throw new MissingMethodException(typeof(KingmakerPatchBridge).FullName, bridgeMethodName);
            var prefix = new HarmonyMethod(bridge) { prioritiy = Priority.VeryLow };
            instance.Patch(original, prefix, null);
        }

        private static void PatchPostfix(HarmonyInstance instance, MethodBase original, string bridgeMethodName)
        {
            MethodInfo bridge = typeof(KingmakerPatchBridge).GetMethod(bridgeMethodName, BindingFlags.Public | BindingFlags.Static);
            if (bridge == null) throw new MissingMethodException(typeof(KingmakerPatchBridge).FullName, bridgeMethodName);
            var postfix = new HarmonyMethod(bridge) { prioritiy = Priority.VeryLow };
            instance.Patch(original, null, postfix);
        }
    }
}
