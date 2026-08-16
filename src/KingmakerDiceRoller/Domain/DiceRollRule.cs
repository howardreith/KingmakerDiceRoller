using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class DiceRollRule
    {
        public DiceRollRule(
            string id,
            string displayName,
            string expression,
            LowScorePolicy lowScorePolicy,
            int minimumScore,
            int maximumScore,
            int maximumScoreAttempts,
            int maximumArrayAttempts)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Rule ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(expression)) throw new ArgumentException("Expression is required.", nameof(expression));
            if (minimumScore < 1 || minimumScore > 20) throw new ArgumentOutOfRangeException(nameof(minimumScore));
            if (maximumScore < minimumScore || maximumScore > 20) throw new ArgumentOutOfRangeException(nameof(maximumScore));
            if (maximumScoreAttempts < 1 || maximumScoreAttempts > 10000) throw new ArgumentOutOfRangeException(nameof(maximumScoreAttempts));
            if (maximumArrayAttempts < 1 || maximumArrayAttempts > 10000) throw new ArgumentOutOfRangeException(nameof(maximumArrayAttempts));

            Id = id;
            DisplayName = displayName;
            Expression = expression;
            LowScorePolicy = lowScorePolicy;
            MinimumScore = minimumScore;
            MaximumScore = maximumScore;
            MaximumScoreAttempts = maximumScoreAttempts;
            MaximumArrayAttempts = maximumArrayAttempts;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Expression { get; }
        public LowScorePolicy LowScorePolicy { get; }
        public int MinimumScore { get; }
        public int MaximumScore { get; }
        public int MaximumScoreAttempts { get; }
        public int MaximumArrayAttempts { get; }

        public static DiceRollRule ForPreset(DiceRollPreset preset, LowScorePolicy lowScorePolicy, int minimumScore = 3)
        {
            switch (preset)
            {
                case DiceRollPreset.FourD6DropLowest:
                    return NewPreset("4d6-drop-lowest", "4d6, drop lowest", "4d[6]kh3", lowScorePolicy, minimumScore, 18);
                case DiceRollPreset.FourD6RerollOnesDropLowest:
                    return NewPreset("4d6-reroll-ones-drop-lowest", "4d6, reroll ones, drop lowest", "4d[6]r[1]kh3", lowScorePolicy, minimumScore, 18);
                case DiceRollPreset.ThreeD6:
                    return NewPreset("3d6", "3d6", "3d[6]", lowScorePolicy, minimumScore, 18);
                case DiceRollPreset.TwoD6PlusSix:
                    return NewPreset("2d6-plus-6", "2d6 + 6", "2d[6]+6", lowScorePolicy, minimumScore, 18);
                case DiceRollPreset.OneD20:
                    return NewPreset("1d20", "1d20", "1d[20]", lowScorePolicy, minimumScore, 20);
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), "Use the constructor for a custom expression.");
            }
        }

        private static DiceRollRule NewPreset(string id, string name, string expression, LowScorePolicy policy, int minimum, int maximum)
        {
            return new DiceRollRule(id, name, expression, policy, minimum, maximum, 1000, 1000);
        }
    }
}
