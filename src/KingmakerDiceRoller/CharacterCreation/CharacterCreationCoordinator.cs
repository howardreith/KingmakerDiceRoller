using System;
using KingmakerDiceRoller.Domain;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class CharacterCreationCoordinator
    {
        private readonly CharacterCreationContextPolicy contextPolicy;
        private readonly PointBudgetTracker budgetTracker;
        private readonly PointBudgetResolver budgetResolver;
        private readonly KingmakerStatAccess statAccess;
        private readonly RollSessionManager sessions;
        private readonly StatApplicationService application;
        private readonly PointBuyRestoreService pointBuyRestore;
        private readonly AbilityPhasePresentationService pointBuyPresentation;
        private readonly RuntimeDiagnostics diagnostics;
        private readonly IModLogger logger;
        private readonly Func<KingmakerContracts> contractsProvider;
        private readonly Func<bool> verboseProvider;

        public CharacterCreationCoordinator(
            CharacterCreationContextPolicy contextPolicy,
            PointBudgetTracker budgetTracker,
            PointBudgetResolver budgetResolver,
            KingmakerStatAccess statAccess,
            RollSessionManager sessions,
            StatApplicationService application,
            PointBuyRestoreService pointBuyRestore,
            AbilityPhasePresentationService pointBuyPresentation,
            RuntimeDiagnostics diagnostics,
            IModLogger logger,
            Func<KingmakerContracts> contractsProvider,
            Func<bool> verboseProvider)
        {
            this.contextPolicy = contextPolicy;
            this.budgetTracker = budgetTracker;
            this.budgetResolver = budgetResolver;
            this.statAccess = statAccess;
            this.sessions = sessions;
            this.application = application;
            this.pointBuyRestore = pointBuyRestore;
            this.pointBuyPresentation = pointBuyPresentation;
            this.diagnostics = diagnostics;
            this.logger = logger;
            this.contractsProvider = contractsProvider;
            this.verboseProvider = verboseProvider;
        }

        public bool HasActiveSession => sessions.Active != null;
        public bool CanRestorePointBuy => sessions.Active != null && sessions.Active.IsRollMode;

        public void OnLevelUpStateConstructed(object state, object unit, object mode)
        {
            KingmakerContracts contracts = contractsProvider();
            if (contracts == null) return;
            CharacterCreationContextDecision context = contextPolicy.Evaluate(state, unit, mode, contracts);
            if (!context.Accepted)
            {
                bool newlyObserved = diagnostics.Rejected(context.Reason);
                if (newlyObserved && verboseProvider()) logger.Info("Character-creation context rejected: " + context.Reason);
                return;
            }

            RollSession session;
            string sessionReason;
            try
            {
                if (!sessions.TryOpenOrRebind(
                    context,
                    generation => CapturePristinePointBuy(context, generation, contracts),
                    generation => GenerationRollbackSnapshot.Capture(
                        generation,
                        context.Distribution,
                        context.Unit,
                        contracts,
                        statAccess),
                    () => new StatAssignment(DiagnosticArrays.FixedPhaseTwoArray()),
                    out session,
                    out sessionReason))
                {
                    diagnostics.Rejected(sessionReason);
                    logger.Warning(sessionReason);
                    return;
                }
            }
            catch (Exception exception)
            {
                diagnostics.Rejected("Pristine point-buy or generation rollback capture failed.");
                logger.Exception("Capture point-buy ownership state", exception);
                return;
            }

            diagnostics.Accepted(
                context.Reason + " " + sessionReason +
                " Budget=" + session.PristinePointBuy.AllocatorBudget +
                " via " + session.PristinePointBuy.BudgetSource + ".");
            RecordEvent(sessionReason + " " + BuildSessionFacts(session));

            if (session.IsRestoringPointBuy)
            {
                RecordEvent(
                    "Observed a same-owner replacement during the bounded point-buy refresh; fixed-array staging is deferred. " +
                    BuildSessionFacts(session));
                return;
            }

            if (session.IsPointBuyMode)
            {
                RecordEvent(
                    "Observed a same-owner preview while durable point-buy mode is active; fixed-array staging remains suppressed. " +
                    BuildSessionFacts(session));
                diagnostics.SetStatus("Point-buy mode is active for the current character-build owner; roll staging is suppressed.");
                return;
            }

            if (session.IsApplied || session.IsStaged) return;

            string error;
            if (application.TryStageCurrentGeneration(session, contracts, out error))
            {
                RecordEvent(
                    "Staged the immutable fixed array on the accepted preview; awaiting live controller verification. " +
                    BuildSessionFacts(session));
                diagnostics.SetStatus("Fixed diagnostic array is staged; awaiting live controller state/preview verification.");
            }
            else
            {
                FailApplication(session, "Fixed-array staging failed closed: " + error);
            }
        }

        public void OnDistributionStarted(object distribution, int pointBudget)
        {
            budgetTracker.Record(distribution, pointBudget);
            RollSession session;
            if (!sessions.TryGetByDistribution(distribution, out session)) return;
            KingmakerContracts contracts = contractsProvider();
            if (contracts == null) return;
            if (!session.IsRollMode) return;
            if (session.IsApplied || session.IsStaged)
            {
                application.SuppressPointBuyAllocator(session, distribution, contracts);
                return;
            }
            string error;
            if (!application.TryStageCurrentGeneration(session, contracts, out error))
            {
                FailApplication(session, "Owned allocator restart failed closed: " + error);
            }
        }

        public void OnDistributionIsComplete(object distribution, ref bool result)
        {
            RollSession session;
            if (!sessions.TryGetByDistribution(distribution, out session)) return;
            if (!session.IsRollMode) return;
            KingmakerContracts contracts = contractsProvider();
            if (contracts != null && application.IsCurrentLiveDistribution(session, distribution, contracts))
            {
                result = true;
            }
        }

        public void Update(float deltaTime)
        {
            RollSession session = sessions.Active;
            if (session == null) return;
            KingmakerContracts contracts = contractsProvider();
            if (contracts == null) return;

            object currentController;
            object currentSourceUnit;
            object currentState;
            object currentPreview;
            bool observationSucceeded = contracts.TryGetLevelUpControllerContext(
                out currentController,
                out currentSourceUnit,
                out currentState,
                out currentPreview);
            RollSession released;
            if (sessions.ReleaseIfStableOwnerLost(
                currentController,
                currentSourceUnit,
                observationSucceeded,
                deltaTime,
                out released))
            {
                diagnostics.Released("The active character-build controller/source owner disappeared; session ownership was cleared.");
                diagnostics.SetStatus("Canceled or completed character-creation session released; waiting for a new exact context.");
                logger.Info("Released the Kingmaker Dice Roller session after its stable controller/source owner left character creation.");
                return;
            }

            session = sessions.Active;
            if (session == null ||
                !session.IsRollMode ||
                session.IsApplied ||
                session.IsApplicationFailed ||
                !session.IsStaged) return;

            LivePreviewObservation live;
            string error;
            if (application.TryMarkLiveVerified(session, contracts, out live, out error))
            {
                CompleteApplication(session, live);
                return;
            }

            if (!live.HasCurrentLiveBinding)
            {
                RecordEvent(
                    "Awaiting the accepted replacement to become the live controller state/preview. " +
                    live.BuildFacts(session, application.RefreshInProgress));
                return;
            }

            // ApplyLevelup replays Kingmaker actions after the LevelUpState constructor postfix.
            // If that replay overwrote the staged values, restage this already-live generation once.
            if (session.ApplicationAttempts < RollSession.MaximumApplicationAttemptsPerGeneration)
            {
                RecordEvent(
                    "The live replacement overwrote its constructor-stage values; performing one bounded live restage. " +
                    live.BuildFacts(session, application.RefreshInProgress));
                if (application.TryStageCurrentGeneration(session, contracts, out error) &&
                    application.TryMarkLiveVerified(session, contracts, out live, out error))
                {
                    CompleteApplication(session, live);
                    return;
                }
            }

            FailApplication(session, "Live fixed-array verification failed closed: " + error + " " +
                live.BuildFacts(session, application.RefreshInProgress));
        }

        public bool TryRestorePointBuy(out string error)
        {
            RollSession session = sessions.Active;
            if (session == null)
            {
                error = null;
                return true;
            }
            KingmakerContracts contracts = contractsProvider();
            if (contracts == null)
            {
                error = "Kingmaker contracts are unavailable.";
                return false;
            }
            PointBuyRestoreObservation restored;
            if (!pointBuyRestore.TryRestore(session, contracts, out restored, out error))
            {
                string detail = "Point-buy restoration failed closed: " + error + " " + BuildSessionFacts(session);
                diagnostics.SetStatus(detail);
                RecordEvent(detail);
                return false;
            }

            PointBuyPresentationObservation presentation;
            string presentationError;
            bool presentationSynchronized = pointBuyPresentation.TrySynchronize(
                session,
                contracts,
                out presentation,
                out presentationError);
            string facts = restored.BuildFacts(session, application.RefreshInProgress) + " " +
                presentation.BuildFacts(session);
            if (presentationSynchronized)
            {
                diagnostics.Restored(
                    "Pristine point-buy model and active ability-page presentation verified; native score rows, " +
                    "racial modifiers, allocator points, and controls now reflect the current live preview. " + facts);
                diagnostics.SetStatus(
                    "Pristine point-buy model and the active native ability page are synchronized; fixed-array staging is suppressed.");
                logger.Info("Verified pristine point-buy model and native ability-page presentation. " + facts);
            }
            else
            {
                string detail =
                    "Pristine point-buy model is verified and durable, but active ability-page presentation synchronization failed: " +
                    presentationError + " " + facts;
                diagnostics.Restored(detail);
                diagnostics.SetStatus(detail);
                logger.Warning(detail);
            }
            error = null;
            return true;
        }

        public bool TryPrepareDisable(out string error)
        {
            RollSession session = sessions.Active;
            if (session == null)
            {
                error = null;
                return true;
            }

            if (session.IsRestoringPointBuy)
            {
                error = "The active session is already restoring point buy and cannot be safely disabled.";
                return false;
            }

            if (session.IsRollMode && !TryRestorePointBuy(out error)) return false;

            session = sessions.Active;
            if (session != null && !session.IsPointBuyMode)
            {
                error = "Point-buy mode was not durably established before disable.";
                return false;
            }

            if (session != null) sessions.Clear(session);
            error = null;
            return true;
        }

        private void CompleteApplication(RollSession session, LivePreviewObservation live)
        {
            string detail = "Live controller state/preview verified for fixed array " + session.Assignment.RolledArray + ". " +
                live.BuildFacts(session, application.RefreshInProgress);
            diagnostics.Applied(detail);
            diagnostics.SetStatus("Fixed diagnostic array is active on the verified live new-character preview.");
            logger.Info("Fixed diagnostic array application verified against the live controller preview. " + detail);
        }

        private void FailApplication(RollSession session, string detail)
        {
            session.MarkApplicationFailed(session.Generation);
            diagnostics.SetStatus(detail);
            if (diagnostics.Event("FAIL " + detail)) logger.Error(detail);
        }

        private void RecordEvent(string detail)
        {
            if (diagnostics.Event(detail)) logger.Info(detail);
        }

        private static string BuildSessionFacts(RollSession session)
        {
            return "Facts: pristineBaselineCaptured=" + BooleanText(session.PristineBaselineCaptured) +
                ", pristineBaselineGeneration=" + session.PristinePointBuy.CapturedGeneration +
                ", currentGeneration=" + session.Generation +
                ", applicationGeneration=" + session.Generation +
                ", candidateBaselineContaminated=" + BooleanText(session.CandidateBaselineContaminated) +
                ", mode=" + session.Mode +
                ", allocatorBudget=" + session.PristinePointBuy.AllocatorBudget +
                ", pendingReplacementObserved=" + BooleanText(session.PendingReplacementObserved) +
                ", reboundPreview=" + BooleanText(session.ReboundPreview) +
                ", sameStableOwner=true" +
                ", rollSuppressedForStableOwner=" + BooleanText(session.RollSuppressedForStableOwner) + ".";
        }

        private PristinePointBuyState CapturePristinePointBuy(
            CharacterCreationContextDecision context,
            int generation,
            KingmakerContracts contracts)
        {
            int budget;
            string budgetSource;
            if (!budgetResolver.TryResolve(context.Distribution, contracts, out budget, out budgetSource))
            {
                throw new InvalidOperationException(
                    "Point-buy budget was not captured; refusing to create a non-restorable session.");
            }

            return PristinePointBuyState.Capture(
                context.Distribution,
                context.Unit,
                budget,
                budgetSource,
                generation,
                contracts,
                statAccess);
        }

        private static string BooleanText(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
