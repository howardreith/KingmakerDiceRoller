using System;

namespace KingmakerDiceRoller.Integration
{
    public sealed class ContractResolutionException : Exception
    {
        public ContractResolutionException(string message)
            : base(message)
        {
        }
    }
}
