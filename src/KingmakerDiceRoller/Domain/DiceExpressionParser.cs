// Grammar concepts are informed by the MIT-licensed
// FakeFriend24/wotr-dice-roller project. This parser is a bounded rewrite.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KingmakerDiceRoller.Domain
{
    public sealed class DiceExpressionParser
    {
        private const int MaximumInputLength = 256;
        private const int MaximumDepth = 16;
        private const int MaximumNodes = 256;

        private string text;
        private int position;
        private int depth;
        private int nodeCount;

        public DiceExpression Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new DiceExpressionException("Dice expression cannot be empty.");
            }

            text = Normalize(expression);
            if (text.Length > MaximumInputLength)
            {
                throw new DiceExpressionException("Dice expression exceeds " + MaximumInputLength + " characters.");
            }

            position = 0;
            depth = 0;
            nodeCount = 0;
            ExpressionNode root = ParseAdditive();
            if (!IsAtEnd)
            {
                throw Error("Unexpected character '" + Current + "'.");
            }

            return new DiceExpression(text, root);
        }

        private ExpressionNode ParseAdditive()
        {
            EnterDepth();
            try
            {
                ExpressionNode node = ParseMultiplicative();
                while (Match('+') || Match('-'))
                {
                    char operation = text[position - 1];
                    node = NewNode(new BinaryNode(operation, node, ParseMultiplicative()));
                }

                return node;
            }
            finally
            {
                ExitDepth();
            }
        }

        private ExpressionNode ParseMultiplicative()
        {
            ExpressionNode node = ParsePrimary();
            while (Match('*'))
            {
                node = NewNode(new BinaryNode('*', node, ParsePrimary()));
            }

            return node;
        }

        private ExpressionNode ParsePrimary()
        {
            ExpressionNode node;
            if (Match('('))
            {
                node = ParseAdditive();
                Require(')', "Expected ')' to close the expression.");
            }
            else
            {
                node = NewNode(new ConstantNode(ParseUnsignedInteger()));
            }

            if (Match('d'))
            {
                node = ParseDiceSuffix(node);
            }

            return node;
        }

        private ExpressionNode ParseDiceSuffix(ExpressionNode count)
        {
            Require('[', "Expected '[' after 'd'.");
            ExpressionNode first = ParseAdditive();
            ExpressionNode minimum;
            ExpressionNode maximum;
            if (Match(','))
            {
                minimum = first;
                maximum = ParseAdditive();
            }
            else
            {
                minimum = NewNode(new ConstantNode(1));
                maximum = first;
            }

            Require(']', "Expected ']' after die bounds.");

            var rerolls = new List<ExpressionNode>();
            if (Match('r'))
            {
                Require('[', "Expected '[' after reroll marker.");
                rerolls.Add(ParseAdditive());
                while (Match(','))
                {
                    rerolls.Add(ParseAdditive());
                }

                Require(']', "Expected ']' after reroll values.");
            }

            ExpressionNode keepCount = null;
            bool keepHighest = true;
            if (Match('k'))
            {
                if (Match('h'))
                {
                    keepHighest = true;
                }
                else if (Match('l'))
                {
                    keepHighest = false;
                }
                else
                {
                    throw Error("Expected 'h' or 'l' after keep marker.");
                }

                keepCount = ParsePrimary();
            }

            return NewNode(new DiceNode(count, minimum, maximum, rerolls, keepCount, keepHighest));
        }

        private int ParseUnsignedInteger()
        {
            int start = position;
            while (!IsAtEnd && char.IsDigit(Current))
            {
                position++;
            }

            if (start == position)
            {
                throw Error("Expected a non-negative integer or parenthesized expression.");
            }

            string token = text.Substring(start, position - start);
            int value;
            if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out value))
            {
                throw Error("Integer literal exceeds the Int32 range.");
            }

            return value;
        }

        private T NewNode<T>(T node) where T : ExpressionNode
        {
            nodeCount++;
            if (nodeCount > MaximumNodes)
            {
                throw Error("Expression exceeds the syntax-node limit.");
            }

            return node;
        }

        private void EnterDepth()
        {
            depth++;
            if (depth > MaximumDepth)
            {
                throw Error("Expression exceeds the nesting limit.");
            }
        }

        private void ExitDepth()
        {
            depth--;
        }

        private bool Match(char expected)
        {
            if (IsAtEnd || Current != expected)
            {
                return false;
            }

            position++;
            return true;
        }

        private void Require(char expected, string message)
        {
            if (!Match(expected))
            {
                throw Error(message);
            }
        }

        private bool IsAtEnd => position >= text.Length;

        private char Current => text[position];

        private DiceExpressionException Error(string message)
        {
            return new DiceExpressionException(message + " Position: " + position + ".");
        }

        private static string Normalize(string expression)
        {
            var builder = new StringBuilder(expression.Length);
            for (int index = 0; index < expression.Length; index++)
            {
                char value = expression[index];
                if (!char.IsWhiteSpace(value))
                {
                    builder.Append(char.ToLowerInvariant(value));
                }
            }

            return builder.ToString();
        }
    }
}
