using System;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Compatibility;
using KingmakerDiceRoller.Domain;
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
        private readonly NativeRollPanelHost nativePanel;
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
            var nativeControls = new NativeAbilityControlService(logger);
            var application = new StatApplicationService(statAccess, livePreview, preview, logger);
            var restore = new PointBuyRestoreService(statAccess, livePreview, preview, logger);
            var presentation = new AbilityPhasePresentationService(livePreview, logger, nativeControls);
            var workflow = new CharacterRollWorkflow(
                new DiceRollEngine(new DiceExpressionParser(), new SystemRandomSource()),
                new PointBuyEquivalentCalculator(),
                settings.CreateRollConfiguration(),
                settings.SavedArrays,
                () => DateTime.UtcNow.ToString("o"),
                settings.ApplyProductState);
            for (int index = 0; index < workflow.Saved.Warnings.Count; index++)
            {
                logger.Warning(workflow.Saved.Warnings[index]);
            }
            coordinator = new CharacterCreationCoordinator(
                new CharacterCreationContextPolicy(),
                budgetTracker,
                new PointBudgetResolver(budgetTracker),
                statAccess,
                sessions,
                application,
                restore,
                presentation,
                diagnostics,
                logger,
                () => contracts.Current,
                () => settings.VerboseDiagnostics,
                workflow);
            nativePanel = new NativeRollPanelHost(
                new RollUiCommandRouter(coordinator),
                new RollPanelPresenter(),
                nativeControls,
                () => contracts.Current,
                logger);
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
            if (enabled)
            {
                coordinator.Update(deltaTime);
                nativePanel.Update();
            }
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
                patches.Install(resolved, coordinator, nativePanel);
                enabled = true;
                diagnostics.SetStatus("Enabled; waiting for an exact new-main-character LevelUpState.");
                logger.Info("Kingmaker Dice Roller 0.0.1-alpha.1 enabled in fixed-array mode.");
                return true;
            }
            catch (Exception exception)
            {
                nativePanel.Detach(contracts.Current);
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
                nativePanel.Detach(contracts.Current);
                patches.Uninstall();
                contracts.Clear();
                return true;
            }

            string error;
            if (!coordinator.TryPrepareDisable(out error))
            {
                diagnostics.SetStatus("Disable refused because point-buy restoration failed: " + error);
                logger.Error("Kingmaker Dice Roller remains enabled to preserve recovery hooks: " + error);
                return false;
            }

            nativePanel.Detach(contracts.Current);
            patches.Uninstall();
            contracts.Clear();
            enabled = false;
            diagnostics.SetStatus("Disabled; no owned roll session remains.");
            logger.Info("Kingmaker Dice Roller disabled.");
            return true;
        }
    }
}
