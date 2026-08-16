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

        private static void RequireSix(int[] values)
        {
            if (values == null || values.Length != 6) throw new ArgumentException("Exactly six ability values are required.", nameof(values));
        }
    }
}
