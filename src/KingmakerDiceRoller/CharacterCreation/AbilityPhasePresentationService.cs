using System;
using System.Reflection;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class AbilityPhasePresentationService
    {
        public const string NativeRefreshMethod =
            "Kingmaker.UI.LevelUp.CharBAbilityScoresAllocator.FillData()";

        private readonly LivePreviewInspector livePreview;
        private readonly IModLogger logger;
        private readonly NativeAbilityControlService nativeControls;
        private bool refreshInProgress;
        private RollSession lastSession;
        private int lastGeneration;
        private int refreshCountForGeneration;

        public AbilityPhasePresentationService(
            LivePreviewInspector livePreview,
            IModLogger logger)
            : this(livePreview, logger, null)
        {
        }

        public AbilityPhasePresentationService(
            LivePreviewInspector livePreview,
            IModLogger logger,
            NativeAbilityControlService nativeControls)
        {
            this.livePreview = livePreview ?? throw new ArgumentNullException(nameof(livePreview));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.nativeControls = nativeControls;
        }

        public bool IsRefreshInProgress => refreshInProgress;
        public int TotalRefreshCount { get; private set; }

        public bool TrySynchronize(
            RollSession session,
            KingmakerContracts contracts,
            out PointBuyPresentationObservation observation,
            out string error)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));
            if (!session.IsPointBuyMode)
            {
                observation = Observe(session, contracts, false, 0, session.Generation,
                    "Presentation synchronization requires durable point-buy mode.");
                error = observation.Failure;
                return false;
            }

            if (refreshInProgress)
            {
                observation = Observe(session, contracts, false, 0, session.Generation,
                    "A point-buy presentation refresh is already in progress; nested refresh was refused.");
                error = observation.Failure;
                return false;
            }

            object characterBuildController;
            bool abilityPhaseActive;
            object abilityPhase;
            object allocator;
            if (!contracts.TryGetAbilityPhasePresentationContext(
                out characterBuildController,
                out abilityPhaseActive,
                out abilityPhase,
                out allocator))
            {
                observation = Observe(session, contracts, false, 0, session.Generation,
                    "The native ability-phase presentation context could not be resolved.");
                error = observation.Failure;
                return false;
            }
            if (characterBuildController == null || !abilityPhaseActive || abilityPhase == null || allocator == null)
            {
                observation = Observe(session, contracts, false, 0, session.Generation,
                    "The native Skills ability-score phase is not the active character-build phase.");
                error = observation.Failure;
                return false;
            }

            int requestedGeneration = session.Generation;
            bool sameAttempt = ReferenceEquals(lastSession, session) && lastGeneration == requestedGeneration;
            if (!sameAttempt)
            {
                lastSession = session;
                lastGeneration = requestedGeneration;
                refreshCountForGeneration = 0;
            }

            if (refreshCountForGeneration == 0)
            {
                refreshInProgress = true;
                refreshCountForGeneration = 1;
                TotalRefreshCount++;
                try
                {
                    contracts.AbilityAllocatorFillDataMethod.Invoke(allocator, null);
                }
                catch (Exception exception)
                {
                    Exception actual = exception is TargetInvocationException && exception.InnerException != null
                        ? exception.InnerException
                        : exception;
                    logger.Exception("Refresh native ability-score phase after point-buy restoration", actual);
                    observation = Observe(
                        session,
                        contracts,
                        true,
                        refreshCountForGeneration,
                        requestedGeneration,
                        "Native ability-score refresh failed with " + actual.GetType().Name + ".");
                    error = observation.Failure;
                    return false;
                }
                finally
                {
                    refreshInProgress = false;
                }
            }

            observation = Observe(
                session,
                contracts,
                true,
                refreshCountForGeneration,
                requestedGeneration,
                null);
            if (!observation.IsSynchronized)
            {
                error = observation.Failure ??
                    "The native ability-score phase did not bind to the restored live point-buy preview.";
                return false;
            }

            nativeControls?.ReleaseAfterNativePointBuyRefresh();
            error = null;
            return true;
        }

        public bool TrySynchronizeRoll(
            RollSession session,
            KingmakerContracts contracts,
            out RollPresentationObservation observation,
            out string error)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));
            if (!session.IsRollMode && !session.IsEnteringRollMode)
            {
                observation = FailedRoll(session, false, 0, false,
                    "Roll presentation synchronization requires EnteringRollMode or Roll mode.");
                error = observation.Failure;
                return false;
            }
            if (nativeControls == null)
            {
                observation = FailedRoll(session, false, 0, false,
                    "Native point-buy control suppression is unavailable.");
                error = observation.Failure;
                return false;
            }
            if (refreshInProgress)
            {
                observation = FailedRoll(session, false, 0, false,
                    "A native ability presentation refresh is already in progress; nested refresh was refused.");
                error = observation.Failure;
                return false;
            }

            object characterBuild;
            bool active;
            object phase;
            object allocator;
            if (!contracts.TryGetAbilityPhasePresentationContext(
                out characterBuild,
                out active,
                out phase,
                out allocator) ||
                characterBuild == null || !active || phase == null || allocator == null)
            {
                observation = FailedRoll(session, false, 0, false,
                    "The native Skills ability-score phase is not active.");
                error = observation.Failure;
                return false;
            }

            int generation = session.Generation;
            refreshInProgress = true;
            TotalRefreshCount++;
            try
            {
                contracts.AbilityAllocatorFillDataMethod.Invoke(allocator, null);
            }
            catch (Exception exception)
            {
                Exception actual = exception is TargetInvocationException && exception.InnerException != null
                    ? exception.InnerException
                    : exception;
                logger.Exception("Refresh native ability-score phase for Roll mode", actual);
                observation = FailedRoll(session, true, 1, false,
                    "Native ability-score refresh failed with " + actual.GetType().Name + ".");
                error = observation.Failure;
                return false;
            }
            finally
            {
                refreshInProgress = false;
            }

            string controlsError;
            bool controlsSuppressed = nativeControls.TrySuppressForRoll(session, contracts, out controlsError);
            observation = ObserveRoll(session, contracts, allocator, generation, controlsSuppressed,
                controlsSuppressed ? null : controlsError);
            if (!observation.IsSynchronized)
            {
                error = observation.Failure ?? "The native ability page did not bind the verified rolled assignment.";
                return false;
            }
            error = null;
            return true;
        }

        public bool TryRefreshCurrentAbilityPhase(KingmakerContracts contracts, out string error)
        {
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));
            if (refreshInProgress)
            {
                error = "A native ability presentation refresh is already in progress.";
                return false;
            }
            object characterBuild;
            bool active;
            object phase;
            object allocator;
            if (!contracts.TryGetAbilityPhasePresentationContext(
                out characterBuild,
                out active,
                out phase,
                out allocator) ||
                characterBuild == null || !active || phase == null || allocator == null)
            {
                error = "The native Skills ability-score phase is not active.";
                return false;
            }
            refreshInProgress = true;
            TotalRefreshCount++;
            try
            {
                contracts.AbilityAllocatorFillDataMethod.Invoke(allocator, null);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                Exception actual = exception is TargetInvocationException && exception.InnerException != null
                    ? exception.InnerException
                    : exception;
                logger.Exception("Refresh current native ability-score phase", actual);
                error = actual.Message;
                return false;
            }
            finally
            {
                refreshInProgress = false;
            }
        }

        private RollPresentationObservation ObserveRoll(
            RollSession session,
            KingmakerContracts contracts,
            object allocator,
            int requestedGeneration,
            bool controlsSuppressed,
            string failure)
        {
            try
            {
                int[] values = session.AssignmentForApplication.ToAssignedArray();
                LivePreviewObservation semantic = livePreview.Observe(
                    session,
                    values,
                    values,
                    false,
                    0,
                    null,
                    contracts);
                object controller;
                object source;
                object state;
                object preview;
                bool observed = contracts.TryGetLevelUpControllerContext(
                    out controller,
                    out source,
                    out state,
                    out preview);
                bool stateMatches = observed && ReferenceEquals(state, session.State);
                bool distributionMatches = stateMatches && ReferenceEquals(
                    ReflectionAccess.Read(contracts.LevelUpStateDistributionMember, state),
                    session.Distribution);
                object expectedSourceEntity;
                object expectedPreviewEntity;
                bool sourceResolved = contracts.TryGetDescriptorEntity(session.StableOwner, out expectedSourceEntity);
                bool previewResolved = contracts.TryGetDescriptorEntity(session.Unit, out expectedPreviewEntity);
                bool sourceMatches = sourceResolved && ReferenceEquals(
                    contracts.AbilityAllocatorSourceEntityField.GetValue(allocator), expectedSourceEntity);
                bool previewMatches = previewResolved && ReferenceEquals(
                    contracts.AbilityAllocatorPreviewEntityField.GetValue(allocator), expectedPreviewEntity);
                return new RollPresentationObservation(
                    semantic.IsVerified,
                    true,
                    1,
                    true,
                    stateMatches,
                    distributionMatches,
                    sourceMatches,
                    previewMatches,
                    controlsSuppressed,
                    requestedGeneration,
                    session.Generation,
                    failure);
            }
            catch (Exception exception)
            {
                return FailedRoll(session, true, 1, controlsSuppressed,
                    failure ?? "Roll presentation observation failed with " + exception.GetType().Name + ".");
            }
        }

        private static RollPresentationObservation FailedRoll(
            RollSession session,
            bool refreshRequested,
            int refreshCount,
            bool controlsSuppressed,
            string failure)
        {
            return new RollPresentationObservation(
                false,
                refreshRequested,
                refreshCount,
                false,
                false,
                false,
                false,
                false,
                controlsSuppressed,
                session.Generation,
                session.Generation,
                failure);
        }

        private PointBuyPresentationObservation Observe(
            RollSession session,
            KingmakerContracts contracts,
            bool refreshRequested,
            int refreshCount,
            int refreshGeneration,
            string failure)
        {
            try
            {
                PointBuyOrigin pristine = session.PointBuyOrigin;
                LivePreviewObservation semantic = livePreview.Observe(
                    session,
                    pristine.Values.DistributionValues,
                    pristine.Values.UnitValues,
                    pristine.AllocatorAvailable,
                    pristine.RemainingPoints,
                    pristine.TotalPoints,
                    contracts);

                object controller;
                object sourceUnit;
                object currentState;
                object currentPreview;
                bool controllerObserved = contracts.TryGetLevelUpControllerContext(
                    out controller,
                    out sourceUnit,
                    out currentState,
                    out currentPreview);
                bool stateMatches = controllerObserved && ReferenceEquals(currentState, session.State);
                bool distributionMatches = false;
                if (stateMatches && currentState != null)
                {
                    object currentDistribution = ReflectionAccess.Read(
                        contracts.LevelUpStateDistributionMember,
                        currentState);
                    distributionMatches = ReferenceEquals(currentDistribution, session.Distribution);
                }

                object characterBuildController;
                bool abilityPhaseActive;
                object abilityPhase;
                object allocator;
                bool presentationObserved = contracts.TryGetAbilityPhasePresentationContext(
                    out characterBuildController,
                    out abilityPhaseActive,
                    out abilityPhase,
                    out allocator);
                bool activeAbilityPhaseFound = presentationObserved &&
                    characterBuildController != null &&
                    abilityPhaseActive &&
                    abilityPhase != null &&
                    allocator != null;

                object expectedSourceEntity;
                object expectedPreviewEntity;
                bool sourceEntityResolved = contracts.TryGetDescriptorEntity(
                    session.StableOwner,
                    out expectedSourceEntity);
                bool previewEntityResolved = contracts.TryGetDescriptorEntity(
                    session.Unit,
                    out expectedPreviewEntity);
                bool sourceMatches = false;
                bool previewMatches = false;
                if (activeAbilityPhaseFound)
                {
                    object boundSource = contracts.AbilityAllocatorSourceEntityField.GetValue(allocator);
                    object boundPreview = contracts.AbilityAllocatorPreviewEntityField.GetValue(allocator);
                    sourceMatches = sourceEntityResolved && ReferenceEquals(boundSource, expectedSourceEntity);
                    previewMatches = previewEntityResolved && ReferenceEquals(boundPreview, expectedPreviewEntity);
                }

                return new PointBuyPresentationObservation(
                    semantic.IsVerified,
                    refreshRequested,
                    NativeRefreshMethod,
                    refreshCount,
                    activeAbilityPhaseFound,
                    stateMatches,
                    distributionMatches,
                    sourceMatches,
                    previewMatches,
                    refreshGeneration,
                    session.Generation,
                    semantic.IsVerified,
                    failure);
            }
            catch (Exception exception)
            {
                return new PointBuyPresentationObservation(
                    false,
                    refreshRequested,
                    NativeRefreshMethod,
                    refreshCount,
                    false,
                    false,
                    false,
                    false,
                    false,
                    refreshGeneration,
                    session.Generation,
                    false,
                    failure ?? "Presentation observation failed with " + exception.GetType().Name + ".");
            }
        }
    }
}
