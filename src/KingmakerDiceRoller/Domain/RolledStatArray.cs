using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerDiceRoller.Domain
{
    public sealed class RolledStatArray : IReadOnlyList<int>, IEquatable<RolledStatArray>
    {
        public const int ScoreCount = 6;
        public const int MinimumScore = 1;
        public const int MaximumScore = 120;
        private readonly int[] values;

        public RolledStatArray(IEnumerable<int> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            this.values = values.ToArray();
            if (this.values.Length != ScoreCount)
            {
                throw new RollValidationException("A rolled ability array must contain exactly six scores.");
            }

            for (int index = 0; index < this.values.Length; index++)
            {
                int score = this.values[index];
                if (score < MinimumScore || score > MaximumScore)
                {
                    throw new RollValidationException(
                        "Score at position " + index + " must be between " + MinimumScore +
                        " and " + MaximumScore + ".");
                }
            }
        }

        public int Count => ScoreCount;
        public int this[int index] => values[index];
        public int Total => values.Sum();

        public int[] ToArray()
        {
            return (int[])values.Clone();
        }

        public IEnumerator<int> GetEnumerator()
        {
            return ((IEnumerable<int>)values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        public bool Equals(RolledStatArray other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;
            return values.SequenceEqual(other.values);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RolledStatArray);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                for (int index = 0; index < values.Length; index++)
                {
                    hash = (hash * 31) + values[index];
                }

                return hash;
            }
        }

        public override string ToString()
        {
            return string.Join(", ", values);
        }
    }
}
