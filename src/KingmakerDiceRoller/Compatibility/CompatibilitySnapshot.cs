using System.Collections.Generic;

namespace KingmakerDiceRoller.Compatibility
{
    public sealed class CompatibilitySnapshot
    {
        internal CompatibilitySnapshot(IReadOnlyList<string> loaded, IReadOnlyList<string> warnings)
        {
            LoadedMods = loaded;
            Warnings = warnings;
        }

        public IReadOnlyList<string> LoadedMods { get; }
        public IReadOnlyList<string> Warnings { get; }
    }
}
