using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Domain;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class PreviewSessionContinuityTests
    {
        private static readonly int[] FixedValues = { 16, 15, 14, 12, 10, 8 };

        internal static void PreviewAOpensWithStableSource()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession session = environment.Open(previewA);

            AssertEx.True(ReferenceEquals(environment.Controller, session.Controller));
            AssertEx.True(ReferenceEquals(environment.Source, session.StableOwner));
            AssertEx.True(ReferenceEquals(previewA.Unit, session.Unit));
            AssertEx.Equal(1, session.Generation);
        }

        internal static void PreviewBRebindsWithDifferentDescriptor()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession first = environment.Open(previewA);
            FakeState previewB = environment.NewReplacementState(previewA, 10);
            string reason;
            RollSession rebound = environment.Rebind(previewB, out reason);

            AssertEx.True(ReferenceEquals(first, rebound));
            AssertEx.True(!ReferenceEquals(previewA.Unit, previewB.Unit));
            AssertEx.True(ReferenceEquals(previewB.Unit, rebound.Unit));
            AssertEx.Equal(2, rebound.Generation);
            AssertEx.True(reason.Contains("same-owner preview generation"));
        }

        internal static void SameOwnerDoesNotReportAnotherUnit()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            environment.Open(previewA);
            FakeState previewB = environment.NewReplacementState(previewA, 10);
            string reason;
            environment.Rebind(previewB, out reason);

            AssertEx.True(!reason.Contains("Another unit"));
            AssertEx.True(reason.Contains("Rebound"));
        }

        internal static void ConstructorStageReplacementIsMarkedPending()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            environment.Open(previewA);
            FakeState previewB = environment.NewReplacementState(previewA, 10);
            CharacterCreationContextDecision decision = environment.Evaluate(previewB, FakeMode.CharGen);
            AssertEx.True(decision.Accepted, decision.Reason);
            AssertEx.True(!decision.ControllerStateMatches);
            AssertEx.True(decision.ControllerPreviewMatches);

            RollSession session;
            string reason;
            AssertEx.True(environment.Sessions.TryOpenOrRebind(
                decision,
                generation => environment.CaptureRollback(previewB, generation),
                out session,
                out reason), reason);
            AssertEx.True(session.PendingReplacementObserved);
        }

        internal static void DifferentStableOwnerIsRejected()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession original = environment.Open(previewA);

            var differentSource = FakeUnitDescriptor.Create(10, true);
            var differentPreview = FakeUnitDescriptor.Create(10, false);
            var differentState = new FakeState(differentPreview, new FakeDistribution(10), true);
            var differentController = new FakeLevelUpController
            {
                Unit = differentSource,
                Preview = differentPreview,
                State = differentState
            };
            environment.CharacterBuild.LevelUpController = differentController;
            environment.Player.MainCharacter = differentSource;
            CharacterCreationContextDecision decision = environment.Policy.Evaluate(
                differentState,
                differentPreview,
                FakeMode.CharGen,
                environment.Contracts);
            AssertEx.True(decision.Accepted, decision.Reason);

            RollSession ignored;
            string reason;
            bool opened = environment.Sessions.TryOpenOrRebind(
                decision,
                generation => environment.CaptureRollback(differentState, generation),
                out ignored,
                out reason);

            AssertEx.True(!opened);
            AssertEx.True(reason.Contains("different controller/source owner"));
            AssertEx.True(ReferenceEquals(original, environment.Sessions.Active));
        }

        internal static void AssignmentSurvivesThreeGenerations()
        {
            TestEnvironment environment = TestEnvironment.Create();
            int factoryCalls = 0;
            FakeState previewA = environment.NewState(10);
            RollSession session = environment.Open(previewA, () =>
            {
                factoryCalls++;
                return environment.Assignment;
            });

            FakeState previewB = environment.NewReplacementState(previewA, 10);
            session = environment.Rebind(previewB, () =>
            {
                factoryCalls++;
                return new StatAssignment(new RolledStatArray(new[] { 8, 10, 12, 14, 15, 16 }));
            });
            environment.Controller.State = previewB;
            FakeState previewC = environment.NewReplacementState(previewB, 10);
            session = environment.Rebind(previewC, () =>
            {
                factoryCalls++;
                return new StatAssignment(new RolledStatArray(new[] { 8, 8, 8, 8, 8, 8 }));
            });

            AssertEx.Equal(1, factoryCalls);
            AssertEx.True(ReferenceEquals(environment.Assignment, session.Assignment));
            AssertEx.SequenceEqual(FixedValues, session.Assignment.ToAssignedArray());
            AssertEx.Equal(3, session.Generation);
        }

        internal static void RebindReplacesTransientObjectsAndRollbackButPreservesPristine()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(9);
            RollSession session = environment.Open(previewA);
            PointBuyOrigin pristine = session.PointBuyOrigin;
            GenerationRollbackSnapshot firstRollback = session.GenerationRollback;
            FakeState previewB = environment.NewReplacementState(previewA, 11);
            environment.StatAccess.WriteDistributionValues(previewB.StatsDistribution, FixedValues, environment.Contracts);
            environment.StatAccess.WriteUnitBaseValues(previewB.Unit, FixedValues, environment.Contracts);
            session = environment.Rebind(previewB, () => environment.Assignment);

            AssertEx.True(ReferenceEquals(previewB, session.State));
            AssertEx.True(ReferenceEquals(previewB.Unit, session.Unit));
            AssertEx.True(ReferenceEquals(previewB.StatsDistribution, session.Distribution));
            AssertEx.True(ReferenceEquals(pristine, session.PristinePointBuy));
            AssertEx.True(!ReferenceEquals(firstRollback, session.GenerationRollback));
            AssertEx.Equal(2, session.GenerationRollback.Generation);
            AssertEx.SequenceEqual(FixedValues, session.GenerationRollback.Values.UnitValues);
            AssertEx.Equal(25, session.PristinePointBuy.AllocatorBudget);
            AssertEx.SequenceEqual(Enumerable.Repeat(9, 6), session.PristinePointBuy.Values.UnitValues);
            AssertEx.True(session.CandidateBaselineContaminated);
        }

        internal static void FirstPreviewCapturesPristinePointBuyOrigin()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            int[] allocation = { 12, 11, 10, 9, 8, 7 };
            environment.StatAccess.WriteDistributionValues(preview.StatsDistribution, allocation, environment.Contracts);
            environment.StatAccess.WriteUnitBaseValues(preview.Unit, allocation, environment.Contracts);
            preview.StatsDistribution.SetAllocatorState(true, 13, 31);

            RollSession session = environment.Open(preview, 31, "captured non-default budget");

            AssertEx.True(session.PristineBaselineCaptured);
            AssertEx.Equal(1, session.PristinePointBuy.CapturedGeneration);
            AssertEx.Equal(31, session.PristinePointBuy.AllocatorBudget);
            AssertEx.Equal("captured non-default budget", session.PristinePointBuy.BudgetSource);
            AssertEx.SequenceEqual(allocation, session.PristinePointBuy.Values.DistributionValues);
            AssertEx.SequenceEqual(allocation, session.PristinePointBuy.Values.UnitValues);
            AssertEx.True(session.PristinePointBuy.AllocatorAvailable);
            AssertEx.Equal(13, session.PristinePointBuy.RemainingPoints);
            AssertEx.Equal(31, session.PristinePointBuy.TotalPoints);
        }

        internal static void FixedStagingDoesNotMutatePristineOrigin()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = environment.Open(preview);
            string error;

            AssertEx.True(environment.Application.TryStageCurrentGeneration(
                session,
                environment.Contracts,
                out error), error);

            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), session.PristinePointBuy.Values.DistributionValues);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), session.PristinePointBuy.Values.UnitValues);
            AssertEx.SequenceEqual(FixedValues, environment.ReadDistribution(preview));
            AssertEx.SequenceEqual(FixedValues, environment.ReadUnit(preview.Unit));
            AssertEx.True(!preview.StatsDistribution.Available);
            AssertEx.Equal(0, preview.StatsDistribution.Points);
        }

        internal static void SameOwnerRebindNeverRecapturesPristineOrigin()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession session = environment.Open(previewA);
            PointBuyOrigin pristine = session.PointBuyOrigin;
            FakeState previewB = environment.NewReplacementState(previewA, 10);
            environment.StatAccess.WriteDistributionValues(previewB.StatsDistribution, FixedValues, environment.Contracts);
            environment.StatAccess.WriteUnitBaseValues(previewB.Unit, FixedValues, environment.Contracts);
            CharacterCreationContextDecision decision = environment.Evaluate(previewB, FakeMode.CharGen);
            string reason;

            AssertEx.True(environment.Sessions.TryOpenOrRebind(
                decision,
                generation => environment.CaptureRollback(previewB, generation),
                out session,
                out reason), reason);

            AssertEx.True(ReferenceEquals(pristine, session.PristinePointBuy));
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), session.PristinePointBuy.Values.UnitValues);
            AssertEx.SequenceEqual(FixedValues, session.GenerationRollback.Values.UnitValues);
            AssertEx.True(session.CandidateBaselineContaminated);
        }

        internal static void GenerationRollbackChangesIndependentlyFromPristineOrigin()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession session = environment.Open(previewA);
            PointBuyOrigin pristine = session.PointBuyOrigin;
            GenerationRollbackSnapshot rollbackA = session.GenerationRollback;
            FakeState previewB = environment.NewReplacementState(previewA, 11);
            previewB.StatsDistribution.SetAllocatorState(true, 7, 29);

            session = environment.Rebind(previewB);

            AssertEx.True(ReferenceEquals(pristine, session.PristinePointBuy));
            AssertEx.True(!ReferenceEquals(rollbackA, session.GenerationRollback));
            AssertEx.Equal(2, session.GenerationRollback.Generation);
            AssertEx.SequenceEqual(Enumerable.Repeat(11, 6), session.GenerationRollback.Values.DistributionValues);
            AssertEx.Equal(7, session.GenerationRollback.RemainingPoints);
            AssertEx.Equal(29, session.GenerationRollback.TotalPoints);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), pristine.Values.DistributionValues);
        }

        internal static void NestedPreviewRefreshIsRefused()
        {
            TestEnvironment environment = TestEnvironment.Create();
            environment.Controller.OnUpdatePreview = () => environment.PreviewRefresh.Refresh(environment.Contracts);

            TargetInvocationException exception = AssertEx.Throws<TargetInvocationException>(
                () => environment.PreviewRefresh.Refresh(environment.Contracts));

            AssertEx.True(exception.InnerException is InvalidOperationException);
            AssertEx.True(exception.InnerException.Message.Contains("already in progress"));
            AssertEx.True(!environment.PreviewRefresh.IsRefreshInProgress);
            AssertEx.Equal(0, environment.PreviewRefresh.RefreshCount);
            AssertEx.Equal(1, environment.Controller.UpdatePreviewCount);
        }

        internal static void ReentrantReplacementUsesOneRefresh()
        {
            ReentrantResult result = RunReentrantReplacement();
            AssertEx.Equal(1, result.Environment.PreviewRefresh.RefreshCount);
            AssertEx.Equal(1, result.Environment.Controller.UpdatePreviewCount);
            AssertEx.Equal(2, result.Session.Generation);
            AssertEx.Equal(1, result.Session.ApplicationAttempts);
        }

        internal static void FinalLiveReplacementContainsFixedArray()
        {
            ReentrantResult result = RunReentrantReplacement();
            LivePreviewObservation observation;
            string error;
            AssertEx.True(result.Environment.Application.TryMarkLiveVerified(
                result.Session,
                result.Environment.Contracts,
                out observation,
                out error), error);
            AssertEx.True(observation.IsVerified);
            AssertEx.SequenceEqual(FixedValues, result.Environment.ReadDistribution(result.Replacement));
            AssertEx.SequenceEqual(FixedValues, result.Environment.ReadUnit(result.Replacement.Unit));
            AssertEx.True(result.Session.IsApplied);
        }

        internal static void ApplicationDoesNotRequestAnotherRefresh()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession session = environment.Open(previewA);
            string error;
            AssertEx.True(environment.Application.TryStageCurrentGeneration(session, environment.Contracts, out error), error);
            AssertEx.Equal(0, environment.PreviewRefresh.RefreshCount);
            AssertEx.Equal(0, environment.Controller.UpdatePreviewCount);
        }

        internal static void CoordinatorCountsOnlyVerifiedLiveApplication()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            var tracker = new PointBudgetTracker();
            tracker.Record(previewA.StatsDistribution, 25);
            var diagnostics = new RuntimeDiagnostics();
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(tracker, diagnostics);

            coordinator.OnLevelUpStateConstructed(previewA, previewA.Unit, FakeMode.CharGen);
            AssertEx.Equal(0, diagnostics.ArraysApplied);
            AssertEx.Equal(0, environment.PreviewRefresh.RefreshCount);

            coordinator.Update(0.1f);
            AssertEx.Equal(0, diagnostics.ArraysApplied);
            AssertEx.Equal(RollSessionMode.PointBuy, environment.Sessions.Active.Mode);
            string error;
            AssertEx.True(coordinator.TryRoll(out error), error);
            AssertEx.Equal(1, diagnostics.ArraysApplied);
            AssertEx.True(environment.Sessions.Active.IsApplied);
            AssertEx.True(diagnostics.Status.Contains("Roll Mode is active"));
        }

        internal static void DetachedMatchingPreviewCannotVerify()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession session = environment.Open(previewA);
            string error;
            AssertEx.True(environment.Application.TryStageCurrentGeneration(session, environment.Contracts, out error), error);

            FakeState previewB = environment.NewState(10);
            environment.Controller.State = previewB;
            environment.Controller.Preview = previewB.Unit;
            LivePreviewObservation observation = environment.Application.InspectLive(session, environment.Contracts);

            AssertEx.True(!observation.IsVerified);
            AssertEx.True(!observation.CurrentControllerStateMatches);
            AssertEx.True(!observation.CurrentControllerPreviewMatches);
            AssertEx.SequenceEqual(FixedValues, environment.ReadUnit(previewA.Unit));
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadUnit(previewB.Unit));
        }

        internal static void SameOwnerReplacementDoesNotRelease()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            environment.Open(previewA);
            FakeState previewB = environment.NewState(10);
            environment.Controller.State = previewB;
            environment.Controller.Preview = previewB.Unit;

            RollSession released;
            bool didRelease = environment.Sessions.ReleaseIfStableOwnerLost(
                environment.Controller,
                environment.Source,
                true,
                20f,
                out released);

            AssertEx.True(!didRelease);
            AssertEx.True(environment.Sessions.Active != null);
        }

        internal static void NullStateWithSameOwnerDoesNotRelease()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            environment.Open(previewA);
            environment.Controller.State = null;

            RollSession released;
            bool didRelease = environment.Sessions.ReleaseIfStableOwnerLost(
                environment.Controller,
                environment.Source,
                true,
                20f,
                out released);

            AssertEx.True(!didRelease);
            AssertEx.True(environment.Sessions.Active != null);
        }

        internal static void MissingControllerEventuallyReleases()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            environment.Open(previewA);
            RollSession released;
            AssertEx.True(!environment.Sessions.ReleaseIfStableOwnerLost(
                environment.Controller,
                environment.Source,
                true,
                0.1f,
                out released));
            AssertEx.True(environment.Sessions.ReleaseIfStableOwnerLost(
                null,
                null,
                true,
                SessionLivenessTracker.ConfirmedGraceSeconds,
                out released));
            AssertEx.True(released != null);
            AssertEx.Equal(null, environment.Sessions.Active);
        }

        internal static void DifferentControllerEventuallyReleases()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            environment.Open(previewA);
            RollSession released;
            AssertEx.True(!environment.Sessions.ReleaseIfStableOwnerLost(
                environment.Controller,
                environment.Source,
                true,
                0.1f,
                out released));
            AssertEx.True(environment.Sessions.ReleaseIfStableOwnerLost(
                new FakeLevelUpController(),
                FakeUnitDescriptor.Create(10, true),
                true,
                SessionLivenessTracker.ConfirmedGraceSeconds,
                out released));
            AssertEx.True(released != null);
        }

        internal static void PointBuyRestoresNewestPreviewOnly()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession session = environment.Open(previewA);
            string error;
            AssertEx.True(environment.Application.TryStageCurrentGeneration(session, environment.Contracts, out error), error);
            LivePreviewObservation initial;
            AssertEx.True(environment.Application.TryMarkLiveVerified(
                session,
                environment.Contracts,
                out initial,
                out error), error);

            FakeState previewB = null;
            environment.Controller.OnUpdatePreview = () =>
            {
                previewB = environment.NewReplacementState(previewA, 10);
                session = environment.Rebind(previewB);
                environment.Controller.State = previewB;
            };

            PointBuyRestoreObservation restoration;
            AssertEx.True(environment.Restore.TryRestore(
                session,
                environment.Contracts,
                out restoration,
                out error), error);
            AssertEx.True(previewB != null);
            AssertEx.SequenceEqual(FixedValues, environment.ReadUnit(previewA.Unit));
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadUnit(previewB.Unit));
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadDistribution(previewB));
            AssertEx.Equal(25, previewB.StatsDistribution.LastStartBudget);
            AssertEx.Equal(0, previewA.StatsDistribution.StartCalls);
            AssertEx.Equal(RollSessionState.PointBuyRestored, session.Lifecycle.State);
            AssertEx.True(restoration.IsVerified);
            AssertEx.Equal(1, environment.PreviewRefresh.RefreshCount);
        }

        internal static void PointBuyRestoresNonDefaultBudgetAndAllocation()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            int[] pristineValues = { 12, 11, 10, 9, 8, 7 };
            environment.StatAccess.WriteDistributionValues(previewA.StatsDistribution, pristineValues, environment.Contracts);
            environment.StatAccess.WriteUnitBaseValues(previewA.Unit, pristineValues, environment.Contracts);
            previewA.StatsDistribution.SetAllocatorState(true, 13, 31);
            RollSession session = environment.Open(previewA, 31, "captured custom allocator");
            StageAndVerify(environment, session);

            FakeState previewB = null;
            environment.Controller.OnUpdatePreview = () =>
            {
                previewB = environment.NewReplacementState(previewA, 10);
                session = environment.Rebind(previewB);
                environment.Controller.State = previewB;
            };

            PointBuyRestoreObservation observation;
            string error;
            AssertEx.True(environment.Restore.TryRestore(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            AssertEx.SequenceEqual(pristineValues, environment.ReadDistribution(previewB));
            AssertEx.SequenceEqual(pristineValues, environment.ReadUnit(previewB.Unit));
            AssertEx.Equal(31, previewB.StatsDistribution.LastStartBudget);
            AssertEx.True(previewB.StatsDistribution.Available);
            AssertEx.Equal(13, previewB.StatsDistribution.Points);
            AssertEx.Equal(31, previewB.StatsDistribution.TotalPoints);
            AssertEx.True(observation.IsVerified);
            AssertEx.True(!observation.RolledAssignmentStillPresent);
            AssertEx.Equal(RollSessionMode.PointBuy, session.Mode);
        }

        internal static void HybridRolledValuesAndFullBudgetCannotVerify()
        {
            var live = new LivePreviewObservation(
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                null);
            var observation = new PointBuyRestoreObservation(live, true, true, true);

            AssertEx.True(observation.HybridStateDetected);
            AssertEx.True(!observation.IsVerified);
        }

        internal static void ZeroBudgetPristineAssignmentIsNotMisclassifiedAsHybrid()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            environment.StatAccess.WriteDistributionValues(preview.StatsDistribution, FixedValues, environment.Contracts);
            environment.StatAccess.WriteUnitBaseValues(preview.Unit, FixedValues, environment.Contracts);
            preview.StatsDistribution.SetAllocatorState(true, 0, 0);
            RollSession session = environment.Open(preview, 0, "captured zero-point allocator");
            StageAndVerify(environment, session);
            PointBuyRestoreObservation observation;
            string error;

            AssertEx.True(environment.Restore.TryRestore(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            AssertEx.True(observation.RolledAssignmentStillPresent);
            AssertEx.True(!observation.FullAllocatorBudgetAvailable);
            AssertEx.True(!observation.HybridStateDetected);
            AssertEx.True(observation.IsVerified);
            AssertEx.Equal(RollSessionMode.PointBuy, session.Mode);
        }

        internal static void RacialModifiersRemainSeparateFromRestoredBaseValues()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = environment.Open(preview);
            StageAndVerify(environment, session);
            preview.Unit.Stats.SetModifiers(new[] { 0, 2, -2, 2, 0, 0 });

            PointBuyRestoreObservation observation;
            string error;
            AssertEx.True(environment.Restore.TryRestore(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadUnit(preview.Unit));
            AssertEx.SequenceEqual(new[] { 10, 12, 8, 12, 10, 10 }, preview.Unit.Stats.ReadDisplayedValues());
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), session.PristinePointBuy.Values.UnitValues);
            AssertEx.SequenceEqual(FixedValues, session.Assignment.ToAssignedArray());
        }

        internal static void PointBuyModeSurvivesSameOwnerRebuildWithoutRestaging()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            var tracker = new PointBudgetTracker();
            tracker.Record(previewA.StatsDistribution, 25);
            var diagnostics = new RuntimeDiagnostics();
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(tracker, diagnostics);
            coordinator.OnLevelUpStateConstructed(previewA, previewA.Unit, FakeMode.CharGen);
            coordinator.Update(0.1f);
            string error;
            AssertEx.True(coordinator.TryRoll(out error), error);

            FakeState previewB = null;
            environment.Controller.OnUpdatePreview = () =>
            {
                previewB = environment.NewReplacementState(previewA, 10);
                tracker.Record(previewB.StatsDistribution, 25);
                coordinator.OnLevelUpStateConstructed(previewB, previewB.Unit, FakeMode.CharGen);
                environment.Controller.State = previewB;
            };
            AssertEx.True(coordinator.TryRestorePointBuy(out error), error);
            AssertEx.Equal(RollSessionMode.PointBuy, environment.Sessions.Active.Mode);

            environment.Controller.OnUpdatePreview = null;
            FakeState previewC = environment.NewReplacementState(previewB, 10);
            tracker.Record(previewC.StatsDistribution, 25);
            coordinator.OnLevelUpStateConstructed(previewC, previewC.Unit, FakeMode.CharGen);
            environment.Controller.State = previewC;
            coordinator.Update(0.1f);

            AssertEx.Equal(3, environment.Sessions.Active.Generation);
            AssertEx.Equal(RollSessionMode.PointBuy, environment.Sessions.Active.Mode);
            AssertEx.True(environment.Sessions.Active.RollSuppressedForStableOwner);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadDistribution(previewC));
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadUnit(previewC.Unit));
            AssertEx.True(previewC.StatsDistribution.Available);
            AssertEx.Equal(25, previewC.StatsDistribution.Points);
        }

        internal static void PointBuyModeDoesNotForceCompletionOrAllocatorRestart()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = environment.Open(preview);
            StageAndVerify(environment, session);
            PointBuyRestoreObservation observation;
            string error;
            AssertEx.True(environment.Restore.TryRestore(
                session,
                environment.Contracts,
                out observation,
                out error), error);
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator();

            bool isComplete = false;
            coordinator.OnDistributionIsComplete(preview.StatsDistribution, ref isComplete);
            preview.StatsDistribution.Start(37);
            coordinator.OnDistributionStarted(preview.StatsDistribution, 37);
            preview.StatsDistribution.StatValues[0] = 12;
            preview.Unit.Stats.GetStat(0).BaseValue = 12;
            coordinator.Update(0.1f);

            AssertEx.True(!isComplete);
            AssertEx.True(preview.StatsDistribution.Available);
            AssertEx.Equal(37, preview.StatsDistribution.Points);
            AssertEx.Equal(12, environment.ReadDistribution(preview)[0]);
            AssertEx.Equal(12, environment.ReadUnit(preview.Unit)[0]);
            AssertEx.Equal(RollSessionMode.PointBuy, session.Mode);
        }

        internal static void DisableDuringRollRestoresBeforeClearingOwnership()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = environment.Open(preview);
            StageAndVerify(environment, session);
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator();
            string error;

            AssertEx.True(coordinator.TryPrepareDisable(out error), error);

            AssertEx.Equal(null, environment.Sessions.Active);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadDistribution(preview));
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadUnit(preview.Unit));
            AssertEx.True(preview.StatsDistribution.Available);
            AssertEx.Equal(25, preview.StatsDistribution.Points);
            AssertEx.Equal(RollSessionState.PointBuyRestored, session.Lifecycle.State);
        }

        internal static void FailedRestorationRollsBackToIsolatedRollMode()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = environment.Open(preview);
            StageAndVerify(environment, session);
            preview.StatsDistribution.ThrowAfterStart = true;
            PointBuyRestoreObservation observation;
            string error;

            AssertEx.True(!environment.Restore.TryRestore(
                session,
                environment.Contracts,
                out observation,
                out error));

            AssertEx.True(error != null);
            AssertEx.Equal(RollSessionMode.Roll, session.Mode);
            AssertEx.SequenceEqual(FixedValues, environment.ReadDistribution(preview));
            AssertEx.SequenceEqual(FixedValues, environment.ReadUnit(preview.Unit));
            AssertEx.True(!preview.StatsDistribution.Available);
            AssertEx.Equal(0, preview.StatsDistribution.Points);
        }

        internal static void FailedRollbackRefusesUnsafeDisable()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = environment.Open(preview);
            StageAndVerify(environment, session);
            preview.StatsDistribution.OnStart = budget =>
            {
                environment.Controller.Preview = FakeUnitDescriptor.Create(10, false);
                throw new InvalidOperationException("simulated allocator failure after detachment");
            };
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator();
            string error;

            AssertEx.True(!coordinator.TryPrepareDisable(out error));

            AssertEx.True(error != null);
            AssertEx.True(ReferenceEquals(session, environment.Sessions.Active));
            AssertEx.Equal(RollSessionMode.RestoringPointBuy, session.Mode);
            AssertEx.True(environment.Logger.Messages.Any(message => message.Contains("Rollback failed point-buy restoration")));
        }

        internal static void PointBuyModeCancellationReleasesAndNewOwnerCanOpen()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession first = environment.Open(preview);
            StageAndVerify(environment, first);
            PointBuyRestoreObservation observation;
            string error;
            AssertEx.True(environment.Restore.TryRestore(
                first,
                environment.Contracts,
                out observation,
                out error), error);
            RollSession released;
            AssertEx.True(!environment.Sessions.ReleaseIfStableOwnerLost(
                environment.Controller,
                environment.Source,
                true,
                0.1f,
                out released));
            AssertEx.True(environment.Sessions.ReleaseIfStableOwnerLost(
                null,
                null,
                true,
                SessionLivenessTracker.ConfirmedGraceSeconds,
                out released));
            AssertEx.Equal(RollSessionState.Abandoned, first.Lifecycle.State);

            environment.ReplaceStableOwner();
            FakeState nextPreview = environment.NewState(10);
            RollSession second = environment.Open(nextPreview);

            AssertEx.True(!ReferenceEquals(first, second));
            AssertEx.Equal(RollSessionMode.Roll, second.Mode);
            AssertEx.SequenceEqual(FixedValues, second.Assignment.ToAssignedArray());
        }

        internal static void RestorationDiagnosticsExposePristineTransition()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            var tracker = new PointBudgetTracker();
            tracker.Record(preview.StatsDistribution, 25);
            var diagnostics = new RuntimeDiagnostics();
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(tracker, diagnostics);
            coordinator.OnLevelUpStateConstructed(preview, preview.Unit, FakeMode.CharGen);
            coordinator.Update(0.1f);
            string error;
            AssertEx.True(coordinator.TryRoll(out error), error);

            AssertEx.True(coordinator.TryRestorePointBuy(out error), error);

            string text = string.Join("\n", diagnostics.SnapshotRecent());
            AssertEx.True(text.Contains("RESTORE Pristine point-buy model and active ability-page presentation verified"));
            AssertEx.True(text.Contains("pointBuyOriginCaptured=true"));
            AssertEx.True(text.Contains("pristineBaselineGeneration=1"));
            AssertEx.True(text.Contains("mode=PointBuy"));
            AssertEx.True(text.Contains("liveDistributionMatchesPristine=true"));
            AssertEx.True(text.Contains("liveUnitMatchesPristine=true"));
            AssertEx.True(text.Contains("rollSuppressedForStableOwner=true"));
            AssertEx.True(text.Contains("semanticPointBuyVerified=true"));
            AssertEx.True(text.Contains("presentationRefreshCount=1"));
        }

        internal static void SemanticRestoreWithoutPresentationIsNotSynchronized()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RollSession session = RestoreSemantically(environment, environment.NewState(10));
            environment.CharacterBuild.CurrentPhase = FakePhaseType.Race;

            PointBuyPresentationObservation observation;
            string error;
            AssertEx.True(!environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error));

            AssertEx.True(observation.SemanticPointBuyVerified);
            AssertEx.True(!observation.ActiveAbilityPhaseFound);
            AssertEx.True(!observation.IsSynchronized);
            AssertEx.Equal(0, observation.PresentationRefreshCount);
        }

        internal static void NativeAbilityRefreshRunsAfterPristineWrites()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RollSession session = RestoreSemantically(environment, environment.NewState(10));
            bool observedPristine = false;
            environment.Allocator.OnFill = () =>
            {
                observedPristine = environment.ReadDistribution(session.State as FakeState)
                    .SequenceEqual(Enumerable.Repeat(10, 6)) &&
                    environment.ReadUnit(session.Unit as FakeUnitDescriptor)
                    .SequenceEqual(Enumerable.Repeat(10, 6)) &&
                    ((FakeDistribution)session.Distribution).Points == 25;
            };

            PointBuyPresentationObservation observation;
            string error;
            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            AssertEx.True(observedPristine);
            AssertEx.True(observation.PresentationRefreshRequested);
            AssertEx.Equal(AbilityPhasePresentationService.NativeRefreshMethod, observation.PresentationRefreshMethod);
        }

        internal static void PresentationRefreshIsBoundedPerGeneration()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RollSession session = RestoreSemantically(environment, environment.NewState(10));
            PointBuyPresentationObservation first;
            PointBuyPresentationObservation second;
            string error;

            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out first,
                out error), error);
            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out second,
                out error), error);

            AssertEx.Equal(1, environment.Allocator.FillDataCalls);
            AssertEx.Equal(1, environment.Presentation.TotalRefreshCount);
            AssertEx.Equal(1, second.PresentationRefreshCount);
        }

        internal static void PresentationRefreshCannotReenterRollMode()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RollSession session = RestoreSemantically(environment, environment.NewState(10));
            RollSessionMode observedMode = RollSessionMode.Roll;
            environment.Allocator.OnFill = () => observedMode = session.Mode;

            PointBuyPresentationObservation observation;
            string error;
            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            AssertEx.Equal(RollSessionMode.PointBuy, observedMode);
            AssertEx.Equal(RollSessionMode.PointBuy, session.Mode);
        }

        internal static void SameOwnerReplacementDuringPresentationStaysSuppressed()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession session = RestoreSemantically(environment, previewA);
            FakeState previewB = null;
            environment.Allocator.OnFill = () =>
            {
                previewB = environment.NewReplacementState(previewA, 10);
                session = environment.Rebind(previewB);
                environment.Controller.State = previewB;
            };

            PointBuyPresentationObservation observation;
            string error;
            AssertEx.True(!environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error));

            AssertEx.Equal(RollSessionMode.PointBuy, session.Mode);
            AssertEx.True(session.RollSuppressedForStableOwner);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadDistribution(previewB));
            AssertEx.True(!environment.ReadDistribution(previewB).SequenceEqual(FixedValues));
        }

        internal static void FixedAssignmentIsNotRestagedByPresentation()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = RestoreSemantically(environment, preview);
            PointBuyPresentationObservation observation;
            string error;

            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadDistribution(preview));
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadUnit(preview.Unit));
            AssertEx.True(!environment.ReadDistribution(preview).SequenceEqual(session.Assignment.ToAssignedArray()));
        }

        internal static void PostRefreshLiveStateRemainsPristine()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = RestoreSemantically(environment, preview);
            PointBuyPresentationObservation observation;
            string error;

            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            AssertEx.True(observation.PostRefreshLiveModelVerified);
            AssertEx.Equal(session.Generation, observation.PostRefreshGeneration);
            AssertEx.SequenceEqual(session.PristinePointBuy.Values.DistributionValues, environment.ReadDistribution(preview));
            AssertEx.SequenceEqual(session.PristinePointBuy.Values.UnitValues, environment.ReadUnit(preview.Unit));
        }

        internal static void PostRefreshAllocatorKeepsObservedBudget()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            preview.StatsDistribution.SetAllocatorState(true, 17, 37);
            RollSession session = environment.Open(preview, 37, "observed custom budget");
            StageAndVerify(environment, session);
            PointBuyRestoreObservation semantic;
            PointBuyPresentationObservation presentation;
            string error;
            AssertEx.True(environment.Restore.TryRestore(session, environment.Contracts, out semantic, out error), error);
            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out presentation,
                out error), error);

            AssertEx.Equal(17, environment.Allocator.DisplayedPoints);
            AssertEx.Equal(37, preview.StatsDistribution.TotalPoints);
            AssertEx.Equal(37, preview.StatsDistribution.LastStartBudget);
            AssertEx.Equal(37, session.PristinePointBuy.AllocatorBudget);
        }

        internal static void PresentationBindsCurrentStateAndDistribution()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = RestoreSemantically(environment, preview);
            PointBuyPresentationObservation observation;
            string error;

            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            AssertEx.True(ReferenceEquals(session.State, environment.Allocator.BoundState));
            AssertEx.True(ReferenceEquals(session.Distribution, environment.Allocator.BoundDistribution));
            AssertEx.True(observation.AbilityPhaseStateMatchesSession);
            AssertEx.True(observation.AbilityPhaseDistributionMatchesSession);
            AssertEx.True(observation.AbilityPhaseViewModelMatchesSession);
        }

        internal static void HumanPresentationImmediatelyShowsPristinePointBuy()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RollSession session = RestoreSemantically(environment, environment.NewState(10));
            PointBuyPresentationObservation observation;
            string error;
            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.Allocator.DisplayedValues);
            AssertEx.Equal(25, environment.Allocator.DisplayedPoints);
            AssertEx.True(environment.Allocator.NativeControlsAvailable);
        }

        internal static void RaceModifiersRemainSeparateInImmediatePresentation()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = RestoreSemantically(environment, preview);
            preview.Unit.Stats.SetModifiers(new[] { 2, 0, 0, 0, 2, -2 });
            PointBuyPresentationObservation observation;
            string error;
            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadUnit(preview.Unit));
            AssertEx.SequenceEqual(new[] { 12, 10, 10, 10, 12, 8 }, environment.Allocator.DisplayedValues);
        }

        internal static void NonDefaultBudgetReachesImmediatePresentation()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            preview.StatsDistribution.SetAllocatorState(true, 31, 31);
            RollSession session = environment.Open(preview, 31, "Bag of Tricks test budget");
            StageAndVerify(environment, session);
            PointBuyRestoreObservation semantic;
            PointBuyPresentationObservation presentation;
            string error;
            AssertEx.True(environment.Restore.TryRestore(session, environment.Contracts, out semantic, out error), error);
            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out presentation,
                out error), error);

            AssertEx.Equal(31, environment.Allocator.DisplayedPoints);
            AssertEx.Equal(31, preview.StatsDistribution.LastStartBudget);
        }

        internal static void NavigationAfterPresentationStaysInPointBuy()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession session = RestoreSemantically(environment, previewA);
            PointBuyPresentationObservation observation;
            string error;
            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            FakeState previewB = environment.NewReplacementState(previewA, 10);
            session = environment.Rebind(previewB);
            environment.Controller.State = previewB;

            AssertEx.Equal(RollSessionMode.PointBuy, session.Mode);
            AssertEx.True(session.RollSuppressedForStableOwner);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadDistribution(previewB));
        }

        internal static void PresentationFailurePreservesSafePointBuy()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = RestoreSemantically(environment, preview);
            environment.Allocator.ThrowOnFill = true;
            PointBuyPresentationObservation observation;
            string error;

            AssertEx.True(!environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error));

            AssertEx.Equal(RollSessionMode.PointBuy, session.Mode);
            AssertEx.True(session.RollSuppressedForStableOwner);
            AssertEx.True(observation.SemanticPointBuyVerified);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadDistribution(preview));
        }

        internal static void PresentationFailureNeverRollsBackToFixedArray()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = RestoreSemantically(environment, preview);
            environment.Allocator.ThrowOnFill = true;
            PointBuyPresentationObservation observation;
            string error;

            environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error);

            AssertEx.True(!environment.ReadDistribution(preview).SequenceEqual(FixedValues));
            AssertEx.True(!environment.ReadUnit(preview.Unit).SequenceEqual(FixedValues));
            AssertEx.True(preview.StatsDistribution.Available);
            AssertEx.Equal(25, preview.StatsDistribution.Points);
        }

        internal static void DisableAfterSemanticRestorationRemainsSafe()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = RestoreSemantically(environment, preview);
            environment.Allocator.ThrowOnFill = true;
            PointBuyPresentationObservation observation;
            string ignored;
            environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out ignored);
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator();

            AssertEx.True(coordinator.TryPrepareDisable(out ignored), ignored);
            AssertEx.Equal(null, environment.Sessions.Active);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadDistribution(preview));
        }

        internal static void DisableDuringRollSynchronizesBeforeClear()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            RollSession session = environment.Open(preview);
            StageAndVerify(environment, session);
            bool observedPointBuyBeforeClear = false;
            environment.Allocator.OnFill = () =>
                observedPointBuyBeforeClear = environment.Sessions.Active != null &&
                    environment.Sessions.Active.IsPointBuyMode;
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator();
            string error;

            AssertEx.True(coordinator.TryPrepareDisable(out error), error);

            AssertEx.True(observedPointBuyBeforeClear);
            AssertEx.Equal(1, environment.Allocator.FillDataCalls);
            AssertEx.Equal(null, environment.Sessions.Active);
        }

        internal static void PresentationRefreshDoesNotRebuildPreview()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RollSession session = RestoreSemantically(environment, environment.NewState(10));
            int semanticRefreshCount = environment.PreviewRefresh.RefreshCount;
            int generation = session.Generation;
            PointBuyPresentationObservation observation;
            string error;
            AssertEx.True(environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error), error);

            AssertEx.Equal(semanticRefreshCount, environment.PreviewRefresh.RefreshCount);
            AssertEx.Equal(generation, session.Generation);
            AssertEx.Equal(1, observation.PresentationRefreshCount);
        }

        internal static void InactiveAbilityPhaseCannotClaimSynchronization()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RollSession session = RestoreSemantically(environment, environment.NewState(10));
            environment.CharacterBuild.CurrentPhase = FakePhaseType.Portrait;
            PointBuyPresentationObservation observation;
            string error;

            AssertEx.True(!environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error));

            AssertEx.True(!observation.ActiveAbilityPhaseFound);
            AssertEx.True(!observation.PresentationRefreshRequested);
            AssertEx.Equal(0, environment.Allocator.FillDataCalls);
        }

        internal static void DiagnosticsSeparateSemanticAndPresentationFailure()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            var tracker = new PointBudgetTracker();
            tracker.Record(preview.StatsDistribution, 25);
            var diagnostics = new RuntimeDiagnostics();
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(tracker, diagnostics);
            coordinator.OnLevelUpStateConstructed(preview, preview.Unit, FakeMode.CharGen);
            coordinator.Update(0.1f);
            string error;
            AssertEx.True(coordinator.TryRoll(out error), error);
            environment.Allocator.ThrowOnFill = true;

            AssertEx.True(coordinator.TryRestorePointBuy(out error), error);

            string text = string.Join("\n", diagnostics.SnapshotRecent());
            AssertEx.True(text.Contains("Pristine point-buy model is verified and durable"));
            AssertEx.True(text.Contains("presentation synchronization failed"));
            AssertEx.True(text.Contains("semanticPointBuyVerified=true"));
            AssertEx.Equal(RollSessionMode.PointBuy, environment.Sessions.Active.Mode);
        }

        internal static void DiagnosticsReportNativePresentationVerification()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState preview = environment.NewState(10);
            var tracker = new PointBudgetTracker();
            tracker.Record(preview.StatsDistribution, 25);
            var diagnostics = new RuntimeDiagnostics();
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(tracker, diagnostics);
            coordinator.OnLevelUpStateConstructed(preview, preview.Unit, FakeMode.CharGen);
            coordinator.Update(0.1f);
            string error;
            AssertEx.True(coordinator.TryRoll(out error), error);

            AssertEx.True(coordinator.TryRestorePointBuy(out error), error);

            string text = string.Join("\n", diagnostics.SnapshotRecent());
            AssertEx.True(text.Contains("RESTORE Pristine point-buy model and active ability-page presentation verified"));
            AssertEx.True(text.Contains("presentationRefreshRequested=true"));
            AssertEx.True(text.Contains("presentationRefreshCount=1"));
            AssertEx.True(text.Contains("abilityPhaseViewModelMatchesSession=true"));
            AssertEx.True(text.Contains("postRefreshLiveModelVerified=true"));
        }

        internal static void ViewBindingMismatchCannotClaimSynchronization()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RollSession session = RestoreSemantically(environment, environment.NewState(10));
            environment.Allocator.OnFill = () =>
                environment.Allocator.m_PreviewUnit = FakeUnitDescriptor.Create(10, false).Unit;
            PointBuyPresentationObservation observation;
            string error;

            AssertEx.True(!environment.Presentation.TrySynchronize(
                session,
                environment.Contracts,
                out observation,
                out error));

            AssertEx.True(observation.SemanticPointBuyVerified);
            AssertEx.True(!observation.AbilityPhasePreviewMatchesSession);
            AssertEx.True(!observation.AbilityPhaseViewModelMatchesSession);
            AssertEx.True(!observation.IsSynchronized);
        }

        internal static void CompletionUsesCurrentLiveDistributionOnly()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession session = environment.Open(previewA);
            string error;
            AssertEx.True(environment.Application.TryStageCurrentGeneration(session, environment.Contracts, out error), error);
            LivePreviewObservation ignored;
            AssertEx.True(environment.Application.TryMarkLiveVerified(session, environment.Contracts, out ignored, out error), error);

            FakeState previewB = environment.NewReplacementState(previewA, 10);
            session = environment.Rebind(previewB);
            environment.Controller.State = previewB;
            AssertEx.True(environment.Application.TryStageCurrentGeneration(session, environment.Contracts, out error), error);
            AssertEx.True(environment.Application.TryMarkLiveVerified(session, environment.Contracts, out ignored, out error), error);

            CharacterCreationCoordinator coordinator = environment.CreateCoordinator();
            bool detachedResult = false;
            coordinator.OnDistributionIsComplete(previewA.StatsDistribution, ref detachedResult);
            bool currentResult = false;
            coordinator.OnDistributionIsComplete(previewB.StatsDistribution, ref currentResult);

            AssertEx.True(!detachedResult);
            AssertEx.True(currentResult);
        }

        internal static void ExistingAndSpecialCreationPathsRemainExcluded()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState state = environment.NewState(10);

            state.IsFirstLevel = false;
            AssertEx.True(!environment.Evaluate(state, FakeMode.LevelUp).Accepted);
            state.IsFirstLevel = true;
            AssertEx.True(!environment.Evaluate(state, FakeMode.PreGen).Accepted);
            AssertEx.True(!environment.Evaluate(state, FakeMode.Respec).Accepted);

            state.Unit.IsPet = true;
            AssertEx.True(!environment.Evaluate(state, FakeMode.CharGen).Accepted);
            state.Unit.IsPet = false;
            state.Unit.IsPlayersEnemy = true;
            AssertEx.True(!environment.Evaluate(state, FakeMode.CharGen).Accepted);
            state.Unit.IsPlayersEnemy = false;

            // Companion/mercenary-style first-level creation retains another campaign main.
            environment.Player.MainCharacter = FakeUnitDescriptor.Create(10, true);
            AssertEx.True(!environment.Evaluate(state, FakeMode.CharGen).Accepted);
        }

        internal static void DiagnosticsDistinguishPreviewLifecycle()
        {
            var diagnostics = new RuntimeDiagnostics();
            diagnostics.Accepted("opened stable-owner session");
            AssertEx.True(diagnostics.Event("same-owner rebind generation=2"));
            AssertEx.True(diagnostics.Event("deferred/reentrant replacement observed"));
            AssertEx.True(!diagnostics.Event("deferred/reentrant replacement observed"));
            diagnostics.Applied("live controller preview verified");
            diagnostics.Released("stable controller/source owner disappeared");

            string text = string.Join("\n", diagnostics.SnapshotRecent());
            AssertEx.True(text.Contains("ACCEPT opened"));
            AssertEx.True(text.Contains("same-owner rebind"));
            AssertEx.True(text.Contains("deferred/reentrant replacement"));
            AssertEx.True(text.Contains("APPLY live controller preview verified"));
            AssertEx.True(text.Contains("RELEASE stable controller/source"));
            AssertEx.Equal(1, diagnostics.AcceptedContexts);
            AssertEx.Equal(1, diagnostics.ArraysApplied);
            AssertEx.Equal(1, diagnostics.SessionsReleased);
        }

        internal static void ExplicitCoordinatorSessionConsumesNoRandomUntilRoll()
        {
            TestEnvironment environment = TestEnvironment.Create();
            var random = new SequenceRandomSource(
                6, 5, 4, 1, 6, 5, 4, 1, 6, 5, 4, 1,
                6, 5, 4, 1, 6, 5, 4, 1, 6, 5, 4, 1);
            var workflow = NewProductWorkflow(random, RollConfiguration.Default());
            var tracker = new PointBudgetTracker();
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(
                tracker,
                new RuntimeDiagnostics(),
                workflow);
            FakeState state = environment.NewState(10);
            coordinator.OnDistributionStarted(state.StatsDistribution, 25);
            coordinator.OnLevelUpStateConstructed(state, state.Unit, FakeMode.CharGen);

            AssertEx.Equal(0, random.Calls);
            AssertEx.Equal(RollSessionMode.PointBuy, coordinator.ActiveSession.Mode);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            AssertEx.Equal(24, random.Calls);
            AssertEx.Equal(RollSessionMode.Roll, coordinator.ActiveSession.Mode);
        }

        internal static void ExplicitRollRestoresModifiedPreRollAllocation()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out FakeState state);
            int[] origin = { 12, 10, 10, 10, 10, 10 };
            environment.StatAccess.WriteDistributionValues(state.StatsDistribution, origin, environment.Contracts);
            environment.StatAccess.WriteUnitBaseValues(state.Unit, origin, environment.Contracts);
            state.StatsDistribution.SetAllocatorState(true, 22, 25);

            AssertEx.True(coordinator.TryRoll(out string error), error);
            AssertEx.True(coordinator.TryRestorePointBuy(out error), error);

            AssertEx.SequenceEqual(origin, environment.ReadDistribution(state));
            AssertEx.SequenceEqual(origin, environment.ReadUnit(state.Unit));
            AssertEx.Equal(22, state.StatsDistribution.Points);
            AssertEx.Equal(25, state.StatsDistribution.TotalPoints);
        }

        internal static void SecondRollTransitionCapturesNewPointBuyOrigin()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 48).ToArray()),
                out FakeState state);
            AssertEx.True(coordinator.TryRoll(out string error), error);
            AssertEx.True(coordinator.TryRestorePointBuy(out error), error);

            int[] secondOrigin = { 10, 11, 10, 10, 10, 10 };
            environment.StatAccess.WriteDistributionValues(state.StatsDistribution, secondOrigin, environment.Contracts);
            environment.StatAccess.WriteUnitBaseValues(state.Unit, secondOrigin, environment.Contracts);
            state.StatsDistribution.SetAllocatorState(true, 24, 25);
            AssertEx.True(coordinator.TryRoll(out error), error);
            AssertEx.True(coordinator.TryRestorePointBuy(out error), error);

            AssertEx.SequenceEqual(secondOrigin, environment.ReadDistribution(state));
            AssertEx.Equal(24, state.StatsDistribution.Points);
        }

        internal static void InvalidExplicitRollLeavesPointBuyUntouched()
        {
            TestEnvironment environment = TestEnvironment.Create();
            var random = new SequenceRandomSource(6);
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                random,
                out FakeState state,
                new RollConfiguration(
                    DiceRollPreset.CustomExpression,
                    LowScorePolicy.Tabletop,
                    3,
                    "not valid"));
            AssertEx.True(!coordinator.TryRoll(out string error));
            AssertEx.True(!string.IsNullOrWhiteSpace(error));
            AssertEx.Equal(0, random.Calls);
            AssertEx.Equal(RollSessionMode.PointBuy, coordinator.ActiveSession.Mode);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadDistribution(state));
            AssertEx.True(state.StatsDistribution.Available);
        }

        internal static void NativePanelEligibilityRequiresCurrentLiveOwnerBinding()
        {
            TestEnvironment environment = TestEnvironment.Create();
            CharacterCreationCoordinator coordinator = OpenProductCoordinator(
                environment,
                new SequenceRandomSource(Enumerable.Repeat(6, 24).ToArray()),
                out FakeState state);
            AssertEx.True(state != null);
            AssertEx.True(coordinator.CanAttachNativePanel);

            environment.Controller.State = new FakeState(
                FakeUnitDescriptor.Create(10, false),
                new FakeDistribution(10),
                true);
            AssertEx.True(!coordinator.CanAttachNativePanel);
        }

        internal static void NativePointBuyControlsAreSuppressedInRollMode()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RollSession session = environment.Open(environment.NewState(10));
            foreach (FakeScoreEntry entry in environment.Allocator.m_StatEntries)
            {
                entry.UpButton.interactable = true;
                entry.DownButton.interactable = true;
            }

            string error;
            AssertEx.True(environment.NativeControls.TrySuppressForRoll(
                session,
                environment.Contracts,
                out error), error);
            AssertEx.True(environment.NativeControls.AreSuppressed(environment.Allocator, environment.Contracts));
        }

        internal static void NativePointBuyControlOriginalStatesAreRestored()
        {
            TestEnvironment environment = TestEnvironment.Create();
            RollSession session = environment.Open(environment.NewState(10));
            for (int index = 0; index < environment.Allocator.m_StatEntries.Count; index++)
            {
                environment.Allocator.m_StatEntries[index].UpButton.interactable = index % 2 == 0;
                environment.Allocator.m_StatEntries[index].DownButton.interactable = index % 2 != 0;
            }

            string error;
            AssertEx.True(environment.NativeControls.TrySuppressForRoll(
                session,
                environment.Contracts,
                out error), error);
            environment.NativeControls.RestoreOwnedStates(environment.Contracts);

            for (int index = 0; index < environment.Allocator.m_StatEntries.Count; index++)
            {
                AssertEx.Equal(index % 2 == 0, environment.Allocator.m_StatEntries[index].UpButton.interactable);
                AssertEx.Equal(index % 2 != 0, environment.Allocator.m_StatEntries[index].DownButton.interactable);
            }
        }

        private static void StageAndVerify(TestEnvironment environment, RollSession session)
        {
            string error;
            AssertEx.True(environment.Application.TryStageCurrentGeneration(
                session,
                environment.Contracts,
                out error), error);
            LivePreviewObservation observation;
            AssertEx.True(environment.Application.TryMarkLiveVerified(
                session,
                environment.Contracts,
                out observation,
                out error), error);
        }

        private static CharacterRollWorkflow NewProductWorkflow(
            IRandomSource random,
            RollConfiguration configuration)
        {
            return new CharacterRollWorkflow(
                new DiceRollEngine(new DiceExpressionParser(), random),
                new PointBuyEquivalentCalculator(),
                configuration,
                null,
                () => "2026-08-22T00:00:00Z",
                null);
        }

        private static CharacterCreationCoordinator OpenProductCoordinator(
            TestEnvironment environment,
            IRandomSource random,
            out FakeState state,
            RollConfiguration configuration = null)
        {
            var tracker = new PointBudgetTracker();
            CharacterCreationCoordinator coordinator = environment.CreateCoordinator(
                tracker,
                new RuntimeDiagnostics(),
                NewProductWorkflow(random, configuration ?? RollConfiguration.Default()));
            state = environment.NewState(10);
            coordinator.OnDistributionStarted(state.StatsDistribution, 25);
            coordinator.OnLevelUpStateConstructed(state, state.Unit, FakeMode.CharGen);
            return coordinator;
        }

        private static RollSession RestoreSemantically(TestEnvironment environment, FakeState preview)
        {
            RollSession session = environment.Open(preview);
            StageAndVerify(environment, session);
            PointBuyRestoreObservation observation;
            string error;
            AssertEx.True(environment.Restore.TryRestore(
                session,
                environment.Contracts,
                out observation,
                out error), error);
            AssertEx.True(observation.IsVerified);
            AssertEx.Equal(RollSessionMode.PointBuy, session.Mode);
            return session;
        }

        private static ReentrantResult RunReentrantReplacement()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(10);
            RollSession session = environment.Open(previewA);
            string error;
            AssertEx.True(environment.Application.TryStageCurrentGeneration(session, environment.Contracts, out error), error);
            FakeState previewB = null;
            environment.Controller.OnUpdatePreview = () =>
            {
                previewB = environment.NewReplacementState(previewA, 10);
                session = environment.Rebind(previewB);
                AssertEx.True(environment.Application.TryStageCurrentGeneration(session, environment.Contracts, out error), error);
                environment.Controller.State = previewB;
            };

            environment.PreviewRefresh.Refresh(environment.Contracts);
            return new ReentrantResult(environment, session, previewB);
        }

        private sealed class ReentrantResult
        {
            internal ReentrantResult(TestEnvironment environment, RollSession session, FakeState replacement)
            {
                Environment = environment;
                Session = session;
                Replacement = replacement;
            }

            internal TestEnvironment Environment { get; }
            internal RollSession Session { get; }
            internal FakeState Replacement { get; }
        }

        private sealed class TestEnvironment
        {
            private TestEnvironment()
            {
                Contracts = CreateContracts();
                Policy = new CharacterCreationContextPolicy();
                Sessions = new RollSessionManager();
                StatAccess = new KingmakerStatAccess();
                PreviewRefresh = new PreviewRefreshService();
                LivePreview = new LivePreviewInspector(StatAccess);
                Logger = new FakeLogger();
                NativeControls = new NativeAbilityControlService(Logger);
                Application = new StatApplicationService(StatAccess, LivePreview, PreviewRefresh, Logger);
                Restore = new PointBuyRestoreService(StatAccess, LivePreview, PreviewRefresh, Logger);
                Presentation = new AbilityPhasePresentationService(LivePreview, Logger, NativeControls);
                Assignment = new StatAssignment(DiagnosticArrays.FixedPhaseTwoArray());
            }

            internal KingmakerContracts Contracts { get; }
            internal CharacterCreationContextPolicy Policy { get; }
            internal RollSessionManager Sessions { get; }
            internal KingmakerStatAccess StatAccess { get; }
            internal PreviewRefreshService PreviewRefresh { get; }
            internal LivePreviewInspector LivePreview { get; }
            internal FakeLogger Logger { get; }
            internal NativeAbilityControlService NativeControls { get; }
            internal StatApplicationService Application { get; }
            internal PointBuyRestoreService Restore { get; }
            internal AbilityPhasePresentationService Presentation { get; }
            internal StatAssignment Assignment { get; }
            internal FakeUnitDescriptor Source { get; private set; }
            internal FakeLevelUpController Controller { get; private set; }
            internal FakeCharacterBuildController CharacterBuild { get; private set; }
            internal FakePlayer Player { get; private set; }
            internal FakeAbilityScoresAllocator Allocator => CharacterBuild.Skills.AbilityScoresAllocator;

            internal static TestEnvironment Create()
            {
                var environment = new TestEnvironment();
                environment.Source = FakeUnitDescriptor.Create(10, true);
                environment.Controller = new FakeLevelUpController
                {
                    Unit = environment.Source,
                    m_RecalculatePreview = false
                };
                environment.CharacterBuild = new FakeCharacterBuildController
                {
                    LevelUpController = environment.Controller,
                    CurrentPhase = FakePhaseType.Skills,
                    Skills = new FakeAbilityPhase
                    {
                        AbilityScoresAllocator = new FakeAbilityScoresAllocator()
                    }
                };
                environment.Player = new FakePlayer { MainCharacter = environment.Source };
                FakeGame.Instance = new FakeGame
                {
                    UI = new FakeUi { CharacterBuildController = environment.CharacterBuild },
                    Player = environment.Player
                };
                return environment;
            }

            internal FakeState NewState(int value)
            {
                var state = new FakeState(
                    FakeUnitDescriptor.Create(value, false),
                    new FakeDistribution(value),
                    true);
                Controller.State = state;
                Controller.Preview = state.Unit;
                return state;
            }

            internal FakeState NewReplacementState(FakeState previous, int value)
            {
                var state = new FakeState(
                    FakeUnitDescriptor.Create(value, false),
                    new FakeDistribution(value),
                    true);
                Controller.State = previous;
                Controller.Preview = state.Unit;
                return state;
            }

            internal CharacterCreationContextDecision Evaluate(FakeState state, FakeMode mode)
            {
                Controller.Preview = state.Unit;
                return Policy.Evaluate(state, state.Unit, mode, Contracts);
            }

            internal RollSession Open(FakeState state)
            {
                return Open(state, () => Assignment);
            }

            internal RollSession Open(FakeState state, int budget, string budgetSource)
            {
                return Open(state, budget, budgetSource, () => Assignment);
            }

            internal RollSession Open(FakeState state, Func<StatAssignment> factory)
            {
                return Open(state, 25, "test budget", factory);
            }

            internal RollSession Open(
                FakeState state,
                int budget,
                string budgetSource,
                Func<StatAssignment> factory)
            {
                Controller.State = state;
                Controller.Preview = state.Unit;
                CharacterCreationContextDecision decision = Evaluate(state, FakeMode.CharGen);
                AssertEx.True(decision.Accepted, decision.Reason);
                RollSession session;
                string reason;
                AssertEx.True(Sessions.TryOpenOrRebind(
                    decision,
                    generation => CaptureRollback(state, generation),
                    out session,
                    out reason), reason);
                if (session.IsPointBuyMode && session.Assignment == null)
                {
                    StatAssignment assignment = factory();
                    session.BeginRollMode(CaptureOrigin(state, budget, budgetSource, 1), assignment);
                    session.CommitRecallOrAssignment(assignment);
                }
                return session;
            }

            internal void ReplaceStableOwner()
            {
                Source = FakeUnitDescriptor.Create(10, true);
                Controller = new FakeLevelUpController
                {
                    Unit = Source,
                    m_RecalculatePreview = false
                };
                CharacterBuild.LevelUpController = Controller;
                Player.MainCharacter = Source;
            }

            internal RollSession Rebind(FakeState state)
            {
                return Rebind(state, () => Assignment);
            }

            internal RollSession Rebind(FakeState state, out string reason)
            {
                return Rebind(state, () => Assignment, out reason);
            }

            internal RollSession Rebind(FakeState state, Func<StatAssignment> factory)
            {
                string reason;
                return Rebind(state, factory, out reason);
            }

            private RollSession Rebind(
                FakeState state,
                Func<StatAssignment> factory,
                out string reason)
            {
                CharacterCreationContextDecision decision = Evaluate(state, FakeMode.CharGen);
                AssertEx.True(decision.Accepted, decision.Reason);
                RollSession session;
                AssertEx.True(Sessions.TryOpenOrRebind(
                    decision,
                    generation => CaptureRollback(state, generation),
                    out session,
                    out reason), reason);
                return session;
            }

            internal PointBuyOrigin CaptureOrigin(
                FakeState state,
                int budget,
                string source,
                int generation)
            {
                return PointBuyOrigin.Capture(
                    state.StatsDistribution,
                    state.Unit,
                    budget,
                    source,
                    generation,
                    Contracts,
                    StatAccess);
            }

            internal GenerationRollbackSnapshot CaptureRollback(FakeState state, int generation)
            {
                return GenerationRollbackSnapshot.Capture(
                    generation,
                    state.StatsDistribution,
                    state.Unit,
                    Contracts,
                    StatAccess);
            }

            internal int[] ReadDistribution(FakeState state)
            {
                return StatAccess.ReadDistributionValues(state.StatsDistribution, Contracts);
            }

            internal int[] ReadUnit(FakeUnitDescriptor unit)
            {
                return StatAccess.ReadUnitBaseValues(unit, Contracts);
            }

            internal CharacterCreationCoordinator CreateCoordinator()
            {
                var tracker = new PointBudgetTracker();
                return CreateCoordinator(tracker, new RuntimeDiagnostics());
            }

            internal CharacterCreationCoordinator CreateCoordinator(
                PointBudgetTracker tracker,
                RuntimeDiagnostics diagnostics)
            {
                return CreateCoordinator(tracker, diagnostics, null);
            }

            internal CharacterCreationCoordinator CreateCoordinator(
                PointBudgetTracker tracker,
                RuntimeDiagnostics diagnostics,
                CharacterRollWorkflow workflow)
            {
                if (workflow == null)
                {
                    return new CharacterCreationCoordinator(
                        Policy,
                        tracker,
                        new PointBudgetResolver(tracker),
                        StatAccess,
                        Sessions,
                        Application,
                        Restore,
                        Presentation,
                        diagnostics,
                        Logger,
                        () => Contracts,
                        () => false);
                }
                return new CharacterCreationCoordinator(
                    Policy,
                    tracker,
                    new PointBudgetResolver(tracker),
                    StatAccess,
                    Sessions,
                    Application,
                    Restore,
                    Presentation,
                    diagnostics,
                    Logger,
                    () => Contracts,
                    () => false,
                    workflow);
            }

            private static KingmakerContracts CreateContracts()
            {
                BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                return new KingmakerContracts(
                    Assembly.GetExecutingAssembly(),
                    typeof(FakeState),
                    typeof(FakeUnitDescriptor),
                    typeof(FakeMode),
                    typeof(FakeDistribution),
                    typeof(int),
                    null,
                    typeof(FakeDistribution).GetMethod("Start", instance),
                    typeof(FakeDistribution).GetMethod("IsComplete", instance),
                    typeof(FakeState).GetProperty("Unit", instance),
                    typeof(FakeState).GetProperty("StatsDistribution", instance),
                    typeof(FakeState).GetProperty("IsFirstLevel", instance),
                    typeof(FakeUnitDescriptor).GetProperty("Stats", instance),
                    typeof(FakeStats).GetMethod("GetStat", instance),
                    typeof(FakeStat).GetProperty("BaseValue", instance),
                    typeof(FakeDistribution).GetProperty("StatValues", instance),
                    typeof(FakeDistribution).GetProperty("Available", instance),
                    typeof(FakeDistribution).GetProperty("Points", instance),
                    typeof(FakeDistribution).GetProperty("TotalPoints", instance),
                    new object[] { 0, 1, 2, 3, 4, 5 },
                    typeof(FakeGame).GetProperty("Instance", staticFlags),
                    typeof(FakeGame).GetProperty("UI", instance),
                    typeof(FakeGame).GetProperty("Player", instance),
                    typeof(FakePlayer).GetProperty("MainCharacter", instance),
                    typeof(FakeUi).GetProperty("CharacterBuildController", instance),
                    typeof(FakeCharacterBuildController).GetProperty("LevelUpController", instance),
                    typeof(FakeLevelUpController).GetProperty("State", instance),
                    typeof(FakeLevelUpController).GetProperty("Unit", instance),
                    typeof(FakeLevelUpController).GetProperty("Preview", instance),
                    typeof(FakeLevelUpController).GetField("m_RecalculatePreview", instance),
                    typeof(FakeLevelUpController).GetMethod("UpdatePreview", instance),
                    typeof(FakeCharacterBuildController).GetProperty("CurrentPhase", instance),
                    FakePhaseType.Skills,
                    typeof(FakeCharacterBuildController).GetProperty("Skills", instance),
                    typeof(FakeAbilityPhase).GetProperty("AbilityScoresAllocator", instance),
                    typeof(FakeAbilityScoresAllocator).GetMethod("FillData", instance),
                    typeof(FakeUnitDescriptor).GetProperty("Unit", instance),
                    typeof(FakeAbilityScoresAllocator).GetField("m_Unit", instance),
                    typeof(FakeAbilityScoresAllocator).GetField("m_PreviewUnit", instance),
                    typeof(FakeAbilityScoresAllocator).GetField("m_StatEntries", instance),
                    typeof(FakeScoreEntry).GetField("UpButton", instance),
                    typeof(FakeScoreEntry).GetField("DownButton", instance),
                    typeof(FakeButton).GetProperty("interactable", instance),
                    typeof(FakeAbilityScoresAllocator).GetField("m_MainLabel", instance),
                    typeof(FakeAbilityScoresAllocator).GetField("m_Frame", instance),
                    new List<string>());
            }
        }

        private enum FakeMode
        {
            LevelUp = 0,
            CharGen = 1,
            PreGen = 2,
            Respec = 3
        }

        private enum FakePhaseType
        {
            Portrait,
            Race,
            Class,
            ClassInChargen,
            Determinator,
            Skills,
            Abilities,
            Spells,
            Character,
            Total
        }

        private sealed class FakeDistribution
        {
            internal FakeDistribution(int value)
            {
                StatValues = new Hashtable();
                for (int index = 0; index < 6; index++) StatValues[index] = value;
                Available = true;
                Points = 25;
                TotalPoints = 25;
                LastStartBudget = -1;
            }

            public IDictionary StatValues { get; }
            public bool Available { get; private set; }
            public int Points { get; private set; }
            public int TotalPoints { get; private set; }
            internal int StartCalls { get; private set; }
            internal int LastStartBudget { get; private set; }
            internal bool ThrowAfterStart { get; set; }
            internal Action<int> OnStart { get; set; }

            public void Start(int budget)
            {
                StartCalls++;
                LastStartBudget = budget;
                Available = true;
                Points = budget;
                TotalPoints = budget;
                Action<int> action = OnStart;
                action?.Invoke(budget);
                if (ThrowAfterStart) throw new InvalidOperationException("simulated allocator start failure");
            }

            internal void SetAllocatorState(bool available, int points, int totalPoints)
            {
                Available = available;
                Points = points;
                TotalPoints = totalPoints;
            }

            public bool IsComplete()
            {
                return false;
            }
        }

        private sealed class FakeUnitDescriptor
        {
            private FakeUnitDescriptor(int value, bool isMainCharacter)
            {
                IsMainCharacter = isMainCharacter;
                IsPlayerFaction = true;
                Stats = new FakeStats(value);
                Unit = new FakeUnitEntityData(this);
            }

            public bool IsMainCharacter { get; set; }
            public bool IsPlayerFaction { get; set; }
            public bool IsPet { get; set; }
            public bool IsPlayersEnemy { get; set; }
            public FakeStats Stats { get; }
            public FakeUnitEntityData Unit { get; }

            internal static FakeUnitDescriptor Create(int value, bool isMainCharacter)
            {
                return new FakeUnitDescriptor(value, isMainCharacter);
            }
        }

        private sealed class FakeUnitEntityData
        {
            internal FakeUnitEntityData(FakeUnitDescriptor descriptor)
            {
                Descriptor = descriptor;
            }

            public FakeUnitDescriptor Descriptor { get; }
        }

        private sealed class FakeStats
        {
            private readonly Dictionary<int, FakeStat> values = new Dictionary<int, FakeStat>();

            internal FakeStats(int value)
            {
                for (int index = 0; index < 6; index++) values[index] = new FakeStat { BaseValue = value };
            }

            public FakeStat GetStat(int key)
            {
                return values[key];
            }

            internal void SetModifiers(int[] modifiers)
            {
                if (modifiers == null || modifiers.Length != 6)
                {
                    throw new ArgumentException("Exactly six modifiers are required.", nameof(modifiers));
                }
                for (int index = 0; index < modifiers.Length; index++) values[index].Modifier = modifiers[index];
            }

            internal int[] ReadDisplayedValues()
            {
                return Enumerable.Range(0, 6)
                    .Select(index => values[index].BaseValue + values[index].Modifier)
                    .ToArray();
            }
        }

        private sealed class FakeStat
        {
            public int BaseValue { get; set; }
            internal int Modifier { get; set; }
        }

        private sealed class FakeState
        {
            internal FakeState(FakeUnitDescriptor unit, FakeDistribution distribution, bool isFirstLevel)
            {
                Unit = unit;
                StatsDistribution = distribution;
                IsFirstLevel = isFirstLevel;
            }

            public FakeUnitDescriptor Unit { get; }
            public FakeDistribution StatsDistribution { get; }
            public bool IsFirstLevel { get; set; }
        }

        private sealed class FakeLevelUpController
        {
            public FakeState State { get; set; }
            public FakeUnitDescriptor Unit { get; set; }
            public FakeUnitDescriptor Preview { get; set; }
            public bool m_RecalculatePreview;
            internal Action OnUpdatePreview { get; set; }
            internal int UpdatePreviewCount { get; private set; }

            public void UpdatePreview()
            {
                UpdatePreviewCount++;
                Action action = OnUpdatePreview;
                action?.Invoke();
            }
        }

        private sealed class FakeCharacterBuildController
        {
            public FakeLevelUpController LevelUpController { get; set; }
            public FakePhaseType? CurrentPhase { get; set; }
            public FakeAbilityPhase Skills { get; set; }
        }

        private sealed class FakeAbilityPhase
        {
            public FakeAbilityScoresAllocator AbilityScoresAllocator { get; set; }
        }

        private sealed class FakeAbilityScoresAllocator
        {
            public FakeUnitEntityData m_Unit;
            public FakeUnitEntityData m_PreviewUnit;
            public readonly List<FakeScoreEntry> m_StatEntries = Enumerable.Range(0, 6)
                .Select(_ => new FakeScoreEntry())
                .ToList();
            public object m_MainLabel = new object();
            public object m_Frame = new object();
            internal FakeState BoundState { get; private set; }
            internal FakeDistribution BoundDistribution { get; private set; }
            internal int[] DisplayedValues { get; private set; }
            internal int DisplayedPoints { get; private set; }
            internal bool NativeControlsAvailable { get; private set; }
            internal int FillDataCalls { get; private set; }
            internal bool ThrowOnFill { get; set; }
            internal Action OnFill { get; set; }

            public void FillData()
            {
                FillDataCalls++;
                FakeLevelUpController controller = FakeGame.Instance.UI.CharacterBuildController.LevelUpController;
                m_Unit = controller.Unit.Unit;
                m_PreviewUnit = controller.Preview.Unit;
                BoundState = controller.State;
                BoundDistribution = controller.State.StatsDistribution;
                DisplayedValues = controller.Preview.Stats.ReadDisplayedValues();
                DisplayedPoints = controller.State.StatsDistribution.Points;
                NativeControlsAvailable = controller.State.StatsDistribution.Available;
                foreach (FakeScoreEntry entry in m_StatEntries)
                {
                    entry.UpButton.interactable = NativeControlsAvailable;
                    entry.DownButton.interactable = NativeControlsAvailable;
                }
                Action action = OnFill;
                action?.Invoke();
                if (ThrowOnFill) throw new InvalidOperationException("simulated native presentation failure");
            }
        }

        private sealed class FakeScoreEntry
        {
            public readonly FakeButton UpButton = new FakeButton();
            public readonly FakeButton DownButton = new FakeButton();
        }

        private sealed class FakeButton
        {
            public bool interactable { get; set; }
        }

        private sealed class FakeUi
        {
            public FakeCharacterBuildController CharacterBuildController { get; set; }
        }

        private sealed class FakePlayer
        {
            public object MainCharacter { get; set; }
        }

        private sealed class FakeGame
        {
            public static FakeGame Instance { get; set; }
            public FakeUi UI { get; set; }
            public FakePlayer Player { get; set; }
        }

        private sealed class FakeLogger : IModLogger
        {
            internal readonly List<string> Messages = new List<string>();

            public void Info(string message) { Messages.Add("INFO " + message); }
            public void Warning(string message) { Messages.Add("WARN " + message); }
            public void Error(string message) { Messages.Add("ERROR " + message); }
            public void Exception(string context, Exception exception)
            {
                Messages.Add("EXCEPTION " + context + ": " + exception.GetType().Name);
            }
        }
    }
}
