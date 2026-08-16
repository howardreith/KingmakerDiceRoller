using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class DiceExpressionException : Exception
    {
        public DiceExpressionException(string message)
            : base(message)
        {
        }
    }
}
