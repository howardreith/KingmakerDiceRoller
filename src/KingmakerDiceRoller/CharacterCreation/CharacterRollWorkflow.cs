using System;
using System.Collections.Generic;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class CharacterRollWorkflow
    {
        private readonly DiceRollEngine engine;
        private readonly PointBuyEquivalentCalculator equivalentCalculator;
        private readonly Func<string> utcNow;
        private readonly Action<RollConfiguration, List<SavedRollArrayRecord>> changed;
        private readonly SavedRollCatalog saved;
        private long nextSequence = 1;
        private string inlineError = string.Empty;
        private string status = "Point Buy is active; no roll has been generated.";

        public CharacterRollWorkflow(
            DiceRollEngine engine,
            PointBuyEquivalentCalculator equivalentCalculator,
            RollConfiguration configuration,
            IEnumerable<SavedRollArrayRecord> savedRecords,
            Func<string> utcNow,
            Action<RollConfiguration, List<SavedRollArrayRecord>> changed)
        {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.equivalentCalculator = equivalentCalculator ?? throw new ArgumentNullException(nameof(equivalentCalculator));
            Configuration = Normalize(configuration ?? RollConfiguration.Default());
            saved = new SavedRollCatalog(savedRecords);
            this.utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            this.changed = changed;
        }

        public RollConfiguration Configuration { get; private set; }
        public string InlineError => inlineError;
        public string Status => status;
        public SavedRollCatalog Saved => saved;

        public bool TryGenerate(out RollCandidate candidate, out string error)
        {
            candidate = null;
            DiceRollRule rule;
            if (!Configuration.TryCreateRule(out rule, out error))
            {
                SetError(error);
                return false;
            }
            try
            {
                RolledStatArray array = engine.Generate(rule);
                var assignment = new StatAssignment(array);
                candidate = new RollCandidate(
                    assignment,
                    rule,
                    equivalentCalculator.Calculate(array),
                    utcNow());
                ClearError();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                SetError(error);
                return false;
            }
        }

        public long CommitGenerated(RollSession session, RollCandidate candidate, bool reroll)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            long sequence = nextSequence++;
            session.CommitRoll(candidate, sequence);
            status = reroll
                ? "Reroll verified on the live preview."
                : "Roll Mode is active on the verified live preview.";
            ClearError();
            return sequence;
        }

        public void CommitAssignment(RollSession session, StatAssignment assignment, string message)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            session.CommitRecallOrAssignment(assignment);
            status = message ?? "Assignment verified on the live preview.";
            ClearError();
        }

        public void SetPointBuyStatus()
        {
            status = "Point Buy is active; the exact pre-roll allocation was restored.";
            ClearError();
        }

        public void SetFailure(string error)
        {
            SetError(error);
        }

        public void SetPreset(DiceRollPreset preset)
        {
            SetConfiguration(new RollConfiguration(
                preset,
                Configuration.LowScorePolicy,
                Configuration.MinimumScore,
                Configuration.CustomExpression));
        }

        public void SetLowScorePolicy(LowScorePolicy policy)
        {
            SetConfiguration(new RollConfiguration(
                Configuration.Preset,
                policy,
                Configuration.MinimumScore,
                Configuration.CustomExpression));
        }

        public void SetMinimumScore(int minimum)
        {
            SetConfiguration(new RollConfiguration(
                Configuration.Preset,
                Configuration.LowScorePolicy,
                minimum,
                Configuration.CustomExpression));
        }

        public void SetCustomExpression(string expression)
        {
            SetConfiguration(new RollConfiguration(
                Configuration.Preset,
                Configuration.LowScorePolicy,
                Configuration.MinimumScore,
                expression));
        }

        public RollHistoryEntry PreviousHistory(RollSession session)
        {
            return session == null ? null : session.History.Previous();
        }

        public RollHistoryEntry NextHistory(RollSession session)
        {
            return session == null ? null : session.History.Next();
        }

        public SavedRollArrayRecord PreviousSaved()
        {
            return saved.Previous();
        }

        public SavedRollArrayRecord NextSaved()
        {
            return saved.Next();
        }

        public void StoreCurrent(RollSession session)
        {
            if (session == null || !session.IsRollMode || session.Assignment == null)
            {
                throw new InvalidOperationException("A verified Roll Mode assignment is required before storing.");
            }
            RollHistoryEntry entry = session.History.Selected;
            bool currentHistoryEntry = entry != null &&
                entry.Assignment.RolledArray.Equals(session.Assignment.RolledArray);
            string ruleId = currentHistoryEntry ? entry.RuleId : "recalled";
            string expression = currentHistoryEntry ? entry.Expression : string.Empty;
            string label = "Saved " + (saved.Count + 1);
            saved.Store(SavedRollArrayRecord.Create(
                session.Assignment,
                ruleId,
                expression,
                utcNow(),
                label));
            status = "Stored the current base array and assignment in UMM settings.";
            NotifyChanged();
        }

        public bool DeleteSelectedSaved()
        {
            bool deleted = saved.DeleteSelected();
            if (deleted)
            {
                status = "Deleted the selected saved array.";
                NotifyChanged();
            }
            return deleted;
        }

        public void Persist()
        {
            NotifyChanged();
        }

        public RollUiSnapshot Snapshot(RollSession session)
        {
            StatAssignment assignment = session == null ? null : session.Assignment;
            RollHistoryEntry history = session == null ? null : session.History.Selected;
            PointBuyEquivalent equivalent = assignment == null
                ? null
                : equivalentCalculator.Calculate(assignment.RolledArray);
            SavedRollArrayRecord selectedSaved = saved.Selected;
            return new RollUiSnapshot(
                session != null,
                session == null ? RollSessionMode.PointBuy : session.Mode,
                Configuration,
                assignment == null ? null : assignment.ToAssignedArray(),
                assignment == null ? 0 : assignment.RolledArray.Total,
                equivalent == null ? 0 : equivalent.Total,
                equivalent != null && equivalent.UsesExtendedValues,
                history == null ? string.Empty : history.Expression,
                history == null ? 0 : session.History.SelectedIndex + 1,
                session == null ? 0 : session.History.Count,
                selectedSaved == null ? 0 : saved.SelectedIndex + 1,
                saved.Count,
                selectedSaved == null ? string.Empty : selectedSaved.Label,
                inlineError,
                status);
        }

        private void SetConfiguration(RollConfiguration configuration)
        {
            Configuration = Normalize(configuration);
            ClearError();
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            changed?.Invoke(Configuration, saved.ToList());
        }

        private void SetError(string error)
        {
            inlineError = string.IsNullOrWhiteSpace(error) ? "The command failed." : error;
        }

        private void ClearError()
        {
            inlineError = string.Empty;
        }

        private static RollConfiguration Normalize(RollConfiguration value)
        {
            DiceRollPreset preset = Enum.IsDefined(typeof(DiceRollPreset), value.Preset)
                ? value.Preset
                : DiceRollPreset.FourD6DropLowest;
            LowScorePolicy policy = Enum.IsDefined(typeof(LowScorePolicy), value.LowScorePolicy)
                ? value.LowScorePolicy
                : LowScorePolicy.Tabletop;
            int minimum = value.MinimumScore < RolledStatArray.MinimumScore ||
                value.MinimumScore > RolledStatArray.MaximumScore
                ? RollConfiguration.DefaultMinimumScore
                : value.MinimumScore;
            string expression = string.IsNullOrWhiteSpace(value.CustomExpression)
                ? RollConfiguration.DefaultCustomExpression
                : value.CustomExpression;
            return new RollConfiguration(preset, policy, minimum, expression);
        }
    }
}
