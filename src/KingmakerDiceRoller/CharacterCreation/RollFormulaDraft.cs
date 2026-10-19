using System;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.CharacterCreation
{
    // View-owned raw text. Only an explicit valid commit changes the persisted
    // next-roll configuration; a partial edit never becomes a fallback roll.
    public sealed class RollFormulaDraft
    {
        private string committed;
        private DiceRollPreset committedPreset;

        public RollFormulaDraft(RollConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            committedPreset = configuration.Preset;
            committed = EffectiveExpression(configuration);
            Text = committed;
            CustomIntent = committedPreset == DiceRollPreset.CustomExpression;
        }

        public string Text { get; private set; }
        public string Error { get; private set; }
        public bool CustomIntent { get; private set; }
        public bool HasUncommittedEdit => Text != committed ||
            (CustomIntent && committedPreset != DiceRollPreset.CustomExpression);

        public void Edit(string text, RollConfiguration configuration)
        {
            Text = text ?? string.Empty;
            CustomIntent = true;
            Validate(configuration, out string ignored);
        }

        public bool Validate(RollConfiguration configuration, out string error)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            RollConfiguration candidate = CustomIntent
                ? new RollConfiguration(DiceRollPreset.CustomExpression,
                    configuration.LowScorePolicy, configuration.MinimumScore, Text)
                : configuration;
            DiceRollRule ignored;
            bool valid = candidate.TryCreateRule(out ignored, out error);
            Error = valid ? null : error;
            return valid;
        }

        public void CommitCustom()
        {
            if (!CustomIntent || !string.IsNullOrWhiteSpace(Error))
                throw new InvalidOperationException("Only a validated custom draft can be committed.");
            committed = Text;
            committedPreset = DiceRollPreset.CustomExpression;
        }

        public void SelectPreset(DiceRollPreset preset, RollConfiguration configuration)
        {
            if (preset == DiceRollPreset.CustomExpression)
                throw new ArgumentException("Custom formula is a focus action, not a preset selection.", nameof(preset));
            Text = DiceRollRule.ForPreset(preset, configuration.LowScorePolicy,
                configuration.MinimumScore).Expression;
            committed = Text;
            committedPreset = preset;
            CustomIntent = false;
            Error = null;
        }

        public void Revert()
        {
            Text = committed;
            CustomIntent = committedPreset == DiceRollPreset.CustomExpression;
            Error = null;
        }

        private static string EffectiveExpression(RollConfiguration configuration)
        {
            return configuration.Preset == DiceRollPreset.CustomExpression
                ? configuration.CustomExpression ?? string.Empty
                : DiceRollRule.ForPreset(configuration.Preset,
                    configuration.LowScorePolicy, configuration.MinimumScore).Expression;
        }
    }
}
