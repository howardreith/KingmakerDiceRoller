using System;
using System.Collections.Generic;

namespace KingmakerDiceRoller.Domain
{
    public sealed class RollHistory
    {
        public const int Capacity = 20;
        private readonly List<RollHistoryEntry> entries = new List<RollHistoryEntry>();
        private int selectedIndex = -1;

        public int Count => entries.Count;
        public int SelectedIndex => selectedIndex;
        public RollHistoryEntry Selected => selectedIndex < 0 ? null : entries[selectedIndex];

        public RollHistoryEntry Add(
            StatAssignment assignment,
            DiceRollRule rule,
            long sequence,
            string createdAtUtc,
            PointBuyEquivalent equivalent)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            var entry = new RollHistoryEntry(
                sequence,
                assignment,
                rule.Id,
                rule.Expression,
                createdAtUtc,
                equivalent);
            entries.Add(entry);
            if (entries.Count > Capacity) entries.RemoveAt(0);
            selectedIndex = entries.Count - 1;
            return entry;
        }

        public RollHistoryEntry Previous()
        {
            if (entries.Count == 0) return null;
            selectedIndex = selectedIndex <= 0 ? entries.Count - 1 : selectedIndex - 1;
            return Selected;
        }

        public RollHistoryEntry Next()
        {
            if (entries.Count == 0) return null;
            selectedIndex = selectedIndex >= entries.Count - 1 ? 0 : selectedIndex + 1;
            return Selected;
        }

        public void SelectNewest()
        {
            selectedIndex = entries.Count - 1;
        }

        public void UpdateCurrentAssignment(StatAssignment assignment)
        {
            if (selectedIndex < 0) return;
            if (!entries[selectedIndex].Assignment.RolledArray.Equals(assignment.RolledArray)) return;
            entries[selectedIndex] = entries[selectedIndex].WithAssignment(assignment);
        }

        public RollHistoryEntry[] Snapshot()
        {
            return entries.ToArray();
        }
    }
}
