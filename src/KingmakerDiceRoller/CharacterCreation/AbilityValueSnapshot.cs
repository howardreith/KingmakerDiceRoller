using System;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class AbilityValueSnapshot
    {
        private AbilityValueSnapshot(int[] distributionValues, int[] unitValues)
        {
            DistributionValues = distributionValues;
            UnitValues = unitValues;
        }

        public int[] DistributionValues { get; }
        public int[] UnitValues { get; }

        public static AbilityValueSnapshot Capture(object distribution, object unit, KingmakerContracts contracts, KingmakerStatAccess access)
        {
            if (access == null) throw new ArgumentNullException(nameof(access));
            return new AbilityValueSnapshot(access.ReadDistributionValues(distribution, contracts), access.ReadUnitBaseValues(unit, contracts));
        }

        public void Restore(object distribution, object unit, KingmakerContracts contracts, KingmakerStatAccess access)
        {
            access.WriteDistributionValues(distribution, DistributionValues, contracts);
            access.WriteUnitBaseValues(unit, UnitValues, contracts);
        }
    }
}
