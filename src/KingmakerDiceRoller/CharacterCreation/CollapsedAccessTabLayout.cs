using System;

namespace KingmakerDiceRoller.CharacterCreation
{
    public enum CollapsedAccessTabAnchorSource
    {
        RacialBonusContainer,
        AllocatorFrame,
        AllocatorRegion,
        AbilityPhaseRoot
    }

    public struct LocalLayoutRect
    {
        public LocalLayoutRect(float xMin, float yMin, float xMax, float yMax)
        {
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
        }

        public float XMin { get; }
        public float YMin { get; }
        public float XMax { get; }
        public float YMax { get; }
        public float Width => XMax - XMin;
        public float Height => YMax - YMin;
        public float CenterX => (XMin + XMax) * 0.5f;

        public bool IsFinitePositive =>
            IsFinite(XMin) && IsFinite(YMin) && IsFinite(XMax) && IsFinite(YMax) &&
            Width > 0f && Height > 0f;

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class CollapsedAccessTabLayoutInput
    {
        public CollapsedAccessTabLayoutInput(
            LocalLayoutRect rootBounds,
            LocalLayoutRect? racialBonusBounds,
            bool racialBonusActive,
            LocalLayoutRect? allocatorFrameBounds,
            LocalLayoutRect? allocatorBounds,
            float tabWidth,
            float tabHeight,
            float safeLeftInset,
            float safeRightInset,
            float safeTopInset,
            float bottomNavigationInset,
            float safeGap)
        {
            RootBounds = rootBounds;
            RacialBonusBounds = racialBonusBounds;
            RacialBonusActive = racialBonusActive;
            AllocatorFrameBounds = allocatorFrameBounds;
            AllocatorBounds = allocatorBounds;
            TabWidth = tabWidth;
            TabHeight = tabHeight;
            SafeLeftInset = safeLeftInset;
            SafeRightInset = safeRightInset;
            SafeTopInset = safeTopInset;
            BottomNavigationInset = bottomNavigationInset;
            SafeGap = safeGap;
        }

        public LocalLayoutRect RootBounds { get; }
        public LocalLayoutRect? RacialBonusBounds { get; }
        public bool RacialBonusActive { get; }
        public LocalLayoutRect? AllocatorFrameBounds { get; }
        public LocalLayoutRect? AllocatorBounds { get; }
        public float TabWidth { get; }
        public float TabHeight { get; }
        public float SafeLeftInset { get; }
        public float SafeRightInset { get; }
        public float SafeTopInset { get; }
        public float BottomNavigationInset { get; }
        public float SafeGap { get; }
    }

    public sealed class CollapsedAccessTabLayoutResult
    {
        internal CollapsedAccessTabLayoutResult(
            CollapsedAccessTabAnchorSource source,
            float centerX,
            float centerY,
            float left,
            float right,
            float bottom,
            float top)
        {
            Source = source;
            CenterX = centerX;
            CenterY = centerY;
            Left = left;
            Right = right;
            Bottom = bottom;
            Top = top;
        }

        public CollapsedAccessTabAnchorSource Source { get; }
        public float CenterX { get; }
        public float CenterY { get; }
        public float Left { get; }
        public float Right { get; }
        public float Bottom { get; }
        public float Top { get; }
    }

    public sealed class CollapsedAccessTabLayoutCalculator
    {
        public CollapsedAccessTabLayoutResult Calculate(CollapsedAccessTabLayoutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            RequireRoot(input.RootBounds);
            RequirePositiveFinite(input.TabWidth, nameof(input.TabWidth));
            RequirePositiveFinite(input.TabHeight, nameof(input.TabHeight));
            RequireNonNegativeFinite(input.SafeLeftInset, nameof(input.SafeLeftInset));
            RequireNonNegativeFinite(input.SafeRightInset, nameof(input.SafeRightInset));
            RequireNonNegativeFinite(input.SafeTopInset, nameof(input.SafeTopInset));
            RequireNonNegativeFinite(input.BottomNavigationInset, nameof(input.BottomNavigationInset));
            RequireNonNegativeFinite(input.SafeGap, nameof(input.SafeGap));

            float halfWidth = input.TabWidth * 0.5f;
            float halfHeight = input.TabHeight * 0.5f;
            float safeLeft = input.RootBounds.XMin + input.SafeLeftInset + halfWidth;
            float safeRight = input.RootBounds.XMax - input.SafeRightInset - halfWidth;
            if (safeRight < safeLeft)
            {
                float rootCenter = input.RootBounds.CenterX;
                safeLeft = rootCenter;
                safeRight = rootCenter;
            }

            LocalLayoutRect horizontalRegion;
            CollapsedAccessTabAnchorSource source;
            if (input.RacialBonusActive && IsUsableCandidate(
                input.RacialBonusBounds,
                input.RootBounds,
                input.TabWidth,
                safeLeft,
                safeRight))
            {
                horizontalRegion = input.RacialBonusBounds.Value;
                source = CollapsedAccessTabAnchorSource.RacialBonusContainer;
            }
            else if (IsUsableCandidate(
                input.AllocatorFrameBounds,
                input.RootBounds,
                input.TabWidth,
                safeLeft,
                safeRight))
            {
                horizontalRegion = input.AllocatorFrameBounds.Value;
                source = CollapsedAccessTabAnchorSource.AllocatorFrame;
            }
            else if (IsUsableCandidate(
                input.AllocatorBounds,
                input.RootBounds,
                input.TabWidth,
                safeLeft,
                safeRight))
            {
                horizontalRegion = input.AllocatorBounds.Value;
                source = CollapsedAccessTabAnchorSource.AllocatorRegion;
            }
            else
            {
                horizontalRegion = input.RootBounds;
                source = CollapsedAccessTabAnchorSource.AbilityPhaseRoot;
            }

            float regionLeft = Math.Max(horizontalRegion.XMin, input.RootBounds.XMin);
            float regionRight = Math.Min(horizontalRegion.XMax, input.RootBounds.XMax);
            float desiredCenterX = regionRight > regionLeft
                ? (regionLeft + regionRight) * 0.5f
                : input.RootBounds.CenterX;
            float centerX = Clamp(desiredCenterX, safeLeft, safeRight);

            float safeBottom = input.RootBounds.YMin + input.BottomNavigationInset + input.SafeGap;
            float safeTop = input.RootBounds.YMax - input.SafeTopInset - input.SafeGap;
            float minimumCenterY = input.RootBounds.YMin + input.SafeGap + halfHeight;
            float maximumCenterY = input.RootBounds.YMax - input.SafeGap - halfHeight;
            float desiredCenterY = safeBottom + halfHeight;
            float navigationBoundedCenter = Math.Min(desiredCenterY, safeTop - halfHeight);
            float centerY = Clamp(navigationBoundedCenter, minimumCenterY, maximumCenterY);

            return new CollapsedAccessTabLayoutResult(
                source,
                centerX,
                centerY,
                centerX - halfWidth,
                centerX + halfWidth,
                centerY - halfHeight,
                centerY + halfHeight);
        }

        private static bool IsUsableCandidate(
            LocalLayoutRect? candidate,
            LocalLayoutRect root,
            float requiredWidth,
            float safeCenterLeft,
            float safeCenterRight)
        {
            if (!candidate.HasValue || !candidate.Value.IsFinitePositive) return false;
            LocalLayoutRect value = candidate.Value;
            float clippedLeft = Math.Max(value.XMin, root.XMin);
            float clippedRight = Math.Min(value.XMax, root.XMax);
            float clippedBottom = Math.Max(value.YMin, root.YMin);
            float clippedTop = Math.Min(value.YMax, root.YMax);
            float clippedCenter = (clippedLeft + clippedRight) * 0.5f;
            return clippedRight - clippedLeft >= requiredWidth &&
                clippedTop > clippedBottom &&
                clippedCenter >= safeCenterLeft && clippedCenter <= safeCenterRight;
        }

        private static void RequireRoot(LocalLayoutRect value)
        {
            if (!value.IsFinitePositive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The ability-phase root bounds must be finite and positive.");
            }
        }

        private static void RequirePositiveFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(name, "Layout values must be finite and positive.");
            }
        }

        private static void RequireNonNegativeFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(name, "Layout values must be finite and non-negative.");
            }
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (maximum < minimum) return minimum;
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
