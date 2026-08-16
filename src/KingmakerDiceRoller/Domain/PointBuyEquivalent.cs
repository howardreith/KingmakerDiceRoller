namespace KingmakerDiceRoller.Domain
{
    public sealed class PointBuyEquivalent
    {
        internal PointBuyEquivalent(int total, bool usesExtendedValues)
        {
            Total = total;
            UsesExtendedValues = usesExtendedValues;
        }

        public int Total { get; }
        public bool UsesExtendedValues { get; }

        public string Label => UsesExtendedValues
            ? Total + " (extended equivalent; values outside Kingmaker's purchase range)"
            : Total + " point-buy equivalent";
    }
}
