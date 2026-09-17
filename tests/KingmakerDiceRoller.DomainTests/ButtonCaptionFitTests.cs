using KingmakerDiceRoller.UI;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class ButtonCaptionFitTests
    {
        internal static void ShortCaptionKeepsDesignMinimum()
        {
            float required = ButtonCaptionFit.RequiredWidth(46f, 4f);
            AssertEx.Equal(46f + 8f + ButtonCaptionFit.HorizontalSafety, required);
            AssertEx.Equal(120f, ButtonCaptionFit.ResolveExtent(120f, required), "a fitting caption keeps the design width");
        }

        internal static void LongCaptionReservesTextPlusPadding()
        {
            float required = ButtonCaptionFit.RequiredWidth(216f, 4f);
            AssertEx.Equal(216f + 8f + ButtonCaptionFit.HorizontalSafety, required);
            AssertEx.Equal(required, ButtonCaptionFit.ResolveExtent(210f, required), "an overflowing caption widens the control");
        }

        internal static void PaddingCountedOncePerSide()
        {
            float narrow = ButtonCaptionFit.RequiredWidth(100f, 4f);
            float wide = ButtonCaptionFit.RequiredWidth(100f, 9f);
            AssertEx.Equal(10f, wide - narrow, "insets contribute exactly twice");
        }

        internal static void InvalidMeasurementFallsBackToDesign()
        {
            AssertEx.Equal(88f, ButtonCaptionFit.ResolveExtent(88f, float.NaN), "NaN measurements never resize");
            AssertEx.Equal(88f, ButtonCaptionFit.ResolveExtent(88f, float.NegativeInfinity));
            AssertEx.Equal(88f, ButtonCaptionFit.ResolveExtent(88f, float.PositiveInfinity));
            AssertEx.Equal(88f, ButtonCaptionFit.ResolveExtent(88f, -5f), "negative measurements never resize");
        }

        internal static void HeightIncludesVerticalInsets()
        {
            float required = ButtonCaptionFit.RequiredHeight(24f, 2f);
            AssertEx.Equal(24f + 4f + ButtonCaptionFit.VerticalSafety, required);
            AssertEx.Equal(34f, ButtonCaptionFit.ResolveExtent(34f, required), "a fitting caption keeps the design height");
        }

        internal static void PairUnificationUsesTheWiderControl()
        {
            AssertEx.Equal(74f, ButtonCaptionFit.UnifyPair(64f, 74f));
            AssertEx.Equal(74f, ButtonCaptionFit.UnifyPair(74f, 64f));
            AssertEx.Equal(0f, ButtonCaptionFit.UnifyPair(0f, 0f));
        }

        internal static void RealisticCaptionRowsFitWideContent()
        {
            // Wide body content is 600 - 2*26 panel padding; the widest repaired
            // row is Roll + Reroll + fitted "Return to Point Buy" + spacing.
            float rollReturn = ButtonCaptionFit.ResolveExtent(210f, ButtonCaptionFit.RequiredWidth(216f, 4f));
            float pointActions = 120f + 120f + rollReturn + (2f * 6f);
            AssertEx.True(pointActions <= 548f - 22f, "point actions fit even with the scrollbar allowance");
            float unified = ButtonCaptionFit.UnifyPair(
                ButtonCaptionFit.ResolveExtent(64f, ButtonCaptionFit.RequiredWidth(30f, 4f)),
                ButtonCaptionFit.ResolveExtent(64f, ButtonCaptionFit.RequiredWidth(52f, 4f)));
            float assignmentRow = 120f + (2f * unified) + (2f * 6f);
            AssertEx.True(assignmentRow <= 548f, "assignment rows stay within the wide body");
        }

        internal static void CompactReflowKeepsRowsWithinBounds()
        {
            // Compact body content is 460 - 2*26 panel padding, minus the
            // scrollbar allowance when scrolling is required.
            float compactContent = 460f - (2f * 26f) - 22f;
            float rollReturn = ButtonCaptionFit.ResolveExtent(210f, ButtonCaptionFit.RequiredWidth(216f, 4f));
            AssertEx.True(120f + 120f + rollReturn + (2f * 6f) > compactContent,
                "one point-actions row cannot fit compact content after caption fitting");
            AssertEx.True(120f + 120f + 6f <= compactContent, "Roll and Reroll fit their compact row");
            AssertEx.True(rollReturn <= compactContent, "the reflowed Return control fits its own compact row");
        }
    }
}
