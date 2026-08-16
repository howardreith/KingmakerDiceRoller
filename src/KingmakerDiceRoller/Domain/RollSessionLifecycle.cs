using System;

namespace KingmakerDiceRoller.Domain
{
    public enum RollSessionState
    {
        Created,
        Active,
        Applied,
        RestoringPointBuy,
        PointBuyRestored,
        Completed,
        Abandoned
    }

    public sealed class RollSessionLifecycle
    {
        public RollSessionLifecycle()
        {
            State = RollSessionState.Created;
        }

        public RollSessionState State { get; private set; }

        public void Activate()
        {
            Transition(RollSessionState.Created, RollSessionState.Active);
        }

        public void MarkApplied()
        {
            if (State != RollSessionState.Active && State != RollSessionState.Applied)
            {
                throw new InvalidOperationException("Only an active session can be marked applied.");
            }

            State = RollSessionState.Applied;
        }

        public void BeginPointBuyRestore()
        {
            if (State != RollSessionState.Active && State != RollSessionState.Applied)
            {
                throw new InvalidOperationException("Point buy can be restored only from an active roll session.");
            }

            State = RollSessionState.RestoringPointBuy;
        }

        public void MarkPointBuyRestored()
        {
            Transition(RollSessionState.RestoringPointBuy, RollSessionState.PointBuyRestored);
        }

        public void Complete()
        {
            if (State != RollSessionState.Applied)
            {
                throw new InvalidOperationException("Only an applied roll session can complete.");
            }

            State = RollSessionState.Completed;
        }

        public void Abandon()
        {
            if (State == RollSessionState.Completed || State == RollSessionState.PointBuyRestored)
            {
                return;
            }

            State = RollSessionState.Abandoned;
        }

        private void Transition(RollSessionState expected, RollSessionState next)
        {
            if (State != expected)
            {
                throw new InvalidOperationException("Expected session state " + expected + " but observed " + State + ".");
            }

            State = next;
        }
    }
}
