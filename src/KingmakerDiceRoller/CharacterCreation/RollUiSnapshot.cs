using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class RollUiSnapshot
    {
        public RollUiSnapshot(
            bool sessionAvailable,
            RollSessionMode mode,
            RollConfiguration configuration,
            int[] assignedValues,
            int total,
            int pointBuyEquivalent,
            bool extendedEquivalent,
            string ruleText,
            int historyPosition,
            int historyCount,
            int savedPosition,
            int savedCount,
            string savedLabel,
            string validationError,
            string status)
        {
            SessionAvailable = sessionAvailable;
            Mode = mode;
            Configuration = configuration;
            AssignedValues = assignedValues;
            Total = total;
            PointBuyEquivalent = pointBuyEquivalent;
            ExtendedEquivalent = extendedEquivalent;
            RuleText = ruleText ?? string.Empty;
            HistoryPosition = historyPosition;
            HistoryCount = historyCount;
            SavedPosition = savedPosition;
            SavedCount = savedCount;
            SavedLabel = savedLabel ?? string.Empty;
            ValidationError = validationError ?? string.Empty;
            Status = status ?? string.Empty;
        }

        public bool SessionAvailable { get; }
        public RollSessionMode Mode { get; }
        public RollConfiguration Configuration { get; }
        public int[] AssignedValues { get; }
        public int Total { get; }
        public int PointBuyEquivalent { get; }
        public bool ExtendedEquivalent { get; }
        public string RuleText { get; }
        public int HistoryPosition { get; }
        public int HistoryCount { get; }
        public int SavedPosition { get; }
        public int SavedCount { get; }
        public string SavedLabel { get; }
        public string ValidationError { get; }
        public string Status { get; }
        public bool CanRoll => SessionAvailable && Mode == RollSessionMode.PointBuy;
        public bool CanReroll => SessionAvailable && Mode == RollSessionMode.Roll;
        public bool CanReturnToPointBuy => CanReroll;
        public bool CanAssign => CanReroll && AssignedValues != null;
        public bool CanStore => CanAssign;
        public bool CanUseHistory => SessionAvailable && HistoryCount > 0;
        public bool CanRecall => SessionAvailable && SavedCount > 0 &&
            (Mode == RollSessionMode.PointBuy || Mode == RollSessionMode.Roll);
    }
}
