using System;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class RollSession
    {
        public const int MaximumApplicationAttemptsPerGeneration = 2;
        private StatAssignment committedAssignment;
        private StatAssignment pendingAssignment;
        private bool pendingEntryFromPointBuy;
        private bool pendingPriorRollWasVerified;

        public RollSession(
            object controller,
            object stableOwner,
            object state,
            object unit,
            object distribution,
            GenerationRollbackSnapshot generationRollback,
            bool pendingReplacementObserved)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            StableOwner = stableOwner ?? throw new ArgumentNullException(nameof(stableOwner));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Distribution = distribution ?? throw new ArgumentNullException(nameof(distribution));
            GenerationRollback = generationRollback ?? throw new ArgumentNullException(nameof(generationRollback));
            if (generationRollback.Generation != 1)
            {
                throw new ArgumentException("The first rollback snapshot must belong to generation 1.", nameof(generationRollback));
            }
            PendingReplacementObserved = pendingReplacementObserved;
            Generation = 1;
            Lifecycle = new RollSessionLifecycle();
            Lifecycle.ActivatePointBuy();
            History = new RollHistory();
        }

        public object Controller { get; }
        public object StableOwner { get; }
        public object State { get; private set; }
        public object Unit { get; private set; }
        public object Distribution { get; private set; }
        public PointBuyOrigin PointBuyOrigin { get; private set; }
        public GenerationRollbackSnapshot GenerationRollback { get; private set; }
        public StatAssignment Assignment => pendingAssignment ?? committedAssignment;
        public StatAssignment AssignmentForApplication => Assignment;
        public RollSessionLifecycle Lifecycle { get; }
        public RollHistory History { get; }
        public int Generation { get; private set; }
        public int ApplicationAttempts { get; private set; }
        public int StagedGeneration { get; private set; }
        public int VerifiedGeneration { get; private set; }
        public int FailedGeneration { get; private set; }
        public bool PendingReplacementObserved { get; private set; }
        public bool CandidateBaselineContaminated { get; private set; }
        public bool ReboundPreview => Generation > 1;

        public bool OwnsState(object state) => ReferenceEquals(State, state);
        public bool OwnsDistribution(object distribution) => ReferenceEquals(Distribution, distribution);
        public bool OwnsUnit(object unit) => ReferenceEquals(Unit, unit);
        public bool OwnsStableOwner(object controller, object stableOwner) =>
            ReferenceEquals(Controller, controller) && ReferenceEquals(StableOwner, stableOwner);

        public RollSessionMode Mode
        {
            get
            {
                switch (Lifecycle.State)
                {
                    case RollSessionState.EnteringRollMode: return RollSessionMode.EnteringRollMode;
                    case RollSessionState.Roll: return RollSessionMode.Roll;
                    case RollSessionState.RestoringPointBuy: return RollSessionMode.RestoringPointBuy;
                    default: return RollSessionMode.PointBuy;
                }
            }
        }

        public bool IsRollMode => Mode == RollSessionMode.Roll;
        public bool IsEnteringRollMode => Mode == RollSessionMode.EnteringRollMode;
        public bool IsRestoringPointBuy => Mode == RollSessionMode.RestoringPointBuy;
        public bool IsPointBuyMode => Mode == RollSessionMode.PointBuy;
        public bool RollSuppressedForStableOwner => !IsRollMode;
        public bool PointBuyOriginCaptured => PointBuyOrigin != null && PointBuyOrigin.CapturedBeforeRollOwnership;
        public PointBuyOrigin PristinePointBuy => PointBuyOrigin;
        public bool PristineBaselineCaptured => PointBuyOriginCaptured;
        public bool IsStaged => StagedGeneration == Generation;
        public bool IsApplied => IsRollMode && VerifiedGeneration == Generation;
        public bool IsApplicationFailed => FailedGeneration == Generation;

        public void BeginRollMode(PointBuyOrigin origin, StatAssignment assignment)
        {
            if (!IsPointBuyMode) throw new InvalidOperationException("Only PointBuy mode can capture a new point-buy origin.");
            PointBuyOrigin = origin ?? throw new ArgumentNullException(nameof(origin));
            pendingAssignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            pendingEntryFromPointBuy = true;
            pendingPriorRollWasVerified = false;
            Lifecycle.BeginRollMode();
            ResetApplicationTracking();
        }

        public void BeginRollReplacement(StatAssignment assignment)
        {
            if (!IsRollMode) throw new InvalidOperationException("Only Roll mode can replace its current assignment.");
            pendingPriorRollWasVerified = IsApplied;
            pendingAssignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            pendingEntryFromPointBuy = false;
            ResetApplicationTracking();
        }

        public void CommitRoll(RollCandidate candidate, long sequence)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            CommitRoll(candidate.Assignment);
            History.Add(Assignment, candidate.Rule, sequence, candidate.CreatedAtUtc, candidate.Equivalent);
        }

        public void CommitRecallOrAssignment(StatAssignment assignment)
        {
            CommitRoll(assignment);
            History.UpdateCurrentAssignment(Assignment);
        }

        public void AbortPendingRoll()
        {
            bool restorePriorVerifiedRoll = !pendingEntryFromPointBuy && pendingPriorRollWasVerified;
            if (pendingEntryFromPointBuy)
            {
                Lifecycle.AbortRollModeEntry();
                PointBuyOrigin = null;
            }
            pendingAssignment = null;
            pendingEntryFromPointBuy = false;
            pendingPriorRollWasVerified = false;
            ResetApplicationTracking();
            if (restorePriorVerifiedRoll)
            {
                StagedGeneration = Generation;
                VerifiedGeneration = Generation;
            }
        }

        public void ReplaceGenerationRollback(GenerationRollbackSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Generation != Generation)
            {
                throw new ArgumentException("Rollback snapshot must match the current preview generation.", nameof(snapshot));
            }
            GenerationRollback = snapshot;
        }

        public bool TryBeginApplicationAttempt(out string error)
        {
            if (!IsRollMode && !IsEnteringRollMode)
            {
                error = "The session is not entering or in Roll mode.";
                return false;
            }
            if (AssignmentForApplication == null)
            {
                error = "No rolled assignment is available for application.";
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
            VerifiedGeneration = generation;
        }

        public void BeginPointBuyRestore()
        {
            if (PointBuyOrigin == null) throw new InvalidOperationException("No point-buy origin is available.");
            Lifecycle.BeginPointBuyRestore();
        }

        public void MarkPointBuyRollbackVerified(int generation)
        {
            RequireCurrentGeneration(generation);
            if (!IsRestoringPointBuy)
            {
                throw new InvalidOperationException("Only a restoring session can complete a rollback to Roll mode.");
            }
            StagedGeneration = generation;
            Lifecycle.AbortPointBuyRestore();
            VerifiedGeneration = generation;
        }

        public void MarkPointBuyRestored(int generation)
        {
            RequireCurrentGeneration(generation);
            Lifecycle.MarkPointBuyRestored();
            pendingAssignment = null;
            pendingEntryFromPointBuy = false;
            ResetApplicationTracking();
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
            GenerationRollbackSnapshot generationRollback,
            bool pendingReplacementObserved)
        {
            if (!OwnsStableOwner(controller, stableOwner))
            {
                throw new InvalidOperationException("A different controller/source owner cannot rebind this roll session.");
            }
            int nextGeneration = Generation + 1;
            if (generationRollback == null) throw new ArgumentNullException(nameof(generationRollback));
            if (generationRollback.Generation != nextGeneration)
            {
                throw new ArgumentException("Rollback snapshot does not belong to the replacement generation.", nameof(generationRollback));
            }
            State = state ?? throw new ArgumentNullException(nameof(state));
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Distribution = distribution ?? throw new ArgumentNullException(nameof(distribution));
            GenerationRollback = generationRollback;
            PendingReplacementObserved = pendingReplacementObserved;
            Generation = nextGeneration;
            StatAssignment current = AssignmentForApplication;
            CandidateBaselineContaminated = current != null &&
                generationRollback.MatchesAssignment(current.ToAssignedArray());
            ResetApplicationTracking();
        }

        private void CommitRoll(StatAssignment assignment)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            if (pendingAssignment == null || !pendingAssignment.Equals(assignment))
            {
                throw new InvalidOperationException("The verified pending assignment does not match the requested commit.");
            }
            committedAssignment = pendingAssignment;
            pendingAssignment = null;
            pendingEntryFromPointBuy = false;
            pendingPriorRollWasVerified = false;
            Lifecycle.CommitRollMode();
            VerifiedGeneration = Generation;
            FailedGeneration = 0;
        }

        private void ResetApplicationTracking()
        {
            ApplicationAttempts = 0;
            StagedGeneration = 0;
            VerifiedGeneration = 0;
            FailedGeneration = 0;
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
