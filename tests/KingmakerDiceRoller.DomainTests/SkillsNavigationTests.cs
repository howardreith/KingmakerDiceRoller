using System;
using KingmakerDiceRoller.UI;
using System.Linq;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.DomainTests
{
    // Regression coverage for the skills-counter refresh and forward-navigation integrity:
    // after any relevant score change, the live allowance, allocated ranks, displayed badge,
    // native phase-completion cache, and the forward-transition decision must agree.
    internal static partial class PreviewSessionContinuityTests
    {
        internal static void ThemeRecoveryPreservesCloseBadgeAndForwardGuards()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenSkillsGuardCoordinator(environment, new RuntimeDiagnostics(),
                new[] { SavedArray(10, "int10"), SavedArray(16, "int16") }, out FakeState state);
            AssertEx.True(coordinator.TryRecallSelectedSaved(out string error), error);
            state.SpentSkillPoints = state.TotalSkillPoints;
            environment.CharacterBuild.Skills.SkillsAllocator.FillLevelUpData();
            var fixture = new ThemeFixture();
            var bindings = new NativeThemeBindings();
            var recovery = new NativeThemeRecovery();
            bindings.Add(NativeThemeCapability.Buttons, values => { }, () => { });
            recovery.Bind(new object(), fixture.Root);
            AssertEx.True(recovery.TryBegin(true, false));
            bindings.Apply(fixture.Resolve(), message => { throw new Exception(message); }); recovery.Complete();
            coordinator.SelectNextSaved();
            AssertEx.True(coordinator.TryRecallSelectedSaved(out error), error);
            RollSession session = coordinator.ActiveSession;
            int revision = session.AssignmentRevision;
            StatAssignment assignment = session.Assignment;
            fixture.Populate();
            AssertEx.True(recovery.TryBegin(true, true));
            NativeThemeResolution theme = fixture.Resolve();
            bindings.Apply(theme, message => { throw new Exception(message); }); recovery.Complete();
            AssertEx.Equal(revision, session.AssignmentRevision);
            AssertEx.True(ReferenceEquals(assignment, session.Assignment));
            var router = new RollUiCommandRouter(coordinator);
            var panel = new NativeRollPanelState();
            panel.ObserveOwner(session.Controller, session.StableOwner); panel.AttachView(); panel.Open();
            AssertEx.True(NativeUiPresentation.CloseDrawer(panel, router.NotifyDrawerClosed));
            AssertEx.Equal(2, environment.CharacterBuild.Skills.SkillsAllocator.DisplayedRemainingPoints);
            AssertEx.True(!coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 6));
            state.SpentSkillPoints = state.TotalSkillPoints;
            AssertEx.True(coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 6));
            // Reverse direction, with a donor lost between score application and Close.
            coordinator.SelectPreviousSaved();
            AssertEx.True(coordinator.TryRecallSelectedSaved(out error), error);
            revision = session.AssignmentRevision;
            fixture.Action.Values[0].Alive = false;
            AssertEx.True(theme.DiscardStale());
            bindings.Apply(theme, message => { throw new Exception(message); });
            AssertEx.Equal(revision, session.AssignmentRevision);
            panel.Open();
            AssertEx.True(NativeUiPresentation.CloseDrawer(panel, router.NotifyDrawerClosed));
            AssertEx.Equal(-2, environment.CharacterBuild.Skills.SkillsAllocator.DisplayedRemainingPoints);
            AssertEx.True(!coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 6));
            AssertEx.True(coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 4));
            state.SpentSkillPoints = state.TotalSkillPoints;
            AssertEx.True(coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 6));
            AssertEx.True(ReferenceEquals(session, coordinator.ActiveSession));
        }

        private static SavedRollArrayRecord SavedArray(int intelligence, string label)
        {
            return SavedRollArrayRecord.Create(
                new StatAssignment(new RolledStatArray(new[] { 13, 13, 13, intelligence, 10, 10 })),
                label,
                "4d[6]kh3",
                "2026-09-11T00:00:00Z",
                "skills guard fixture");
        }

        private static CharacterCreationCoordinator OpenSkillsGuardCoordinator(
            TestEnvironment environment,
            RuntimeDiagnostics diagnostics,
            SavedRollArrayRecord[] saved,
            out FakeState state,
            int classSkillPoints = 2)
        {
            var workflow = new CharacterRollWorkflow(
                new DiceRollEngine(new DiceExpressionParser(), new SequenceRandomSource(6)),
                new PointBuyEquivalentCalculator(),
                RollConfiguration.Default(),
                saved,
                () => "2026-09-11T00:00:00Z",
                null);
            var tracker = new PointBudgetTracker();
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(
                tracker,
                diagnostics,
                workflow);
            state = environment.NewState(10);
            state.ClassSkillPoints = classSkillPoints;
            coordinator.OnDistributionStarted(state.StatsDistribution, 25);
            coordinator.OnLevelUpStateConstructed(state, state.Unit, FakeMode.CharGen);
            return coordinator;
        }

        // Owner reproduction: spend the whole budget at Intelligence 10, apply a
        // higher-Intelligence array, close the drawer, and touch nothing else. The badge must
        // show the live remainder and Next must be vetoed until the player spends it.
        internal static void SpentBudgetRerollRefreshesBadgeAndBlocksNext()
        {
            TestEnvironment environment = TestEnvironment.Create();
            var diagnostics = new RuntimeDiagnostics();
            CharacterCreationCoordinator coordinator = OpenSkillsGuardCoordinator(
                environment,
                diagnostics,
                new[] { SavedArray(10, "int10"), SavedArray(16, "int16") },
                out FakeState state);
            AssertEx.True(coordinator.TryRecallSelectedSaved(out string error), error); // Intelligence 10
            state.SpentSkillPoints = state.TotalSkillPoints; // player spent everything
            environment.CharacterBuild.Skills.SkillsAllocator.FillLevelUpData(); // the native repaint a real click performs
            AssertEx.Equal(0, state.SkillPointsRemaining);
            AssertEx.True(state.IsSkillPointsComplete(), "complete after spending the whole low-Intelligence budget");
            AssertEx.Equal(0, environment.CharacterBuild.Skills.SkillsAllocator.DisplayedRemainingPoints); // the pre-change page state

            coordinator.SelectNextSaved();
            AssertEx.True(coordinator.TryRecallSelectedSaved(out error), error); // Intelligence 16
            coordinator.OnRollDrawerClosed();

            // The badge shows the true live remainder with no further skill interaction.
            FakeSkillsAllocator skillsAllocator = environment.CharacterBuild.Skills.SkillsAllocator;
            AssertEx.True(skillsAllocator.FillLevelUpDataCalls >= 1, "the skills page was never repainted");
            AssertEx.Equal(2, skillsAllocator.DisplayedRemainingPoints);
            AssertEx.Equal(2, state.SkillPointsRemaining);
            AssertEx.True(environment.CharacterBuild.DefineAvailibleDataCalls >= 1, "selection sections were not rebuilt");
            AssertEx.True(environment.CharacterBuild.SetupUiCalls >= 1, "phase unlocks were not recomputed");
            AssertEx.True(!state.IsSkillPointsComplete(), "unspent budget must be incomplete");

            // Forward transitions out of Skills are vetoed; Back stays available.
            AssertEx.True(!coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 6));
            AssertEx.True(environment.CharacterBuild.Skills.BlinkMarksCalls >= 1, "native attention marks were not shown");
            AssertEx.True(diagnostics.Status.Contains("Spend 2 remaining skill point"), diagnostics.Status);
            AssertEx.True(coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 4));

            // Legal correction through the native controls immediately restores progression.
            state.SpentSkillPoints = state.TotalSkillPoints;
            AssertEx.True(state.IsSkillPointsComplete(), "corrected allocation must be complete");
            AssertEx.True(coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 6), "corrected allocation must allow progression");
        }

        // Reverse direction: overspending after a lower-Intelligence array is visible and
        // repairable, never silently accepted.
        internal static void OverspendAfterLowerIntelligenceBlocksForward()
        {
            TestEnvironment environment = TestEnvironment.Create();
            var diagnostics = new RuntimeDiagnostics();
            CharacterCreationCoordinator coordinator = OpenSkillsGuardCoordinator(
                environment,
                diagnostics,
                new[] { SavedArray(16, "int16"), SavedArray(10, "int10") },
                out FakeState state);
            AssertEx.True(coordinator.TryRecallSelectedSaved(out string error), error); // Intelligence 16
            state.SpentSkillPoints = state.TotalSkillPoints;
            AssertEx.True(state.IsSkillPointsComplete());

            coordinator.SelectNextSaved();
            AssertEx.True(coordinator.TryRecallSelectedSaved(out error), error); // Intelligence 10
            coordinator.OnRollDrawerClosed();

            AssertEx.Equal(-2, state.SkillPointsRemaining);
            AssertEx.True(!state.IsSkillPointsComplete());
            AssertEx.Equal(-2, environment.CharacterBuild.Skills.SkillsAllocator.DisplayedRemainingPoints);
            AssertEx.True(!coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 6));
            AssertEx.True(diagnostics.Status.Contains("Remove 2 excess skill rank"), diagnostics.Status);

            state.SpentSkillPoints = state.TotalSkillPoints; // player removes the excess rank
            AssertEx.True(coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 6));
        }

        internal static void RepeatedDrawerCloseWithoutChangesIsHarmless()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out FakeState state);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            coordinator.OnRollDrawerClosed();
            int syncCount = environment.SkillsSync.TotalSyncCount;
            int setupCount = environment.CharacterBuild.SetupUiCalls;

            coordinator.OnRollDrawerClosed();
            coordinator.OnRollDrawerClosed();
            coordinator.OnRollDrawerClosed();
            coordinator.Update(0.1f);
            coordinator.Update(0.1f);

            AssertEx.Equal(syncCount, environment.SkillsSync.TotalSyncCount);
            AssertEx.Equal(setupCount, environment.CharacterBuild.SetupUiCalls);
            AssertEx.True(coordinator.ActiveSession.IsApplied);
        }

        internal static void ForwardGuardIsScopedToOwnedSessionAndSkillsPhase()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out FakeState state);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            state.SpentSkillPoints = 0; // unspent rolled budget remains

            // No active session: native rules alone govern an unowned build.
            environment.Sessions.Clear(coordinator.ActiveSession);
            AssertEx.True(coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 6));

            // A controller that is not bound to the session's LevelUpController is untouched.
            environment.ReplaceMercenaryOwner();
            AssertEx.True(coordinator.AllowForwardPhaseTransition(
                new FakeCharacterBuildController
                {
                    LevelUpController = new FakeLevelUpController
                    {
                        Unit = FakeUnitDescriptor.Create(10, false, true)
                    }
                },
                6));

            // Owned session: transitions that do not leave or cross Skills are never vetoed.
            environment = TestEnvironment.Create();
            coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out state);
            AssertEx.True(coordinator.TryRoll(out error), error);
            state.SpentSkillPoints = 0;
            environment.CharacterBuild.CurrentPhase = FakePhaseType.Portrait;
            AssertEx.True(coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 1)); // Race
            environment.CharacterBuild.CurrentPhase = FakePhaseType.Race;
            AssertEx.True(coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 1)); // same phase

            // Leaving Skills with an unspent budget is vetoed, including a later-phase jump.
            environment.CharacterBuild.CurrentPhase = FakePhaseType.Skills;
            AssertEx.True(!coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 6));
            environment.CharacterBuild.CurrentPhase = FakePhaseType.Race;
            AssertEx.True(!coordinator.AllowForwardPhaseTransition(environment.CharacterBuild, 7)); // crosses Skills
        }

        internal static void RerollWithIdenticalScoresStillSynchronizesExactlyOnce()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 48).ToArray()),
                out FakeState state);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            AssertEx.Equal(1, environment.SkillsSync.TotalSyncCount);
            AssertEx.True(coordinator.TryReroll(out error), error); // identical all-18 array

            AssertEx.Equal(2, environment.SkillsSync.TotalSyncCount);
            coordinator.Update(0.1f);
            coordinator.Update(0.1f);
            AssertEx.Equal(2, environment.SkillsSync.TotalSyncCount); // bounded, no refresh loop
        }

        internal static void DrawerCloseSettlesSynchronizationAfterFailedCommand()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 48).ToArray()),
                out FakeState state);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            int syncCount = environment.SkillsSync.TotalSyncCount;
            state.ThrowOnApplyAction = true;
            AssertEx.True(!coordinator.TryReroll(out error), "reroll unexpectedly succeeded: " + error); // derived refresh failed; command rolled back
            state.ThrowOnApplyAction = false;
            AssertEx.True(environment.SkillsSync.IsSynchronizationPending(coordinator.ActiveSession), "expected the failed command to leave the skills page pending synchronization");

            coordinator.OnRollDrawerClosed();

            AssertEx.Equal(syncCount + 1, environment.SkillsSync.TotalSyncCount);
            AssertEx.True(!environment.SkillsSync.IsSynchronizationPending(coordinator.ActiveSession));
        }

        internal static void PreviewReplacementSynchronizesSkillsPage()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out FakeState previewA);
            AssertEx.True(coordinator.TryRoll(out string error), error);

            FakeState previewB = environment.NewReplacementState(previewA, 10);
            coordinator.OnLevelUpStateConstructed(previewB, previewB.Unit, FakeMode.CharGen);
            environment.Controller.State = previewB;
            coordinator.Update(0.1f);

            AssertEx.True(coordinator.ActiveSession.IsApplied);
            AssertEx.Equal(2, coordinator.ActiveSession.Generation);
            AssertEx.True(environment.SkillsSync.TotalSyncCount >= 2);
            FakeSkillsAllocator skillsAllocator = environment.CharacterBuild.Skills.SkillsAllocator;
            AssertEx.True(skillsAllocator.FillLevelUpDataCalls >= 1);
            AssertEx.Equal(
                previewB.SkillPointsRemaining,
                skillsAllocator.DisplayedRemainingPoints);
        }

        internal static void PointBuyReturnSynchronizesSkillsPageForPointBuyBudget()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenSkillsGuardCoordinator(
                environment,
                new RuntimeDiagnostics(),
                new[] { SavedArray(16, "int16") },
                out FakeState state);
            AssertEx.True(coordinator.TryRecallSelectedSaved(out string error), error);
            state.SpentSkillPoints = state.TotalSkillPoints;

            AssertEx.True(coordinator.TryRestorePointBuy(out error), error);

            // The restored point-buy scores (Intelligence 10) drive the page again.
            AssertEx.Equal(0, state.IntelligenceSkillPoints);
            AssertEx.True(environment.SkillsSync.IsSynchronizationPending(coordinator.ActiveSession) == false);
            FakeSkillsAllocator skillsAllocator = environment.CharacterBuild.Skills.SkillsAllocator;
            AssertEx.Equal(state.SkillPointsRemaining, skillsAllocator.DisplayedRemainingPoints);
        }
    }
}
