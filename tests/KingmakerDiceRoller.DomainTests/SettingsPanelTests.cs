using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Compatibility;
using KingmakerDiceRoller.Domain;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.UI;

namespace KingmakerDiceRoller.DomainTests
{
    // The UMM panel presenter runs against the real coordinator, context policy, session state
    // machine, and point-buy restoration over the runner's fake Kingmaker boundary.
    internal static partial class PreviewSessionContinuityTests
    {
        private const string EnabledStatus = "Enabled; waiting for supported creation or an owned starting-score respec.";
        private const string BagOfTricksDetail =
            "Bag of Tricks changes character-creation point-buy behavior. This mod observes the live allocator budget, " +
            "but compatibility is not qualified until the dedicated smoke matrix passes.";
        private const string RespecializationDetail =
            "newman55 Respecialization was detected. Its replacement/Original-score contracts are not qualified; this provider has no adapter.";

        internal static void SettingsPanelHealthyMainMenuIsShortAndCollapsed()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RuntimeDiagnostics diagnostics = ReadyDiagnostics();
            var panel = new PanelHarness(environment.CreateCoordinator(new PointBudgetTracker(), diagnostics), diagnostics, environment);

            SettingsPanelModel model = panel.Build();

            AssertEx.Equal(SettingsPanel.Explanation, model.Explanation);
            foreach (string context in new[] { "ability-score page", "new main character", "mercenary", "respec all six starting scores" })
            {
                AssertEx.True(model.Explanation.Contains(context), "explanation must name: " + context);
            }
            AssertEx.True(!model.Explanation.Contains("level"), "the explanation must not imply ordinary level-ups");
            AssertEx.Equal("Status: Ready.", model.Status.Text);
            AssertEx.Equal(PanelTone.Normal, model.Status.Tone);
            AssertEx.Equal(0, model.Notices.Count);
            AssertEx.True(model.Recovery == null, "no disabled emergency button without a roll state");
            AssertEx.True(!model.TroubleshootingExpanded);
            AssertEx.Equal("Show troubleshooting", model.TroubleshootingButtonLabel);

            List<string> visible = VisibleLines(model);
            AssertEx.True(visible.Count <= 4, "the healthy main-menu panel grew to " + visible.Count + " rows");
            AssertHidden(visible, "Mr. Bard", "Roll For Stats", "0.1.", "Verbose", "MVID", "contracts", "Accepted",
                "rejected", "Workflow", "history", "saved arrays", "Detected", "waiting", "Emergency", "Bag of Tricks");
        }

        internal static void SettingsPanelUnrelatedLevelUpIsNotAlarming()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RuntimeDiagnostics diagnostics = ReadyDiagnostics();
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(new PointBudgetTracker(), diagnostics);
            var panel = new PanelHarness(coordinator, diagnostics, environment);

            for (int index = 0; index < 40; index++)
            {
                FakeState levelUp = environment.NewState(10);
                levelUp.IsFirstLevel = false;
                coordinator.OnLevelUpStateConstructed(levelUp, levelUp.Unit, FakeMode.LevelUp);
            }

            AssertEx.Equal(40, diagnostics.RejectedContexts);
            AssertEx.Equal(null, coordinator.ActiveSession);
            SettingsPanelModel model = panel.Build();
            AssertEx.Equal("Status: Ready.", model.Status.Text);
            AssertEx.Equal(PanelTone.Normal, model.Status.Tone);
            AssertEx.Equal(0, model.Notices.Count);
            AssertEx.True(model.Recovery == null);
            AssertHidden(VisibleLines(model), "REJECT", "rejected", "40");

            panel.Panel.ToggleTroubleshooting();
            string details = string.Join("\n", panel.Build().Troubleshooting.Details);
            AssertEx.True(details.Contains("rejected: 40"), details);
            AssertEx.True(details.Contains(" REJECT "), "the deduplicated rejection reason belongs in Troubleshooting");
        }

        internal static void SettingsPanelSupportedCreationShowsAvailabilityAndRecovery()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RuntimeDiagnostics diagnostics = ReadyDiagnostics();
            FakeState state;
            CharacterCreationCoordinator coordinator = OpenPanelCoordinator(environment, diagnostics, out state);
            var panel = new PanelHarness(coordinator, diagnostics, environment);

            SettingsPanelModel pointBuy = panel.Build();
            AssertEx.Equal("Status: This new character can use Roll Stats.", pointBuy.Status.Text);
            AssertEx.Equal(0, pointBuy.Notices.Count);
            AssertEx.True(pointBuy.Recovery == null, "point buy needs no recovery action");

            AssertEx.True(coordinator.TryRoll(out string error), error);
            SettingsPanelModel rolled = panel.Build();
            AssertEx.Equal("Status: This new character is using rolled scores.", rolled.Status.Text);
            AssertEx.Equal(0, rolled.Notices.Count);
            AssertEx.True(rolled.Recovery != null && rolled.Recovery.Enabled, "an active roll surfaces Return to Point Buy");
            AssertEx.Equal("Return to Point Buy", RecoveryAction.Label);
            AssertEx.Equal("Restores the point-buy scores you had before rolling.", rolled.Recovery.Explanation.Text);
            AssertEx.True(VisibleLines(rolled).Count <= 6, "the rolled panel grew beyond six rows");

            panel.Panel.ReturnToPointBuy();

            AssertEx.Equal(RollSessionMode.PointBuy, coordinator.ActiveSession.Mode);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadUnit(state.Unit));
            AssertEx.True(state.StatsDistribution.Available);
            AssertEx.Equal(25, state.StatsDistribution.Points);
            SettingsPanelModel restored = panel.Build();
            AssertEx.Equal("Status: This new character can use Roll Stats.", restored.Status.Text);
            AssertEx.Equal(0, restored.Notices.Count);
            AssertEx.True(restored.Recovery == null);
        }

        internal static void SettingsPanelNamesMercenaryAndRespecContexts()
        {
            TestEnvironment mercenaryEnvironment = TestEnvironment.Create();
            RuntimeDiagnostics mercenaryDiagnostics = ReadyDiagnostics();
            CharacterCreationCoordinator mercenary = OpenMercenaryProductCoordinator(
                mercenaryEnvironment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                20,
                null,
                out FakeState mercenaryState,
                out FakeUnitDescriptor campaignMain,
                mercenaryDiagnostics);
            var mercenaryPanel = new PanelHarness(mercenary, mercenaryDiagnostics, mercenaryEnvironment);
            AssertEx.Equal("Status: This mercenary can use Roll Stats.", mercenaryPanel.Build().Status.Text);
            AssertEx.True(mercenary.TryRoll(out string error), error);
            SettingsPanelModel mercenaryRolled = mercenaryPanel.Build();
            AssertEx.Equal("Status: This mercenary is using rolled scores.", mercenaryRolled.Status.Text);
            AssertEx.True(mercenaryRolled.Recovery != null && mercenaryRolled.Recovery.Enabled);

            var respec = new RespecFixture();
            Ready(respec.Diagnostics);
            var respecPanel = new PanelHarness(respec.Coordinator, respec.Diagnostics, respec.Environment);
            SettingsPanelModel respecPointBuy = respecPanel.Build();
            AssertEx.Equal("Status: This respec can use Roll Stats.", respecPointBuy.Status.Text);
            AssertEx.True(respecPointBuy.Recovery == null);
            int[] origin = respec.Environment.ReadUnit(respec.Preview.Unit);

            respec.Roll();
            SettingsPanelModel respecRolled = respecPanel.Build();
            AssertEx.Equal("Status: This respec is using rolled scores.", respecRolled.Status.Text);
            AssertEx.True(respecRolled.Recovery != null && respecRolled.Recovery.Enabled);

            respecPanel.Panel.ReturnToPointBuy();
            AssertEx.Equal(RollSessionMode.PointBuy, respec.Coordinator.ActiveSession.Mode);
            AssertEx.SequenceEqual(origin, respec.Environment.ReadUnit(respec.Preview.Unit));
            AssertEx.True(respecPanel.Build().Recovery == null);
            AssertEx.Equal(0, respecPanel.Build().Notices.Count);
        }

        internal static void SettingsPanelRejectedOwnershipIsNotAlarming()
        {
            var respec = new RespecFixture();
            Ready(respec.Diagnostics);
            var panel = new PanelHarness(respec.Coordinator, respec.Diagnostics, respec.Environment);
            RollSession owned = respec.Coordinator.ActiveSession;
            int rejectedBefore = respec.Diagnostics.RejectedContexts;

            for (int index = 0; index < 5; index++)
            {
                var stranger = new FakeState(FakeUnitDescriptor.Create(10, false), new FakeDistribution(10), true) { Mode = FakeMode.Respec };
                respec.Coordinator.OnLevelUpStateConstructed(stranger, stranger.Unit, FakeMode.Respec);
            }

            AssertEx.True(respec.Diagnostics.RejectedContexts > rejectedBefore, "the unrelated owner must be rejected");
            AssertEx.True(ReferenceEquals(owned, respec.Coordinator.ActiveSession));
            SettingsPanelModel model = panel.Build();
            AssertEx.Equal("Status: This respec can use Roll Stats.", model.Status.Text);
            AssertEx.Equal(0, model.Notices.Count);
            AssertEx.True(model.Recovery == null);
        }

        internal static void SettingsPanelStartFailureStaysVisible()
        {
            TestEnvironment environment = TestEnvironment.Create();
            var diagnostics = new RuntimeDiagnostics();
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(new PointBudgetTracker(), diagnostics);
            KingmakerContracts contracts = null;
            var panel = new PanelHarness(coordinator, diagnostics, () => contracts);

            AssertEx.Equal("Status: Off.", panel.Build().Status.Text);

            diagnostics.SetActivation(RuntimeActivation.EnableFailed, "Enable failed closed: simulated contract mismatch");
            SettingsPanelModel failed = panel.Build();
            AssertEx.True(!failed.TroubleshootingExpanded);
            AssertEx.Equal(PanelTone.Problem, failed.Status.Tone);
            AssertEx.Equal("Status: Could not start, so roll controls are unavailable. See Troubleshooting.", failed.Status.Text);
            AssertHidden(VisibleLines(failed), "simulated contract mismatch");
            panel.Panel.ToggleTroubleshooting();
            string details = string.Join("\n", panel.Build().Troubleshooting.Details);
            AssertEx.True(details.Contains("Status: Enable failed closed: simulated contract mismatch"), details);
            AssertEx.True(details.Contains("Kingmaker contracts: unavailable"), details);

            // Readiness comes from resolved contracts, never from the enabled flag alone.
            diagnostics.SetActivation(RuntimeActivation.Enabled, EnabledStatus);
            AssertEx.Equal(PanelTone.Problem, panel.Build().Status.Tone);
            AssertEx.Equal("Status: Unavailable. See Troubleshooting.", panel.Build().Status.Text);
            contracts = environment.Contracts;
            AssertEx.Equal("Status: Ready.", panel.Build().Status.Text);

            diagnostics.SetActivation(RuntimeActivation.Disabled, "Disabled; no owned roll session remains.");
            contracts = null;
            SettingsPanelModel off = panel.Build();
            AssertEx.Equal("Status: Off.", off.Status.Text);
            AssertEx.Equal(PanelTone.Normal, off.Status.Tone);
            AssertEx.Equal(0, off.Notices.Count);
        }

        internal static void SettingsPanelApplicationFailureKeepsRecoveryVisible()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RuntimeDiagnostics diagnostics = ReadyDiagnostics();
            FakeState previewA;
            CharacterCreationCoordinator coordinator = OpenPanelCoordinator(environment, diagnostics, out previewA);
            var panel = new PanelHarness(coordinator, diagnostics, environment);
            AssertEx.True(coordinator.TryRoll(out string error), error);

            // A replacement preview whose replay overwrites the staged scores, and whose bounded
            // restage cannot refresh the derived allowances, fails the application closed.
            FakeState previewB = environment.NewReplacementState(previewA, 10);
            coordinator.OnLevelUpStateConstructed(previewB, previewB.Unit, FakeMode.CharGen);
            environment.WriteUnit(previewB.Unit, Enumerable.Repeat(10, 6).ToArray());
            previewB.ThrowOnApplyAction = true;
            environment.Controller.State = previewB;
            coordinator.Update(0.1f);
            AssertEx.True(coordinator.ActiveSession.IsApplicationFailed, diagnostics.Status);

            SettingsPanelModel failed = panel.Build();
            AssertEx.True(!failed.TroubleshootingExpanded);
            PanelNotice notice = failed.Notices.Single(item => item.Kind == PanelNoticeKind.RollNotApplied);
            AssertEx.Equal(PanelTone.Problem, notice.Line.Tone);
            AssertEx.Equal("Rolled scores could not be applied to this new character. See Troubleshooting.", notice.Line.Text);
            AssertEx.Equal("Status: Roll Mode is on for this new character.", failed.Status.Text);
            AssertEx.True(failed.Recovery != null && failed.Recovery.Enabled, "the failed roll stays recoverable");

            previewB.ThrowOnApplyAction = false;
            panel.Panel.ReturnToPointBuy();

            AssertEx.Equal(RollSessionMode.PointBuy, coordinator.ActiveSession.Mode);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadUnit(previewB.Unit));
            SettingsPanelModel recovered = panel.Build();
            AssertEx.Equal(0, recovered.Notices.Count);
            AssertEx.True(recovered.Recovery == null);
        }

        internal static void SettingsPanelBlockedRecoveryExplainsReason()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RuntimeDiagnostics diagnostics = ReadyDiagnostics();
            FakeState state;
            CharacterCreationCoordinator coordinator = OpenPanelCoordinator(environment, diagnostics, out state);
            var panel = new PanelHarness(coordinator, diagnostics, environment);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            state.StatsDistribution.OnStart = budget =>
            {
                environment.Controller.Preview = FakeUnitDescriptor.Create(10, false);
                throw new InvalidOperationException("simulated allocator failure after detachment");
            };

            panel.Panel.ReturnToPointBuy();

            RollSession session = coordinator.ActiveSession;
            AssertEx.Equal(RollSessionMode.RestoringPointBuy, session.Mode);
            AssertEx.True(diagnostics.Status.StartsWith("Point-buy restoration failed: ", StringComparison.Ordinal), diagnostics.Status);
            SettingsPanelModel blocked = panel.Build();
            AssertEx.True(blocked.Recovery != null, "an interrupted restore must not hide the recovery action");
            AssertEx.True(!blocked.Recovery.Enabled);
            AssertEx.Equal(PanelTone.Problem, blocked.Recovery.Explanation.Tone);
            AssertEx.Equal("Unavailable because an earlier Return to Point Buy did not finish. See Troubleshooting.",
                blocked.Recovery.Explanation.Text);
            AssertEx.Equal("Status: Roll Mode is on for this new character.", blocked.Status.Text);

            // The blocked action is inert: no new attempt, status, or state change.
            string status = diagnostics.Status;
            int messages = environment.Logger.Messages.Count;
            int revision = session.AssignmentRevision;
            panel.Panel.ReturnToPointBuy();
            AssertEx.Equal(RollSessionMode.RestoringPointBuy, session.Mode);
            AssertEx.Equal(status, diagnostics.Status);
            AssertEx.Equal(messages, environment.Logger.Messages.Count);
            AssertEx.Equal(revision, session.AssignmentRevision);

            // A refused disable is reported as a current problem while the session still needs it.
            string disableError;
            AssertEx.True(!coordinator.TryPrepareDisable(out disableError));
            diagnostics.RefuseDisable("Disable refused because point-buy restoration failed: " + disableError);
            SettingsPanelModel refused = panel.Build();
            AssertEx.Equal(PanelTone.Problem, refused.Notices.Single(item => item.Kind == PanelNoticeKind.DisableRefused).Line.Tone);
            AssertEx.True(refused.Recovery != null && !refused.Recovery.Enabled);
        }

        internal static void SettingsPanelFailedReturnKeepsActionAvailable()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RuntimeDiagnostics diagnostics = ReadyDiagnostics();
            FakeState state;
            CharacterCreationCoordinator coordinator = OpenPanelCoordinator(
                environment, diagnostics, out state, new SequenceRandomSource(Enumerable.Repeat(6, 48).ToArray()));
            var panel = new PanelHarness(coordinator, diagnostics, environment);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            int[] rolled = coordinator.ActiveSession.Assignment.ToAssignedArray();
            state.StatsDistribution.ThrowAfterStart = true;

            panel.Panel.ReturnToPointBuy();

            AssertEx.Equal(RollSessionMode.Roll, coordinator.ActiveSession.Mode);
            AssertEx.SequenceEqual(rolled, environment.ReadUnit(state.Unit));
            SettingsPanelModel failed = panel.Build();
            PanelNotice notice = failed.Notices.Single(item => item.Kind == PanelNoticeKind.ReturnFailed);
            AssertEx.Equal(PanelTone.Problem, notice.Line.Tone);
            AssertEx.Equal("Return to Point Buy did not complete. See Troubleshooting.", notice.Line.Text);
            AssertEx.True(failed.Recovery != null && failed.Recovery.Enabled, "the rolled-back session may retry");
            panel.Panel.ToggleTroubleshooting();
            AssertEx.True(panel.Build().Troubleshooting.Details.Any(line => line.StartsWith("Last Return to Point Buy: Failed;", StringComparison.Ordinal)));

            // A later change to the assignment supersedes the old attempt.
            state.StatsDistribution.ThrowAfterStart = false;
            AssertEx.True(coordinator.TryReroll(out error), error);
            AssertEx.True(panel.Build().Notices.All(item => item.Kind != PanelNoticeKind.ReturnFailed));
        }

        internal static void SettingsPanelIncompleteReturnStaysVisible()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RuntimeDiagnostics diagnostics = ReadyDiagnostics();
            FakeState state;
            CharacterCreationCoordinator coordinator = OpenPanelCoordinator(environment, diagnostics, out state);
            var panel = new PanelHarness(coordinator, diagnostics, environment);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            state.ThrowOnApplyAction = true;

            panel.Panel.ReturnToPointBuy();

            AssertEx.Equal(RollSessionMode.PointBuy, coordinator.ActiveSession.Mode);
            SettingsPanelModel model = panel.Build();
            PanelNotice notice = model.Notices.Single(item => item.Kind == PanelNoticeKind.ReturnDerivedValuesStale);
            AssertEx.Equal(PanelTone.Problem, notice.Line.Tone);
            AssertEx.True(!model.TroubleshootingExpanded);
            AssertEx.True(model.Recovery == null, "point buy is already restored; eligibility is unchanged");
        }

        internal static void SettingsPanelFinalizationFailureStaysVisibleAfterSessionEnds()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RuntimeDiagnostics diagnostics = ReadyDiagnostics();
            CharacterCreationCoordinator coordinator = OpenMercenaryProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                20,
                null,
                out FakeState preview,
                out FakeUnitDescriptor campaignMain,
                diagnostics);
            var panel = new PanelHarness(coordinator, diagnostics, environment);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            FakeState finalState = environment.BeginNativeMercenaryFinalization();
            coordinator.OnLevelUpStateConstructed(finalState, environment.Source, FakeMode.CharGen);
            environment.Controller.State = finalState;
            coordinator.OnLevelUpAppliedToAuthoritativeUnit(environment.Controller, environment.Source);
            environment.WriteUnit(environment.Source, new[] { 9, 18, 18, 18, 18, 18 });
            coordinator.OnLevelUpCommitCompleted(environment.Controller);
            AssertEx.Equal(null, coordinator.ActiveSession);

            SettingsPanelModel failed = panel.Build();
            AssertEx.Equal("Status: Ready.", failed.Status.Text);
            PanelNotice notice = failed.Notices.Single(item => item.Kind == PanelNoticeKind.FinalizationFailed);
            AssertEx.Equal(PanelTone.Problem, notice.Line.Tone);
            AssertEx.Equal("Your last finished character may not have kept its rolled scores. See Troubleshooting.", notice.Line.Text);
            panel.Panel.ToggleTroubleshooting();
            AssertEx.True(panel.Build().Troubleshooting.Details.Any(line =>
                line.StartsWith("Last finalization: FAIL Mercenary rolled-stat final verification:", StringComparison.Ordinal) &&
                line.Contains("observedFinalBase=[9,18,18,18,18,18]")));

            // A later verified finalization supersedes the earlier failure.
            TestEnvironment next = TestEnvironment.Create();
            CharacterCreationCoordinator nextCoordinator = OpenMercenaryProductCoordinator(
                next,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                20,
                null,
                out FakeState nextPreview,
                out FakeUnitDescriptor nextMain,
                diagnostics);
            AssertEx.True(nextCoordinator.TryRoll(out error), error);
            FakeState nextFinal = next.BeginNativeMercenaryFinalization();
            nextCoordinator.OnLevelUpStateConstructed(nextFinal, next.Source, FakeMode.CharGen);
            next.Controller.State = nextFinal;
            nextCoordinator.OnLevelUpAppliedToAuthoritativeUnit(next.Controller, next.Source);
            nextCoordinator.OnLevelUpCommitCompleted(next.Controller);
            AssertEx.Equal(1, diagnostics.FinalizationsVerified);
            var nextPanel = new PanelHarness(nextCoordinator, diagnostics, next);
            AssertEx.Equal(0, nextPanel.Build().Notices.Count);
        }

        internal static void SettingsPanelUnverifiedCompatibilityIsConcise()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RuntimeDiagnostics diagnostics = ReadyDiagnostics();
            var panel = new PanelHarness(environment.CreateCoordinator(new PointBudgetTracker(), diagnostics), diagnostics, environment);
            panel.Panel.SetCompatibility(new CompatibilitySnapshot(
                new[] { "BagOfTricks 1.12.0 enabled=True active=True", "CallOfTheWild 1.6.0 enabled=True active=True" },
                new[] { new CompatibilityNotice("Bag of Tricks", BagOfTricksDetail) }));

            SettingsPanelModel model = panel.Build();

            AssertEx.Equal("Status: Ready.", model.Status.Text);
            AssertEx.Equal(PanelTone.Normal, model.Status.Tone);
            PanelNotice notice = model.Notices.Single();
            AssertEx.Equal(PanelNoticeKind.CompatibilityUnverified, notice.Kind);
            AssertEx.Equal(PanelTone.Notice, notice.Line.Tone);
            AssertEx.Equal("Compatibility with Bag of Tricks is unverified. See Troubleshooting.", notice.Line.Text);
            AssertHidden(VisibleLines(model), "conflict", "incompatible", "allocator budget", "smoke matrix", "Call of the Wild",
                "CallOfTheWild", "enabled=");

            panel.Panel.ToggleTroubleshooting();
            List<string> details = panel.Build().Troubleshooting.Details.ToList();
            AssertEx.True(details.Contains("Detected: BagOfTricks 1.12.0 enabled=True active=True"));
            AssertEx.True(details.Contains("Detected: CallOfTheWild 1.6.0 enabled=True active=True"));
            AssertEx.True(details.Contains("Compatibility (Bag of Tricks): " + BagOfTricksDetail));

            panel.Panel.SetCompatibility(new CompatibilitySnapshot(
                new[] { "BagOfTricks 1.12.0 enabled=True active=True", "Respecialization 1.0 enabled=True active=True" },
                new[]
                {
                    new CompatibilityNotice("Bag of Tricks", BagOfTricksDetail),
                    new CompatibilityNotice("Respecialization", RespecializationDetail)
                }));
            PanelNotice combined = panel.Build().Notices.Single();
            AssertEx.Equal("Compatibility with Bag of Tricks and Respecialization is unverified. See Troubleshooting.", combined.Line.Text);
        }

        internal static void SettingsPanelTroubleshootingIsPresentationOnly()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RuntimeDiagnostics diagnostics = ReadyDiagnostics();
            var random = new SequenceRandomSource(Enumerable.Repeat(6, 48).ToArray());
            FakeState state;
            CharacterCreationCoordinator coordinator = OpenPanelCoordinator(environment, diagnostics, out state, random);
            var panel = new PanelHarness(coordinator, diagnostics, environment);
            panel.Panel.SetCompatibility(new CompatibilitySnapshot(
                new[] { "BagOfTricks 1.12.0 enabled=True active=True" },
                new[] { new CompatibilityNotice("Bag of Tricks", BagOfTricksDetail) }));
            AssertEx.True(coordinator.TryRoll(out string error), error);
            RollSession session = coordinator.ActiveSession;
            var before = PanelSideEffectProbe.Capture(environment, diagnostics, coordinator, state, random);

            SettingsPanelModel expanded = null;
            for (int pass = 0; pass < 3; pass++)
            {
                panel.Build();
                panel.Panel.ToggleTroubleshooting();
                expanded = panel.Build();
                AssertEx.True(expanded.TroubleshootingExpanded);
                panel.Panel.ToggleTroubleshooting();
                AssertEx.True(!panel.Build().TroubleshootingExpanded);
            }

            before.AssertUnchanged(PanelSideEffectProbe.Capture(environment, diagnostics, coordinator, state, random));
            AssertEx.Equal(0, panel.VerboseWrites);
            AssertEx.True(panel.Verbose, "a saved verbose preference stays on");
            AssertEx.Equal("Hide troubleshooting", expanded.TroubleshootingButtonLabel);
            AssertEx.True(expanded.Troubleshooting.VerboseDiagnostics);
            AssertEx.Equal("Verbose diagnostic logging", TroubleshootingSection.VerboseDiagnosticsLabel);
            string details = string.Join("\n", expanded.Troubleshooting.Details);
            foreach (string expected in new[]
            {
                "Status: Roll Mode is active", "Activation: Enabled", "Workflow: Roll; history: 1/20; saved arrays: 0/10",
                "Session: creationKind=NewMainCharacter, mode=Roll", "Return to Point Buy: available.",
                "Accepted contexts: ", "; rejected: ", "; final pass/fail: 0/0", "Kingmaker contracts: resolved; MVID ",
                "Detected: BagOfTricks", "Compatibility (Bag of Tricks): " + BagOfTricksDetail, "Recent diagnostics (", " APPLY "
            })
            {
                AssertEx.True(details.Contains(expected), "Troubleshooting is missing: " + expected);
            }

            // The verbose toggle changes only the preference and leaves expansion alone.
            panel.Panel.ToggleTroubleshooting();
            panel.Panel.SetVerboseDiagnostics(false);
            AssertEx.Equal(1, panel.VerboseWrites);
            SettingsPanelModel quiet = panel.Build();
            AssertEx.True(quiet.TroubleshootingExpanded);
            AssertEx.True(!quiet.Troubleshooting.VerboseDiagnostics);
            AssertEx.True(quiet.Troubleshooting.Details.All(line => !line.Contains(" APPLY ")));
            panel.Panel.SetVerboseDiagnostics(false);
            AssertEx.Equal(1, panel.VerboseWrites);
            AssertEx.True(ReferenceEquals(session, coordinator.ActiveSession));
            AssertEx.Equal(RollSessionMode.Roll, session.Mode);
        }

        internal static void SettingsPanelPreservesSavedPreferences()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RuntimeDiagnostics diagnostics = ReadyDiagnostics();
            int persists = 0;
            var configuration = new RollConfiguration(DiceRollPreset.ThreeD6, LowScorePolicy.Tabletop, 7, "3d6");
            var workflow = new CharacterRollWorkflow(
                new DiceRollEngine(new DiceExpressionParser(), new SequenceRandomSource(Enumerable.Repeat(4, 24).ToArray())),
                new PointBuyEquivalentCalculator(),
                configuration,
                null,
                () => "2026-10-15T00:00:00Z",
                (config, saved) => persists++);
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(new PointBudgetTracker(), diagnostics, workflow);
            var panel = new PanelHarness(coordinator, diagnostics, environment) { Verbose = false };

            panel.Build();
            panel.Panel.ToggleTroubleshooting();
            SettingsPanelModel expanded = panel.Build();
            panel.Panel.ToggleTroubleshooting();
            panel.Build();
            panel.Panel.ReturnToPointBuy(); // no roll state: inert

            AssertEx.Equal(0, persists);
            AssertEx.Equal(0, panel.VerboseWrites);
            AssertEx.True(!panel.Verbose, "a saved verbose-off preference is not re-enabled");
            AssertEx.True(!expanded.Troubleshooting.VerboseDiagnostics);
            RollConfiguration current = coordinator.UiSnapshot.Configuration;
            AssertEx.Equal(DiceRollPreset.ThreeD6, current.Preset);
            AssertEx.Equal(LowScorePolicy.Tabletop, current.LowScorePolicy);
            AssertEx.Equal(7, current.MinimumScore);
            AssertEx.Equal(0, coordinator.UiSnapshot.SavedCount);
        }

        private static RuntimeDiagnostics ReadyDiagnostics()
        {
            return Ready(new RuntimeDiagnostics());
        }

        private static RuntimeDiagnostics Ready(RuntimeDiagnostics diagnostics)
        {
            // The exact transition CompositionRoot makes after contracts resolve and patches install.
            diagnostics.SetActivation(RuntimeActivation.Enabled, EnabledStatus);
            return diagnostics;
        }

        private static CharacterCreationCoordinator OpenPanelCoordinator(
            TestEnvironment environment,
            RuntimeDiagnostics diagnostics,
            out FakeState state,
            IRandomSource random = null)
        {
            var tracker = new PointBudgetTracker();
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(
                tracker,
                diagnostics,
                NewProductWorkflow(random ?? new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()), RollConfiguration.Default()));
            state = environment.NewState(10);
            coordinator.OnDistributionStarted(state.StatsDistribution, 25);
            coordinator.OnLevelUpStateConstructed(state, state.Unit, FakeMode.CharGen);
            AssertEx.True(coordinator.ActiveSession != null);
            return coordinator;
        }

        private static List<string> VisibleLines(SettingsPanelModel model)
        {
            var lines = new List<string> { model.Explanation };
            if (model.Status != null) lines.Add(model.Status.Text);
            lines.AddRange(model.Notices.Select(notice => notice.Line.Text));
            if (model.Recovery != null)
            {
                lines.Add(RecoveryAction.Label);
                lines.Add(model.Recovery.Explanation.Text);
            }
            lines.Add(model.TroubleshootingButtonLabel);
            if (model.Troubleshooting != null)
            {
                lines.Add(TroubleshootingSection.VerboseDiagnosticsLabel);
                lines.AddRange(model.Troubleshooting.Details);
            }
            return lines;
        }

        private static void AssertHidden(List<string> visible, params string[] hidden)
        {
            string text = string.Join("\n", visible);
            foreach (string token in hidden)
            {
                AssertEx.True(text.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0,
                    "the default panel shows troubleshooting detail '" + token + "':\n" + text);
            }
        }

        private sealed class PanelHarness
        {
            internal PanelHarness(CharacterCreationCoordinator coordinator, RuntimeDiagnostics diagnostics, TestEnvironment environment)
                : this(coordinator, diagnostics, () => environment.Contracts)
            {
            }

            internal PanelHarness(CharacterCreationCoordinator coordinator, RuntimeDiagnostics diagnostics, Func<KingmakerContracts> contracts)
            {
                Panel = new SettingsPanel(coordinator, diagnostics, contracts, () => Verbose, value =>
                {
                    Verbose = value;
                    VerboseWrites++;
                });
            }

            internal SettingsPanel Panel { get; }
            internal bool Verbose { get; set; } = true;
            internal int VerboseWrites { get; private set; }

            internal SettingsPanelModel Build()
            {
                return Panel.Build();
            }
        }

        private sealed class PanelSideEffectProbe
        {
            private RollSession session;
            private RollSessionMode mode;
            private int generation;
            private int revision;
            private int[] assignment;
            private int[] unit;
            private int[] distribution;
            private bool available;
            private int points;
            private int randomCalls;
            private string status;
            private string[] recent;
            private int accepted;
            private int rejected;
            private int applied;
            private int historyCount;

            internal static PanelSideEffectProbe Capture(
                TestEnvironment environment,
                RuntimeDiagnostics diagnostics,
                CharacterCreationCoordinator coordinator,
                FakeState state,
                SequenceRandomSource random)
            {
                RollSession session = coordinator.ActiveSession;
                return new PanelSideEffectProbe
                {
                    session = session,
                    mode = session.Mode,
                    generation = session.Generation,
                    revision = session.AssignmentRevision,
                    assignment = session.Assignment.ToAssignedArray(),
                    unit = environment.ReadUnit(state.Unit),
                    distribution = environment.ReadDistribution(state),
                    available = state.StatsDistribution.Available,
                    points = state.StatsDistribution.Points,
                    randomCalls = random.Calls,
                    status = diagnostics.Status,
                    recent = diagnostics.SnapshotRecent(),
                    accepted = diagnostics.AcceptedContexts,
                    rejected = diagnostics.RejectedContexts,
                    applied = diagnostics.ArraysApplied,
                    historyCount = coordinator.UiSnapshot.HistoryCount
                };
            }

            internal void AssertUnchanged(PanelSideEffectProbe after)
            {
                AssertEx.True(ReferenceEquals(session, after.session), "the session changed");
                AssertEx.Equal(mode, after.mode);
                AssertEx.Equal(generation, after.generation);
                AssertEx.Equal(revision, after.revision);
                AssertEx.SequenceEqual(assignment, after.assignment);
                AssertEx.SequenceEqual(unit, after.unit);
                AssertEx.SequenceEqual(distribution, after.distribution);
                AssertEx.Equal(available, after.available);
                AssertEx.Equal(points, after.points);
                AssertEx.Equal(randomCalls, after.randomCalls, "the panel rolled dice");
                AssertEx.Equal(status, after.status);
                AssertEx.SequenceEqual(recent, after.recent, "the diagnostic history changed");
                AssertEx.Equal(accepted, after.accepted);
                AssertEx.Equal(rejected, after.rejected);
                AssertEx.Equal(applied, after.applied);
                AssertEx.Equal(historyCount, after.historyCount);
            }
        }
    }
}
