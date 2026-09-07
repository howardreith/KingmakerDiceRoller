using System;
using System.Linq;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.DomainTests
{
    // Reuses the runner's reflection/stat/allocator boundaries. No simulated Unity or respec
    // engine: native launch/copy ordering is qualified separately against installed assemblies.
    internal static partial class PreviewSessionContinuityTests
    {
        internal static void RespecMainCloneAdmitted() { AssertRespecAdmission("main"); }
        internal static void RespecMercenaryAdmitted() { AssertRespecAdmission("mercenary"); }
        internal static void RespecStoryCompanionAdmitted() { AssertRespecAdmission("story"); }
        private static void AssertRespecAdmission(string kind)
        {
            var f = new RespecFixture(kind);
            AssertEx.Equal(SupportedCharacterCreationKind.Respec, f.Coordinator.ActiveSession.CreationKind);
            AssertEx.True(f.Coordinator.ActiveSession.IsPointBuyMode);
            AssertEx.True(f.Coordinator.CanAttachNativePanel);
            AssertEx.Equal(0, f.Random.Calls);
            AssertEx.True(!ReferenceEquals(f.Original, f.Environment.Source));
            AssertEx.SequenceEqual(Enumerable.Repeat(17, 6).ToArray(), f.Environment.ReadUnit(f.Original));
        }

        internal static void RespecNegativeContextMatrix()
        {
            foreach (string invalid in new[] { "levelup", "locked", "pet", "enemy", "nonplayer", "pregen", "unknown", "unowned", "unrelated", "source" })
            {
                var f = new RespecFixture("story", false);
                FakeState state = f.Preview;
                FakeMode mode = FakeMode.Respec;
                switch (invalid)
                {
                    case "levelup": state.IsFirstLevel = false; mode = FakeMode.LevelUp; break;
                    case "locked": state.StatsDistribution.SetAllocatorState(false, 0, 0); break;
                    case "pet": state.Unit.IsPet = true; break;
                    case "enemy": state.Unit.IsPlayersEnemy = true; break;
                    case "nonplayer": state.Unit.IsPlayerFaction = false; break;
                    case "pregen": mode = FakeMode.PreGen; break;
                    case "unknown": mode = (FakeMode)987; break;
                    case "unowned": f.OwnerCurrent = false; break;
                    case "unrelated": state = new FakeState(FakeUnitDescriptor.Create(10, false), new FakeDistribution(10), true); break;
                    case "source": state = new FakeState(f.Environment.Source, new FakeDistribution(10), true); break;
                }
                CharacterCreationContextDecision decision = f.Environment.Policy.Evaluate(state, state.Unit, mode, f.Environment.Contracts, f.Owner);
                AssertEx.True(!decision.Accepted, invalid + ": " + decision.Reason);
                AssertEx.Equal(0, f.Random.Calls);
            }
        }

        internal static void RespecConstructorIncompleteThenBound()
        {
            var f = new RespecFixture("main", false);
            f.Environment.CharacterBuild.LevelUpController = null;
            f.Coordinator.OnLevelUpStateConstructed(f.Preview, f.Preview.Unit, FakeMode.Respec);
            AssertEx.True(!f.Coordinator.HasActiveSession);
            f.Environment.CharacterBuild.LevelUpController = f.Environment.Controller;
            f.Coordinator.OnRespecBound(f.Owner); // proven HandleLevelUpStart postfix
            AssertEx.True(f.Coordinator.CanAttachNativePanel);
            AssertEx.Equal(0, f.Random.Calls);
        }

        internal static void RespecReplacementKeepsAssignmentAndRng()
        {
            var f = new RespecFixture(); f.Roll();
            AssertEx.True(f.Coordinator.TryMoveAssignment(AbilityScore.Strength, false, out string error), error);
            RollSession session = f.Coordinator.ActiveSession;
            int[] values = session.Assignment.ToAssignedArray(); int calls = f.Random.Calls;
            f.ReplacePreview();
            f.Environment.CharacterBuild.Skills.AbilityScoresAllocator = new FakeAbilityScoresAllocator();
            f.Environment.Allocator.FillData();
            f.Coordinator.ObserveRespecPreview(f.Environment.Controller);
            f.Coordinator.Update(0f);
            AssertEx.True(ReferenceEquals(session, f.Coordinator.ActiveSession));
            AssertEx.SequenceEqual(values, f.Environment.ReadUnit(f.Preview.Unit));
            AssertEx.SequenceEqual(values, session.Assignment.ToAssignedArray());
            AssertEx.Equal(calls, f.Random.Calls);
            AssertEx.True(session.Generation > 1);
            AssertEx.True(f.Coordinator.CanAttachNativePanel);
        }

        internal static void RespecPointBuyRestoresModifiedOrigin()
        {
            var f = new RespecFixture();
            int[] before = { 13, 11, 8, 16, 9, 10 };
            f.Environment.WriteUnit(f.Preview.Unit, before);
            for (int i = 0; i < 6; i++) f.Preview.StatsDistribution.StatValues[i] = before[i];
            f.Preview.StatsDistribution.SetAllocatorState(true, 29, 47);
            f.Coordinator.OnDistributionStarted(f.Preview.StatsDistribution, 47);
            f.Roll();
            AssertEx.True(f.Coordinator.TryReroll(out string error), error);
            f.Environment.Controller.OnUpdatePreview = f.ReplacePreview;
            AssertEx.True(f.Coordinator.TryRestorePointBuy(out error), error);
            AssertEx.SequenceEqual(before, f.Environment.ReadUnit(f.Preview.Unit));
            AssertEx.SequenceEqual(before, f.Environment.ReadDistribution(f.Preview));
            AssertEx.Equal(47, f.Preview.StatsDistribution.LastStartBudget);
            AssertEx.Equal(47, f.Preview.StatsDistribution.TotalPoints);
            AssertEx.Equal(29, f.Preview.StatsDistribution.Points);
            AssertEx.True(f.Preview.StatsDistribution.Available);
            AssertEx.True(f.Environment.Allocator.m_StatEntries.All(e => e.UpButton.interactable && e.DownButton.interactable));
            AssertEx.SequenceEqual(Enumerable.Repeat(17, 6).ToArray(), f.Environment.ReadUnit(f.Original));
        }

        internal static void RespecOldRolledOriginDoesNotBecomeNewRoll()
        {
            var f = new RespecFixture();
            int[] alreadyRolled = { 18, 15, 12, 9, 6, 3 };
            f.Environment.WriteUnit(f.Preview.Unit, alreadyRolled);
            for (int i = 0; i < 6; i++) f.Preview.StatsDistribution.StatValues[i] = alreadyRolled[i];
            f.Roll();
            AssertEx.SequenceEqual(alreadyRolled, f.Coordinator.ActiveSession.Assignment.ToAssignedArray());
            AssertEx.True(f.Coordinator.TryRestorePointBuy(out string error), error);
            AssertEx.SequenceEqual(alreadyRolled, f.Environment.ReadUnit(f.Preview.Unit));
            AssertEx.Equal(25, f.Preview.StatsDistribution.Points);
        }

        internal static void RespecReplayAndCopyReachOriginalRecipient()
        {
            var f = new RespecFixture(); f.Roll(); int[] expected = f.Values;
            f.BeginCommit(); f.Replay();
            AssertEx.SequenceEqual(expected, f.Environment.ReadUnit(f.Environment.Source));
            AssertEx.SequenceEqual(Enumerable.Repeat(17, 6).ToArray(), f.Environment.ReadUnit(f.Original));
            f.CopyAndComplete();
            AssertEx.True(f.Coordinator.Respec.LastPassed == true, f.Coordinator.Respec.LastResult);
            AssertEx.SequenceEqual(expected, f.Environment.ReadUnit(f.Original));
            AssertEx.True(!f.Coordinator.HasActiveSession && !f.Coordinator.Respec.HasPendingCommit);
        }

        internal static void RespecPostCallbackOverwriteDetected()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit(); f.Replay();
            f.Environment.WriteUnit(f.Original, f.Environment.ReadUnit(f.Environment.Source));
            f.Coordinator.OnRespecCopyCompleted(f.Owner.CopyContext);
            f.Environment.WriteUnit(f.Original, Enumerable.Repeat(8, 6).ToArray());
            f.Coordinator.OnLevelUpCommitCompleted(f.Environment.Controller);
            AssertEx.True(f.Coordinator.Respec.LastPassed == false);
            AssertEx.True(!f.Coordinator.Respec.HasPendingCommit);
        }
        internal static void RespecMissingReplayCannotPass()
        {
            var f = new RespecFixture(); f.Roll(); int[] expected = f.Values; f.BeginCommit();
            f.Environment.WriteUnit(f.Original, expected); // matching values alone are insufficient
            f.Coordinator.OnLevelUpCommitCompleted(f.Environment.Controller);
            AssertEx.True(f.Coordinator.Respec.LastPassed == false);
        }
        internal static void RespecWrongTargetCannotConsumeWrite()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit();
            var wrongState = new FakeState(f.Original, new FakeDistribution(10), true) { Mode = FakeMode.Respec };
            f.Coordinator.OnLevelUpStateConstructed(wrongState, f.Original, FakeMode.Respec);
            f.Coordinator.OnLevelUpAppliedToAuthoritativeUnit(new object(), f.Environment.Source);
            f.Coordinator.OnLevelUpAppliedToAuthoritativeUnit(f.Environment.Controller, f.Original);
            AssertEx.SequenceEqual(Enumerable.Repeat(17, 6).ToArray(), f.Environment.ReadUnit(f.Original));
            f.Replay(); f.CopyAndComplete(); AssertEx.True(f.Coordinator.Respec.LastPassed == true);
        }
        internal static void RespecDuplicateReplayDoesNotOverwriteIncrease()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit(); f.Replay();
            f.Environment.Source.Stats.GetStat(0).BaseValue++;
            f.Coordinator.OnLevelUpAppliedToAuthoritativeUnit(f.Environment.Controller, f.Environment.Source);
            AssertEx.Equal(19, f.Environment.Source.Stats.GetStat(0).BaseValue);
        }
        internal static void RespecDuplicateCompletionIsInert()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit(); f.Replay(); f.CopyAndComplete();
            string result = f.Coordinator.Respec.LastResult;
            f.Original.Stats.GetStat(1).BaseValue++;
            f.Coordinator.OnLevelUpCommitCompleted(f.Environment.Controller);
            AssertEx.Equal(result, f.Coordinator.Respec.LastResult);
            AssertEx.Equal(16, f.Original.Stats.GetStat(1).BaseValue);
        }
        internal static void RespecCatchUpRetainsAbilityIncreases()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit(); f.Replay(); f.CopyAndComplete();
            f.Original.Stats.GetStat(0).BaseValue += 2; // native SpendAttributePoint at levels 4 and 8
            f.Environment.Controller.State = new FakeState(f.Original, new FakeDistribution(10), false) { Mode = FakeMode.LevelUp };
            f.Coordinator.OnLevelUpStateConstructed(f.Environment.Controller.State, f.Original, FakeMode.LevelUp);
            f.Coordinator.OnLevelUpAppliedToAuthoritativeUnit(f.Environment.Controller, f.Original);
            f.Coordinator.OnLevelUpCommitCompleted(f.Environment.Controller);
            AssertEx.Equal(20, f.Original.Stats.GetStat(0).BaseValue);
            AssertEx.True(!f.Coordinator.HasActiveSession);
        }
        internal static void RespecRaceAndPermanentModifiersStaySeparate()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit();
            f.Environment.Source.Stats.SetModifiers(new[] { 2, -2, 0, 4, 0, 0 });
            f.Replay();
            AssertEx.SequenceEqual(new[] { 20, 13, 12, 13, 6, 3 }, f.Environment.Source.Stats.ReadDisplayedValues());
            f.CopyAndComplete();
            AssertEx.SequenceEqual(new[] { 18, 15, 12, 9, 6, 3 }, f.Environment.ReadUnit(f.Original));
        }
        internal static void RespecCancelReopenStartsPointBuy()
        {
            var f = new RespecFixture(); f.Roll(); f.Coordinator.OnBuildCanceled(f.Environment.Controller);
            f.Coordinator.OnLevelUpAppliedToAuthoritativeUnit(f.Environment.Controller, f.Environment.Source);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6).ToArray(), f.Environment.ReadUnit(f.Environment.Source));
            f.Owner = f.MakeOwner(); f.ReplacePreview(); f.Coordinator.OnRespecBound(f.Owner);
            AssertEx.True(f.Coordinator.ActiveSession.IsPointBuyMode);
            AssertEx.Equal(null, f.Coordinator.ActiveSession.Assignment);
            AssertEx.Equal(null, f.Coordinator.ActiveSession.PointBuyOrigin);
            AssertEx.Equal(24, f.Random.Calls);
        }
        internal static void RespecOwnerLossPreventsLateWrites()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit(); f.OwnerCurrent = false; f.Replay();
            f.Coordinator.OnLevelUpCommitCompleted(f.Environment.Controller);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6).ToArray(), f.Environment.ReadUnit(f.Environment.Source));
            AssertEx.True(f.Coordinator.Respec.LastPassed == false);
        }
        internal static void RespecCommitExceptionExpiresTicket()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit(); f.CommitInProgress = false;
            f.Replay(); // exception unwound the synchronous commit lease
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6).ToArray(), f.Environment.ReadUnit(f.Environment.Source));
            f.Coordinator.Update(0f);
            AssertEx.True(!f.Coordinator.Respec.HasPendingCommit);
            AssertEx.True(f.Coordinator.Respec.LastPassed == false);
        }
        internal static void RespecDisableRestoresAndCloses()
        {
            var f = new RespecFixture(); f.Roll();
            AssertEx.True(f.Coordinator.TryPrepareDisable(out string error), error);
            AssertEx.True(!f.Coordinator.HasActiveSession && f.Owner.Closed);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6).ToArray(), f.Environment.ReadUnit(f.Preview.Unit));
            f.Coordinator.OnLevelUpAppliedToAuthoritativeUnit(f.Environment.Controller, f.Environment.Source);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6).ToArray(), f.Environment.ReadUnit(f.Environment.Source));
        }
        internal static void RespecUnrolledCompletionNeverWrites()
        {
            var f = new RespecFixture(); f.BeginCommit(); f.Replay(); f.Coordinator.OnLevelUpCommitCompleted(f.Environment.Controller);
            AssertEx.Equal(0, f.Random.Calls);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6).ToArray(), f.Environment.ReadUnit(f.Environment.Source));
            AssertEx.True(!f.Coordinator.HasActiveSession && f.Owner.Closed);
        }
        internal static void RespecHistoryRecallAndPointBuyOrigin()
        {
            var f = new RespecFixture(); f.Roll(); AssertEx.True(f.Coordinator.TryReroll(out string error), error);
            f.Coordinator.SelectPreviousHistory(); AssertEx.True(f.Coordinator.TryUseSelectedHistory(out error), error);
            AssertEx.True(f.Coordinator.TryStoreCurrent(out error), error);
            AssertEx.True(f.Coordinator.TryRestorePointBuy(out error), error);
            AssertEx.True(f.Coordinator.TryRecallSelectedSaved(out error), error);
            AssertEx.True(f.Coordinator.TryRestorePointBuy(out error), error);
            AssertEx.Equal(48, f.Random.Calls);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6).ToArray(), f.Environment.ReadUnit(f.Preview.Unit));
        }

        internal static void RespecSourceStagedBeforeNativeChecks()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit();
            var state = new FakeState(f.Environment.Source, new FakeDistribution(10), true) { Mode = FakeMode.Respec };
            f.Coordinator.OnLevelUpStateConstructed(state, f.Environment.Source, FakeMode.Respec);
            AssertEx.SequenceEqual(new[] { 18, 15, 12, 9, 6, 3 }, f.Environment.ReadUnit(f.Environment.Source));
            AssertEx.True(!state.StatsDistribution.Available);
            AssertEx.Equal(0, state.StatsDistribution.Points);
            AssertEx.True(!f.Coordinator.HasActiveSession);
        }
        internal static void RespecPreviewStagedBeforeNativeChecks()
        {
            var f = new RespecFixture(); f.Roll();
            FakeState replacement = f.Environment.NewReplacementState(f.Preview, 10); replacement.Mode = FakeMode.Respec;
            f.Coordinator.OnLevelUpStateConstructed(replacement, replacement.Unit, FakeMode.Respec);
            AssertEx.SequenceEqual(f.Values, f.Environment.ReadUnit(replacement.Unit));
            f.Environment.Controller.State = replacement;
            f.Coordinator.OnLevelUpAppliedToAuthoritativeUnit(f.Environment.Controller, replacement.Unit);
            f.Coordinator.Update(0f);
            AssertEx.True(f.Coordinator.ActiveSession.IsApplied);
            AssertEx.Equal(24, f.Random.Calls);
        }
        internal static void RespecNativeReplayOverwriteIsDetected()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit(); f.Replay();
            f.Environment.Source.Stats.GetStat(0).BaseValue = 7;
            f.Coordinator.OnLevelUpAppliedToAuthoritativeUnit(f.Environment.Controller, f.Environment.Source);
            f.CopyAndComplete();
            AssertEx.Equal(7, f.Original.Stats.GetStat(0).BaseValue);
            AssertEx.True(f.Coordinator.Respec.LastPassed == false);
        }
        internal static void RespecMissingCopyCannotPassCoincidentalScores()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit(); f.Replay();
            f.Environment.WriteUnit(f.Original, f.Environment.ReadUnit(f.Environment.Source));
            f.Coordinator.OnRespecCopyCompleted(new object());
            f.Coordinator.OnLevelUpCommitCompleted(f.Environment.Controller);
            AssertEx.True(f.Coordinator.Respec.LastPassed == false);
        }
        internal static void RespecReenteredCommitInvalidatesOldTicket()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit();
            f.BeginCommit(); f.Replay();
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6).ToArray(), f.Environment.ReadUnit(f.Environment.Source));
            AssertEx.True(!f.Coordinator.Respec.HasPendingCommit);
        }
        internal static void RespecPendingCommitDisablePreventsWrites()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit();
            AssertEx.True(f.Coordinator.TryPrepareDisable(out string error), error); f.Replay();
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6).ToArray(), f.Environment.ReadUnit(f.Environment.Source));
        }
        internal static void RespecLostOwnerDisableDoesNotRestoreAnotherUnit()
        {
            var f = new RespecFixture(); f.Roll(); f.OwnerCurrent = false;
            AssertEx.True(f.Coordinator.TryPrepareDisable(out string error), error);
            AssertEx.SequenceEqual(Enumerable.Repeat(17, 6).ToArray(), f.Environment.ReadUnit(f.Original));
            AssertEx.True(!f.Coordinator.HasActiveSession);
        }
        internal static void RespecDerivedStatsRefreshOnRollAndReturn()
        {
            var f = new RespecFixture("story", false); int calls = 0;
            f.Owner = new RespecOwnership(f.Environment.Controller, f.Environment.Source, f.Original.Unit,
                new object(), "native boundary", () => true, () => f.Original, state => { calls++; });
            f.Coordinator.OnRespecBound(f.Owner); f.Roll();
            AssertEx.Equal(1, calls);
            AssertEx.True(f.Coordinator.TryRestorePointBuy(out string error), error);
            AssertEx.Equal(2, calls);
        }

        internal static void RespecFailedSourceWriteRollsBackExactSnapshot()
        {
            var f = new RespecFixture(); f.Roll(); f.BeginCommit();
            f.Environment.Source.Stats.GetStat(2).FailNextWrite = true;
            f.Replay();
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6).ToArray(), f.Environment.ReadUnit(f.Environment.Source));
            AssertEx.True(f.Environment.Controller.State.StatsDistribution.Available);
            AssertEx.Equal(25, f.Environment.Controller.State.StatsDistribution.Points);
            f.CopyAndComplete(); AssertEx.True(f.Coordinator.Respec.LastPassed == false);
        }
        internal static void RespecNewProviderLockPreventsRoll()
        {
            var f = new RespecFixture(); f.Preview.StatsDistribution.SetAllocatorState(false, 0, 0);
            AssertEx.True(!f.Coordinator.CanAttachNativePanel);
            AssertEx.True(!f.Coordinator.TryRoll(out string error));
            AssertEx.Equal(0, f.Random.Calls);
        }
        internal static void RespecDifferentCharacterHasNoAssignmentOrOrigin()
        {
            var f = new RespecFixture(); f.Roll();
            RollSession firstSession = f.Coordinator.ActiveSession;
            FakeLevelUpController firstController = f.Environment.Controller;
            FakeUnitDescriptor firstSource = f.Environment.Source;
            f.Coordinator.OnBuildCanceled(firstController);
            f.Environment.ReplaceMercenaryOwner();
            FakeUnitDescriptor secondOriginal = FakeUnitDescriptor.Create(14, false, true);
            f.Preview = f.Environment.NewMercenaryState(10); f.Preview.Mode = FakeMode.Respec;
            f.Preview.StatsDistribution.SetAllocatorState(true, 37, 42);
            f.Coordinator.OnDistributionStarted(f.Preview.StatsDistribution, 42);
            f.Owner = new RespecOwnership(f.Environment.Controller, f.Environment.Source, secondOriginal.Unit,
                new object(), "native second-character boundary", () => true, () => secondOriginal);
            f.Coordinator.OnRespecBound(f.Owner);
            AssertEx.True(!ReferenceEquals(firstSession, f.Coordinator.ActiveSession));
            AssertEx.True(f.Coordinator.ActiveSession.IsPointBuyMode);
            AssertEx.Equal(null, f.Coordinator.ActiveSession.Assignment);
            AssertEx.Equal(null, f.Coordinator.ActiveSession.PointBuyOrigin);
            AssertEx.Equal(24, f.Random.Calls);
            f.Coordinator.OnLevelUpAppliedToAuthoritativeUnit(firstController, firstSource);
            AssertEx.SequenceEqual(Enumerable.Repeat(10, 6).ToArray(), f.Environment.ReadUnit(f.Environment.Source));
            AssertEx.SequenceEqual(Enumerable.Repeat(17, 6).ToArray(), f.Environment.ReadUnit(f.Original));
            f.Roll();
            AssertEx.True(f.Coordinator.TryRestorePointBuy(out string error), error);
            AssertEx.Equal(42, f.Preview.StatsDistribution.TotalPoints);
            AssertEx.Equal(37, f.Preview.StatsDistribution.Points);
            AssertEx.SequenceEqual(Enumerable.Repeat(14, 6).ToArray(), f.Environment.ReadUnit(secondOriginal));
        }

        private sealed class RespecFixture
        {
            internal readonly TestEnvironment Environment = TestEnvironment.Create();
            internal readonly SequenceRandomSource Random = new SequenceRandomSource(Enumerable.Range(0, 240).Select(i => 6 - (i / 4) % 6).ToArray());
            internal readonly FakeUnitDescriptor Original;
            internal readonly CharacterCreationCoordinator Coordinator;
            internal FakeState Preview;
            internal RespecOwnership Owner;
            internal bool OwnerCurrent = true;
            internal bool CommitInProgress = true;
            internal int[] Values => Coordinator.ActiveSession.Assignment.ToAssignedArray();
            internal RespecFixture(string category = "story", bool bind = true)
            {
                Original = FakeUnitDescriptor.Create(17, category == "main", category == "mercenary");
                Environment.Source.IsMainCharacter = false; // native main-character clone differs from Player.MainCharacter
                Environment.Source.IsCustomCompanion = category == "mercenary";
                if (category == "main") Environment.Player.MainCharacter = Original;
                Preview = Environment.NewState(10); Preview.Mode = FakeMode.Respec;
                Coordinator = Environment.CreateCoordinator(new PointBudgetTracker(), new RuntimeDiagnostics(), NewProductWorkflow(Random, RollConfiguration.Default()));
                Owner = MakeOwner();
                if (bind) Coordinator.OnRespecBound(Owner);
                Environment.Allocator.FillData();
            }
            internal RespecOwnership MakeOwner() => new RespecOwnership(Environment.Controller, Environment.Source,
                Original.Unit, new object(), "native copy boundary / Eddic equivalent", () => OwnerCurrent, () => Original);
            internal void Roll() { AssertEx.True(Coordinator.TryRoll(out string error), error); }
            internal void ReplacePreview()
            {
                Preview = Environment.NewState(10); Preview.Mode = FakeMode.Respec;
                Coordinator.OnLevelUpAppliedToAuthoritativeUnit(Environment.Controller, Preview.Unit);
                Coordinator.Update(0f);
            }
            internal void BeginCommit() { Coordinator.OnRespecCommitStarted(Environment.Controller, () => CommitInProgress); }
            internal void Replay()
            {
                var replayState = new FakeState(Environment.Source, new FakeDistribution(10), true) { Mode = FakeMode.Respec };
                Coordinator.OnLevelUpStateConstructed(replayState, Environment.Source, FakeMode.Respec);
                Environment.Controller.State = replayState;
                Coordinator.OnLevelUpAppliedToAuthoritativeUnit(Environment.Controller, Environment.Source);
            }
            internal void CopyAndComplete()
            {
                Environment.WriteUnit(Original, Environment.ReadUnit(Environment.Source)); // native serialization boundary
                Coordinator.OnRespecCopyCompleted(Owner.CopyContext);
                Coordinator.OnLevelUpCommitCompleted(Environment.Controller);
                CommitInProgress = false;
            }
        }
    }
}
