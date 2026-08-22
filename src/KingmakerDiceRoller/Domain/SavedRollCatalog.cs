using System;
using System.Collections.Generic;

namespace KingmakerDiceRoller.Domain
{
    public sealed class SavedRollCatalog
    {
        public const int Capacity = 10;
        private readonly List<SavedRollArrayRecord> records;
        private readonly List<string> warnings;
        private int selectedIndex;

        public SavedRollCatalog(IEnumerable<SavedRollArrayRecord> source)
        {
            records = new List<SavedRollArrayRecord>();
            warnings = new List<string>();
            if (source != null)
            {
                int sourceIndex = 0;
                foreach (SavedRollArrayRecord record in source)
                {
                    StatAssignment assignment;
                    string error = null;
                    if (record != null && record.TryCreateAssignment(out assignment, out error))
                    {
                        records.Add(SavedRollArrayRecord.Create(
                            assignment,
                            record.RuleId,
                            record.Expression,
                            record.SavedAtUtc,
                            record.Label));
                        if (records.Count == Capacity) break;
                    }
                    else
                    {
                        warnings.Add(
                            "Skipped saved array " + (sourceIndex + 1) + ": " +
                            (error ?? "record is null") + ".");
                    }
                    sourceIndex++;
                }
            }
            selectedIndex = records.Count == 0 ? -1 : 0;
        }

        public int Count => records.Count;
        public int SelectedIndex => selectedIndex;
        public SavedRollArrayRecord Selected => selectedIndex < 0 ? null : records[selectedIndex];
        public IReadOnlyList<string> Warnings => warnings;

        public void Store(SavedRollArrayRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            StatAssignment ignored;
            string error;
            if (!record.TryCreateAssignment(out ignored, out error))
            {
                throw new RollValidationException("Cannot store an invalid saved array: " + error);
            }
            records.Add(record);
            if (records.Count > Capacity) records.RemoveAt(0);
            selectedIndex = records.Count - 1;
        }

        public SavedRollArrayRecord Previous()
        {
            if (records.Count == 0) return null;
            selectedIndex = selectedIndex <= 0 ? records.Count - 1 : selectedIndex - 1;
            return Selected;
        }

        public SavedRollArrayRecord Next()
        {
            if (records.Count == 0) return null;
            selectedIndex = selectedIndex >= records.Count - 1 ? 0 : selectedIndex + 1;
            return Selected;
        }

        public bool DeleteSelected()
        {
            if (selectedIndex < 0) return false;
            records.RemoveAt(selectedIndex);
            if (records.Count == 0) selectedIndex = -1;
            else if (selectedIndex >= records.Count) selectedIndex = records.Count - 1;
            return true;
        }

        public List<SavedRollArrayRecord> ToList()
        {
            return new List<SavedRollArrayRecord>(records);
        }
    }
}
