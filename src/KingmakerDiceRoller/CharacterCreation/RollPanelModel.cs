using System;
using System.Collections.Generic;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class RollPanelAssignmentRow
    {
        internal RollPanelAssignmentRow(
            AbilityScore ability,
            string label,
            int value,
            bool canMoveUp,
            bool canMoveDown)
        {
            Ability = ability;
            Label = label;
            Value = value;
            CanMoveUp = canMoveUp;
            CanMoveDown = canMoveDown;
        }

        public AbilityScore Ability { get; }
        public string Label { get; }
        public int Value { get; }
        public bool CanMoveUp { get; }
        public bool CanMoveDown { get; }
    }

    public sealed class RollPanelModel
    {
        internal RollPanelModel(
            string mode,
            string preset,
            string policy,
            string minimum,
            bool minimumEnabled,
            bool customVisible,
            string customExpression,
            bool canRoll,
            bool canReroll,
            bool canReturn,
            IReadOnlyList<RollPanelAssignmentRow> rows,
            string summary,
            string history,
            bool canUseHistory,
            string saved,
            bool canStore,
            bool canRecall,
            string error,
            string status)
        {
            Mode = mode;
            Preset = preset;
            Policy = policy;
            Minimum = minimum;
            MinimumEnabled = minimumEnabled;
            CustomVisible = customVisible;
            CustomExpression = customExpression;
            CanRoll = canRoll;
            CanReroll = canReroll;
            CanReturnToPointBuy = canReturn;
            AssignmentRows = rows;
            Summary = summary;
            History = history;
            CanUseHistory = canUseHistory;
            Saved = saved;
            CanStore = canStore;
            CanRecall = canRecall;
            Error = error;
            Status = status;
        }

        public string Mode { get; }
        public string Preset { get; }
        public string Policy { get; }
        public string Minimum { get; }
        public bool MinimumEnabled { get; }
        public bool CustomVisible { get; }
        public string CustomExpression { get; }
        public bool CanRoll { get; }
        public bool CanReroll { get; }
        public bool CanReturnToPointBuy { get; }
        public IReadOnlyList<RollPanelAssignmentRow> AssignmentRows { get; }
        public string Summary { get; }
        public string History { get; }
        public bool CanUseHistory { get; }
        public string Saved { get; }
        public bool CanStore { get; }
        public bool CanRecall { get; }
        public string Error { get; }
        public string Status { get; }
    }

    public sealed class RollPanelPresenter
    {
        private static readonly string[] AbilityLabels = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

        public RollPanelModel Present(RollUiSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var rows = new List<RollPanelAssignmentRow>();
            if (snapshot.AssignedValues != null)
            {
                for (int index = 0; index < snapshot.AssignedValues.Length; index++)
                {
                    rows.Add(new RollPanelAssignmentRow(
                        (AbilityScore)index,
                        AbilityLabels[index],
                        snapshot.AssignedValues[index],
                        snapshot.CanAssign && index > 0,
                        snapshot.CanAssign && index < snapshot.AssignedValues.Length - 1));
                }
            }

            string summary = snapshot.AssignedValues == null
                ? "No rolled array selected."
                : "Total: " + snapshot.Total + "    Point-buy equivalent: " +
                    snapshot.PointBuyEquivalent + (snapshot.ExtendedEquivalent ? " (extended)" : string.Empty) +
                    "    Rule: " + snapshot.RuleText;
            string history = snapshot.HistoryCount == 0
                ? "History: empty"
                : "History: " + snapshot.HistoryPosition + "/" + snapshot.HistoryCount +
                    (string.IsNullOrWhiteSpace(snapshot.HistoryLabel) ? string.Empty : "  " + snapshot.HistoryLabel);
            string saved = snapshot.SavedCount == 0
                ? "Saved: empty"
                : "Saved: " + snapshot.SavedPosition + "/" + snapshot.SavedCount +
                    (string.IsNullOrWhiteSpace(snapshot.SavedLabel) ? string.Empty : "  " + snapshot.SavedLabel);

            return new RollPanelModel(
                "Mode: " + FormatMode(snapshot.Mode),
                FormatPreset(snapshot.Configuration.Preset),
                FormatPolicy(snapshot.Configuration.LowScorePolicy),
                "Minimum: " + snapshot.Configuration.MinimumScore,
                snapshot.Configuration.MinimumApplies,
                snapshot.Configuration.Preset == DiceRollPreset.CustomExpression,
                snapshot.Configuration.CustomExpression,
                snapshot.CanRoll,
                snapshot.CanReroll,
                snapshot.CanReturnToPointBuy,
                rows,
                summary,
                history,
                snapshot.CanUseHistory,
                saved,
                snapshot.CanStore,
                snapshot.CanRecall,
                snapshot.ValidationError,
                snapshot.Status);
        }

        public static string FormatPreset(DiceRollPreset preset)
        {
            switch (preset)
            {
                case DiceRollPreset.FourD6DropLowest: return "4d6, drop lowest";
                case DiceRollPreset.FourD6RerollOnesDropLowest: return "4d6, reroll ones, drop lowest";
                case DiceRollPreset.ThreeD6: return "3d6";
                case DiceRollPreset.TwoD6PlusSix: return "2d6 + 6";
                case DiceRollPreset.OneD20: return "1d20";
                case DiceRollPreset.CustomExpression: return "Custom expression";
                default: return "Unsupported preset";
            }
        }

        public static string FormatPolicy(LowScorePolicy policy)
        {
            switch (policy)
            {
                case LowScorePolicy.Tabletop: return "Tabletop";
                case LowScorePolicy.RerollIndividualBelowMinimum: return "Reroll individual below minimum";
                case LowScorePolicy.RerollEntireArrayBelowMinimum: return "Reroll entire array below minimum";
                default: return "Unsupported policy";
            }
        }

        private static string FormatMode(RollSessionMode mode)
        {
            switch (mode)
            {
                case RollSessionMode.EnteringRollMode: return "Entering Roll Mode";
                case RollSessionMode.Roll: return "Roll";
                case RollSessionMode.RestoringPointBuy: return "Restoring Point Buy";
                default: return "Point Buy";
            }
        }
    }
}
