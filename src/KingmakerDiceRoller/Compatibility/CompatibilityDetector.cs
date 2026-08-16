using System;
using System.Collections.Generic;
using UnityModManagerNet;

namespace KingmakerDiceRoller.Compatibility
{
    public sealed class CompatibilityDetector
    {
        private static readonly string[][] KnownMods =
        {
            new[] { "BagOfTricks", "Bag of Tricks" },
            new[] { "CallOfTheWild", "Call of the Wild" },
            new[] { "TweakOrTreat", "Tweak or Treat" },
            new[] { "RacesUnleashed", "Races Unleashed" },
            new[] { "Respecialization", "Respec", "RespecMod" }
        };

        public CompatibilitySnapshot Detect()
        {
            var loaded = new List<string>();
            var warnings = new List<string>();
            for (int group = 0; group < KnownMods.Length; group++)
            {
                UnityModManager.ModEntry found = null;
                string observedId = null;
                for (int candidate = 0; candidate < KnownMods[group].Length; candidate++)
                {
                    observedId = KnownMods[group][candidate];
                    found = UnityModManager.FindMod(observedId);
                    if (found != null) break;
                }

                if (found == null) continue;
                loaded.Add(found.Info.Id + " " + found.Info.Version);
                if (string.Equals(KnownMods[group][0], "BagOfTricks", StringComparison.Ordinal))
                {
                    warnings.Add("Bag of Tricks changes character-creation point-buy behavior. The fixed-array path is isolated, but compatibility is not qualified until the dedicated smoke matrix passes.");
                }
                else if (string.Equals(KnownMods[group][0], "Respecialization", StringComparison.Ordinal))
                {
                    warnings.Add("A respec mod is loaded. Kingmaker Dice Roller rejects respec contexts and must not be used as a respec stat editor.");
                }
            }

            return new CompatibilitySnapshot(loaded, warnings);
        }
    }
}
