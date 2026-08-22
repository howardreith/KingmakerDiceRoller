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
                harmony = candidate;
            }
            catch
            {
                try { candidate.UnpatchAll(HarmonyId); }
                finally { KingmakerPatchBridge.Clear(); }
                throw;
            }
        }

        public void Uninstall()
        {
            HarmonyInstance installed = harmony;
            harmony = null;
            try
            {
                installed?.UnpatchAll(HarmonyId);
            }
            finally
            {
                KingmakerPatchBridge.Clear();
            }
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
