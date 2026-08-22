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
        internal RollPanelModel() { }

        public string AccessTabLabel { get; internal set; }
        public string CloseLabel { get; internal set; }
        public string Mode { get; internal set; }
        public string RollMethodCaption { get; internal set; }
        public string Preset { get; internal set; }
        public string AdvancedLabel { get; internal set; }
        public string LowScoreRuleCaption { get; internal set; }
        public string Policy { get; internal set; }
        public string MinimumCaption { get; internal set; }
        public string Minimum { get; internal set; }
        public bool MinimumEnabled { get; internal set; }
        public bool MinimumVisible { get; internal set; }
        public bool CustomVisible { get; internal set; }
        public string CustomExpression { get; internal set; }
        public bool AdvancedVisible { get; internal set; }
        public bool AdvancedExpanded { get; internal set; }
        public bool RollVisible { get; internal set; }
        public bool RerollVisible { get; internal set; }
        public bool ReturnToPointBuyVisible { get; internal set; }
        public string ReturnToPointBuyLabel { get; internal set; }
        public bool CanRoll { get; internal set; }
        public bool CanReroll { get; internal set; }
        public bool CanReturnToPointBuy { get; internal set; }
        public bool AssignmentVisible { get; internal set; }
        public IReadOnlyList<RollPanelAssignmentRow> AssignmentRows { get; internal set; }
        public bool SummaryVisible { get; internal set; }
        public string Summary { get; internal set; }
        public string HistoryDisclosureLabel { get; internal set; }
        public bool HistoryDisclosureVisible { get; internal set; }
        public bool HistoryDetailsVisible { get; internal set; }
        public string History { get; internal set; }
        public bool CanUseHistory { get; internal set; }
        public string SavedDisclosureLabel { get; internal set; }
        public bool SavedDisclosureVisible { get; internal set; }
        public bool SavedDetailsVisible { get; internal set; }
        public string Saved { get; internal set; }
        public bool CanStore { get; internal set; }
        public bool CanRecall { get; internal set; }
        public bool CanDeleteSaved { get; internal set; }
        public string Error { get; internal set; }
        public string Status { get; internal set; }
    }

    public sealed class RollPanelPresenter
    {
        private static readonly string[] AbilityLabels = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

        public RollPanelModel Present(RollUiSnapshot snapshot) =>
            Present(snapshot, RollPanelDisclosureState.AllCollapsed);

        public RollPanelModel Present(
            RollUiSnapshot snapshot,
            RollPanelDisclosureState disclosure)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (disclosure == null) throw new ArgumentNullException(nameof(disclosure));

            bool rollMode = snapshot.Mode == RollSessionMode.Roll;
            bool minimumPolicy = snapshot.Configuration.MinimumApplies;
            var rows = new List<RollPanelAssignmentRow>();
            if (rollMode && snapshot.AssignedValues != null)
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
                ? string.Empty
                : "Total: " + snapshot.Total + "\nPoint-buy equivalent: " +
                    snapshot.PointBuyEquivalent + (snapshot.ExtendedEquivalent ? " (extended)" : string.Empty) +
                    "\nRoll method: " + snapshot.RuleText;
            string history = snapshot.HistoryCount == 0
                ? "No rolls in this session."
                : snapshot.HistoryPosition + "/" + snapshot.HistoryCount +
                    (string.IsNullOrWhiteSpace(snapshot.HistoryLabel) ? string.Empty : "  " + snapshot.HistoryLabel);
            string saved = snapshot.SavedCount == 0
                ? "No saved arrays."
                : snapshot.SavedPosition + "/" + snapshot.SavedCount +
                    (string.IsNullOrWhiteSpace(snapshot.SavedLabel) ? string.Empty : "  " + snapshot.SavedLabel);

            return new RollPanelModel
            {
                AccessTabLabel = "Roll Stats",
                CloseLabel = "Close",
                Mode = FormatMode(snapshot.Mode),
                RollMethodCaption = "Roll method",
                Preset = FormatPreset(snapshot.Configuration.Preset),
                AdvancedLabel = disclosure.AdvancedExpanded ? "Roll Options -" : "Roll Options +",
                LowScoreRuleCaption = "Low-score rule",
                Policy = FormatPolicy(snapshot.Configuration.LowScorePolicy),
                MinimumCaption = "Minimum",
                Minimum = snapshot.Configuration.MinimumScore.ToString(),
                MinimumEnabled = minimumPolicy,
                MinimumVisible = disclosure.AdvancedExpanded && minimumPolicy,
                CustomVisible = disclosure.AdvancedExpanded &&
                    snapshot.Configuration.Preset == DiceRollPreset.CustomExpression,
                CustomExpression = snapshot.Configuration.CustomExpression,
                AdvancedVisible = !rollMode,
                AdvancedExpanded = !rollMode && disclosure.AdvancedExpanded,
                RollVisible = !rollMode,
                RerollVisible = rollMode,
                ReturnToPointBuyVisible = rollMode,
                ReturnToPointBuyLabel = "Return to Point Buy",
                CanRoll = snapshot.CanRoll,
                CanReroll = snapshot.CanReroll,
                CanReturnToPointBuy = snapshot.CanReturnToPointBuy,
                AssignmentVisible = rollMode && rows.Count > 0,
                AssignmentRows = rows,
                SummaryVisible = rollMode && snapshot.AssignedValues != null,
                Summary = summary,
                HistoryDisclosureLabel = "History (" + snapshot.HistoryCount + ") " +
                    (disclosure.HistoryExpanded ? "-" : "+"),
                HistoryDisclosureVisible = rollMode && snapshot.HistoryCount > 0,
                HistoryDetailsVisible = rollMode && snapshot.HistoryCount > 0 && disclosure.HistoryExpanded,
                History = history,
                CanUseHistory = snapshot.CanUseHistory,
                SavedDisclosureLabel = "Saved (" + snapshot.SavedCount + ") " +
                    (disclosure.SavedExpanded ? "-" : "+"),
                SavedDisclosureVisible = snapshot.SavedCount > 0 || snapshot.CanStore,
                SavedDetailsVisible = (snapshot.SavedCount > 0 || snapshot.CanStore) && disclosure.SavedExpanded,
                Saved = saved,
                CanStore = snapshot.CanStore,
                CanRecall = snapshot.CanRecall,
                CanDeleteSaved = snapshot.SavedCount > 0,
                Error = snapshot.ValidationError,
                Status = FormatStatus(snapshot)
            };
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
                case LowScorePolicy.Tabletop: return "Keep all rolls";
                case LowScorePolicy.RerollIndividualBelowMinimum: return "Reroll low scores";
                case LowScorePolicy.RerollEntireArrayBelowMinimum: return "Reroll whole array";
                default: return "Unsupported policy";
            }
        }

        private static string FormatMode(RollSessionMode mode)
        {
            switch (mode)
            {
                case RollSessionMode.EnteringRollMode: return "Entering Roll Mode";
                case RollSessionMode.Roll: return "Roll Mode";
                case RollSessionMode.RestoringPointBuy: return "Restoring Point Buy";
                default: return "Point Buy";
            }
        }

        private static string FormatStatus(RollUiSnapshot snapshot)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.ValidationError)) return "Check the highlighted option.";
            switch (snapshot.Mode)
            {
                case RollSessionMode.Roll: return "Array applied.";
                case RollSessionMode.RestoringPointBuy: return "Restoring Point Buy...";
                case RollSessionMode.EnteringRollMode: return "Applying roll...";
                default: return snapshot.AssignedValues == null ? "Roll ready." : "Point Buy restored.";
            }
        }
    }
}
