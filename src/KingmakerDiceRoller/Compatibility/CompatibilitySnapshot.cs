using System.Collections.Generic;

namespace KingmakerDiceRoller.Compatibility
{
    // A detected mod whose interaction is not qualified. Detection is not proof of a conflict.
    public sealed class CompatibilityNotice
    {
        internal CompatibilityNotice(string modName, string detail)
        {
            ModName = modName;
            Detail = detail;
        }

        public string ModName { get; }
        public string Detail { get; }
    }

    public sealed class CompatibilitySnapshot
    {
        internal CompatibilitySnapshot(IReadOnlyList<string> loaded, IReadOnlyList<CompatibilityNotice> unverified)
        {
            LoadedMods = loaded;
            Unverified = unverified;
            var warnings = new List<string>();
            for (int index = 0; index < unverified.Count; index++) warnings.Add(unverified[index].Detail);
            Warnings = warnings;
        }

        public IReadOnlyList<string> LoadedMods { get; }
        public IReadOnlyList<CompatibilityNotice> Unverified { get; }
        public IReadOnlyList<string> Warnings { get; }
    }
}
