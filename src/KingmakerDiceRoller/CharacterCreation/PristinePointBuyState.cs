using System;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class PristinePointBuyState
    {
        private PristinePointBuyState(
            int allocatorBudget,
            string budgetSource,
            int capturedGeneration,
            AbilityValueSnapshot values,
            bool allocatorAvailable,
            int remainingPoints,
            int totalPoints)
        {
            if (allocatorBudget < 0) throw new ArgumentOutOfRangeException(nameof(allocatorBudget));
            if (capturedGeneration != 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capturedGeneration),
                    "The pristine point-buy state must be captured before roll ownership in generation 1.");
            }

            AllocatorBudget = allocatorBudget;
            BudgetSource = budgetSource ?? throw new ArgumentNullException(nameof(budgetSource));
            CapturedGeneration = capturedGeneration;
            Values = values ?? throw new ArgumentNullException(nameof(values));
            AllocatorAvailable = allocatorAvailable;
            RemainingPoints = remainingPoints;
            TotalPoints = totalPoints;
        }

        public int AllocatorBudget { get; }
        public string BudgetSource { get; }
        public int CapturedGeneration { get; }
        public AbilityValueSnapshot Values { get; }
        public bool AllocatorAvailable { get; }
        public int RemainingPoints { get; }
        public int TotalPoints { get; }
        public bool CapturedBeforeRollOwnership => CapturedGeneration == 1;

        public static PristinePointBuyState Capture(
            object distribution,
            object unit,
            int allocatorBudget,
            string budgetSource,
            int capturedGeneration,
            KingmakerContracts contracts,
            KingmakerStatAccess access)
        {
            if (access == null) throw new ArgumentNullException(nameof(access));
            return new PristinePointBuyState(
                allocatorBudget,
                budgetSource,
                capturedGeneration,
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

        public bool AllocatorMatches(
            object distribution,
            KingmakerContracts contracts,
            KingmakerStatAccess access)
        {
            return access.ReadDistributionAvailable(distribution, contracts) == AllocatorAvailable &&
                access.ReadDistributionPoints(distribution, contracts) == RemainingPoints &&
                access.ReadDistributionTotalPoints(distribution, contracts) == TotalPoints;
        }
    }
}
