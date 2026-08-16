using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class RollSessionOwnership
    {
        private object owner;

        public bool IsOwned => owner != null;

        public void Claim(object candidate)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (owner != null && !ReferenceEquals(owner, candidate))
            {
                throw new InvalidOperationException("The session is already owned by a different object.");
            }

            owner = candidate;
        }

        public bool BelongsTo(object candidate)
        {
            return owner != null && ReferenceEquals(owner, candidate);
        }

        public void Transfer(object current, object replacement)
        {
            if (!BelongsTo(current)) throw new InvalidOperationException("Only the current owner can transfer ownership.");
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            owner = replacement;
        }

        public void Release(object candidate)
        {
            if (BelongsTo(candidate)) owner = null;
        }
    }
}
