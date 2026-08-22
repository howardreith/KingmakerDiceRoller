using System;

namespace KingmakerDiceRoller.CharacterCreation
{
    public enum NativeRollPanelBackgroundShape
    {
        SolidRectangle
    }

    public enum NativeRollPanelAccessAnchor
    {
        RacialBonusContainerWithUpperRightFallback
    }

    public sealed class NativeRollPanelLayoutSpec
    {
        private NativeRollPanelLayoutSpec()
        {
            BackgroundShape = NativeRollPanelBackgroundShape.SolidRectangle;
            AccessAnchor = NativeRollPanelAccessAnchor.RacialBonusContainerWithUpperRightFallback;
            ExpandedWidth = 400f;
            ExpandedHeight = 570f;
            AccessTabWidth = 140f;
            AccessTabHeight = 34f;
            InternalPadding = 16;
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
        public float ExpandedWidth { get; }
        public float ExpandedHeight { get; }
        public float AccessTabWidth { get; }
        public float AccessTabHeight { get; }
        public int InternalPadding { get; }
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
        public bool ContentIsMasked { get; }
        public bool SelectorValuesAreSingleLine { get; }
        public bool NonInteractiveLabelsRaycast { get; }
        public bool InteractiveControlsRaycast { get; }
        public bool OwnedRootRaycast { get; }

        public float AvailableContentWidth => ExpandedWidth - (2f * InternalPadding);

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
            if (ExpandedWidth < 380f || ExpandedWidth > 420f ||
                ExpandedHeight < 540f || ExpandedHeight > 590f)
            {
                throw new InvalidOperationException("Expanded panel dimensions are outside the usability boundary.");
            }
            if (AccessTabWidth < 120f || AccessTabWidth > 150f ||
                AccessTabHeight < 30f || AccessTabHeight > 38f)
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
            if (!UsesBoundedVerticalScroll || !ContentIsMasked)
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
