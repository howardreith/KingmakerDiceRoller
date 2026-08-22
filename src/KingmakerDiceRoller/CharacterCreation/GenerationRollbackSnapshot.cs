using System;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class GenerationRollbackSnapshot
    {
        private GenerationRollbackSnapshot(
            int generation,
            AbilityValueSnapshot values,
            bool allocatorAvailable,
            int remainingPoints,
            int totalPoints)
        {
            if (generation <= 0) throw new ArgumentOutOfRangeException(nameof(generation));
            Generation = generation;
            Values = values ?? throw new ArgumentNullException(nameof(values));
            AllocatorAvailable = allocatorAvailable;
            RemainingPoints = remainingPoints;
            TotalPoints = totalPoints;
        }

        public int Generation { get; }
        public AbilityValueSnapshot Values { get; }
        public bool AllocatorAvailable { get; }
        public int RemainingPoints { get; }
        public int TotalPoints { get; }

        public static GenerationRollbackSnapshot Capture(
            int generation,
            object distribution,
            object unit,
            KingmakerContracts contracts,
            KingmakerStatAccess access)
        {
            if (access == null) throw new ArgumentNullException(nameof(access));
            return new GenerationRollbackSnapshot(
                generation,
                AbilityValueSnapshot.Capture(distribution, unit, contracts, access),
                access.ReadDistributionAvailable(distribution, contracts),
                access.ReadDistributionPoints(distribution, contracts),
                access.ReadDistributionTotalPoints(distribution, contracts));
        }

        public void Restore(
            object distribution,
            object unit,
            KingmakerContracts contracts,
            KingmakerStatAccess access)
        {
            Values.Restore(distribution, unit, contracts, access);
            access.WriteDistributionAllocatorState(
                distribution,
                AllocatorAvailable,
                RemainingPoints,
                TotalPoints,
                contracts);
        }

        public bool MatchesAssignment(int[] assignment)
        {
            return Values.DistributionMatches(assignment) || Values.UnitMatches(assignment);
        }
    }
}
