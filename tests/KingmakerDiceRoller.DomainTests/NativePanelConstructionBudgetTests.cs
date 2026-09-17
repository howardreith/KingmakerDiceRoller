using KingmakerDiceRoller.UI;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class NativePanelConstructionBudgetTests
    {
        internal static void RepeatedFailureExhaustsConstruction()
        {
            var budget = new NativePanelConstructionBudget();
            var allocator = new object();
            var controller = new object();
            budget.Observe(allocator, controller);
            for (int index = 0; index < NativePanelConstructionBudget.MaximumFailures; index++)
            {
                AssertEx.True(!budget.Exhausted, "construction must remain available before the failure limit");
                budget.RecordFailure();
            }
            AssertEx.True(budget.Exhausted, "the same failed construction must stop repeating");
            AssertEx.Equal(NativePanelConstructionBudget.MaximumFailures, budget.Failures);
        }

        internal static void AllocatorIdentityChangeReopensConstruction()
        {
            var budget = new NativePanelConstructionBudget();
            var first = new object();
            var controller = new object();
            budget.Observe(first, controller);
            Exhaust(budget);
            budget.Observe(new object(), controller);
            AssertEx.True(!budget.Exhausted, "a rebuilt allocator must be constructible again");
            AssertEx.Equal(0, budget.Failures);
        }

        internal static void ControllerIdentityChangeReopensConstruction()
        {
            var budget = new NativePanelConstructionBudget();
            var allocator = new object();
            budget.Observe(allocator, new object());
            Exhaust(budget);
            budget.Observe(allocator, new object());
            AssertEx.True(!budget.Exhausted, "a new session controller must be constructible again");
        }

        internal static void SameIdentityObservationKeepsFailureCount()
        {
            var budget = new NativePanelConstructionBudget();
            var allocator = new object();
            var controller = new object();
            budget.Observe(allocator, controller);
            budget.RecordFailure();
            budget.Observe(allocator, controller);
            AssertEx.Equal(1, budget.Failures, "repeated frames must not reset the failure count");
        }

        internal static void SuccessfulConstructionClearsFailureCount()
        {
            var budget = new NativePanelConstructionBudget();
            var allocator = new object();
            var controller = new object();
            budget.Observe(allocator, controller);
            budget.RecordFailure();
            budget.Clear();
            AssertEx.Equal(0, budget.Failures);
            AssertEx.True(!budget.Exhausted);
            budget.Observe(allocator, controller);
            AssertEx.Equal(0, budget.Failures, "clearing keeps the identity observation stable");
        }

        private static void Exhaust(NativePanelConstructionBudget budget)
        {
            for (int index = 0; index < NativePanelConstructionBudget.MaximumFailures; index++) budget.RecordFailure();
            AssertEx.True(budget.Exhausted);
        }
    }
}
