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
            Func<int, PristinePointBuyState> pristineFactory,
            Func<int, GenerationRollbackSnapshot> rollbackFactory,
            Func<StatAssignment> assignmentFactory,
            out RollSession session,
            out string reason)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!context.Accepted) throw new ArgumentException("Only an accepted context can own a roll session.", nameof(context));
            if (pristineFactory == null) throw new ArgumentNullException(nameof(pristineFactory));
            if (rollbackFactory == null) throw new ArgumentNullException(nameof(rollbackFactory));
            if (assignmentFactory == null) throw new ArgumentNullException(nameof(assignmentFactory));

            lock (sync)
            {
                bool pendingReplacement = !context.ControllerStateMatches && context.ControllerPreviewMatches;
                if (active == null)
                {
                    PristinePointBuyState pristine = pristineFactory(1);
                    if (pristine == null) throw new InvalidOperationException("The pristine point-buy factory returned null.");
                    GenerationRollbackSnapshot rollback = rollbackFactory(1);
                    if (rollback == null) throw new InvalidOperationException("The rollback snapshot factory returned null.");
                    StatAssignment assignment = assignmentFactory();
                    if (assignment == null) throw new InvalidOperationException("The assignment factory returned null.");
                    active = new RollSession(
                        context.Controller,
                        context.StableOwner,
                        context.State,
                        context.Unit,
                        context.Distribution,
                        pristine,
                        rollback,
                        assignment,
                        pendingReplacement);
                    liveness.Reset();
                    session = active;
                    reason = "Opened a new roll session for the stable controller/source owner.";
                    return true;
                }

                if (!active.OwnsStableOwner(context.Controller, context.StableOwner))
                {
                    session = null;
                    reason = "A different controller/source owner already owns the active roll session.";
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
                    reason = "Rebound the existing array to same-owner preview generation " + active.Generation + ".";
                }
                else
                {
                    reason = "Reused the existing roll session and current preview generation.";
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
