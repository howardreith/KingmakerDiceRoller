using System;

namespace KingmakerDiceRoller.CharacterCreation
{
    // Maps the native row's verified StatType key to the existing six-ability
    // assignment order. Scene traversal order and displayed values are not keys.
    public static class NativeScoreRowIdentity
    {
        public static int[] Map(object[] abilityKeys, object[] rowKeys)
        {
            if (abilityKeys == null || rowKeys == null ||
                abilityKeys.Length != 6 || rowKeys.Length != 6)
                throw new InvalidOperationException("Six native ability keys and rows are required.");
            var rowIndexByAbility = new int[6];
            for (int ability = 0; ability < 6; ability++)
            {
                if (abilityKeys[ability] == null)
                    throw new InvalidOperationException("An ability key is missing.");
                int found = -1;
                for (int row = 0; row < 6; row++)
                {
                    if (!Equals(abilityKeys[ability], rowKeys[row])) continue;
                    if (found >= 0)
                        throw new InvalidOperationException("A native ability row is duplicated.");
                    found = row;
                }
                if (found < 0)
                    throw new InvalidOperationException("A native ability row is missing.");
                rowIndexByAbility[ability] = found;
            }
            for (int first = 0; first < 6; first++)
                for (int second = first + 1; second < 6; second++)
                    if (rowIndexByAbility[first] == rowIndexByAbility[second])
                        throw new InvalidOperationException("Ability keys are not unique.");
            return rowIndexByAbility;
        }
    }
}
