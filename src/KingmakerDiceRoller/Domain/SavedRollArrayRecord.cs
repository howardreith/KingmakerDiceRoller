using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class SavedRollArrayRecord
    {
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public int[] Values { get; set; }
        public int[] SourcePositions { get; set; }
        public string RuleId { get; set; }
        public string Expression { get; set; }
        public string SavedAtUtc { get; set; }
        public string Label { get; set; }

        public bool TryCreateArray(out RolledStatArray array, out string error)
        {
            StatAssignment assignment;
            return TryCreateAssignment(out assignment, out array, out error);
        }

        public bool TryCreateAssignment(out StatAssignment assignment, out string error)
        {
            RolledStatArray ignored;
            return TryCreateAssignment(out assignment, out ignored, out error);
        }

        public static SavedRollArrayRecord Create(
            StatAssignment assignment,
            string ruleId,
            string expression,
            string savedAtUtc,
            string label)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            return new SavedRollArrayRecord
            {
                SchemaVersion = CurrentSchemaVersion,
                Values = assignment.RolledArray.ToArray(),
                SourcePositions = assignment.ToSourcePositions(),
                RuleId = ruleId,
                Expression = expression,
                SavedAtUtc = savedAtUtc,
                Label = label
            };
        }

        private bool TryCreateAssignment(
            out StatAssignment assignment,
            out RolledStatArray array,
            out string error)
        {
            assignment = null;
            array = null;
            error = null;
            if (SchemaVersion != 1 && SchemaVersion != CurrentSchemaVersion)
            {
                error = "Unsupported saved-array schema version: " + SchemaVersion + ".";
                return false;
            }

            try
            {
                array = new RolledStatArray(Values);
                assignment = SchemaVersion == 1
                    ? new StatAssignment(array)
                    : StatAssignment.FromSourcePositions(array, SourcePositions);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
