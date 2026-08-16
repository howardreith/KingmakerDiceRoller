using UnityModManagerNet;

namespace KingmakerDiceRoller
{
    public sealed class Settings : UnityModManager.ModSettings
    {
        public bool VerboseDiagnostics = true;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
