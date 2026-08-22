using System;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class RollSession
    {
        public const int MaximumApplicationAttemptsPerGeneration = 2;

        public RollSession(
            object controller,
            object stableOwner,
            object state,
            object unit,
            object distribution,
            PointBuyBaseline baseline,
            StatAssignment assignment,
            bool pendingReplacementObserved)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            StableOwner = stableOwner ?? throw new ArgumentNullException(nameof(stableOwner));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Distribution = distribution ?? throw new ArgumentNullException(nameof(distribution));
            Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
            Assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            PendingReplacementObserved = pendingReplacementObserved;
            Generation = 1;
            Lifecycle = new RollSessionLifecycle();
            Lifecycle.Activate();
        }

        public object Controller { get; }
        public object StableOwner { get; }
        public object State { get; private set; }
        public object Unit { get; private set; }
        public object Distribution { get; private set; }
        public PointBuyBaseline Baseline { get; private set; }
        public StatAssignment Assignment { get; }
        public RollSessionLifecycle Lifecycle { get; }
        public int Generation { get; private set; }
        public int ApplicationAttempts { get; private set; }
        public int StagedGeneration { get; private set; }
        public int VerifiedGeneration { get; private set; }
        public int FailedGeneration { get; private set; }
        public bool PendingReplacementObserved { get; private set; }
        public bool ReboundPreview => Generation > 1;

        public bool OwnsState(object state) => ReferenceEquals(State, state);
        public bool OwnsDistribution(object distribution) => ReferenceEquals(Distribution, distribution);
        public bool OwnsUnit(object unit) => ReferenceEquals(Unit, unit);
        public bool OwnsStableOwner(object controller, object stableOwner) =>
            ReferenceEquals(Controller, controller) && ReferenceEquals(StableOwner, stableOwner);
        public bool IsRestoringPointBuy => Lifecycle.State == RollSessionState.RestoringPointBuy;
        public bool IsStaged => StagedGeneration == Generation;
        public bool IsApplied => Lifecycle.State == RollSessionState.Applied && VerifiedGeneration == Generation;
        public bool IsApplicationFailed => FailedGeneration == Generation;

        public bool TryBeginApplicationAttempt(out string error)
        {
            if (IsRestoringPointBuy)
            {
                error = "The session is restoring point buy and cannot apply the fixed array.";
                return false;
            }
            if (Lifecycle.State != RollSessionState.Active && Lifecycle.State != RollSessionState.Applied)
            {
                error = "The roll session is not active.";
                return false;
            }
            if (ApplicationAttempts >= MaximumApplicationAttemptsPerGeneration)
            {
                error = "The current preview generation exhausted its bounded application attempts.";
                return false;
            }

            ApplicationAttempts++;
            error = null;
            return true;
        }

        public void MarkApplicationStaged(int generation)
        {
            RequireCurrentGeneration(generation);
            StagedGeneration = generation;
        }

        public void MarkLiveApplicationVerified(int generation)
        {
            RequireCurrentGeneration(generation);
            if (StagedGeneration != generation)
            {
                throw new InvalidOperationException("The current preview generation has not been staged.");
            }
            Lifecycle.MarkApplied();
            VerifiedGeneration = generation;
        }

        public void MarkPointBuyRollbackVerified(int generation)
        {
            RequireCurrentGeneration(generation);
            if (Lifecycle.State != RollSessionState.RestoringPointBuy)
            {
                throw new InvalidOperationException("Only a restoring session can complete a point-buy rollback.");
            }
            StagedGeneration = generation;
            Lifecycle.AbortPointBuyRestore();
            VerifiedGeneration = generation;
        }

        public void MarkApplicationFailed(int generation)
        {
            RequireCurrentGeneration(generation);
            FailedGeneration = generation;
        }

        public void Rebind(
            object controller,
            object stableOwner,
            object state,
            object unit,
            object distribution,
            PointBuyBaseline baseline,
            bool pendingReplacementObserved)
        {
            if (!OwnsStableOwner(controller, stableOwner))
            {
                throw new InvalidOperationException("A different controller/source owner cannot rebind this roll session.");
            }

            State = state ?? throw new ArgumentNullException(nameof(state));
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Distribution = distribution ?? throw new ArgumentNullException(nameof(distribution));
            Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
            PendingReplacementObserved = pendingReplacementObserved;
            Generation++;
            ApplicationAttempts = 0;
            StagedGeneration = 0;
        }

        private void RequireCurrentGeneration(int generation)
        {
            if (generation != Generation)
            {
                throw new InvalidOperationException(
                    "Preview generation " + generation + " is detached; current generation is " + Generation + ".");
            }
        }
    }
}
