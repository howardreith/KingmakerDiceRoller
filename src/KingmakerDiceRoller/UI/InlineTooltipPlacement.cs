using System;
using KingmakerDiceRoller.CharacterCreation;

namespace KingmakerDiceRoller.UI
{
    // Pure local-rectangle placement. The caller converts world corners into
    // its owned root before applying the returned bottom-left coordinates.
    public static class InlineTooltipPlacement
    {
        public static LocalLayoutRect Place(LocalLayoutRect viewport,
            LocalLayoutRect trigger, float width, float height, float inset, float gap)
        {
            if (!viewport.IsFinitePositive || !trigger.IsFinitePositive ||
                !FinitePositive(width) || !FinitePositive(height) ||
                !FiniteNonnegative(inset) || !FiniteNonnegative(gap) ||
                width > viewport.Width - 2f * inset ||
                height > viewport.Height - 2f * inset)
                throw new ArgumentException("Tooltip frame or viewport geometry is invalid.");

            float left = Clamp(trigger.CenterX - width * 0.5f,
                viewport.XMin + inset, viewport.XMax - inset - width);
            float above = trigger.YMax + gap;
            float below = trigger.YMin - gap - height;
            float bottom = above + height <= viewport.YMax - inset
                ? above : below >= viewport.YMin + inset
                    ? below : Clamp(above, viewport.YMin + inset,
                        viewport.YMax - inset - height);
            return new LocalLayoutRect(left, bottom, left + width, bottom + height);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static bool FinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }

        private static bool FiniteNonnegative(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}
