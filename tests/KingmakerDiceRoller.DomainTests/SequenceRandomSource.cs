using System;
using System.Collections.Generic;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.DomainTests
{
    internal sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<int> values;

        internal SequenceRandomSource(params int[] values)
        {
            this.values = new Queue<int>(values ?? throw new ArgumentNullException(nameof(values)));
        }

        public int NextInclusive(int minimum, int maximum)
        {
            Calls++;
            if (values.Count == 0) throw new InvalidOperationException("Test random sequence was exhausted.");
            int value = values.Dequeue();
            if (value < minimum || value > maximum)
            {
                throw new InvalidOperationException("Test random value " + value + " is outside " + minimum + "-" + maximum + ".");
            }

            return value;
        }

        internal int Calls { get; private set; }
        internal int Remaining => values.Count;
    }
}
