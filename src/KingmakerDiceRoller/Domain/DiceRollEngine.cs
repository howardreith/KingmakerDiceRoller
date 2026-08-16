using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class DiceRollEngine
    {
        private readonly DiceExpressionParser parser;
        private readonly IRandomSource randomSource;

        public DiceRollEngine(DiceExpressionParser parser, IRandomSource randomSource)
        {
            this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
            this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        }

        public RolledStatArray Generate(DiceRollRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            DiceExpression expression = parser.Parse(rule.Expression);

            switch (rule.LowScorePolicy)
            {
                case LowScorePolicy.Tabletop:
                    return GenerateOnce(expression, rule, false);
                case LowScorePolicy.RerollIndividualBelowMinimum:
                    return GenerateOnce(expression, rule, true);
                case LowScorePolicy.RerollEntireArrayBelowMinimum:
                    for (int attempt = 1; attempt <= rule.MaximumArrayAttempts; attempt++)
                    {
                        RolledStatArray candidate = GenerateOnce(expression, rule, false);
                        if (AllAtLeast(candidate, rule.MinimumScore))
                        {
                            return candidate;
                        }
                    }

                    throw new RollValidationException("Unable to generate an array meeting the configured minimum within the array-attempt limit.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(rule.LowScorePolicy));
            }
        }

        private RolledStatArray GenerateOnce(DiceExpression expression, DiceRollRule rule, bool rerollIndividual)
        {
            var scores = new int[RolledStatArray.ScoreCount];
            for (int index = 0; index < scores.Length; index++)
            {
                int attempts = 0;
                int score;
                do
                {
                    attempts++;
                    if (attempts > rule.MaximumScoreAttempts)
                    {
                        throw new RollValidationException("Unable to generate a score meeting the configured minimum within the score-attempt limit.");
                    }

                    score = expression.Evaluate(randomSource);
                    ValidateGeneratedScore(score, rule);
                }
                while (rerollIndividual && score < rule.MinimumScore);

                scores[index] = score;
            }

            return new RolledStatArray(scores);
        }

        private static void ValidateGeneratedScore(int score, DiceRollRule rule)
        {
            if (score < 1 || score > rule.MaximumScore)
            {
                throw new RollValidationException(
                    "Expression produced " + score + ", outside this rule's explicit range 1-" + rule.MaximumScore + ". No clamping was performed.");
            }
        }

        private static bool AllAtLeast(RolledStatArray array, int minimum)
        {
            for (int index = 0; index < array.Count; index++)
            {
                if (array[index] < minimum) return false;
            }

            return true;
        }
    }
}
