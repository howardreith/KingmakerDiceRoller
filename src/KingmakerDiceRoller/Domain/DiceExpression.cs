// Dice grammar and bucket concepts are informed by the MIT-licensed
// FakeFriend24/wotr-dice-roller project. This implementation is rewritten.
using System;
using System.Collections.Generic;

namespace KingmakerDiceRoller.Domain
{
    public sealed class DiceExpression
    {
        private readonly ExpressionNode root;

        internal DiceExpression(string normalizedText, ExpressionNode root)
        {
            NormalizedText = normalizedText ?? throw new ArgumentNullException(nameof(normalizedText));
            this.root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public string NormalizedText { get; }

        public void ValidateForRule(DiceRollRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            ExpressionBounds bounds = root.Analyze();
            if (bounds.Known && (bounds.Maximum < 1 || bounds.Minimum > rule.MaximumScore))
                throw new RollValidationException("Expression cannot produce a score in this rule's range 1-" +
                    rule.MaximumScore + ".");
            if (rule.LowScorePolicy != LowScorePolicy.Tabletop && bounds.Known &&
                bounds.Maximum < rule.MinimumScore)
                throw new RollValidationException("Expression cannot reach the configured minimum score " +
                    rule.MinimumScore + ".");
        }

        public int Evaluate(IRandomSource randomSource)
        {
            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            var budget = new EvaluationBudget(10000, 1000);
            long value = root.Evaluate(randomSource, budget);
            if (value < int.MinValue || value > int.MaxValue)
            {
                throw new DiceExpressionException("Expression result exceeds the Int32 range.");
            }

            return (int)value;
        }
    }

    internal sealed class EvaluationBudget
    {
        private int operationsRemaining;
        private int diceRemaining;

        internal EvaluationBudget(int operations, int dice)
        {
            operationsRemaining = operations;
            diceRemaining = dice;
        }

        internal void ConsumeOperation()
        {
            operationsRemaining--;
            if (operationsRemaining < 0)
            {
                throw new DiceExpressionException("Expression exceeded the operation limit.");
            }
        }

        internal void ConsumeDice(int count)
        {
            if (count < 0 || count > diceRemaining)
            {
                throw new DiceExpressionException("Expression exceeded the dice-count limit.");
            }

            diceRemaining -= count;
        }
    }

    internal abstract class ExpressionNode
    {
        internal abstract long Evaluate(IRandomSource randomSource, EvaluationBudget budget);
        internal abstract ExpressionBounds Analyze();
    }

    internal struct ExpressionBounds
    {
        internal ExpressionBounds(long minimum, long maximum, bool known)
        {
            Minimum = minimum;
            Maximum = maximum;
            Known = known;
        }
        internal long Minimum { get; }
        internal long Maximum { get; }
        internal bool Known { get; }
        internal bool IsConstant => Known && Minimum == Maximum;
        internal static ExpressionBounds Unknown => new ExpressionBounds(0, 0, false);
    }

    internal sealed class ConstantNode : ExpressionNode
    {
        private readonly int value;

        internal ConstantNode(int value)
        {
            this.value = value;
        }

        internal override long Evaluate(IRandomSource randomSource, EvaluationBudget budget)
        {
            budget.ConsumeOperation();
            return value;
        }

        internal override ExpressionBounds Analyze() => new ExpressionBounds(value, value, true);
    }

    internal sealed class BinaryNode : ExpressionNode
    {
        private readonly char operation;
        private readonly ExpressionNode left;
        private readonly ExpressionNode right;

        internal BinaryNode(char operation, ExpressionNode left, ExpressionNode right)
        {
            this.operation = operation;
            this.left = left ?? throw new ArgumentNullException(nameof(left));
            this.right = right ?? throw new ArgumentNullException(nameof(right));
        }

        internal override long Evaluate(IRandomSource randomSource, EvaluationBudget budget)
        {
            budget.ConsumeOperation();
            long leftValue = left.Evaluate(randomSource, budget);
            long rightValue = right.Evaluate(randomSource, budget);
            try
            {
                checked
                {
                    switch (operation)
                    {
                        case '+': return leftValue + rightValue;
                        case '-': return leftValue - rightValue;
                        case '*': return leftValue * rightValue;
                        default: throw new DiceExpressionException("Unsupported arithmetic operator: " + operation);
                    }
                }
            }
            catch (OverflowException)
            {
                throw new DiceExpressionException("Arithmetic overflow while evaluating the expression.");
            }
        }

        internal override ExpressionBounds Analyze()
        {
            ExpressionBounds a = left.Analyze();
            ExpressionBounds b = right.Analyze();
            if (!a.Known || !b.Known) return ExpressionBounds.Unknown;
            try
            {
                checked
                {
                    switch (operation)
                    {
                        case '+': return new ExpressionBounds(a.Minimum + b.Minimum, a.Maximum + b.Maximum, true);
                        case '-': return new ExpressionBounds(a.Minimum - b.Maximum, a.Maximum - b.Minimum, true);
                        case '*':
                            long first = a.Minimum * b.Minimum;
                            long second = a.Minimum * b.Maximum;
                            long third = a.Maximum * b.Minimum;
                            long fourth = a.Maximum * b.Maximum;
                            return new ExpressionBounds(
                                Math.Min(Math.Min(first, second), Math.Min(third, fourth)),
                                Math.Max(Math.Max(first, second), Math.Max(third, fourth)), true);
                        default: throw new DiceExpressionException("Unsupported arithmetic operator.");
                    }
                }
            }
            catch (OverflowException)
            {
                if (a.IsConstant && b.IsConstant)
                    throw new DiceExpressionException("Constant arithmetic overflows the supported range.");
                return ExpressionBounds.Unknown;
            }
        }
    }

    internal sealed class DiceNode : ExpressionNode
    {
        private readonly ExpressionNode count;
        private readonly ExpressionNode minimum;
        private readonly ExpressionNode maximum;
        private readonly IReadOnlyList<ExpressionNode> rerollValues;
        private readonly ExpressionNode keepCount;
        private readonly bool keepHighest;

        internal DiceNode(
            ExpressionNode count,
            ExpressionNode minimum,
            ExpressionNode maximum,
            IReadOnlyList<ExpressionNode> rerollValues,
            ExpressionNode keepCount,
            bool keepHighest)
        {
            this.count = count ?? throw new ArgumentNullException(nameof(count));
            this.minimum = minimum ?? throw new ArgumentNullException(nameof(minimum));
            this.maximum = maximum ?? throw new ArgumentNullException(nameof(maximum));
            this.rerollValues = rerollValues ?? throw new ArgumentNullException(nameof(rerollValues));
            this.keepCount = keepCount;
            this.keepHighest = keepHighest;
        }

        internal override long Evaluate(IRandomSource randomSource, EvaluationBudget budget)
        {
            budget.ConsumeOperation();
            int diceCount = RequireBoundedInteger(count.Evaluate(randomSource, budget), 1, 1000, "dice count");
            int minimumValue = RequireBoundedInteger(minimum.Evaluate(randomSource, budget), 1, 10000, "die minimum");
            int maximumValue = RequireBoundedInteger(maximum.Evaluate(randomSource, budget), 1, 10000, "die maximum");
            if (minimumValue > maximumValue)
            {
                throw new DiceExpressionException("Die minimum cannot exceed die maximum.");
            }

            var rerolls = new HashSet<int>();
            for (int index = 0; index < rerollValues.Count; index++)
            {
                rerolls.Add(RequireBoundedInteger(rerollValues[index].Evaluate(randomSource, budget), int.MinValue, int.MaxValue, "reroll value"));
            }

            bool everyFaceRerolled = true;
            for (int face = minimumValue; face <= maximumValue; face++)
            {
                if (!rerolls.Contains(face))
                {
                    everyFaceRerolled = false;
                    break;
                }

                if (face == int.MaxValue)
                {
                    break;
                }
            }

            if (everyFaceRerolled)
            {
                throw new DiceExpressionException("Reroll values cover every possible face.");
            }

            budget.ConsumeDice(diceCount);
            var rolls = new List<int>(diceCount);
            for (int die = 0; die < diceCount; die++)
            {
                int rerollGuard = 0;
                int roll;
                do
                {
                    roll = randomSource.NextInclusive(minimumValue, maximumValue);
                    rerollGuard++;
                    if (rerollGuard > 1000)
                    {
                        throw new DiceExpressionException("Reroll limit exceeded for one die.");
                    }
                }
                while (rerolls.Contains(roll));

                rolls.Add(roll);
            }

            if (keepCount != null)
            {
                int amount = RequireBoundedInteger(keepCount.Evaluate(randomSource, budget), 0, diceCount, "keep count");
                rolls.Sort();
                if (keepHighest)
                {
                    rolls.Reverse();
                }

                if (amount < rolls.Count)
                {
                    rolls.RemoveRange(amount, rolls.Count - amount);
                }
            }

            long total = 0;
            for (int index = 0; index < rolls.Count; index++)
            {
                total += rolls[index];
            }

            return total;
        }

        internal override ExpressionBounds Analyze()
        {
            ExpressionBounds dice = count.Analyze();
            ExpressionBounds low = minimum.Analyze();
            ExpressionBounds high = maximum.Analyze();
            if (dice.Known && (dice.Maximum < 1 || dice.Minimum > 1000))
                throw new DiceExpressionException("Dice count must be between 1 and 1000.");
            if (low.Known && (low.Maximum < 1 || low.Minimum > 10000))
                throw new DiceExpressionException("Die minimum must be between 1 and 10000.");
            if (high.Known && (high.Maximum < 1 || high.Minimum > 10000))
                throw new DiceExpressionException("Die maximum must be between 1 and 10000.");
            if (low.Known && high.Known && low.Minimum > high.Maximum)
                throw new DiceExpressionException("Die minimum cannot exceed die maximum.");

            var staticRerolls = new HashSet<long>();
            bool allStaticRerolls = true;
            foreach (ExpressionNode reroll in rerollValues)
            {
                ExpressionBounds value = reroll.Analyze();
                if (value.IsConstant) staticRerolls.Add(value.Minimum);
                else allStaticRerolls = false;
            }
            if (low.IsConstant && high.IsConstant && allStaticRerolls &&
                low.Minimum >= 1 && high.Maximum <= 10000 && low.Minimum <= high.Maximum)
            {
                bool allFaces = true;
                for (long face = low.Minimum; face <= high.Maximum; face++)
                    if (!staticRerolls.Contains(face)) { allFaces = false; break; }
                if (allFaces) throw new DiceExpressionException("Reroll values cover every possible face.");
            }

            ExpressionBounds keep = keepCount == null ? dice : keepCount.Analyze();
            if (keepCount != null && keep.Known &&
                (keep.Maximum < 0 || (dice.Known && keep.Minimum > dice.Maximum)))
                throw new DiceExpressionException("Keep count cannot exceed the dice count.");
            if (!dice.Known || !low.Known || !high.Known || !keep.Known)
                return ExpressionBounds.Unknown;
            long minDice = Math.Max(1, dice.Minimum);
            long maxDice = Math.Min(1000, dice.Maximum);
            long minKept = keepCount == null ? minDice : Math.Max(0, keep.Minimum);
            long maxKept = keepCount == null ? maxDice : Math.Min(maxDice, keep.Maximum);
            if (minKept > maxKept) return ExpressionBounds.Unknown;
            return new ExpressionBounds(minKept * Math.Max(1, low.Minimum),
                maxKept * Math.Min(10000, high.Maximum), true);
        }

        private static int RequireBoundedInteger(long value, int minimum, int maximum, string label)
        {
            if (value < minimum || value > maximum)
            {
                throw new DiceExpressionException(label + " must be between " + minimum + " and " + maximum + ".");
            }

            return (int)value;
        }
    }
}
