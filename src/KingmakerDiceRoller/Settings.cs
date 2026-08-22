using System.Collections.Generic;
using KingmakerDiceRoller.Domain;
using UnityModManagerNet;

namespace KingmakerDiceRoller
{
    public sealed class Settings : UnityModManager.ModSettings
    {
        public bool VerboseDiagnostics = true;
        public int SelectedPreset = (int)DiceRollPreset.FourD6DropLowest;
        public int SelectedLowScorePolicy = (int)LowScorePolicy.Tabletop;
        public int MinimumScore = RollConfiguration.DefaultMinimumScore;
        public string CustomExpression = RollConfiguration.DefaultCustomExpression;
        public List<SavedRollArrayRecord> SavedArrays = new List<SavedRollArrayRecord>();

        public RollConfiguration CreateRollConfiguration()
        {
            return new RollConfiguration(
                (DiceRollPreset)SelectedPreset,
                (LowScorePolicy)SelectedLowScorePolicy,
                MinimumScore,
                CustomExpression);
        }

        public void ApplyProductState(
            RollConfiguration configuration,
            List<SavedRollArrayRecord> savedArrays)
        {
            SelectedPreset = (int)configuration.Preset;
            SelectedLowScorePolicy = (int)configuration.LowScorePolicy;
            MinimumScore = configuration.MinimumScore;
            CustomExpression = configuration.CustomExpression;
            SavedArrays = savedArrays ?? new List<SavedRollArrayRecord>();
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
