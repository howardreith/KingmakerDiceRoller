using System;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class RollSessionManager
    {
        private readonly object sync = new object();
        private RollSession active;

        public RollSession Active
        {
            get { lock (sync) return active; }
        }

        public bool TryOpenOrRebind(
            CharacterCreationContextDecision context,
            PointBuyBaseline baseline,
            StatAssignment assignment,
            out RollSession session,
            out string reason)
        {
            lock (sync)
            {
                if (active == null)
                {
                    active = new RollSession(context.State, context.Unit, context.Distribution, baseline, assignment);
                    session = active;
                    reason = "Opened a new roll session.";
                    return true;
                }

                if (!active.OwnsUnit(context.Unit))
                {
                    session = null;
                    reason = "Another unit already owns the active roll session.";
                    return false;
                }

                if (!active.OwnsState(context.State) || !active.OwnsDistribution(context.Distribution))
                {
                    active.Rebind(context.State, context.Distribution, baseline);
                    reason = "Rebound the existing array to a rebuilt preview state.";
                }
                else
                {
                    reason = "Reused the existing roll session.";
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

        public void Clear(RollSession session)
        {
            lock (sync)
            {
                if (ReferenceEquals(active, session)) active = null;
            }
        }
    }
}
