using KingmakerDiceRoller.CharacterCreation;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class NativePanelUsabilityTests
    {
        internal static void NewOwnerStartsCollapsed()
        {
            NativeRollPanelState state = AttachedState();
            AssertEx.True(!state.IsExpanded);
            AssertEx.True(state.AccessTabActive);
        }

        internal static void CollapsedSurfaceAndBackgroundAreInactive()
        {
            NativeRollPanelState state = AttachedState();
            AssertEx.True(!state.ExpandedSurfaceActive);
            AssertEx.True(!state.ExpandedBackgroundActive);
            AssertEx.True(!state.ExpandedContentActive);
        }

        internal static void OnlyAccessTabRaycastsWhenCollapsed()
        {
            NativeRollPanelState state = AttachedState();
            AssertEx.True(state.AccessTabBlocksRaycasts);
            AssertEx.True(!state.ExpandedSurfaceBlocksRaycasts);
            AssertEx.True(!state.OwnedRootBlocksRaycasts);
        }

        internal static void OpeningChangesOnlyPresentationState()
        {
            NativeRollPanelState state = AttachedState();
            RollPanelDisclosureState before = state.Disclosure;
            state.Open();
            AssertEx.True(state.IsExpanded);
            AssertEx.Equal(before.AdvancedExpanded, state.AdvancedExpanded);
            AssertEx.Equal(before.HistoryExpanded, state.HistoryExpanded);
            AssertEx.Equal(before.SavedExpanded, state.SavedExpanded);
        }

        internal static void ClosingPreservesDisclosuresAndOwner()
        {
            NativeRollPanelState state = AttachedState();
            state.Open();
            state.ToggleAdvanced();
            state.ToggleHistory();
            state.ToggleSaved();
            state.Close();
            AssertEx.True(state.HasOwner);
            AssertEx.True(state.AdvancedExpanded && state.HistoryExpanded && state.SavedExpanded);
            AssertEx.True(state.AccessTabActive);
        }

        internal static void SameOwnerRebindPreservesExpandedChoice()
        {
            var controller = new object();
            var owner = new object();
            var state = new NativeRollPanelState();
            state.ObserveOwner(controller, owner);
            state.AttachView();
            state.Open();
            state.DetachView();
            AssertEx.True(!state.ObserveOwner(controller, owner));
            state.AttachView();
            AssertEx.True(state.ExpandedSurfaceActive);
        }

        internal static void NewOwnerResetsPresentationChoice()
        {
            var state = AttachedState();
            state.Open();
            state.ToggleAdvanced();
            AssertEx.True(state.ObserveOwner(new object(), new object()));
            state.AttachView();
            AssertEx.True(!state.IsExpanded);
            AssertEx.True(!state.AdvancedExpanded);
            AssertEx.True(state.AccessTabActive);
        }

        internal static void DetachedViewHasNoRaycastFootprint()
        {
            NativeRollPanelState state = AttachedState();
            state.Open();
            state.DetachView();
            AssertEx.True(!state.ExpandedSurfaceBlocksRaycasts);
            AssertEx.True(!state.AccessTabBlocksRaycasts);
            AssertEx.True(!state.OwnedRootBlocksRaycasts);
        }

        internal static void EndOwnerClearsAllPresentationState()
        {
            NativeRollPanelState state = AttachedState();
            state.Open();
            state.ToggleAdvanced();
            state.EndOwner();
            AssertEx.True(!state.HasOwner);
            AssertEx.True(!state.IsAttached);
            AssertEx.True(!state.IsExpanded);
            AssertEx.True(!state.AdvancedExpanded);
        }

        internal static void ExpandedStateUsesOnlyVisibleSurfaceRaycast()
        {
            NativeRollPanelState state = AttachedState();
            state.Open();
            AssertEx.True(state.ExpandedSurfaceActive);
            AssertEx.True(state.ExpandedSurfaceBlocksRaycasts);
            AssertEx.True(!state.AccessTabActive);
            AssertEx.True(!state.AccessTabBlocksRaycasts);
            AssertEx.True(!state.OwnedRootBlocksRaycasts);
        }

        internal static void LayoutUsesCodeOwnedRectangle()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            AssertEx.Equal(NativeRollPanelBackgroundShape.SolidRectangle, spec.BackgroundShape);
            AssertEx.True(!spec.UsesAllocatorFrameSprite);
            spec.Validate();
        }

        internal static void ExpandedDimensionsAreBounded()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            AssertEx.True(spec.ExpandedWidth >= 600f && spec.ExpandedWidth <= 660f);
            AssertEx.True(spec.ExpandedHeight >= 720f && spec.ExpandedHeight <= 800f);
            AssertEx.True(spec.MinimumWideWidth < spec.ExpandedWidth);
            AssertEx.True(spec.MinimumWideHeight < spec.ExpandedHeight);
        }

        internal static void HeaderAndCloseDimensionsAreBounded()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            AssertEx.True(spec.HeaderHeight >= 36f && spec.HeaderHeight <= 42f);
            AssertEx.True(spec.CloseButtonWidth >= 64f && spec.CloseButtonWidth <= 80f);
            AssertEx.True(spec.CloseButtonHeight >= 28f && spec.CloseButtonHeight <= 32f);
        }

        internal static void AccessTabDimensionsAreBounded()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            AssertEx.True(spec.AccessTabWidth >= 120f && spec.AccessTabWidth <= 150f);
            AssertEx.True(spec.AccessTabHeight >= 30f && spec.AccessTabHeight <= 38f);
        }

        internal static void TypographyAndPaddingRemainReadable()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            AssertEx.True(spec.InternalPadding >= 14);
            AssertEx.True(spec.TitleFontSize >= 18f);
            AssertEx.True(spec.SectionFontSize >= 15f);
            AssertEx.True(spec.BodyFontSize >= 14f);
            AssertEx.True(spec.StatusFontSize >= 13f);
            AssertEx.True(spec.BackgroundOpacity >= 0.94f);
        }

        internal static void SelectorAndLabelRaycastPolicyIsSafe()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            AssertEx.True(spec.SelectorValuesAreSingleLine);
            AssertEx.True(!spec.NonInteractiveLabelsRaycast);
            AssertEx.True(spec.InteractiveControlsRaycast);
            AssertEx.True(!spec.OwnedRootRaycast);
        }

        internal static void ContentIsMaskedAndScrollable()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            AssertEx.True(spec.ContentIsMasked);
            AssertEx.True(spec.UsesBoundedVerticalScroll);
            AssertEx.True(spec.UsesConditionalVerticalScroll);
            AssertEx.True(!spec.HorizontalScrollingEnabled);
        }

        internal static void AssignmentControlsFitWithinPanel()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            AssertEx.True(spec.AssignmentRowRequiredWidth <= spec.AvailableContentWidth);
        }

        internal static void AccessTabUsesBottomCenterAbilityGeometry()
        {
            AssertEx.Equal(
                NativeRollPanelAccessAnchor.BottomCenterFromAbilityGeometry,
                NativeRollPanelLayoutSpec.Default.AccessAnchor);
        }

        internal static void RepeatedAttachDetachCreatesOneModeledView()
        {
            var controller = new object();
            var owner = new object();
            var state = new NativeRollPanelState();
            state.ObserveOwner(controller, owner);
            state.AttachView();
            state.DetachView();
            state.AttachView();
            AssertEx.True(state.IsAttached);
            AssertEx.True(state.AccessTabActive);
            AssertEx.True(!state.ExpandedSurfaceActive);
        }

        internal static void MovingBetweenCreationOwnersResetsPresentation()
        {
            var state = new NativeRollPanelState();
            var controller = new object();
            state.ObserveOwner(controller, new object());
            state.AttachView();
            state.Open();
            state.ToggleAdvanced();
            AssertEx.True(state.ObserveOwner(new object(), new object()));
            state.AttachView();
            AssertEx.True(!state.IsExpanded);
            AssertEx.True(!state.AdvancedExpanded);
        }

        internal static void ResponsiveProfileIsNotCharacterState()
        {
            AssertEx.True(typeof(NativeRollPanelState).GetProperty("Profile") == null);
            AssertEx.True(typeof(NativeRollPanelState).GetProperty("LayoutProfile") == null);
        }

        private static NativeRollPanelState AttachedState()
        {
            var state = new NativeRollPanelState();
            state.ObserveOwner(new object(), new object());
            state.AttachView();
            return state;
        }
    }
}
