using KingmakerDiceRoller.CharacterCreation;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class ResponsiveRollPanelLayoutTests
    {
        internal static void AmpleBoundsSelectWide()
        {
            ResponsiveRollPanelLayoutResult result = Calculate(1600f, 900f, 520f);
            AssertEx.Equal(RollPanelPresentationProfile.Wide, result.Profile);
            AssertEx.Equal(600f, result.PanelWidth);
            AssertEx.Equal(728f, result.PanelHeight);
        }

        internal static void ConstrainedWidthSelectsCompact()
        {
            ResponsiveRollPanelLayoutResult result = Calculate(575f, 900f, 300f);
            AssertEx.Equal(RollPanelPresentationProfile.Compact, result.Profile);
            AssertEx.True(result.PanelWidth <= NativeRollPanelLayoutSpec.Default.CompactPreferredWidth);
        }

        internal static void ConstrainedHeightSelectsCompact()
        {
            ResponsiveRollPanelLayoutResult result = Calculate(1600f, 760f, 300f);
            AssertEx.Equal(RollPanelPresentationProfile.Compact, result.Profile);
            AssertEx.True(result.PanelHeight <= result.SafeHeight);
        }

        internal static void PreferredDimensionsClampToSafeBounds()
        {
            ResponsiveRollPanelLayoutResult result = Calculate(610f, 800f, 300f);
            AssertEx.True(result.PanelWidth <= result.SafeWidth);
            AssertEx.True(result.PanelHeight <= result.SafeHeight);
        }

        internal static void SafeInsetsArePreserved()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            ResponsiveRollPanelLayoutResult result = Calculate(1600f, 900f, 300f);
            AssertEx.Equal(spec.SafeLeftInset, result.LeftInset);
            AssertEx.Equal(spec.SafeTopInset, result.TopInset);
            AssertEx.Equal(spec.SafeRightInset, result.RightInset);
            AssertEx.Equal(spec.SafeBottomInset, result.BottomInset);
            AssertEx.True(result.PanelWidth + result.LeftInset + result.RightInset <= 1600f);
            AssertEx.True(result.PanelHeight + result.TopInset + result.BottomInset <= 900f);
        }

        internal static void BodyViewportCannotBecomeNegative()
        {
            ResponsiveRollPanelLayoutResult result = Calculate(100f, 100f, 300f);
            AssertEx.True(result.BodyViewportHeight >= 0f);
            AssertEx.True(result.PanelWidth >= 0f);
            AssertEx.True(result.PanelHeight >= 0f);
        }

        internal static void OrdinaryWidePointBuyDoesNotScroll()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            ResponsiveRollPanelLayoutResult result = Calculate(
                1600f,
                900f,
                spec.OrdinaryWidePointBuyContentHeight);
            AssertEx.Equal(RollPanelPresentationProfile.Wide, result.Profile);
            AssertEx.True(!result.ScrollingRequired);
        }

        internal static void OrdinaryWideRollModeDoesNotScroll()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            ResponsiveRollPanelLayoutResult result = Calculate(
                1600f,
                900f,
                spec.OrdinaryWideRollContentHeight);
            AssertEx.Equal(RollPanelPresentationProfile.Wide, result.Profile);
            AssertEx.True(!result.ScrollingRequired);
            AssertEx.True(result.BodyViewportHeight >= spec.OrdinaryWideRollContentHeight);
        }

        internal static void OversizedContentRequiresScrolling()
        {
            ResponsiveRollPanelLayoutResult result = Calculate(1600f, 900f, 1000f);
            AssertEx.True(result.ScrollingRequired);
        }

        internal static void SmallGeometryChangesDoNotFlickerProfile()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            float firstWidth = spec.SafeLeftInset + spec.MinimumWideWidth + spec.SafeRightInset;
            ResponsiveRollPanelLayoutResult first = Calculate(firstWidth, 900f, 300f);
            var calculator = new ResponsiveRollPanelLayoutCalculator(spec);
            var input = new ResponsiveRollPanelLayoutInput(
                firstWidth - 4f,
                900f,
                spec.SafeLeftInset,
                spec.SafeTopInset,
                spec.SafeRightInset,
                spec.SafeBottomInset,
                300f,
                first.Profile,
                first.ScrollingRequired);
            ResponsiveRollPanelLayoutResult second = calculator.Calculate(input);
            AssertEx.Equal(RollPanelPresentationProfile.Wide, first.Profile);
            AssertEx.Equal(first.Profile, second.Profile);
        }

        internal static void SmallContentChangesDoNotFlickerScrolling()
        {
            ResponsiveRollPanelLayoutResult first = Calculate(1600f, 900f, 1000f);
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            var calculator = new ResponsiveRollPanelLayoutCalculator(spec);
            var input = new ResponsiveRollPanelLayoutInput(
                1600f,
                900f,
                spec.SafeLeftInset,
                spec.SafeTopInset,
                spec.SafeRightInset,
                spec.SafeBottomInset,
                first.BodyViewportHeight - 1f,
                first.Profile,
                first.ScrollingRequired);
            AssertEx.True(calculator.Calculate(input).ScrollingRequired);
        }

        internal static void LayoutCalculationHasNoRandomSideEffects()
        {
            var random = new SequenceRandomSource(6);
            Calculate(1600f, 900f, 520f);
            AssertEx.Equal(0, random.Calls);
        }

        internal static void EmptyMessagesReserveNoFooterAndErrorsCanScroll()
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            ResponsiveRollPanelLayoutResult empty = Calculate(1600f, 900f, 500f);
            ResponsiveRollPanelLayoutResult error = Calculate(1600f, 900f, 1600f);
            AssertEx.Equal(0f, spec.FooterHeight);
            AssertEx.Equal(empty.PanelHeight, error.PanelHeight);
            AssertEx.Equal(empty.HeaderHeight, error.HeaderHeight);
            AssertEx.Equal(empty.BodyViewportHeight, error.BodyViewportHeight);
            AssertEx.True(!empty.ScrollingRequired && error.ScrollingRequired);
            AssertEx.True(error.BodyViewportHeight > 0f);
            AssertEx.True(spec.CloseButtonHeight <= error.HeaderHeight);
        }

        internal static void RequiredResolutionMatrixPreservesNavigationAndContentBounds()
        {
            // Screen-sized and deliberately constrained effective canvas inputs;
            // this is geometry coverage, not a claim about live CanvasScaler values.
            float[,] bounds = { {1920,1200}, {1920,1080}, {1600,900},
                {1366,768}, {1280,720}, {1152,720}, {720,600}, {460,610} };
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            for (int row = 0; row < bounds.GetLength(0); row++)
            {
                ResponsiveRollPanelLayoutResult result = Calculate(bounds[row,0], bounds[row,1], 1800f);
                AssertEx.True(result.PanelWidth <= result.SafeWidth);
                AssertEx.True(result.PanelHeight <= result.SafeHeight);
                AssertEx.True(result.BodyViewportHeight > 0f && result.ScrollingRequired);
                AssertEx.True(result.PanelHeight + spec.SafeTopInset + spec.SafeBottomInset <= bounds[row,1]);
                float contentWidth = result.PanelWidth - 2f * spec.InternalPadding - spec.ScrollbarWidth - 2f;
                AssertEx.True(spec.AssignmentRowRequiredWidth <= contentWidth);
                AssertEx.True(spec.CloseButtonHeight <= result.HeaderHeight);
            }
        }

        private static ResponsiveRollPanelLayoutResult Calculate(
            float width,
            float height,
            float preferredContentHeight)
        {
            NativeRollPanelLayoutSpec spec = NativeRollPanelLayoutSpec.Default;
            return new ResponsiveRollPanelLayoutCalculator(spec).Calculate(
                new ResponsiveRollPanelLayoutInput(
                    width,
                    height,
                    spec.SafeLeftInset,
                    spec.SafeTopInset,
                    spec.SafeRightInset,
                    spec.SafeBottomInset,
                    preferredContentHeight,
                    null,
                    null));
        }
    }
}
