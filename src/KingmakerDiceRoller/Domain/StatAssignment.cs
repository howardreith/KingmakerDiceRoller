using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerDiceRoller.Domain
{
    public sealed class StatAssignment : IEquatable<StatAssignment>
    {
        private readonly int[] sourceIndexByAbility;

        public StatAssignment(RolledStatArray rolledArray)
            : this(rolledArray, new[] { 0, 1, 2, 3, 4, 5 })
        {
        }

        private StatAssignment(RolledStatArray rolledArray, int[] sourceIndexByAbility)
        {
            RolledArray = rolledArray ?? throw new ArgumentNullException(nameof(rolledArray));
            ValidatePermutation(sourceIndexByAbility);
            this.sourceIndexByAbility = (int[])sourceIndexByAbility.Clone();
        }

        public RolledStatArray RolledArray { get; }

        public static StatAssignment FromSourcePositions(
            RolledStatArray rolledArray,
            IEnumerable<int> sourcePositions)
        {
            if (sourcePositions == null) throw new ArgumentNullException(nameof(sourcePositions));
            return new StatAssignment(rolledArray, sourcePositions.ToArray());
        }

        public int GetValue(AbilityScore ability)
        {
            int index = RequireAbilityIndex(ability);
            return RolledArray[sourceIndexByAbility[index]];
        }

        public int GetSourcePosition(AbilityScore ability)
        {
            return sourceIndexByAbility[RequireAbilityIndex(ability)];
        }

        public StatAssignment Swap(AbilityScore first, AbilityScore second)
        {
            int firstIndex = RequireAbilityIndex(first);
            int secondIndex = RequireAbilityIndex(second);
            var next = (int[])sourceIndexByAbility.Clone();
            int temporary = next[firstIndex];
            next[firstIndex] = next[secondIndex];
            next[secondIndex] = temporary;
            return new StatAssignment(RolledArray, next);
        }

        public StatAssignment MoveUp(AbilityScore ability)
        {
            int index = RequireAbilityIndex(ability);
            return index == 0 ? this : Swap((AbilityScore)index, (AbilityScore)(index - 1));
        }

        public StatAssignment MoveDown(AbilityScore ability)
        {
            int index = RequireAbilityIndex(ability);
            return index == RolledStatArray.ScoreCount - 1 ? this : Swap((AbilityScore)index, (AbilityScore)(index + 1));
        }

        public int[] ToAssignedArray()
        {
            var result = new int[RolledStatArray.ScoreCount];
            for (int index = 0; index < result.Length; index++)
            {
                result[index] = RolledArray[sourceIndexByAbility[index]];
            }

            return result;
        }

        public int[] ToSourcePositions()
        {
            return (int[])sourceIndexByAbility.Clone();
        }

        public bool Equals(StatAssignment other)
        {
            if (ReferenceEquals(other, null)) return false;
            return RolledArray.Equals(other.RolledArray) && sourceIndexByAbility.SequenceEqual(other.sourceIndexByAbility);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StatAssignment);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RolledArray.GetHashCode();
                for (int index = 0; index < sourceIndexByAbility.Length; index++)
                {
                    hash = (hash * 31) + sourceIndexByAbility[index];
                }

                return hash;
            }
        }

        private static int RequireAbilityIndex(AbilityScore ability)
        {
            int index = (int)ability;
            if (index < 0 || index >= RolledStatArray.ScoreCount)
            {
                throw new ArgumentOutOfRangeException(nameof(ability));
            }

            return index;
        }

        private static void ValidatePermutation(IReadOnlyCollection<int> positions)
        {
            if (positions == null || positions.Count != RolledStatArray.ScoreCount)
            {
                throw new RollValidationException("An assignment must map exactly six positions.");
            }

            var seen = new HashSet<int>();
            foreach (int position in positions)
            {
                if (position < 0 || position >= RolledStatArray.ScoreCount || !seen.Add(position))
                {
                    throw new RollValidationException("An assignment must contain each source position exactly once.");
                }
            }
        }
    }
}
