using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class RollConfiguration
    {
        public const int DefaultMinimumScore = 3;
        public const string DefaultCustomExpression = "4d[6]kh3";

        public RollConfiguration(
            DiceRollPreset preset,
            LowScorePolicy lowScorePolicy,
            int minimumScore,
            string customExpression)
        {
            Preset = preset;
            LowScorePolicy = lowScorePolicy;
            MinimumScore = minimumScore;
            CustomExpression = customExpression;
        }

        public DiceRollPreset Preset { get; }
        public LowScorePolicy LowScorePolicy { get; }
        public int MinimumScore { get; }
        public string CustomExpression { get; }
        public bool MinimumApplies => LowScorePolicy != LowScorePolicy.Tabletop;

        public static RollConfiguration Default()
        {
            return new RollConfiguration(
                DiceRollPreset.FourD6DropLowest,
                LowScorePolicy.Tabletop,
                DefaultMinimumScore,
                DefaultCustomExpression);
        }

        public bool TryCreateRule(out DiceRollRule rule, out string error)
        {
            rule = null;
            error = null;
            if (!Enum.IsDefined(typeof(DiceRollPreset), Preset))
            {
                error = "Select a supported roll preset.";
                return false;
            }
            if (!Enum.IsDefined(typeof(LowScorePolicy), LowScorePolicy))
            {
                error = "Select a supported low-score policy.";
                return false;
            }
            if (MinimumScore < RolledStatArray.MinimumScore || MinimumScore > RolledStatArray.MaximumScore)
            {
                error = "Minimum score must be between " + RolledStatArray.MinimumScore +
                    " and " + RolledStatArray.MaximumScore + ".";
                return false;
            }

            try
            {
                rule = Preset == DiceRollPreset.CustomExpression
                    ? new DiceRollRule(
                        "custom",
                        "Custom expression",
                        CustomExpression,
                        LowScorePolicy,
                        MinimumScore,
                        RolledStatArray.MaximumScore,
                        1000,
                        1000)
                    : DiceRollRule.ForPreset(Preset, LowScorePolicy, MinimumScore);
                new DiceExpressionParser().Parse(rule.Expression);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
