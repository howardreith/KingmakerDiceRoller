using KingmakerDiceRoller.CharacterCreation;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class NativeViewCommandBindingTests
    {
        internal static void SameOwnerExactAllocatorCanDispatch()
        {
            object controller = new object();
            object owner = new object();
            object allocator = new object();
            var binding = new NativeViewCommandBinding(controller, owner, allocator);
            AssertEx.True(binding.Matches(controller, owner, allocator, true));
            AssertEx.True(!binding.Matches(controller, owner, allocator, false));
        }

        internal static void OldWidgetCannotRouteToNewOwnerOrAllocator()
        {
            object controller = new object();
            object owner = new object();
            object allocator = new object();
            var binding = new NativeViewCommandBinding(controller, owner, allocator);
            AssertEx.True(!binding.Matches(new object(), owner, allocator, true));
            AssertEx.True(!binding.Matches(controller, new object(), allocator, true));
            AssertEx.True(!binding.Matches(controller, owner, new object(), true));
        }
    }
}
