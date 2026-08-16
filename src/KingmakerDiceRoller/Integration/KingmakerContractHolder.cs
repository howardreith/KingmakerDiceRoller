namespace KingmakerDiceRoller.Integration
{
    public sealed class KingmakerContractHolder
    {
        public KingmakerContracts Current { get; private set; }

        public void Set(KingmakerContracts contracts)
        {
            Current = contracts;
        }

        public void Clear()
        {
            Current = null;
        }
    }
}
