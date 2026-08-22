using System;
using System.Collections.Generic;

namespace KingmakerDiceRoller.Domain
{
    public sealed class PointBuyEquivalentCalculator
    {
        private static readonly IReadOnlyDictionary<int, int> StandardCosts = new Dictionary<int, int>
        {
            { 7, -4 }, { 8, -2 }, { 9, -1 }, { 10, 0 }, { 11, 1 }, { 12, 2 },
            { 13, 3 }, { 14, 5 }, { 15, 7 }, { 16, 10 }, { 17, 13 }, { 18, 17 }
        };

        public PointBuyEquivalent Calculate(RolledStatArray array)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            int total = 0;
            bool extended = false;
            for (int index = 0; index < array.Count; index++)
            {
                int score = array[index];
                int cost;
                if (!StandardCosts.TryGetValue(score, out cost))
                {
                    cost = CalculateExtendedCost(score);
                    extended = true;
                }

                total += cost;
            }

            return new PointBuyEquivalent(total, extended);
        }

        public int CalculateScoreCost(int score)
        {
            int standard;
            return StandardCosts.TryGetValue(score, out standard) ? standard : CalculateExtendedCost(score);
        }

        private static int CalculateExtendedCost(int score)
        {
            if (score < RolledStatArray.MinimumScore || score > RolledStatArray.MaximumScore)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(score),
                    "Score must be between " + RolledStatArray.MinimumScore +
                    " and " + RolledStatArray.MaximumScore + ".");
            }

            if (score < 7)
            {
                int cost = -4;
                for (int value = 6; value >= score; value--)
                {
                    int refund = ((7 - value) / 2) + 2;
                    cost -= refund;
                }

                return cost;
            }

            int highCost = 17;
            for (int value = 19; value <= score; value++)
            {
                int increment = (value - 10) / 2;
                highCost += increment;
            }

            return highCost;
        }
    }
}
