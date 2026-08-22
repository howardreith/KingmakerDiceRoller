using System;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class RollSessionManager
    {
        private readonly object sync = new object();
        private readonly SessionLivenessTracker liveness = new SessionLivenessTracker();
        private RollSession active;

        public RollSession Active
        {
            get { lock (sync) return active; }
        }

        public bool TryOpenOrRebind(
            CharacterCreationContextDecision context,
            Func<int, GenerationRollbackSnapshot> rollbackFactory,
            out RollSession session,
            out string reason)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!context.Accepted) throw new ArgumentException("Only an accepted context can own a roll session.", nameof(context));
            if (rollbackFactory == null) throw new ArgumentNullException(nameof(rollbackFactory));

            lock (sync)
            {
                bool pendingReplacement = !context.ControllerStateMatches && context.ControllerPreviewMatches;
                if (active == null)
                {
                    GenerationRollbackSnapshot rollback = rollbackFactory(1);
                    if (rollback == null) throw new InvalidOperationException("The rollback snapshot factory returned null.");
                    active = new RollSession(
                        context.Controller,
                        context.StableOwner,
                        context.State,
                        context.Unit,
                        context.Distribution,
                        rollback,
                        pendingReplacement);
                    liveness.Reset();
                    session = active;
                    reason = "Opened a PointBuy-first session for the stable controller/source owner; no roll was generated.";
                    return true;
                }

                if (!active.OwnsStableOwner(context.Controller, context.StableOwner))
                {
                    session = null;
                    reason = "A different controller/source owner already owns the active character-roll session.";
                    return false;
                }

                if (!active.OwnsState(context.State) ||
                    !active.OwnsUnit(context.Unit) ||
                    !active.OwnsDistribution(context.Distribution))
                {
                    int replacementGeneration = active.Generation + 1;
                    GenerationRollbackSnapshot rollback = rollbackFactory(replacementGeneration);
                    if (rollback == null) throw new InvalidOperationException("The rollback snapshot factory returned null.");
                    active.Rebind(
                        context.Controller,
                        context.StableOwner,
                        context.State,
                        context.Unit,
                        context.Distribution,
                        rollback,
                        pendingReplacement);
                    reason = "Rebound the same-owner preview generation " + active.Generation +
                        " for the stable controller/source owner.";
                }
                else
                {
                    reason = "Reused the current same-owner preview generation.";
                }

                session = active;
                return true;
            }
        }

        public bool TryGetByDistribution(object distribution, out RollSession session)
        {
            lock (sync)
            {
                session = active;
                return session != null && session.OwnsDistribution(distribution);
            }
        }

        public bool ReleaseIfStableOwnerLost(
            object currentController,
            object currentSourceUnit,
            bool observationSucceeded,
            float deltaTime,
            out RollSession released)
        {
            lock (sync)
            {
                released = null;
                if (active == null)
                {
                    liveness.Reset();
                    return false;
                }
                bool shouldRelease = liveness.Observe(
                    observationSucceeded,
                    active.OwnsStableOwner(currentController, currentSourceUnit),
                    deltaTime);
                if (!shouldRelease) return false;
                released = active;
                released.Lifecycle.Abandon();
                active = null;
                liveness.Reset();
                return true;
            }
        }

        public void Clear(RollSession session)
        {
            lock (sync)
            {
                if (ReferenceEquals(active, session))
                {
                    active = null;
                    liveness.Reset();
                }
            }
        }
    }
}
