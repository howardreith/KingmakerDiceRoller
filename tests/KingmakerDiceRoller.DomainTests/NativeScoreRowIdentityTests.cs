using System;
using KingmakerDiceRoller.CharacterCreation;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class NativeScoreRowIdentityTests
    {
        internal static void ReorderedRowsUseExactStatKeys()
        {
            object[] abilities = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
            object[] rows = { "CON", "STR", "CHA", "DEX", "WIS", "INT" };
            int[] mapped = NativeScoreRowIdentity.Map(abilities, rows);
            AssertEx.Equal(1, mapped[0]);
            AssertEx.Equal(3, mapped[1]);
            AssertEx.Equal(0, mapped[2]);
            AssertEx.Equal(5, mapped[3]);
            AssertEx.Equal(4, mapped[4]);
            AssertEx.Equal(2, mapped[5]);
        }

        internal static void DuplicateOrUnknownRowFailsClosed()
        {
            object[] abilities = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
            AssertFails(() => NativeScoreRowIdentity.Map(abilities,
                new object[] { "STR", "DEX", "CON", "INT", "WIS", "WIS" }));
            AssertFails(() => NativeScoreRowIdentity.Map(abilities,
                new object[] { "STR", "DEX", "CON", "INT", "WIS", "LCK" }));
        }

        private static void AssertFails(Action action)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException("Invalid native row identity was accepted.");
        }
    }
}
