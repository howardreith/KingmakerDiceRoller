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
                environment.CaptureBaseline(previewB, 25, "replacement budget"),
                () => environment.Assignment,
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
                environment.CaptureBaseline(differentState, 25, "different owner"),
                () => environment.Assignment,
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

        internal static void RebindReplacesAllTransientObjectsAndBaseline()
        {
            TestEnvironment environment = TestEnvironment.Create();
            FakeState previewA = environment.NewState(9);
            RollSession session = environment.Open(previewA);
            PointBuyBaseline firstBaseline = session.Baseline;
            FakeState previewB = environment.NewReplacementState(previewA, 11);
            PointBuyBaseline secondBaseline = environment.CaptureBaseline(previewB, 20, "replacement budget");
            session = environment.Rebind(previewB, secondBaseline, () => environment.Assignment);

            AssertEx.True(ReferenceEquals(previewB, session.State));
            AssertEx.True(ReferenceEquals(previewB.Unit, session.Unit));
            AssertEx.True(ReferenceEquals(previewB.StatsDistribution, session.Distribution));
            AssertEx.True(!ReferenceEquals(firstBaseline, session.Baseline));
            AssertEx.True(ReferenceEquals(secondBaseline, session.Baseline));
            AssertEx.Equal(20, session.Baseline.Budget);
            AssertEx.SequenceEqual(Enumerable.Repeat(11, 6), session.Baseline.Values.UnitValues);
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
            AssertEx.Equal(1, diagnostics.ArraysApplied);
            AssertEx.True(environment.Sessions.Active.IsApplied);
            AssertEx.True(diagnostics.Status.Contains("verified live"));
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

            AssertEx.True(environment.Restore.TryRestore(session, environment.Contracts, out error), error);
            AssertEx.True(previewB != null);
            AssertEx.SequenceEqual(FixedValues, environment.ReadUnit(previewA.Unit));
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadUnit(previewB.Unit));
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6), environment.ReadDistribution(previewB));
            AssertEx.Equal(25, previewB.StatsDistribution.LastStartBudget);
            AssertEx.Equal(0, previewA.StatsDistribution.StartCalls);
            AssertEx.Equal(RollSessionState.PointBuyRestored, session.Lifecycle.State);
            AssertEx.Equal(1, environment.PreviewRefresh.RefreshCount);
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
                Application = new StatApplicationService(StatAccess, LivePreview, PreviewRefresh, Logger);
                Restore = new PointBuyRestoreService(StatAccess, LivePreview, PreviewRefresh, Logger);
                Assignment = new StatAssignment(DiagnosticArrays.FixedPhaseTwoArray());
            }

            internal KingmakerContracts Contracts { get; }
            internal CharacterCreationContextPolicy Policy { get; }
            internal RollSessionManager Sessions { get; }
            internal KingmakerStatAccess StatAccess { get; }
            internal PreviewRefreshService PreviewRefresh { get; }
            internal LivePreviewInspector LivePreview { get; }
            internal FakeLogger Logger { get; }
            internal StatApplicationService Application { get; }
            internal PointBuyRestoreService Restore { get; }
            internal StatAssignment Assignment { get; }
            internal FakeUnitDescriptor Source { get; private set; }
            internal FakeLevelUpController Controller { get; private set; }
            internal FakeCharacterBuildController CharacterBuild { get; private set; }
            internal FakePlayer Player { get; private set; }

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
                    LevelUpController = environment.Controller
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

            internal RollSession Open(FakeState state, Func<StatAssignment> factory)
            {
                Controller.State = state;
                Controller.Preview = state.Unit;
                CharacterCreationContextDecision decision = Evaluate(state, FakeMode.CharGen);
                AssertEx.True(decision.Accepted, decision.Reason);
                RollSession session;
                string reason;
                AssertEx.True(Sessions.TryOpenOrRebind(
                    decision,
                    CaptureBaseline(state, 25, "test budget"),
                    factory,
                    out session,
                    out reason), reason);
                return session;
            }

            internal RollSession Rebind(FakeState state)
            {
                return Rebind(state, () => Assignment);
            }

            internal RollSession Rebind(FakeState state, out string reason)
            {
                return Rebind(state, CaptureBaseline(state, 25, "replacement budget"), () => Assignment, out reason);
            }

            internal RollSession Rebind(FakeState state, Func<StatAssignment> factory)
            {
                string reason;
                return Rebind(state, CaptureBaseline(state, 25, "replacement budget"), factory, out reason);
            }

            internal RollSession Rebind(
                FakeState state,
                PointBuyBaseline baseline,
                Func<StatAssignment> factory)
            {
                string reason;
                return Rebind(state, baseline, factory, out reason);
            }

            private RollSession Rebind(
                FakeState state,
                PointBuyBaseline baseline,
                Func<StatAssignment> factory,
                out string reason)
            {
                CharacterCreationContextDecision decision = Evaluate(state, FakeMode.CharGen);
                AssertEx.True(decision.Accepted, decision.Reason);
                RollSession session;
                AssertEx.True(Sessions.TryOpenOrRebind(
                    decision,
                    baseline,
                    factory,
                    out session,
                    out reason), reason);
                return session;
            }

            internal PointBuyBaseline CaptureBaseline(FakeState state, int budget, string source)
            {
                return PointBuyBaseline.Capture(
                    state.StatsDistribution,
                    state.Unit,
                    budget,
                    source,
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
                return new CharacterCreationCoordinator(
                    Policy,
                    tracker,
                    new PointBudgetResolver(tracker),
                    StatAccess,
                    Sessions,
                    Application,
                    Restore,
                    diagnostics,
                    Logger,
                    () => Contracts,
                    () => false);
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

        private sealed class FakeDistribution
        {
            internal FakeDistribution(int value)
            {
                StatValues = new Hashtable();
                for (int index = 0; index < 6; index++) StatValues[index] = value;
                TotalPoints = 25;
                LastStartBudget = -1;
            }

            public IDictionary StatValues { get; }
            public int TotalPoints { get; private set; }
            internal int StartCalls { get; private set; }
            internal int LastStartBudget { get; private set; }

            public void Start(int budget)
            {
                StartCalls++;
                LastStartBudget = budget;
                TotalPoints = budget;
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
            }

            public bool IsMainCharacter { get; set; }
            public bool IsPlayerFaction { get; set; }
            public bool IsPet { get; set; }
            public bool IsPlayersEnemy { get; set; }
            public FakeStats Stats { get; }

            internal static FakeUnitDescriptor Create(int value, bool isMainCharacter)
            {
                return new FakeUnitDescriptor(value, isMainCharacter);
            }
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
        }

        private sealed class FakeStat
        {
            public int BaseValue { get; set; }
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
