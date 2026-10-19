using System;

namespace KingmakerDiceRoller.CharacterCreation
{
    // A widget may outlive one frame of its controller. Its captured identities
    // must all still match before a UI command reaches the current router.
    public sealed class NativeViewCommandBinding
    {
        private readonly object controller;
        private readonly object stableOwner;
        private readonly object allocator;

        public NativeViewCommandBinding(object controller, object stableOwner, object allocator)
        {
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
            this.stableOwner = stableOwner ?? throw new ArgumentNullException(nameof(stableOwner));
            this.allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
        }

        public bool Matches(object currentController, object currentStableOwner,
            object currentAllocator, bool active)
        {
            return active && ReferenceEquals(controller, currentController) &&
                ReferenceEquals(stableOwner, currentStableOwner) &&
                ReferenceEquals(allocator, currentAllocator);
        }
    }
}
