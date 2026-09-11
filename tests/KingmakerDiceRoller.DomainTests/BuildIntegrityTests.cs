using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.DomainTests
{
    // Regression coverage for rolled-stat character-build integrity: the native derived
    // allowances must agree with the staged scores, and the authoritative replay must
    // consume those same scores before native checks validate player selections.
    internal static partial class PreviewSessionContinuityTests
    {
        // Confirmed defect: staging six scores left LevelUpState.IntelligenceSkillPoints
        // cached from the pre-roll Intelligence, so skill-point allowances were wrong until
        // an unrelated native rebuild happened.
        internal static void RolledIntelligenceRefreshesNativeSkillAllowance()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out FakeState state);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            RollSession session = coordinator.ActiveSession;
            int[] rolled = session.Assignment.ToAssignedArray();

            AssertEx.Equal(1, state.OnApplyActionCalls);
            AssertEx.Equal(
                FakeLevelUpHelper.GetTotalIntelligenceSkillPoints(state.Unit, 1),
                state.IntelligenceSkillPoints);
            AssertEx.Equal(
                rolled[3],
                environment.ReadUnit(state.Unit)[3]);
            AssertEx.Equal(
                state.IntelligenceSkillPoints,
                state.Unit.Progression.TotalIntelligenceSkillPoints);
        }

        internal static void DerivedAllowanceRefreshIsIdempotentAcrossRerolls()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 48).ToArray()),
                out FakeState state);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            int firstGranted = state.IntelligenceSkillPoints;
            AssertEx.True(coordinator.TryReroll(out error), error);

            AssertEx.Equal(2, state.OnApplyActionCalls);
            AssertEx.Equal(
                FakeLevelUpHelper.GetTotalIntelligenceSkillPoints(state.Unit, 1),
                state.IntelligenceSkillPoints);
            AssertEx.Equal(firstGranted, state.IntelligenceSkillPoints);
        }

        internal static void FailedDerivedRefreshFailsRollAndRollsBack()
        {
            TestEnvironment environment = TestEnvironment.Create();
            int[] origin = { 12, 10, 10, 10, 10, 10 };
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out FakeState state);
            environment.StatAccess.WriteDistributionValues(state.StatsDistribution, origin, environment.Contracts);
            environment.StatAccess.WriteUnitBaseValues(state.Unit, origin, environment.Contracts);
            state.StatsDistribution.SetAllocatorState(true, 22, 25);
            state.ThrowOnApplyAction = true;

            AssertEx.True(!coordinator.TryRoll(out string error));
            AssertEx.True(error.Contains("Roll failed without committing"));

            AssertEx.SequenceEqual(origin, environment.ReadDistribution(state));
            AssertEx.SequenceEqual(origin, environment.ReadUnit(state.Unit));
            AssertEx.True(state.StatsDistribution.Available);
            AssertEx.Equal(22, state.StatsDistribution.Points);
            AssertEx.Equal(RollSessionMode.PointBuy, coordinator.ActiveSession.Mode);
        }

        internal static void PointBuyReturnRefreshesDerivedAllowance()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out FakeState state);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            int firstGranted = state.IntelligenceSkillPoints;
            AssertEx.Equal(1, state.OnApplyActionCalls);
            AssertEx.True(firstGranted != 0, "the fixed 18-array Intelligence must grant points");

            AssertEx.True(coordinator.TryRestorePointBuy(out error), error);

            AssertEx.Equal(2, state.OnApplyActionCalls);
            AssertEx.Equal(0, state.IntelligenceSkillPoints); // restored 10s grant nothing
        }

        // Confirmed defect: the mercenary authoritative replay consumed the pre-roll source
        // scores; six numbers were corrected only after checks had already dropped selections.
        internal static void MercenaryCommitStagesScoresBeforeNativeReplay()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview;
            FakeUnitDescriptor campaignMain;
            CharacterCreationCoordinator coordinator = OpenMercenaryProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                20,
                null,
                out preview,
                out campaignMain);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            RollSession session = coordinator.ActiveSession;
            int[] expected = session.Assignment.ToAssignedArray();
            environment.Controller.LevelUpActions.AddRange(new object[]
            {
                new FakeLevelUpAction("ApplySkillPoints"),
                new FakeLevelUpAction("SelectClass"),
                new FakeLevelUpAction("SelectSpell")
            });

            FakeState commitState = environment.BeginNativeMercenaryFinalization();
            coordinator.OnLevelUpStateConstructed(commitState, environment.Source, FakeMode.CharGen);

            // The staged replay state holds the rolled assignment before actions run.
            AssertEx.SequenceEqual(expected, environment.ReadUnit(environment.Source));
            AssertEx.SequenceEqual(expected, environment.ReadDistribution(commitState));
            AssertEx.True(!commitState.StatsDistribution.Available);
            environment.Controller.State = commitState;
            var survivors = new List<object>
            {
                environment.Controller.LevelUpActions[0],
                environment.Controller.LevelUpActions[1],
                environment.Controller.LevelUpActions[2]
            };
            coordinator.OnLevelUpAppliedToAuthoritativeUnit(
                environment.Controller,
                environment.Source,
                survivors);
            coordinator.OnLevelUpCommitCompleted(environment.Controller);

            AssertEx.True(session.FinalizationVerified);
            AssertEx.SequenceEqual(expected, environment.ReadUnit(environment.Source));
            AssertEx.Equal(null, coordinator.ActiveSession);
        }

        internal static void MercenaryReplayOverwriteFailsWithoutCorrectiveWrite()
        {
            TestEnvironment environment = TestEnvironment.Create();
            var diagnostics = new RuntimeDiagnostics();
            FakeState preview;
            FakeUnitDescriptor campaignMain;
            CharacterCreationCoordinator coordinator = OpenMercenaryProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                20,
                null,
                out preview,
                out campaignMain,
                diagnostics);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            RollSession session = coordinator.ActiveSession;
            int[] overwritten = { 9, 18, 18, 18, 18, 18 };
            FakeState commitState = environment.BeginNativeMercenaryFinalization();
            coordinator.OnLevelUpStateConstructed(commitState, environment.Source, FakeMode.CharGen);
            environment.Controller.State = commitState;
            environment.WriteUnit(environment.Source, overwritten);

            coordinator.OnLevelUpAppliedToAuthoritativeUnit(
                environment.Controller,
                environment.Source,
                environment.Controller.LevelUpActions);
            coordinator.OnLevelUpCommitCompleted(environment.Controller);

            AssertEx.True(session.FinalizationFailed);
            AssertEx.SequenceEqual(overwritten, environment.ReadUnit(environment.Source));
            AssertEx.Equal(0, diagnostics.FinalizationsVerified);
            AssertEx.Equal(1, diagnostics.FinalizationFailures);
            AssertEx.True(environment.Logger.Messages.Any(
                message => message.Contains("no corrective late write")));
        }

        internal static void MercenaryDroppedSelectionsFailCompletion()
        {
            TestEnvironment environment = TestEnvironment.Create();
            var diagnostics = new RuntimeDiagnostics();
            FakeState preview;
            FakeUnitDescriptor campaignMain;
            CharacterCreationCoordinator coordinator = OpenMercenaryProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                20,
                null,
                out preview,
                out campaignMain,
                diagnostics);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            RollSession session = coordinator.ActiveSession;
            environment.Controller.LevelUpActions.AddRange(new object[]
            {
                new FakeLevelUpAction("ApplySkillPoints"),
                new FakeLevelUpAction("SelectSpell"),
                new FakeLevelUpAction("SelectSpell")
            });
            FakeState commitState = environment.BeginNativeMercenaryFinalization();
            coordinator.OnLevelUpStateConstructed(commitState, environment.Source, FakeMode.CharGen);
            environment.Controller.State = commitState;
            // Native replay dropped the player's final spell selection.
            var survivors = new List<object> { environment.Controller.LevelUpActions[0] };

            coordinator.OnLevelUpAppliedToAuthoritativeUnit(
                environment.Controller,
                environment.Source,
                survivors);
            coordinator.OnLevelUpCommitCompleted(environment.Controller);

            AssertEx.True(session.FinalizationFailed);
            AssertEx.Equal(0, diagnostics.FinalizationsVerified);
            AssertEx.True(environment.Logger.Messages.Any(message =>
                message.Contains("2 of 3 recorded level-up actions failed native replay checks")));
        }

        internal static void MercenaryInterruptedCommitRestoresExactPreCommitState()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview;
            FakeUnitDescriptor campaignMain;
            CharacterCreationCoordinator coordinator = OpenMercenaryProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                20,
                null,
                out preview,
                out campaignMain);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            int[] original = { 10, 12, 8, 12, 10, 10 };
            environment.WriteUnit(environment.Source, original);
            FakeState commitState = environment.BeginNativeMercenaryFinalization();
            coordinator.OnLevelUpStateConstructed(commitState, environment.Source, FakeMode.CharGen);

            // The native commit throws after construction; the update loop expires the ticket.
            coordinator.Update(0f);

            AssertEx.SequenceEqual(original, environment.ReadUnit(environment.Source));
            AssertEx.True(commitState.StatsDistribution.Available);
            AssertEx.True(!coordinator.MercenaryFinalization.HasPendingCommit);
        }

        internal static void MercenaryCatchUpLevelNeverStagesStartingScores()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview;
            FakeUnitDescriptor campaignMain;
            CharacterCreationCoordinator coordinator = OpenMercenaryProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                20,
                null,
                out preview,
                out campaignMain);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            int[] original = environment.ReadUnit(environment.Source);
            coordinator.TryRestorePointBuy(out error);
            environment.Sessions.Clear(coordinator.ActiveSession);

            // A later ordinary level-up constructs a non-first-level state on the same source.
            var laterState = new FakeState(environment.Source, new FakeDistribution(10), false)
            {
                Mode = FakeMode.LevelUp
            };
            coordinator.OnLevelUpStateConstructed(laterState, environment.Source, FakeMode.LevelUp);
            coordinator.OnLevelUpAppliedToAuthoritativeUnit(
                environment.Controller,
                environment.Source);
            coordinator.OnLevelUpCommitCompleted(environment.Controller);

            AssertEx.SequenceEqual(original, environment.ReadUnit(environment.Source));
        }

        // Confirmed working seam made explicit: the new-main commit-time constructor state
        // receives the rolled assignment before native replay, and the commit postfix audit
        // verifies the final recipient and closes the session.
        internal static void NewMainCommitStagesAndVerifiesAuthoritativeScores()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out FakeState preview);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            RollSession session = coordinator.ActiveSession;
            int[] expected = session.Assignment.ToAssignedArray();

            FakeState commitState = new FakeState(environment.Source, new FakeDistribution(10), true);
            coordinator.OnLevelUpStateConstructed(commitState, environment.Source, FakeMode.CharGen);

            // Rebind staged the assignment on the authoritative source before replay.
            AssertEx.SequenceEqual(expected, environment.ReadUnit(environment.Source));
            AssertEx.True(!commitState.StatsDistribution.Available);
            AssertEx.True(ReferenceEquals(session.Unit, environment.Source));
            environment.Controller.State = commitState;
            coordinator.OnLevelUpAppliedToAuthoritativeUnit(
                environment.Controller,
                environment.Source);
            coordinator.OnLevelUpCommitCompleted(environment.Controller);

            AssertEx.SequenceEqual(expected, environment.ReadUnit(environment.Source));
            AssertEx.Equal(null, coordinator.ActiveSession);
            AssertEx.True(environment.Logger.Messages.Any(message =>
                message.Contains("New-main rolled-stat final verification: passed=true") &&
                message.Contains("expectedBase=[18,18,18,18,18,18]") &&
                message.Contains("observedFinalBase=[18,18,18,18,18,18]")));
            AssertEx.True(!environment.Logger.Messages.Any(message =>
                message.Contains("New-main rolled-stat final verification: passed=false")));
        }

        internal static void UpdateRestageAlsoRefreshesDerivedAllowances()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out FakeState previewA);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            RollSession session = coordinator.ActiveSession;
            int[] expected = session.Assignment.ToAssignedArray();

            // A replacement generation whose constructor staging is overwritten by the replay.
            FakeState previewB = environment.NewReplacementState(previewA, 10);
            coordinator.OnLevelUpStateConstructed(previewB, previewB.Unit, FakeMode.CharGen);
            environment.StatAccess.WriteUnitBaseValues(
                previewB.Unit,
                Enumerable.Repeat(10, 6).ToArray(),
                environment.Contracts);
            environment.Controller.State = previewB;
            int beforeRefresh = previewB.OnApplyActionCalls;
            coordinator.Update(0.1f);

            AssertEx.True(session.IsApplied);
            AssertEx.SequenceEqual(expected, environment.ReadUnit(previewB.Unit));
            AssertEx.Equal(
                FakeLevelUpHelper.GetTotalIntelligenceSkillPoints(previewB.Unit, 1),
                previewB.IntelligenceSkillPoints);
            AssertEx.True(previewB.OnApplyActionCalls > beforeRefresh);
        }

        internal static void DerivedRefreshFailureOnPointBuyReturnIsReported()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out FakeState state);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            state.ThrowOnApplyAction = true;

            AssertEx.True(!coordinator.TryRestorePointBuy(out error));
            AssertEx.True(error != null && error.Contains("derived allowances did not refresh"), "error=" + error);

            // The durable point-buy restoration itself still stands.
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadUnit(state.Unit));
            AssertEx.True(state.StatsDistribution.Available);
        }

        private sealed class FakeLevelUpAction
        {
            internal FakeLevelUpAction(string name) { Name = name; }
            internal string Name { get; }
        }
    }
}
