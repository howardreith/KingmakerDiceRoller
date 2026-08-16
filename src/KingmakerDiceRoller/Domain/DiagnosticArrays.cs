namespace KingmakerDiceRoller.Domain
{
    public static class DiagnosticArrays
    {
        public static RolledStatArray FixedPhaseTwoArray()
        {
            return new RolledStatArray(new[] { 16, 15, 14, 12, 10, 8 });
        }
    }
}
