using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class RollCandidate
    {
        public RollCandidate(
            StatAssignment assignment,
            DiceRollRule rule,
            PointBuyEquivalent equivalent,
            string createdAtUtc)
        {
            Assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            Equivalent = equivalent ?? throw new ArgumentNullException(nameof(equivalent));
            CreatedAtUtc = createdAtUtc ?? string.Empty;
        }

        public StatAssignment Assignment { get; }
        public DiceRollRule Rule { get; }
        public PointBuyEquivalent Equivalent { get; }
        public string CreatedAtUtc { get; }
    }
}
