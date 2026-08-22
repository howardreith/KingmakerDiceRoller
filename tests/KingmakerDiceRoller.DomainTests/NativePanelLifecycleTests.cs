using KingmakerDiceRoller.CharacterCreation;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class NativePanelLifecycleTests
    {
        internal static void EligibleAllocatorAttachesExactlyOnce()
        {
            var lifecycle = new NativePanelAttachmentLifecycle();
            var allocator = new object();
            AssertEx.Equal(NativePanelAttachmentAction.Attach, lifecycle.Observe(true, allocator));
            AssertEx.Equal(NativePanelAttachmentAction.Refresh, lifecycle.Observe(true, allocator));
            AssertEx.True(lifecycle.IsAttached);
            AssertEx.True(ReferenceEquals(allocator, lifecycle.Allocator));
        }

        internal static void ReplacementAllocatorRebindsWithoutDuplicateOwnership()
        {
            var lifecycle = new NativePanelAttachmentLifecycle();
            var first = new object();
            var second = new object();
            lifecycle.Observe(true, first);
            AssertEx.Equal(NativePanelAttachmentAction.Rebind, lifecycle.Observe(true, second));
            AssertEx.True(ReferenceEquals(second, lifecycle.Allocator));
        }

        internal static void PhaseExitDetachesOnce()
        {
            var lifecycle = new NativePanelAttachmentLifecycle();
            lifecycle.Observe(true, new object());
            AssertEx.Equal(NativePanelAttachmentAction.Detach, lifecycle.Observe(false, null));
            AssertEx.Equal(NativePanelAttachmentAction.None, lifecycle.Observe(false, null));
            AssertEx.True(!lifecycle.IsAttached);
        }

        internal static void MissingAllocatorFailsClosed()
        {
            var lifecycle = new NativePanelAttachmentLifecycle();
            AssertEx.Equal(NativePanelAttachmentAction.None, lifecycle.Observe(true, null));
            AssertEx.True(!lifecycle.IsAttached);
        }

        internal static void DisableResetPermitsFreshAttachment()
        {
            var lifecycle = new NativePanelAttachmentLifecycle();
            var allocator = new object();
            lifecycle.Observe(true, allocator);
            lifecycle.Reset();
            AssertEx.Equal(NativePanelAttachmentAction.Attach, lifecycle.Observe(true, allocator));
        }
    }
}
