using System;
using KingmakerDiceRoller.CharacterCreation;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class CollapsedAccessTabLayoutTests
    {
        private const float TabWidth = 140f;
        private const float TabHeight = 34f;
        private const float LeftInset = 18f;
        private const float RightInset = 18f;
        private const float TopInset = 18f;
        private const float BottomNavigationInset = 92f;
        private const float SafeGap = 8f;

        internal static void PreferredRacialAnchorIsBottomCentered()
        {
            LocalLayoutRect root = Root(1152f, 720f);
            var race = new LocalLayoutRect(-230f, -120f, 210f, -72f);
            CollapsedAccessTabLayoutResult result = Calculate(
                root,
                race,
                true,
                new LocalLayoutRect(-360f, -250f, 360f, 250f),
                new LocalLayoutRect(-410f, -290f, 410f, 290f));

            AssertEx.Equal(CollapsedAccessTabAnchorSource.RacialBonusContainer, result.Source);
            AssertNear(-10f, result.CenterX);
            AssertNear(root.YMin + BottomNavigationInset + SafeGap, result.Bottom);
            AssertBounded(result, root, BottomNavigationInset);
        }

        internal static void MissingRacialAnchorUsesAllocatorFrame()
        {
            LocalLayoutRect root = Root(1280f, 720f);
            var frame = new LocalLayoutRect(-430f, -270f, 210f, 245f);
            CollapsedAccessTabLayoutResult result = Calculate(
                root,
                null,
                false,
                frame,
                new LocalLayoutRect(-500f, -300f, 300f, 300f));

            AssertEx.Equal(CollapsedAccessTabAnchorSource.AllocatorFrame, result.Source);
            AssertNear(frame.CenterX, result.CenterX);
            AssertBounded(result, root, BottomNavigationInset);
        }

        internal static void InactiveRacialAnchorUsesAllocatorFrame()
        {
            LocalLayoutRect root = Root(1366f, 768f);
            var race = new LocalLayoutRect(280f, 150f, 600f, 210f);
            var frame = new LocalLayoutRect(-360f, -260f, 360f, 260f);
            CollapsedAccessTabLayoutResult result = Calculate(
                root,
                race,
                false,
                frame,
                null);

            AssertEx.Equal(CollapsedAccessTabAnchorSource.AllocatorFrame, result.Source);
            AssertNear(0f, result.CenterX);
            AssertBounded(result, root, BottomNavigationInset);
        }

        internal static void UnsuitableRacialAnchorUsesAllocatorFrame()
        {
            LocalLayoutRect root = Root(1152f, 720f);
            var clippedRace = new LocalLayoutRect(540f, -80f, 720f, -20f);
            var frame = new LocalLayoutRect(-350f, -260f, 350f, 260f);
            CollapsedAccessTabLayoutResult result = Calculate(
                root,
                clippedRace,
                true,
                frame,
                null);

            AssertEx.Equal(CollapsedAccessTabAnchorSource.AllocatorFrame, result.Source);
            AssertNear(frame.CenterX, result.CenterX);
            AssertBounded(result, root, BottomNavigationInset);
        }

        internal static void ConstrainedWidthClampsInsideSafeBounds()
        {
            LocalLayoutRect root = Root(360f, 720f);
            var frame = new LocalLayoutRect(80f, -250f, 320f, 250f);
            CollapsedAccessTabLayoutResult result = Calculate(
                root,
                null,
                false,
                frame,
                null);

            AssertEx.True(result.Left >= root.XMin + LeftInset);
            AssertEx.True(result.Right <= root.XMax - RightInset);
            AssertBounded(result, root, BottomNavigationInset);
        }

        internal static void ConstrainedHeightPreservesNavigationInset()
        {
            LocalLayoutRect root = Root(800f, 480f);
            CollapsedAccessTabLayoutResult result = Calculate(
                root,
                null,
                false,
                null,
                new LocalLayoutRect(-280f, -170f, 280f, 170f));

            AssertEx.Equal(CollapsedAccessTabAnchorSource.AllocatorRegion, result.Source);
            AssertBounded(result, root, BottomNavigationInset);
        }

        internal static void BottomNavigationInsetMovesAccessTabUpward()
        {
            LocalLayoutRect root = Root(1152f, 720f);
            CollapsedAccessTabLayoutResult ordinary = Calculate(
                root,
                null,
                false,
                null,
                null,
                92f);
            CollapsedAccessTabLayoutResult enlarged = Calculate(
                root,
                null,
                false,
                null,
                null,
                132f);

            AssertNear(40f, enlarged.CenterY - ordinary.CenterY);
            AssertNear(root.YMin + 132f + SafeGap, enlarged.Bottom);
        }

        internal static void MissingAbilityChildrenNeverUsesUpperRight()
        {
            LocalLayoutRect root = Root(1152f, 720f);
            CollapsedAccessTabLayoutResult result = Calculate(
                root,
                null,
                false,
                null,
                null);

            AssertEx.Equal(CollapsedAccessTabAnchorSource.AbilityPhaseRoot, result.Source);
            AssertNear(root.CenterX, result.CenterX);
            AssertEx.True(result.CenterY < 0f);
            AssertEx.True(result.Right < root.XMax - RightInset);
            AssertEx.True(result.Top < root.YMax - TopInset);
        }

        internal static void AccessTabCentersWithinVerifiedAbilityRegion()
        {
            LocalLayoutRect root = Root(1600f, 900f);
            var allocator = new LocalLayoutRect(-620f, -330f, -140f, 330f);
            CollapsedAccessTabLayoutResult result = Calculate(
                root,
                null,
                false,
                null,
                allocator);

            AssertEx.Equal(CollapsedAccessTabAnchorSource.AllocatorRegion, result.Source);
            AssertNear(allocator.CenterX, result.CenterX);
            AssertBounded(result, root, BottomNavigationInset);
        }

        internal static void Resolution1152x720IsBounded()
        {
            AssertRequiredResolution(1152f, 720f);
        }

        internal static void Resolution1280x720IsBounded()
        {
            AssertRequiredResolution(1280f, 720f);
        }

        internal static void Resolution1366x768IsBounded()
        {
            AssertRequiredResolution(1366f, 768f);
        }

        internal static void Resolution1600x900IsBounded()
        {
            AssertRequiredResolution(1600f, 900f);
        }

        internal static void Resolution1920x1080IsBounded()
        {
            AssertRequiredResolution(1920f, 1080f);
        }

        private static void AssertRequiredResolution(float width, float height)
        {
            LocalLayoutRect root = Root(width, height);
            var frame = new LocalLayoutRect(
                root.XMin + (width * 0.18f),
                root.YMin + 120f,
                root.XMax - (width * 0.18f),
                root.YMax - 90f);
            CollapsedAccessTabLayoutResult result = Calculate(
                root,
                null,
                false,
                frame,
                null);

            AssertEx.Equal(CollapsedAccessTabAnchorSource.AllocatorFrame, result.Source);
            AssertNear(frame.CenterX, result.CenterX);
            AssertBounded(result, root, BottomNavigationInset);
        }

        private static CollapsedAccessTabLayoutResult Calculate(
            LocalLayoutRect root,
            LocalLayoutRect? race,
            bool raceActive,
            LocalLayoutRect? frame,
            LocalLayoutRect? allocator,
            float bottomInset = BottomNavigationInset)
        {
            return new CollapsedAccessTabLayoutCalculator().Calculate(
                new CollapsedAccessTabLayoutInput(
                    root,
                    race,
                    raceActive,
                    frame,
                    allocator,
                    TabWidth,
                    TabHeight,
                    LeftInset,
                    RightInset,
                    TopInset,
                    bottomInset,
                    SafeGap));
        }

        private static LocalLayoutRect Root(float width, float height)
        {
            return new LocalLayoutRect(
                width * -0.5f,
                height * -0.5f,
                width * 0.5f,
                height * 0.5f);
        }

        private static void AssertBounded(
            CollapsedAccessTabLayoutResult result,
            LocalLayoutRect root,
            float bottomInset)
        {
            AssertEx.True(result.Left >= root.XMin + LeftInset);
            AssertEx.True(result.Right <= root.XMax - RightInset);
            AssertEx.True(result.Bottom >= root.YMin + bottomInset + SafeGap);
            AssertEx.True(result.Top <= root.YMax - TopInset - SafeGap);
        }

        private static void AssertNear(float expected, float actual)
        {
            if (Math.Abs(expected - actual) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Values differ. Expected: " + expected + "; actual: " + actual + ".");
            }
        }
    }
}
