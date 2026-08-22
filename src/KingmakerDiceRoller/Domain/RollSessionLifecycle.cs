using System;

namespace KingmakerDiceRoller.Domain
{
    public enum RollSessionState
    {
        Created,
        PointBuy,
        PointBuyRestored = PointBuy,
        EnteringRollMode,
        Roll,
        Applied = Roll,
        RestoringPointBuy,
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

        public void ActivatePointBuy()
        {
            Transition(RollSessionState.Created, RollSessionState.PointBuy);
        }

        public void Activate()
        {
            ActivatePointBuy();
            BeginRollMode();
        }

        public void BeginRollMode()
        {
            Transition(RollSessionState.PointBuy, RollSessionState.EnteringRollMode);
        }

        public void CommitRollMode()
        {
            if (State != RollSessionState.EnteringRollMode && State != RollSessionState.Roll)
            {
                throw new InvalidOperationException("A roll can be committed only while entering or already in Roll mode.");
            }
            State = RollSessionState.Roll;
        }

        public void MarkApplied()
        {
            CommitRollMode();
        }

        public void AbortRollModeEntry()
        {
            Transition(RollSessionState.EnteringRollMode, RollSessionState.PointBuy);
        }

        public void BeginPointBuyRestore()
        {
            Transition(RollSessionState.Roll, RollSessionState.RestoringPointBuy);
        }

        public void MarkPointBuyRestored()
        {
            Transition(RollSessionState.RestoringPointBuy, RollSessionState.PointBuy);
        }

        public void AbortPointBuyRestore()
        {
            Transition(RollSessionState.RestoringPointBuy, RollSessionState.Roll);
        }

        public void Complete()
        {
            if (State != RollSessionState.Roll && State != RollSessionState.PointBuy)
            {
                throw new InvalidOperationException("Only a stable Roll or PointBuy session can complete.");
            }
            State = RollSessionState.Completed;
        }

        public void Abandon()
        {
            if (State == RollSessionState.Completed || State == RollSessionState.Abandoned) return;
            State = RollSessionState.Abandoned;
        }

        private void Transition(RollSessionState expected, RollSessionState next)
        {
            if (State != expected)
            {
                throw new InvalidOperationException(
                    "Expected session state " + expected + " but observed " + State + ".");
            }
            State = next;
        }
    }
}
