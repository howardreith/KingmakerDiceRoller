using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class PointBuyBaseline
    {
        private PointBuyBaseline(int budget, string budgetSource, AbilityValueSnapshot values)
        {
            Budget = budget;
            BudgetSource = budgetSource;
            Values = values;
        }

        public int Budget { get; }
        public string BudgetSource { get; }
        public AbilityValueSnapshot Values { get; }

        public static PointBuyBaseline Capture(
            object distribution,
            object unit,
            int budget,
            string budgetSource,
            KingmakerContracts contracts,
            KingmakerStatAccess access)
        {
            return new PointBuyBaseline(budget, budgetSource, AbilityValueSnapshot.Capture(distribution, unit, contracts, access));
        }
    }
}
