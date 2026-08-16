using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;
        private readonly object sync = new object();

        public SystemRandomSource()
            : this(new Random())
        {
        }

        public SystemRandomSource(int seed)
            : this(new Random(seed))
        {
        }

        private SystemRandomSource(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int NextInclusive(int minimum, int maximum)
        {
            if (minimum > maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(minimum), "Minimum cannot exceed maximum.");
            }

            if (maximum == int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum), "The inclusive maximum must be below Int32.MaxValue.");
            }

            lock (sync)
            {
                return random.Next(minimum, maximum + 1);
            }
        }
    }
}
