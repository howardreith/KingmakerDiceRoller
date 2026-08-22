using System;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Compatibility;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;
using KingmakerDiceRoller.Patches;
using KingmakerDiceRoller.UI;

namespace KingmakerDiceRoller
{
    public sealed class CompositionRoot
    {
        private readonly Settings settings;
        private readonly IModLogger logger;
        private readonly CompatibilityDetector compatibilityDetector;
        private readonly KingmakerContractHolder contracts;
        private readonly RuntimeDiagnostics diagnostics;
        private readonly CharacterCreationCoordinator coordinator;
        private readonly KingmakerPatchController patches;
        private readonly SettingsView view;
        private bool enabled;

        public CompositionRoot(Settings settings, IModLogger logger)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            compatibilityDetector = new CompatibilityDetector();
            contracts = new KingmakerContractHolder();
            diagnostics = new RuntimeDiagnostics();
            var budgetTracker = new PointBudgetTracker();
            var statAccess = new KingmakerStatAccess();
            var preview = new PreviewRefreshService();
            var livePreview = new LivePreviewInspector(statAccess);
            var sessions = new RollSessionManager();
            var application = new StatApplicationService(statAccess, livePreview, preview, logger);
            var restore = new PointBuyRestoreService(statAccess, livePreview, preview, logger);
            coordinator = new CharacterCreationCoordinator(
                new CharacterCreationContextPolicy(),
                budgetTracker,
                new PointBudgetResolver(budgetTracker),
                statAccess,
                sessions,
                application,
                restore,
                diagnostics,
                logger,
                () => contracts.Current,
                () => settings.VerboseDiagnostics);
            patches = new KingmakerPatchController(logger);
            view = new SettingsView(settings, coordinator, diagnostics, contracts);
        }

        public bool SetEnabled(bool value)
        {
            if (value == enabled) return true;
            if (value) return Enable();
            return Disable();
        }

        public void Update(float deltaTime)
        {
            if (enabled) coordinator.Update(deltaTime);
        }

        public void DrawGui()
        {
            view.Draw();
        }

        public void Save(UnityModManagerNet.UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        public bool TryUnload()
        {
            return Disable();
        }

        private bool Enable()
        {
            try
            {
                CompatibilitySnapshot compatibility = compatibilityDetector.Detect();
                view.SetCompatibility(compatibility);
                for (int index = 0; index < compatibility.Warnings.Count; index++) logger.Warning(compatibility.Warnings[index]);
                KingmakerContracts resolved = new KingmakerContractResolver().Resolve();
                contracts.Set(resolved);
                for (int index = 0; index < resolved.Evidence.Count; index++) logger.Info("Contract: " + resolved.Evidence[index]);
                patches.Install(resolved, coordinator);
                enabled = true;
                diagnostics.SetStatus("Enabled; waiting for an exact new-main-character LevelUpState.");
                logger.Info("Kingmaker Dice Roller 0.0.1-alpha.1 enabled in fixed-array mode.");
                return true;
            }
            catch (Exception exception)
            {
                patches.Uninstall();
                contracts.Clear();
                diagnostics.SetStatus("Enable failed closed: " + exception.Message);
                logger.Exception("Enable Kingmaker Dice Roller", exception);
                enabled = false;
                return false;
            }
        }

        private bool Disable()
        {
            if (!enabled)
            {
                patches.Uninstall();
                contracts.Clear();
                return true;
            }

            string error;
            if (!coordinator.TryRestorePointBuy(out error))
            {
                diagnostics.SetStatus("Disable refused because point-buy restoration failed: " + error);
                logger.Error("Kingmaker Dice Roller remains enabled to preserve recovery hooks: " + error);
                return false;
            }

            patches.Uninstall();
            contracts.Clear();
            enabled = false;
            diagnostics.SetStatus("Disabled; no owned roll session remains.");
            logger.Info("Kingmaker Dice Roller disabled.");
            return true;
        }
    }
}
