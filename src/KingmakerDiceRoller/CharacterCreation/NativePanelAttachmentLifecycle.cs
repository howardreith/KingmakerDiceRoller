using System;

namespace KingmakerDiceRoller.CharacterCreation
{
    public enum NativePanelAttachmentAction
    {
        None,
        Attach,
        Refresh,
        Rebind,
        Detach
    }

    public sealed class NativePanelAttachmentLifecycle
    {
        private object allocator;

        public bool IsAttached { get; private set; }
        public object Allocator => allocator;

        public NativePanelAttachmentAction Observe(bool eligible, object currentAllocator)
        {
            if (!eligible || currentAllocator == null)
            {
                if (!IsAttached) return NativePanelAttachmentAction.None;
                Reset();
                return NativePanelAttachmentAction.Detach;
            }

            if (!IsAttached)
            {
                allocator = currentAllocator;
                IsAttached = true;
                return NativePanelAttachmentAction.Attach;
            }

            if (ReferenceEquals(allocator, currentAllocator))
            {
                return NativePanelAttachmentAction.Refresh;
            }

            allocator = currentAllocator;
            return NativePanelAttachmentAction.Rebind;
        }

        public void Reset()
        {
            allocator = null;
            IsAttached = false;
        }
    }
}
