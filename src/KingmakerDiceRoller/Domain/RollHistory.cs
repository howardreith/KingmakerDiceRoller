using System;
using System.Collections.Generic;

namespace KingmakerDiceRoller.Domain
{
    public sealed class RollHistory
    {
        public const int Capacity = 20;
        private readonly List<RollHistoryEntry> entries = new List<RollHistoryEntry>();
        private int selectedIndex = -1;
        private long activeSequence;

        public int Count => entries.Count;
        public int SelectedIndex => selectedIndex;
        public RollHistoryEntry Selected => selectedIndex < 0 ? null : entries[selectedIndex];
        public long ActiveSequence => activeSequence;

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
            activeSequence = sequence;
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
            if (activeSequence <= 0) return;
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].Sequence != activeSequence) continue;
                if (!entries[index].Assignment.RolledArray.Equals(assignment.RolledArray)) return;
                entries[index] = entries[index].WithAssignment(assignment);
                return;
            }
        }

        public void MarkSelectedActive()
        {
            activeSequence = Selected == null ? 0 : Selected.Sequence;
        }

        public void ClearActive()
        {
            activeSequence = 0;
        }

        public RollHistoryEntry[] Snapshot()
        {
            return entries.ToArray();
        }
    }
}
