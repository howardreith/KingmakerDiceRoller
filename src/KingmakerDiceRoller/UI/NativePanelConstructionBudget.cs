namespace KingmakerDiceRoller.UI
{
    internal sealed class NativePanelConstructionBudget
    {
        internal const int MaximumFailures = 3;
        private object allocator;
        private object controller;
        private int failures;

        internal int Failures => failures;
        internal bool Exhausted => failures >= MaximumFailures;

        // A deterministic owned-view construction failure must not rebuild the
        // whole panel every frame; any allocator/controller identity change
        // reopens construction because the failed objects are no longer reused.
        internal void Observe(object currentAllocator, object currentController)
        {
            if (ReferenceEquals(allocator, currentAllocator) && ReferenceEquals(controller, currentController)) return;
            allocator = currentAllocator;
            controller = currentController;
            failures = 0;
        }

        internal void RecordFailure() { failures++; }

        internal void Clear() { failures = 0; }
    }
}
