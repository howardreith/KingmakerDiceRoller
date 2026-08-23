using System;

namespace KingmakerDiceRoller.CharacterCreation
{
    public enum RollPanelPresentationProfile
    {
        Wide,
        Compact
    }

    public sealed class ResponsiveRollPanelLayoutInput
    {
        public ResponsiveRollPanelLayoutInput(
            float availableWidth,
            float availableHeight,
            float safeLeftInset,
            float safeTopInset,
            float safeRightInset,
            float safeBottomInset,
            float preferredBodyContentHeight,
            RollPanelPresentationProfile? previousProfile,
            bool? previousScrolling)
        {
            AvailableWidth = availableWidth;
            AvailableHeight = availableHeight;
            SafeLeftInset = safeLeftInset;
            SafeTopInset = safeTopInset;
            SafeRightInset = safeRightInset;
            SafeBottomInset = safeBottomInset;
            PreferredBodyContentHeight = preferredBodyContentHeight;
            PreviousProfile = previousProfile;
            PreviousScrolling = previousScrolling;
        }

        public float AvailableWidth { get; }
        public float AvailableHeight { get; }
        public float SafeLeftInset { get; }
        public float SafeTopInset { get; }
        public float SafeRightInset { get; }
        public float SafeBottomInset { get; }
        public float PreferredBodyContentHeight { get; }
        public RollPanelPresentationProfile? PreviousProfile { get; }
        public bool? PreviousScrolling { get; }
    }

    public sealed class ResponsiveRollPanelLayoutResult
    {
        internal ResponsiveRollPanelLayoutResult(
            RollPanelPresentationProfile profile,
            float safeWidth,
            float safeHeight,
            float panelWidth,
            float panelHeight,
            float headerHeight,
            float footerHeight,
            float bodyViewportHeight,
            bool scrollingRequired,
            float leftInset,
            float topInset,
            float rightInset,
            float bottomInset)
        {
            Profile = profile;
            SafeWidth = safeWidth;
            SafeHeight = safeHeight;
            PanelWidth = panelWidth;
            PanelHeight = panelHeight;
            HeaderHeight = headerHeight;
            FooterHeight = footerHeight;
            BodyViewportHeight = bodyViewportHeight;
            ScrollingRequired = scrollingRequired;
            LeftInset = leftInset;
            TopInset = topInset;
            RightInset = rightInset;
            BottomInset = bottomInset;
        }

        public RollPanelPresentationProfile Profile { get; }
        public float SafeWidth { get; }
        public float SafeHeight { get; }
        public float PanelWidth { get; }
        public float PanelHeight { get; }
        public float HeaderHeight { get; }
        public float FooterHeight { get; }
        public float BodyViewportHeight { get; }
        public bool ScrollingRequired { get; }
        public float LeftInset { get; }
        public float TopInset { get; }
        public float RightInset { get; }
        public float BottomInset { get; }
        public float AnchoredPositionX => -RightInset;
        public float AnchoredPositionY => -TopInset;
    }

    public sealed class ResponsiveRollPanelLayoutCalculator
    {
        private readonly NativeRollPanelLayoutSpec spec;

        public ResponsiveRollPanelLayoutCalculator(NativeRollPanelLayoutSpec spec)
        {
            this.spec = spec ?? throw new ArgumentNullException(nameof(spec));
            spec.Validate();
        }

        public ResponsiveRollPanelLayoutResult Calculate(ResponsiveRollPanelLayoutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            RequireFiniteNonNegative(input.AvailableWidth, nameof(input.AvailableWidth));
            RequireFiniteNonNegative(input.AvailableHeight, nameof(input.AvailableHeight));
            RequireFiniteNonNegative(input.SafeLeftInset, nameof(input.SafeLeftInset));
            RequireFiniteNonNegative(input.SafeTopInset, nameof(input.SafeTopInset));
            RequireFiniteNonNegative(input.SafeRightInset, nameof(input.SafeRightInset));
            RequireFiniteNonNegative(input.SafeBottomInset, nameof(input.SafeBottomInset));
            RequireFiniteNonNegative(
                input.PreferredBodyContentHeight,
                nameof(input.PreferredBodyContentHeight));

            float safeWidth = Math.Max(
                0f,
                input.AvailableWidth - input.SafeLeftInset - input.SafeRightInset);
            float safeHeight = Math.Max(
                0f,
                input.AvailableHeight - input.SafeTopInset - input.SafeBottomInset);
            bool retainWide = input.PreviousProfile == RollPanelPresentationProfile.Wide;
            float wideWidthThreshold = spec.MinimumWideWidth -
                (retainWide ? spec.GeometryHysteresis : 0f);
            float wideHeightThreshold = spec.MinimumWideHeight -
                (retainWide ? spec.GeometryHysteresis : 0f);
            RollPanelPresentationProfile profile =
                safeWidth >= wideWidthThreshold && safeHeight >= wideHeightThreshold
                    ? RollPanelPresentationProfile.Wide
                    : RollPanelPresentationProfile.Compact;

            float preferredWidth = profile == RollPanelPresentationProfile.Wide
                ? spec.PreferredExpandedWidth
                : spec.CompactPreferredWidth;
            float preferredHeight = profile == RollPanelPresentationProfile.Wide
                ? spec.PreferredExpandedHeight
                : spec.CompactPreferredHeight;
            float panelWidth = Math.Min(preferredWidth, safeWidth);
            float panelHeight = Math.Min(preferredHeight, safeHeight);
            float headerHeight = Math.Min(spec.HeaderHeight, panelHeight);
            float remainingAfterHeader = Math.Max(0f, panelHeight - headerHeight);
            float footerHeight = Math.Min(spec.FooterHeight, remainingAfterHeader);
            float bodyViewportHeight = Math.Max(
                0f,
                panelHeight - headerHeight - footerHeight -
                (2f * spec.SurfaceVerticalPadding) -
                (2f * spec.MajorVerticalSpacing));

            float overflowDelta = input.PreferredBodyContentHeight - bodyViewportHeight;
            bool scrollingRequired;
            if (input.PreviousScrolling == true)
            {
                scrollingRequired = overflowDelta > -spec.OverflowTolerance;
            }
            else
            {
                scrollingRequired = overflowDelta > spec.OverflowTolerance;
            }

            return new ResponsiveRollPanelLayoutResult(
                profile,
                safeWidth,
                safeHeight,
                panelWidth,
                panelHeight,
                headerHeight,
                footerHeight,
                bodyViewportHeight,
                scrollingRequired,
                input.SafeLeftInset,
                input.SafeTopInset,
                input.SafeRightInset,
                input.SafeBottomInset);
        }

        private static void RequireFiniteNonNegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(name, "Layout inputs must be finite and non-negative.");
            }
        }
    }
}
