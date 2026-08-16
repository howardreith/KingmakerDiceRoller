using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class PointBudgetResolver
    {
        private readonly PointBudgetTracker tracker;

        public PointBudgetResolver(PointBudgetTracker tracker)
        {
            this.tracker = tracker;
        }

        public bool TryResolve(object distribution, KingmakerContracts contracts, out int budget, out string source)
        {
            if (tracker.TryGet(distribution, out budget))
            {
                source = "captured StatsDistribution.Start argument";
                return true;
            }

            if (contracts.DistributionTotalPointsMember != null)
            {
                object value = ReflectionAccess.Read(contracts.DistributionTotalPointsMember, distribution);
                if (value is int && (int)value >= 0)
                {
                    budget = (int)value;
                    source = "allocator total-points fallback";
                    return true;
                }
            }

            budget = 0;
            source = "unavailable";
            return false;
        }
    }
}
