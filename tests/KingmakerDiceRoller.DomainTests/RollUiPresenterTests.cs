using System;
using System.Linq;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class RollUiPresenterTests
    {
        internal static void PointBuySnapshotOffersRollWithoutAssignment()
        {
            RollPanelModel model = new RollPanelPresenter().Present(Snapshot(RollSessionMode.PointBuy, null));
            AssertEx.Equal("Mode: Point Buy", model.Mode);
            AssertEx.True(model.CanRoll);
            AssertEx.True(!model.CanReroll);
            AssertEx.Equal(0, model.AssignmentRows.Count);
            AssertEx.Equal("No rolled array selected.", model.Summary);
        }

        internal static void RollSnapshotBuildsSixAssignmentRows()
        {
            int[] values = { 16, 15, 14, 12, 10, 8 };
            RollPanelModel model = new RollPanelPresenter().Present(Snapshot(RollSessionMode.Roll, values));
            AssertEx.Equal(6, model.AssignmentRows.Count);
            AssertEx.Equal("STR", model.AssignmentRows[0].Label);
            AssertEx.Equal(16, model.AssignmentRows[0].Value);
            AssertEx.True(!model.AssignmentRows[0].CanMoveUp);
            AssertEx.True(model.AssignmentRows[0].CanMoveDown);
            AssertEx.True(model.AssignmentRows[5].CanMoveUp);
            AssertEx.True(!model.AssignmentRows[5].CanMoveDown);
        }

        internal static void RollSummaryShowsTotalEquivalentAndRule()
        {
            RollPanelModel model = new RollPanelPresenter().Present(Snapshot(
                RollSessionMode.Roll,
                new[] { 16, 15, 14, 12, 10, 8 }));
            AssertEx.True(model.Summary.Contains("Total: 75"));
            AssertEx.True(model.Summary.Contains("Point-buy equivalent: 22"));
            AssertEx.True(model.Summary.Contains("4d[6]kh3"));
        }

        internal static void ExtendedSummaryIsMarked()
        {
            RollUiSnapshot snapshot = new RollUiSnapshot(
                true, RollSessionMode.Roll, RollConfiguration.Default(),
                new[] { 20, 16, 14, 12, 10, 6 }, 78, 40, true, "1d[20]",
                1, 1, "[20, 16, 14, 12, 10, 6]", 0, 0, string.Empty, string.Empty, "ok");
            RollPanelModel model = new RollPanelPresenter().Present(snapshot);
            AssertEx.True(model.Summary.Contains("(extended)"));
        }

        internal static void CustomExpressionVisibilityFollowsPreset()
        {
            var configuration = new RollConfiguration(
                DiceRollPreset.CustomExpression,
                LowScorePolicy.Tabletop,
                3,
                "2d[6]+6");
            RollPanelModel model = new RollPanelPresenter().Present(
                Snapshot(RollSessionMode.PointBuy, null, configuration));
            AssertEx.True(model.CustomVisible);
            AssertEx.Equal("2d[6]+6", model.CustomExpression);
        }

        internal static void TabletopPolicyLeavesMinimumInactive()
        {
            RollPanelModel tabletop = new RollPanelPresenter().Present(Snapshot(
                RollSessionMode.PointBuy,
                null,
                new RollConfiguration(DiceRollPreset.ThreeD6, LowScorePolicy.Tabletop, 3, "3d[6]")));
            RollPanelModel individual = new RollPanelPresenter().Present(Snapshot(
                RollSessionMode.PointBuy,
                null,
                new RollConfiguration(DiceRollPreset.ThreeD6, LowScorePolicy.RerollIndividualBelowMinimum, 9, "3d[6]")));
            AssertEx.True(!tabletop.MinimumEnabled);
            AssertEx.True(individual.MinimumEnabled);
        }

        internal static void HistorySavedAndErrorArePresented()
        {
            RollUiSnapshot snapshot = new RollUiSnapshot(
                true, RollSessionMode.Roll, RollConfiguration.Default(),
                Enumerable.Repeat(10, 6).ToArray(), 60, 0, false, "3d[6]",
                2, 3, "[10, 10, 10, 10, 10, 10]", 1, 2, "Saved 1", "bad input", "stable");
            RollPanelModel model = new RollPanelPresenter().Present(snapshot);
            AssertEx.True(model.History.Contains("2/3"));
            AssertEx.True(model.Saved.Contains("1/2"));
            AssertEx.Equal("bad input", model.Error);
            AssertEx.Equal("stable", model.Status);
        }

        internal static void PresenterHasNoCommandSideEffects()
        {
            var target = new FakeTarget(Snapshot(RollSessionMode.PointBuy, null));
            var router = new RollUiCommandRouter(target);
            new RollPanelPresenter().Present(router.Snapshot);
            AssertEx.Equal(0, target.CommandCalls);
        }

        internal static void RouterRoutesPrimaryCommands()
        {
            var target = new FakeTarget(Snapshot(RollSessionMode.Roll, Enumerable.Repeat(10, 6).ToArray()));
            var router = new RollUiCommandRouter(target);
            string error;
            AssertEx.True(router.Execute(RollUiCommand.Roll, AbilityScore.Strength, out error));
            AssertEx.True(router.Execute(RollUiCommand.Reroll, AbilityScore.Strength, out error));
            AssertEx.True(router.Execute(RollUiCommand.ReturnToPointBuy, AbilityScore.Strength, out error));
            AssertEx.Equal(3, target.CommandCalls);
        }

        internal static void RouterRoutesPositionMove()
        {
            var target = new FakeTarget(Snapshot(RollSessionMode.Roll, Enumerable.Repeat(10, 6).ToArray()));
            var router = new RollUiCommandRouter(target);
            string error;
            AssertEx.True(router.Execute(RollUiCommand.MoveDown, AbilityScore.Dexterity, out error));
            AssertEx.Equal(AbilityScore.Dexterity, target.LastAbility);
            AssertEx.True(!target.LastMoveUp);
        }

        internal static void RouterCyclesPresetAndPolicyWithWrap()
        {
            var target = new FakeTarget(Snapshot(
                RollSessionMode.PointBuy,
                null,
                new RollConfiguration(DiceRollPreset.FourD6DropLowest, LowScorePolicy.Tabletop, 3, "x")));
            var router = new RollUiCommandRouter(target);
            string error;
            AssertEx.True(router.Execute(RollUiCommand.PreviousPreset, AbilityScore.Strength, out error));
            AssertEx.Equal(DiceRollPreset.CustomExpression, target.UiSnapshot.Configuration.Preset);
            AssertEx.True(router.Execute(RollUiCommand.PreviousPolicy, AbilityScore.Strength, out error));
            AssertEx.Equal(LowScorePolicy.RerollEntireArrayBelowMinimum, target.UiSnapshot.Configuration.LowScorePolicy);
        }

        internal static void RouterKeepsMinimumWithinBoundary()
        {
            var target = new FakeTarget(Snapshot(
                RollSessionMode.PointBuy,
                null,
                new RollConfiguration(DiceRollPreset.CustomExpression, LowScorePolicy.RerollIndividualBelowMinimum, 1, "1")));
            var router = new RollUiCommandRouter(target);
            string error;
            AssertEx.True(router.Execute(RollUiCommand.DecreaseMinimum, AbilityScore.Strength, out error));
            AssertEx.Equal(1, target.UiSnapshot.Configuration.MinimumScore);
            target.SetMinimumScore(120);
            AssertEx.True(router.Execute(RollUiCommand.IncreaseMinimum, AbilityScore.Strength, out error));
            AssertEx.Equal(120, target.UiSnapshot.Configuration.MinimumScore);
        }

        internal static void RouterRoutesHistoryAndSavedCommands()
        {
            var target = new FakeTarget(Snapshot(RollSessionMode.Roll, Enumerable.Repeat(10, 6).ToArray()));
            var router = new RollUiCommandRouter(target);
            string error;
            RollUiCommand[] commands =
            {
                RollUiCommand.PreviousHistory, RollUiCommand.NextHistory, RollUiCommand.UseHistory,
                RollUiCommand.StoreCurrent, RollUiCommand.PreviousSaved, RollUiCommand.NextSaved,
                RollUiCommand.RecallSaved, RollUiCommand.DeleteSaved
            };
            foreach (RollUiCommand command in commands)
            {
                AssertEx.True(router.Execute(command, AbilityScore.Strength, out error), error);
            }
            AssertEx.Equal(commands.Length, target.CommandCalls);
        }

        internal static void RouterRoutesCustomExpression()
        {
            var target = new FakeTarget(Snapshot(RollSessionMode.PointBuy, null));
            var router = new RollUiCommandRouter(target);
            router.SetCustomExpression("2d[6]+6");
            AssertEx.Equal("2d[6]+6", target.UiSnapshot.Configuration.CustomExpression);
            AssertEx.Equal(1, target.CommandCalls);
        }

        private static RollUiSnapshot Snapshot(
            RollSessionMode mode,
            int[] values,
            RollConfiguration configuration = null)
        {
            return new RollUiSnapshot(
                true,
                mode,
                configuration ?? RollConfiguration.Default(),
                values,
                values == null ? 0 : values.Sum(),
                values == null ? 0 : new PointBuyEquivalentCalculator().Calculate(new RolledStatArray(values)).Total,
                values != null && values.Any(value => value < 7 || value > 18),
                "4d[6]kh3",
                values == null ? 0 : 1,
                values == null ? 0 : 1,
                values == null ? string.Empty : "[" + string.Join(", ", values) + "]",
                0,
                0,
                string.Empty,
                string.Empty,
                "ready");
        }

        private sealed class FakeTarget : IRollUiCommandTarget
        {
            internal FakeTarget(RollUiSnapshot snapshot) { UiSnapshot = snapshot; }
            public RollUiSnapshot UiSnapshot { get; private set; }
            public RollSession ActiveSession => null;
            public bool CanAttachNativePanel => UiSnapshot.SessionAvailable;
            internal int CommandCalls { get; private set; }
            internal AbilityScore LastAbility { get; private set; }
            internal bool LastMoveUp { get; private set; }

            public bool TryRoll(out string error) { return Hit(out error); }
            public bool TryReroll(out string error) { return Hit(out error); }
            public bool TryMoveAssignment(AbilityScore ability, bool moveUp, out string error)
            {
                LastAbility = ability;
                LastMoveUp = moveUp;
                return Hit(out error);
            }
            public void SelectPreviousHistory() { CommandCalls++; }
            public void SelectNextHistory() { CommandCalls++; }
            public bool TryUseSelectedHistory(out string error) { return Hit(out error); }
            public bool TryStoreCurrent(out string error) { return Hit(out error); }
            public void SelectPreviousSaved() { CommandCalls++; }
            public void SelectNextSaved() { CommandCalls++; }
            public bool TryRecallSelectedSaved(out string error) { return Hit(out error); }
            public bool DeleteSelectedSaved() { CommandCalls++; return true; }
            public bool TryRestorePointBuy(out string error) { return Hit(out error); }
            public void SetPreset(DiceRollPreset preset) { Replace(preset, UiSnapshot.Configuration.LowScorePolicy, UiSnapshot.Configuration.MinimumScore, UiSnapshot.Configuration.CustomExpression); }
            public void SetLowScorePolicy(LowScorePolicy policy) { Replace(UiSnapshot.Configuration.Preset, policy, UiSnapshot.Configuration.MinimumScore, UiSnapshot.Configuration.CustomExpression); }
            public void SetMinimumScore(int minimum) { Replace(UiSnapshot.Configuration.Preset, UiSnapshot.Configuration.LowScorePolicy, minimum, UiSnapshot.Configuration.CustomExpression); }
            public void SetCustomExpression(string expression) { Replace(UiSnapshot.Configuration.Preset, UiSnapshot.Configuration.LowScorePolicy, UiSnapshot.Configuration.MinimumScore, expression); }

            private bool Hit(out string error)
            {
                CommandCalls++;
                error = null;
                return true;
            }

            private void Replace(DiceRollPreset preset, LowScorePolicy policy, int minimum, string expression)
            {
                CommandCalls++;
                UiSnapshot = new RollUiSnapshot(
                    UiSnapshot.SessionAvailable, UiSnapshot.Mode,
                    new RollConfiguration(preset, policy, minimum, expression),
                    UiSnapshot.AssignedValues, UiSnapshot.Total, UiSnapshot.PointBuyEquivalent,
                    UiSnapshot.ExtendedEquivalent, UiSnapshot.RuleText,
                    UiSnapshot.HistoryPosition, UiSnapshot.HistoryCount, UiSnapshot.HistoryLabel,
                    UiSnapshot.SavedPosition, UiSnapshot.SavedCount, UiSnapshot.SavedLabel,
                    UiSnapshot.ValidationError, UiSnapshot.Status);
            }
        }
    }
}
