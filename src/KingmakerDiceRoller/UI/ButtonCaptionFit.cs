using System;

namespace KingmakerDiceRoller.UI
{
    // Pure sizing policy: converts an unconstrained styled text extent into the
    // room a control must reserve so its caption never ellipsizes. The Unity
    // measurement itself lives in the panel host; only arithmetic is decided here.
    internal static class ButtonCaptionFit
    {
        internal const float HorizontalSafety = 2f;
        internal const float VerticalSafety = 2f;

        internal static float RequiredWidth(float styledTextWidth, float horizontalInset)
        {
            return styledTextWidth + (2f * horizontalInset) + HorizontalSafety;
        }

        internal static float RequiredHeight(float styledTextHeight, float verticalInset)
        {
            return styledTextHeight + (2f * verticalInset) + VerticalSafety;
        }

        internal static float ResolveExtent(float designMinimum, float requiredExtent)
        {
            if (float.IsNaN(requiredExtent) || requiredExtent < 0f || float.IsInfinity(requiredExtent))
                return designMinimum;
            return Math.Max(designMinimum, requiredExtent);
        }

        internal static float UnifyPair(float first, float second)
        {
            return Math.Max(first, second);
        }
    }
}
