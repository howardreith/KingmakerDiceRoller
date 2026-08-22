using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class SessionLivenessTracker
    {
        public const float UnconfirmedGraceSeconds = 5.0f;
        public const float ConfirmedGraceSeconds = 0.75f;

        private bool confirmed;
        private float mismatchSeconds;

        public bool IsConfirmed => confirmed;
        public float MismatchSeconds => mismatchSeconds;

        public bool Observe(bool observationSucceeded, bool ownsStableBuild, float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time must be finite and non-negative.");
            }

            if (!observationSucceeded)
            {
                return false;
            }

            if (ownsStableBuild)
            {
                confirmed = true;
                mismatchSeconds = 0f;
                return false;
            }

            mismatchSeconds += deltaTime;
            float threshold = confirmed ? ConfirmedGraceSeconds : UnconfirmedGraceSeconds;
            return mismatchSeconds >= threshold;
        }

        public void Reset()
        {
            confirmed = false;
            mismatchSeconds = 0f;
        }
    }
}
