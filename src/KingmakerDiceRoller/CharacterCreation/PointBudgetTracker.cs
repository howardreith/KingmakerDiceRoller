using System;
using System.Runtime.CompilerServices;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class PointBudgetTracker
    {
        private sealed class BudgetBox
        {
            internal BudgetBox(int value) { Value = value; }
            internal int Value { get; }
        }

        private readonly ConditionalWeakTable<object, BudgetBox> budgets = new ConditionalWeakTable<object, BudgetBox>();
        private readonly object sync = new object();

        public void Record(object distribution, int budget)
        {
            if (distribution == null || budget < 0) return;
            lock (sync)
            {
                budgets.Remove(distribution);
                budgets.Add(distribution, new BudgetBox(budget));
            }
        }

        public bool TryGet(object distribution, out int budget)
        {
            budget = 0;
            if (distribution == null) return false;
            BudgetBox box;
            lock (sync)
            {
                if (!budgets.TryGetValue(distribution, out box)) return false;
            }
            budget = box.Value;
            return true;
        }
    }
}
