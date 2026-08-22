using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class ProductWorkflowTests
    {
        internal static void DefaultConfigurationIsTabletopFourD6()
        {
            RollConfiguration value = RollConfiguration.Default();
            AssertEx.Equal(DiceRollPreset.FourD6DropLowest, value.Preset);
            AssertEx.Equal(LowScorePolicy.Tabletop, value.LowScorePolicy);
            AssertEx.Equal(3, value.MinimumScore);
            AssertEx.True(!value.MinimumApplies);
        }

        internal static void EveryPresetBuildsExpectedExpression()
        {
            var expected = new Dictionary<DiceRollPreset, string>
            {
                { DiceRollPreset.FourD6DropLowest, "4d[6]kh3" },
                { DiceRollPreset.FourD6RerollOnesDropLowest, "4d[6]r[1]kh3" },
                { DiceRollPreset.ThreeD6, "3d[6]" },
                { DiceRollPreset.TwoD6PlusSix, "2d[6]+6" },
                { DiceRollPreset.OneD20, "1d[20]" }
            };
            foreach (KeyValuePair<DiceRollPreset, string> pair in expected)
            {
                DiceRollRule rule;
                string error;
                AssertEx.True(new RollConfiguration(pair.Key, LowScorePolicy.Tabletop, 3, "ignored")
                    .TryCreateRule(out rule, out error), error);
                AssertEx.Equal(pair.Value, rule.Expression);
            }
        }

        internal static void CustomExpressionUsesExplicitBoundary()
        {
            DiceRollRule rule;
            string error;
            AssertEx.True(new RollConfiguration(
                DiceRollPreset.CustomExpression,
                LowScorePolicy.Tabletop,
                3,
                "120").TryCreateRule(out rule, out error), error);
            AssertEx.Equal(120, rule.MaximumScore);
            RolledStatArray array = new DiceRollEngine(
                new DiceExpressionParser(),
                new SequenceRandomSource()).Generate(rule);
            AssertEx.SequenceEqual(Enumerable.Repeat(120, 6), array);
        }

        internal static void CustomExpressionRejectsAboveBoundaryWithoutClamping()
        {
            DiceRollRule rule;
            string error;
            AssertEx.True(new RollConfiguration(
                DiceRollPreset.CustomExpression,
                LowScorePolicy.Tabletop,
                3,
                "121").TryCreateRule(out rule, out error), error);
            AssertEx.Throws<RollValidationException>(() => new DiceRollEngine(
                new DiceExpressionParser(),
                new SequenceRandomSource()).Generate(rule));
        }

        internal static void InvalidCustomExpressionFailsBeforeRandomConsumption()
        {
            var random = new SequenceRandomSource(6);
            CharacterRollWorkflow workflow = NewWorkflow(
                random,
                new RollConfiguration(DiceRollPreset.CustomExpression, LowScorePolicy.Tabletop, 3, "bad"));
            RollCandidate candidate;
            string error;
            AssertEx.True(!workflow.TryGenerate(out candidate, out error));
            AssertEx.Equal(0, random.Calls);
            AssertEx.True(!string.IsNullOrWhiteSpace(workflow.InlineError));
        }

        internal static void WorkflowConstructionAndSnapshotConsumeNoRandom()
        {
            var random = new SequenceRandomSource(6);
            CharacterRollWorkflow workflow = NewWorkflow(random, RollConfiguration.Default());
            workflow.Snapshot(null);
            workflow.SetMinimumScore(4);
            AssertEx.Equal(0, random.Calls);
        }

        internal static void ExplicitGenerateProducesExactlySixScores()
        {
            var random = new SequenceRandomSource(6, 5, 4, 1, 6, 5, 4, 1, 6, 5, 4, 1,
                6, 5, 4, 1, 6, 5, 4, 1, 6, 5, 4, 1);
            CharacterRollWorkflow workflow = NewWorkflow(random, RollConfiguration.Default());
            RollCandidate candidate;
            string error;
            AssertEx.True(workflow.TryGenerate(out candidate, out error), error);
            AssertEx.SequenceEqual(Enumerable.Repeat(15, 6), candidate.Assignment.ToAssignedArray());
            AssertEx.Equal(24, random.Calls);
        }

        internal static void InvalidPresetAndPolicyFailClosed()
        {
            DiceRollRule ignored;
            string error;
            AssertEx.True(!new RollConfiguration((DiceRollPreset)99, LowScorePolicy.Tabletop, 3, "3d[6]")
                .TryCreateRule(out ignored, out error));
            AssertEx.True(!new RollConfiguration(DiceRollPreset.ThreeD6, (LowScorePolicy)99, 3, "3d[6]")
                .TryCreateRule(out ignored, out error));
        }

        internal static void MinimumOutsideBoundaryFailsClosed()
        {
            DiceRollRule ignored;
            string error;
            AssertEx.True(!new RollConfiguration(DiceRollPreset.CustomExpression, LowScorePolicy.Tabletop, 0, "3")
                .TryCreateRule(out ignored, out error));
            AssertEx.True(!new RollConfiguration(DiceRollPreset.CustomExpression, LowScorePolicy.Tabletop, 121, "3")
                .TryCreateRule(out ignored, out error));
        }

        internal static void IndividualPolicyExhaustionPreservesFailure()
        {
            var rule = new DiceRollRule("test", "test", "1", LowScorePolicy.RerollIndividualBelowMinimum, 2, 20, 2, 2);
            AssertEx.Throws<RollValidationException>(() => new DiceRollEngine(
                new DiceExpressionParser(), new SequenceRandomSource()).Generate(rule));
        }

        internal static void WholeArrayPolicyExhaustionPreservesFailure()
        {
            var rule = new DiceRollRule("test", "test", "1", LowScorePolicy.RerollEntireArrayBelowMinimum, 2, 20, 2, 2);
            AssertEx.Throws<RollValidationException>(() => new DiceRollEngine(
                new DiceExpressionParser(), new SequenceRandomSource()).Generate(rule));
        }

        internal static void SourcePositionRoundTripPreservesDuplicates()
        {
            var array = new RolledStatArray(new[] { 15, 15, 14, 12, 10, 8 });
            StatAssignment moved = new StatAssignment(array)
                .Swap(AbilityScore.Strength, AbilityScore.Dexterity)
                .MoveDown(AbilityScore.Intelligence);
            StatAssignment restored = StatAssignment.FromSourcePositions(array, moved.ToSourcePositions());
            AssertEx.True(moved.Equals(restored));
            AssertEx.SequenceEqual(new[] { 1, 0, 2, 4, 3, 5 }, restored.ToSourcePositions());
        }

        internal static void InvalidPermutationFailsClosed()
        {
            var array = new RolledStatArray(new[] { 16, 15, 14, 12, 10, 8 });
            AssertEx.Throws<RollValidationException>(() =>
                StatAssignment.FromSourcePositions(array, new[] { 0, 0, 2, 3, 4, 5 }));
        }

        internal static void AssignmentDoesNotChangeSummary()
        {
            var array = new RolledStatArray(new[] { 16, 15, 14, 12, 10, 8 });
            var calculator = new PointBuyEquivalentCalculator();
            PointBuyEquivalent before = calculator.Calculate(array);
            StatAssignment after = new StatAssignment(array).Swap(AbilityScore.Strength, AbilityScore.Charisma);
            PointBuyEquivalent afterEquivalent = calculator.Calculate(after.RolledArray);
            AssertEx.Equal(array.Total, after.RolledArray.Total);
            AssertEx.Equal(before.Total, afterEquivalent.Total);
        }

        internal static void PointBuyEquivalentSupportsScoreBoundary()
        {
            var calculator = new PointBuyEquivalentCalculator();
            AssertEx.True(calculator.CalculateScoreCost(120) > calculator.CalculateScoreCost(20));
            AssertEx.Throws<ArgumentOutOfRangeException>(() => calculator.CalculateScoreCost(121));
        }

        internal static void HistoryAddsAndSelectsNewest()
        {
            var history = new RollHistory();
            RollHistoryEntry entry = history.Add(Assignment(10), Rule(), 1, "one", Equivalent(10));
            AssertEx.Equal(1, history.Count);
            AssertEx.True(ReferenceEquals(entry, history.Selected));
        }

        internal static void HistoryEvictsOldestAtTwenty()
        {
            var history = new RollHistory();
            for (int value = 1; value <= 21; value++)
            {
                history.Add(Assignment(value), Rule(), value, value.ToString(), Equivalent(value));
            }
            AssertEx.Equal(RollHistory.Capacity, history.Count);
            AssertEx.Equal(2L, history.Snapshot()[0].Sequence);
            AssertEx.Equal(21L, history.Selected.Sequence);
        }

        internal static void HistoryNavigationWraps()
        {
            var history = new RollHistory();
            history.Add(Assignment(8), Rule(), 1, "one", Equivalent(8));
            history.Add(Assignment(9), Rule(), 2, "two", Equivalent(9));
            AssertEx.Equal(1L, history.Previous().Sequence);
            AssertEx.Equal(2L, history.Previous().Sequence);
            AssertEx.Equal(1L, history.Next().Sequence);
        }

        internal static void ReassignmentUpdatesSelectedHistoryWithoutAdding()
        {
            var history = new RollHistory();
            StatAssignment original = Assignment(10);
            history.Add(original, Rule(), 1, "one", Equivalent(10));
            StatAssignment moved = original.MoveDown(AbilityScore.Strength);
            history.UpdateCurrentAssignment(moved);
            AssertEx.Equal(1, history.Count);
            AssertEx.True(history.Selected.Assignment.Equals(moved));
        }

        internal static void DifferentArrayCannotContaminateSelectedHistory()
        {
            var history = new RollHistory();
            StatAssignment original = Assignment(10);
            history.Add(original, Rule(), 1, "one", Equivalent(10));
            history.UpdateCurrentAssignment(Assignment(11));
            AssertEx.True(history.Selected.Assignment.Equals(original));
        }

        internal static void SavedVersionOneMigratesIdentity()
        {
            var record = new SavedRollArrayRecord
            {
                SchemaVersion = 1,
                Values = new[] { 16, 15, 14, 12, 10, 8 },
                RuleId = "legacy"
            };
            StatAssignment assignment;
            string error;
            AssertEx.True(record.TryCreateAssignment(out assignment, out error), error);
            AssertEx.SequenceEqual(new[] { 0, 1, 2, 3, 4, 5 }, assignment.ToSourcePositions());
        }

        internal static void SavedVersionTwoRoundTripsAssignment()
        {
            StatAssignment moved = new StatAssignment(
                new RolledStatArray(new[] { 16, 15, 14, 12, 10, 8 }))
                .Swap(AbilityScore.Strength, AbilityScore.Charisma);
            SavedRollArrayRecord record = SavedRollArrayRecord.Create(moved, "rule", "expr", "time", "label");
            StatAssignment restored;
            string error;
            AssertEx.True(record.TryCreateAssignment(out restored, out error), error);
            AssertEx.True(moved.Equals(restored));
            AssertEx.Equal(2, record.SchemaVersion);
        }

        internal static void SavedUnsupportedSchemaIsRejected()
        {
            var record = new SavedRollArrayRecord
            {
                SchemaVersion = 99,
                Values = Enumerable.Repeat(10, 6).ToArray()
            };
            StatAssignment ignored;
            string error;
            AssertEx.True(!record.TryCreateAssignment(out ignored, out error));
            AssertEx.True(error.Contains("Unsupported"));
        }

        internal static void SavedMalformedPermutationIsRejected()
        {
            var record = new SavedRollArrayRecord
            {
                SchemaVersion = 2,
                Values = Enumerable.Repeat(10, 6).ToArray(),
                SourcePositions = new[] { 0, 1, 2, 3, 4, 4 }
            };
            StatAssignment ignored;
            string error;
            AssertEx.True(!record.TryCreateAssignment(out ignored, out error));
        }

        internal static void SavedCatalogIsolatesCorruptEntries()
        {
            var corrupt = new SavedRollArrayRecord { SchemaVersion = 2, Values = new[] { 1 } };
            SavedRollArrayRecord valid = SavedRollArrayRecord.Create(Assignment(10), "rule", "expr", "time", "valid");
            var catalog = new SavedRollCatalog(new[] { corrupt, valid });
            AssertEx.Equal(1, catalog.Count);
            AssertEx.Equal("valid", catalog.Selected.Label);
        }

        internal static void SavedCatalogEvictsAtTen()
        {
            var catalog = new SavedRollCatalog(null);
            for (int value = 1; value <= 11; value++)
            {
                catalog.Store(SavedRollArrayRecord.Create(
                    Assignment(value), "rule", "expr", value.ToString(), "Saved " + value));
            }
            AssertEx.Equal(SavedRollCatalog.Capacity, catalog.Count);
            AssertEx.Equal("Saved 2", catalog.ToList()[0].Label);
            AssertEx.Equal("Saved 11", catalog.Selected.Label);
        }

        internal static void SavedDeleteAndNavigationAreBounded()
        {
            var catalog = new SavedRollCatalog(new[]
            {
                SavedRollArrayRecord.Create(Assignment(8), "r", "e", "1", "one"),
                SavedRollArrayRecord.Create(Assignment(9), "r", "e", "2", "two")
            });
            AssertEx.Equal("two", catalog.Previous().Label);
            AssertEx.Equal("one", catalog.Next().Label);
            AssertEx.True(catalog.DeleteSelected());
            AssertEx.Equal(1, catalog.Count);
            AssertEx.True(catalog.DeleteSelected());
            AssertEx.True(!catalog.DeleteSelected());
        }

        internal static void NewSessionSnapshotStartsPointBuyWithoutArray()
        {
            CharacterRollWorkflow workflow = NewWorkflow(new SequenceRandomSource(), RollConfiguration.Default());
            RollSession session = NewSession();
            RollUiSnapshot snapshot = workflow.Snapshot(session);
            AssertEx.Equal(RollSessionMode.PointBuy, snapshot.Mode);
            AssertEx.Equal(null, snapshot.AssignedValues);
            AssertEx.True(snapshot.CanRoll);
            AssertEx.True(!snapshot.CanReroll);
        }

        internal static void GeneratedCommitAddsOneHistoryEntry()
        {
            CharacterRollWorkflow workflow = NewWorkflow(new SequenceRandomSource(), RollConfiguration.Default());
            RollSession session = NewSession();
            RollCandidate candidate = Candidate(12);
            session.BeginRollMode(NewOrigin(), candidate.Assignment);
            session.MarkApplicationStaged(1);
            session.MarkLiveApplicationVerified(1);
            workflow.CommitGenerated(session, candidate, false);
            AssertEx.Equal(RollSessionMode.Roll, session.Mode);
            AssertEx.Equal(1, session.History.Count);
            AssertEx.True(workflow.Snapshot(session).CanReroll);
        }

        internal static void RerollPreservesPointBuyOrigin()
        {
            CharacterRollWorkflow workflow = NewWorkflow(new SequenceRandomSource(), RollConfiguration.Default());
            RollSession session = NewSession();
            PointBuyOrigin origin = NewOrigin();
            RollCandidate first = Candidate(10);
            CommitCandidate(workflow, session, origin, first, false);
            RollCandidate second = Candidate(11);
            session.BeginRollReplacement(second.Assignment);
            session.MarkApplicationStaged(1);
            session.MarkLiveApplicationVerified(1);
            workflow.CommitGenerated(session, second, true);
            AssertEx.True(ReferenceEquals(origin, session.PointBuyOrigin));
            AssertEx.Equal(2, session.History.Count);
        }

        internal static void PreviewRebindAddsNoHistoryAndKeepsAssignment()
        {
            CharacterRollWorkflow workflow = NewWorkflow(new SequenceRandomSource(), RollConfiguration.Default());
            RollSession session = NewSession();
            RollCandidate candidate = Candidate(12);
            CommitCandidate(workflow, session, NewOrigin(), candidate, false);
            StatAssignment assignment = session.Assignment;
            session.Rebind(
                session.Controller,
                session.StableOwner,
                new object(),
                new object(),
                new object(),
                NewRollback(2),
                true);
            AssertEx.True(ReferenceEquals(assignment, session.Assignment));
            AssertEx.Equal(1, session.History.Count);
        }

        internal static void AbortedEntryReturnsToPointBuyWithoutOrigin()
        {
            RollSession session = NewSession();
            session.BeginRollMode(NewOrigin(), Assignment(12));
            session.AbortPendingRoll();
            AssertEx.Equal(RollSessionMode.PointBuy, session.Mode);
            AssertEx.Equal(null, session.PointBuyOrigin);
            AssertEx.Equal(null, session.Assignment);
        }

        private static CharacterRollWorkflow NewWorkflow(
            IRandomSource random,
            RollConfiguration configuration)
        {
            return new CharacterRollWorkflow(
                new DiceRollEngine(new DiceExpressionParser(), random),
                new PointBuyEquivalentCalculator(),
                configuration,
                null,
                () => "2026-08-21T00:00:00Z",
                null);
        }

        private static StatAssignment Assignment(int value)
        {
            return new StatAssignment(new RolledStatArray(Enumerable.Repeat(value, 6)));
        }

        private static DiceRollRule Rule()
        {
            return new DiceRollRule("test", "Test", "3d[6]", LowScorePolicy.Tabletop, 3, 18, 10, 10);
        }

        private static PointBuyEquivalent Equivalent(int value)
        {
            return new PointBuyEquivalentCalculator().Calculate(
                new RolledStatArray(Enumerable.Repeat(value, 6)));
        }

        private static RollCandidate Candidate(int value)
        {
            StatAssignment assignment = Assignment(value);
            return new RollCandidate(assignment, Rule(), Equivalent(value), "time");
        }

        private static void CommitCandidate(
            CharacterRollWorkflow workflow,
            RollSession session,
            PointBuyOrigin origin,
            RollCandidate candidate,
            bool reroll)
        {
            session.BeginRollMode(origin, candidate.Assignment);
            session.MarkApplicationStaged(1);
            session.MarkLiveApplicationVerified(1);
            workflow.CommitGenerated(session, candidate, reroll);
        }

        private static RollSession NewSession()
        {
            return new RollSession(
                new object(),
                new object(),
                new object(),
                new object(),
                new object(),
                NewRollback(1),
                false);
        }

        private static GenerationRollbackSnapshot NewRollback(int generation)
        {
            ConstructorInfo constructor = typeof(GenerationRollbackSnapshot).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(int), typeof(AbilityValueSnapshot), typeof(bool), typeof(int), typeof(int) },
                null);
            return (GenerationRollbackSnapshot)constructor.Invoke(
                new object[] { generation, NewAbilitySnapshot(), true, 25, 25 });
        }

        private static PointBuyOrigin NewOrigin()
        {
            ConstructorInfo constructor = typeof(PointBuyOrigin).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(int), typeof(string), typeof(int), typeof(AbilityValueSnapshot),
                    typeof(bool), typeof(int), typeof(int)
                },
                null);
            return (PointBuyOrigin)constructor.Invoke(
                new object[] { 25, "test", 1, NewAbilitySnapshot(), true, 25, 25 });
        }

        private static AbilityValueSnapshot NewAbilitySnapshot()
        {
            ConstructorInfo constructor = typeof(AbilityValueSnapshot).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(int[]), typeof(int[]) },
                null);
            return (AbilityValueSnapshot)constructor.Invoke(
                new object[] { Enumerable.Repeat(10, 6).ToArray(), Enumerable.Repeat(10, 6).ToArray() });
        }
    }
}
