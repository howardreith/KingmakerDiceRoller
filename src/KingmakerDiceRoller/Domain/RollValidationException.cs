using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class RollValidationException : Exception
    {
        public RollValidationException(string message)
            : base(message)
        {
        }
    }
}
