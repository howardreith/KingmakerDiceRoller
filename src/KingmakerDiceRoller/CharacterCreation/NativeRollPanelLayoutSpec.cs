using System;

namespace KingmakerDiceRoller.CharacterCreation
{
    public enum NativeRollPanelBackgroundShape
    {
        SolidRectangle
    }

    public enum NativeRollPanelAccessAnchor
    {
        BottomCenterFromAbilityGeometry
    }

    public sealed class NativeRollPanelLayoutSpec
    {
        private NativeRollPanelLayoutSpec()
        {
            BackgroundShape = NativeRollPanelBackgroundShape.SolidRectangle;
            AccessAnchor = NativeRollPanelAccessAnchor.BottomCenterFromAbilityGeometry;
            PreferredExpandedWidth = 620f;
            PreferredExpandedHeight = 760f;
            CompactPreferredWidth = 460f;
            CompactPreferredHeight = 650f;
            MinimumWideWidth = 560f;
            MinimumWideHeight = 680f;
            CompactMinimumWidth = 380f;
            CompactMinimumHeight = 500f;
            AccessTabWidth = 140f;
            AccessTabHeight = 34f;
            AccessTabSafeGap = 8f;
            InternalPadding = 16;
            SafeLeftInset = 18f;
            SafeTopInset = 18f;
            SafeRightInset = 18f;
            SafeBottomInset = 92f;
            HeaderHeight = 38f;
            FooterHeight = 38f;
            SurfaceVerticalPadding = 12f;
            MajorVerticalSpacing = 6f;
            OrdinaryControlHeight = 30f;
            AssignmentRowHeight = 28f;
            CloseButtonWidth = 76f;
            CloseButtonHeight = 30f;
            ScrollbarWidth = 10f;
            OverflowTolerance = 2f;
            GeometryHysteresis = 8f;
            OrdinaryWidePointBuyContentHeight = 310f;
            OrdinaryWideRollContentHeight = 520f;
            TitleFontSize = 20f;
            SectionFontSize = 16f;
            BodyFontSize = 14f;
            StatusFontSize = 13f;
            BackgroundOpacity = 0.98f;
            AssignmentLabelWidth = 120f;
            AssignmentButtonWidth = 64f;
            HorizontalSpacing = 6f;
            UsesAllocatorFrameSprite = false;
            UsesBoundedVerticalScroll = true;
            UsesConditionalVerticalScroll = true;
            HorizontalScrollingEnabled = false;
            ContentIsMasked = true;
            SelectorValuesAreSingleLine = true;
            NonInteractiveLabelsRaycast = false;
            InteractiveControlsRaycast = true;
            OwnedRootRaycast = false;
        }

        public static NativeRollPanelLayoutSpec Default { get; } =
            new NativeRollPanelLayoutSpec();

        public NativeRollPanelBackgroundShape BackgroundShape { get; }
        public NativeRollPanelAccessAnchor AccessAnchor { get; }
        public float PreferredExpandedWidth { get; }
        public float PreferredExpandedHeight { get; }
        public float CompactPreferredWidth { get; }
        public float CompactPreferredHeight { get; }
        public float MinimumWideWidth { get; }
        public float MinimumWideHeight { get; }
        public float CompactMinimumWidth { get; }
        public float CompactMinimumHeight { get; }
        public float ExpandedWidth => PreferredExpandedWidth;
        public float ExpandedHeight => PreferredExpandedHeight;
        public float AccessTabWidth { get; }
        public float AccessTabHeight { get; }
        public float AccessTabSafeGap { get; }
        public int InternalPadding { get; }
        public float SafeLeftInset { get; }
        public float SafeTopInset { get; }
        public float SafeRightInset { get; }
        public float SafeBottomInset { get; }
        public float HeaderHeight { get; }
        public float FooterHeight { get; }
        public float SurfaceVerticalPadding { get; }
        public float MajorVerticalSpacing { get; }
        public float OrdinaryControlHeight { get; }
        public float AssignmentRowHeight { get; }
        public float CloseButtonWidth { get; }
        public float CloseButtonHeight { get; }
        public float ScrollbarWidth { get; }
        public float OverflowTolerance { get; }
        public float GeometryHysteresis { get; }
        public float OrdinaryWidePointBuyContentHeight { get; }
        public float OrdinaryWideRollContentHeight { get; }
        public float TitleFontSize { get; }
        public float SectionFontSize { get; }
        public float BodyFontSize { get; }
        public float StatusFontSize { get; }
        public float BackgroundOpacity { get; }
        public float AssignmentLabelWidth { get; }
        public float AssignmentButtonWidth { get; }
        public float HorizontalSpacing { get; }
        public bool UsesAllocatorFrameSprite { get; }
        public bool UsesBoundedVerticalScroll { get; }
        public bool UsesConditionalVerticalScroll { get; }
        public bool HorizontalScrollingEnabled { get; }
        public bool ContentIsMasked { get; }
        public bool SelectorValuesAreSingleLine { get; }
        public bool NonInteractiveLabelsRaycast { get; }
        public bool InteractiveControlsRaycast { get; }
        public bool OwnedRootRaycast { get; }

        public float AvailableContentWidth => PreferredExpandedWidth - (2f * InternalPadding);

        public float AssignmentRowRequiredWidth =>
            AssignmentLabelWidth + (2f * AssignmentButtonWidth) +
            (2f * HorizontalSpacing);

        public void Validate()
        {
            if (BackgroundShape != NativeRollPanelBackgroundShape.SolidRectangle ||
                UsesAllocatorFrameSprite)
            {
                throw new InvalidOperationException(
                    "The native roll panel must use a code-owned rectangular background.");
            }
            if (PreferredExpandedWidth < 600f || PreferredExpandedWidth > 660f ||
                PreferredExpandedHeight < 720f || PreferredExpandedHeight > 800f)
            {
                throw new InvalidOperationException("Expanded panel dimensions are outside the usability boundary.");
            }
            if (MinimumWideWidth < 540f || MinimumWideWidth > PreferredExpandedWidth ||
                MinimumWideHeight < 640f || MinimumWideHeight > PreferredExpandedHeight ||
                CompactPreferredWidth < CompactMinimumWidth ||
                CompactPreferredHeight < CompactMinimumHeight)
            {
                throw new InvalidOperationException("Responsive profile thresholds are outside the usability boundary.");
            }
            if (AccessTabWidth < 120f || AccessTabWidth > 150f ||
                AccessTabHeight < 30f || AccessTabHeight > 38f ||
                AccessTabSafeGap < 6f || AccessTabSafeGap > 12f)
            {
                throw new InvalidOperationException("Collapsed access-tab dimensions are outside the usability boundary.");
            }
            if (InternalPadding < 14 || InternalPadding > 18)
            {
                throw new InvalidOperationException("Panel padding is outside the usability boundary.");
            }
            if (TitleFontSize < 18f || SectionFontSize < 15f ||
                BodyFontSize < 14f || StatusFontSize < 13f)
            {
                throw new InvalidOperationException("One or more essential font sizes are unreadably small.");
            }
            if (BackgroundOpacity < 0.94f)
            {
                throw new InvalidOperationException("The panel background is too translucent.");
            }
            if (HeaderHeight < 36f || HeaderHeight > 42f ||
                FooterHeight < 30f || FooterHeight > 46f ||
                CloseButtonWidth < 64f || CloseButtonWidth > 80f ||
                CloseButtonHeight < 28f || CloseButtonHeight > 32f ||
                OrdinaryControlHeight < 28f || OrdinaryControlHeight > 32f ||
                AssignmentRowHeight < 28f || AssignmentRowHeight > 30f)
            {
                throw new InvalidOperationException("Header, footer, or control dimensions are outside the usability boundary.");
            }
            if (!UsesBoundedVerticalScroll || !UsesConditionalVerticalScroll ||
                HorizontalScrollingEnabled || !ContentIsMasked)
            {
                throw new InvalidOperationException("Expanded content must be bounded and masked.");
            }
            if (!SelectorValuesAreSingleLine || NonInteractiveLabelsRaycast ||
                !InteractiveControlsRaycast || OwnedRootRaycast)
            {
                throw new InvalidOperationException("The panel text or raycast policy is unsafe.");
            }
            if (AssignmentRowRequiredWidth > AvailableContentWidth)
            {
                throw new InvalidOperationException("Assignment controls do not fit the bounded panel width.");
            }
        }
    }
}
