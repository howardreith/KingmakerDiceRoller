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
