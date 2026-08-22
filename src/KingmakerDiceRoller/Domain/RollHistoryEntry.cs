using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class RollHistoryEntry
    {
        public RollHistoryEntry(
            long sequence,
            StatAssignment assignment,
            string ruleId,
            string expression,
            string createdAtUtc,
            PointBuyEquivalent equivalent)
        {
            if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            Sequence = sequence;
            Assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            RuleId = ruleId ?? string.Empty;
            Expression = expression ?? string.Empty;
            CreatedAtUtc = createdAtUtc ?? string.Empty;
            Equivalent = equivalent ?? throw new ArgumentNullException(nameof(equivalent));
        }

        public long Sequence { get; }
        public StatAssignment Assignment { get; }
        public string RuleId { get; }
        public string Expression { get; }
        public string CreatedAtUtc { get; }
        public PointBuyEquivalent Equivalent { get; }
        public int TotalScore => Assignment.RolledArray.Total;

        public RollHistoryEntry WithAssignment(StatAssignment assignment)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            if (!assignment.RolledArray.Equals(Assignment.RolledArray))
            {
                throw new ArgumentException("A history assignment must use the entry's original rolled array.", nameof(assignment));
            }
            return new RollHistoryEntry(Sequence, assignment, RuleId, Expression, CreatedAtUtc, Equivalent);
        }
    }
}
