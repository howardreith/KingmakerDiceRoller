using System;
using System.Linq;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class AbilityValueSnapshot
    {
        private readonly int[] distributionValues;
        private readonly int[] unitValues;

        private AbilityValueSnapshot(int[] distributionValues, int[] unitValues)
        {
            RequireSix(distributionValues, nameof(distributionValues));
            RequireSix(unitValues, nameof(unitValues));
            this.distributionValues = (int[])distributionValues.Clone();
            this.unitValues = (int[])unitValues.Clone();
        }

        public int[] DistributionValues => (int[])distributionValues.Clone();
        public int[] UnitValues => (int[])unitValues.Clone();

        public static AbilityValueSnapshot Capture(object distribution, object unit, KingmakerContracts contracts, KingmakerStatAccess access)
        {
            if (access == null) throw new ArgumentNullException(nameof(access));
            return new AbilityValueSnapshot(access.ReadDistributionValues(distribution, contracts), access.ReadUnitBaseValues(unit, contracts));
        }

        public void Restore(object distribution, object unit, KingmakerContracts contracts, KingmakerStatAccess access)
        {
            access.WriteDistributionValues(distribution, distributionValues, contracts);
            access.WriteUnitBaseValues(unit, unitValues, contracts);
        }

        public bool DistributionMatches(int[] expected)
        {
            RequireSix(expected, nameof(expected));
            return distributionValues.SequenceEqual(expected);
        }

        public bool UnitMatches(int[] expected)
        {
            RequireSix(expected, nameof(expected));
            return unitValues.SequenceEqual(expected);
        }

        public bool Matches(int[] expected)
        {
            return DistributionMatches(expected) && UnitMatches(expected);
        }

        private static void RequireSix(int[] values, string parameterName)
        {
            if (values == null || values.Length != 6)
            {
                throw new ArgumentException("Exactly six ability values are required.", parameterName);
            }
        }
    }
}
