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
            return AddRecorded(assignment, rule.Id, rule.Expression, sequence, createdAtUtc, equivalent);
        }

        public RollHistoryEntry AddRecorded(
            StatAssignment assignment,
            string ruleId,
            string expression,
            long sequence,
            string createdAtUtc,
            PointBuyEquivalent equivalent)
        {
            var entry = new RollHistoryEntry(
                sequence,
                assignment,
                ruleId,
                expression,
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
            if (selectedIndex <= 0) return null;
            selectedIndex--;
            return Selected;
        }

        public RollHistoryEntry Next()
        {
            if (entries.Count == 0) return null;
            if (selectedIndex < 0 || selectedIndex >= entries.Count - 1) return null;
            selectedIndex++;
            return Selected;
        }

        // Inspect first; a failed live application must not change the cursor.
        public RollHistoryEntry AdjacentActive(bool previous, bool pointBuy, out int index)
        {
            index = -1;
            if (entries.Count == 0) return null;
            if (pointBuy || activeSequence == 0)
            {
                if (!previous) return null;
                index = entries.Count - 1;
                return entries[index];
            }
            int activeIndex = entries.FindIndex(item => item.Sequence == activeSequence);
            if (activeIndex < 0) return null;
            index = activeIndex + (previous ? -1 : 1);
            return index < 0 || index >= entries.Count ? null : entries[index];
        }

        public void Activate(int index, long sequence)
        {
            if (index < 0 || index >= entries.Count || entries[index].Sequence != sequence)
                throw new InvalidOperationException("History target changed before activation.");
            selectedIndex = index;
            activeSequence = sequence;
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

        internal RollHistoryCheckpoint CaptureCheckpoint()
        {
            return new RollHistoryCheckpoint(entries.ToArray(), selectedIndex, activeSequence);
        }

        internal void RestoreCheckpoint(RollHistoryCheckpoint checkpoint)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            entries.Clear();
            entries.AddRange(checkpoint.Entries);
            selectedIndex = checkpoint.SelectedIndex;
            activeSequence = checkpoint.ActiveSequence;
        }
    }

    internal sealed class RollHistoryCheckpoint
    {
        internal RollHistoryCheckpoint(
            RollHistoryEntry[] entries, int selectedIndex, long activeSequence)
        {
            Entries = entries;
            SelectedIndex = selectedIndex;
            ActiveSequence = activeSequence;
        }

        internal RollHistoryEntry[] Entries { get; }
        internal int SelectedIndex { get; }
        internal long ActiveSequence { get; }
    }
}
