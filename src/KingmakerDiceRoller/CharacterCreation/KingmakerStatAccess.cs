using System;
using System.Collections;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class KingmakerStatAccess
    {
        public int[] ReadDistributionValues(object distribution, KingmakerContracts contracts)
        {
            IDictionary dictionary = RequireDictionary(distribution, contracts);
            var values = new int[6];
            for (int index = 0; index < values.Length; index++)
            {
                object value = dictionary[contracts.AbilityStatKeys[index]];
                if (!(value is int)) throw new InvalidOperationException("Distribution value at index " + index + " is not Int32.");
                values[index] = (int)value;
            }
            return values;
        }

        public void WriteDistributionValues(object distribution, int[] values, KingmakerContracts contracts)
        {
            RequireSix(values);
            IDictionary dictionary = RequireDictionary(distribution, contracts);
            for (int index = 0; index < values.Length; index++) dictionary[contracts.AbilityStatKeys[index]] = values[index];
        }

        public bool ReadDistributionAvailable(object distribution, KingmakerContracts contracts)
        {
            object value = ReflectionAccess.Read(contracts.DistributionAvailableMember, distribution);
            if (!(value is bool)) throw new InvalidOperationException("StatsDistribution.Available is not Boolean.");
            return (bool)value;
        }

        public int ReadDistributionPoints(object distribution, KingmakerContracts contracts)
        {
            return ReadDistributionInteger(contracts.DistributionPointsMember, distribution, "Points");
        }

        public int ReadDistributionTotalPoints(object distribution, KingmakerContracts contracts)
        {
            return ReadDistributionInteger(contracts.DistributionTotalPointsMember, distribution, "TotalPoints");
        }

        public void WriteDistributionAllocatorState(
            object distribution,
            bool available,
            int remainingPoints,
            int totalPoints,
            KingmakerContracts contracts)
        {
            ReflectionAccess.Write(contracts.DistributionAvailableMember, distribution, available);
            ReflectionAccess.Write(contracts.DistributionPointsMember, distribution, remainingPoints);
            ReflectionAccess.Write(contracts.DistributionTotalPointsMember, distribution, totalPoints);
        }

        public void DisablePointBuyAllocator(object distribution, KingmakerContracts contracts)
        {
            ReflectionAccess.Write(contracts.DistributionAvailableMember, distribution, false);
            ReflectionAccess.Write(contracts.DistributionPointsMember, distribution, 0);
        }

        public int[] ReadUnitBaseValues(object unit, KingmakerContracts contracts)
        {
            object stats = ReflectionAccess.Read(contracts.UnitStatsMember, unit);
            if (stats == null) throw new InvalidOperationException("Unit stats container is unavailable.");
            var values = new int[6];
            for (int index = 0; index < values.Length; index++)
            {
                object stat = contracts.UnitStatsGetStatMethod.Invoke(stats, new[] { contracts.AbilityStatKeys[index] });
                object value = ReflectionAccess.Read(contracts.StatBaseValueMember, stat);
                if (!(value is int)) throw new InvalidOperationException("Unit base value at index " + index + " is not Int32.");
                values[index] = (int)value;
            }
            return values;
        }

        public void WriteUnitBaseValues(object unit, int[] values, KingmakerContracts contracts)
        {
            RequireSix(values);
            object stats = ReflectionAccess.Read(contracts.UnitStatsMember, unit);
            if (stats == null) throw new InvalidOperationException("Unit stats container is unavailable.");
            for (int index = 0; index < values.Length; index++)
            {
                object stat = contracts.UnitStatsGetStatMethod.Invoke(stats, new[] { contracts.AbilityStatKeys[index] });
                ReflectionAccess.Write(contracts.StatBaseValueMember, stat, values[index]);
            }
        }

        private static IDictionary RequireDictionary(object distribution, KingmakerContracts contracts)
        {
            IDictionary dictionary = ReflectionAccess.Read(contracts.DistributionStatValuesMember, distribution) as IDictionary;
            if (dictionary == null) throw new InvalidOperationException("StatsDistribution.StatValues is unavailable.");
            return dictionary;
        }

        private static int ReadDistributionInteger(System.Reflection.MemberInfo member, object distribution, string name)
        {
            object value = ReflectionAccess.Read(member, distribution);
            if (!(value is int)) throw new InvalidOperationException("StatsDistribution." + name + " is not Int32.");
            return (int)value;
        }

        private static void RequireSix(int[] values)
        {
            if (values == null || values.Length != 6) throw new ArgumentException("Exactly six ability values are required.", nameof(values));
        }
    }
}
