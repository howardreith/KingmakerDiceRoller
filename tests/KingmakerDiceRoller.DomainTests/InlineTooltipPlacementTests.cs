using System;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.UI;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class InlineTooltipPlacementTests
    {
        private static readonly LocalLayoutRect Viewport =
            new LocalLayoutRect(-300f, -200f, 300f, 200f);

        internal static void PrefersAboveWhenFullFrameFits()
        {
            LocalLayoutRect frame = InlineTooltipPlacement.Place(Viewport,
                new LocalLayoutRect(-40f, 40f, 40f, 70f), 180f, 50f, 16f, 7f);
            AssertEx.Equal(-90f, frame.XMin);
            AssertEx.Equal(77f, frame.YMin);
            AssertEx.Equal(127f, frame.YMax);
        }

        internal static void FlipsBelowNearTop()
        {
            LocalLayoutRect frame = InlineTooltipPlacement.Place(Viewport,
                new LocalLayoutRect(-40f, 160f, 40f, 184f), 180f, 50f, 16f, 7f);
            AssertEx.Equal(103f, frame.YMin);
            AssertEx.Equal(153f, frame.YMax);
        }

        internal static void ClampsFullFrameAtBothSideEdges()
        {
            LocalLayoutRect left = InlineTooltipPlacement.Place(Viewport,
                new LocalLayoutRect(-294f, 0f, -270f, 30f), 180f, 50f, 16f, 7f);
            LocalLayoutRect right = InlineTooltipPlacement.Place(Viewport,
                new LocalLayoutRect(270f, 0f, 294f, 30f), 180f, 50f, 16f, 7f);
            AssertEx.Equal(-284f, left.XMin);
            AssertEx.Equal(284f, right.XMax);
        }

        internal static void ClampsWhenNeitherSideFits()
        {
            LocalLayoutRect small = new LocalLayoutRect(-150f, -50f, 150f, 50f);
            LocalLayoutRect frame = InlineTooltipPlacement.Place(small,
                new LocalLayoutRect(-10f, -4f, 10f, 4f), 180f, 70f, 10f, 7f);
            AssertEx.True(frame.YMin >= -40f && frame.YMax <= 40f);
        }

        internal static void RejectsFrameLargerThanSafeViewport()
        {
            AssertEx.Throws<ArgumentException>(() => InlineTooltipPlacement.Place(
                Viewport, new LocalLayoutRect(-10f, -10f, 10f, 10f),
                590f, 40f, 16f, 7f));
        }
    }
}
