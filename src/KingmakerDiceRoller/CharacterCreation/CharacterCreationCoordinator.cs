using System;
using System.Linq;
using KingmakerDiceRoller.Domain;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class CharacterCreationCoordinator : IRollUiCommandTarget
    {
        private readonly CharacterCreationContextPolicy contextPolicy;
        private readonly PointBudgetTracker budgetTracker;
        private readonly PointBudgetResolver budgetResolver;
        private readonly KingmakerStatAccess statAccess;
        private readonly RollSessionManager sessions;
        private readonly StatApplicationService application;
        private readonly MercenaryFinalizationService mercenaryFinalization;
        private readonly PointBuyRestoreService pointBuyRestore;
        private readonly AbilityPhasePresentationService pointBuyPresentation;
        private readonly RuntimeDiagnostics diagnostics;
        private readonly IModLogger logger;
        private readonly Func<KingmakerContracts> contractsProvider;
        private readonly Func<bool> verboseProvider;
        private readonly CharacterRollWorkflow workflow;

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
            : this(
                contextPolicy,
                budgetTracker,
                budgetResolver,
                statAccess,
                sessions,
                application,
                pointBuyRestore,
                pointBuyPresentation,
                diagnostics,
                logger,
                contractsProvider,
                verboseProvider,
                new CharacterRollWorkflow(
                    new DiceRollEngine(new DiceExpressionParser(), new SystemRandomSource()),
                    new PointBuyEquivalentCalculator(),
                    RollConfiguration.Default(),
                    null,
                    () => DateTime.UtcNow.ToString("o"),
                    null))
        {
        }

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
            Func<bool> verboseProvider,
            CharacterRollWorkflow workflow)
        {
            this.contextPolicy = contextPolicy;
            this.budgetTracker = budgetTracker;
            this.budgetResolver = budgetResolver;
            this.statAccess = statAccess;
            this.sessions = sessions;
            this.application = application;
            mercenaryFinalization = new MercenaryFinalizationService(statAccess);
            this.pointBuyRestore = pointBuyRestore;
            this.pointBuyPresentation = pointBuyPresentation;
            this.diagnostics = diagnostics;
            this.logger = logger;
            this.contractsProvider = contractsProvider;
            this.verboseProvider = verboseProvider;
            this.workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        }

        public bool HasActiveSession => sessions.Active != null;
        public bool CanRestorePointBuy => sessions.Active != null && sessions.Active.IsRollMode;
        public RollSession ActiveSession => sessions.Active;
        public RollUiSnapshot UiSnapshot => workflow.Snapshot(sessions.Active);
        public bool CanAttachNativePanel
        {
            get
            {
                try
                {
                    RollSession session = sessions.Active;
                    KingmakerContracts contracts = contractsProvider();
                    return session != null && contracts != null && HasCurrentLiveBinding(session, contracts);
                }
                catch
                {
                    return false;
                }
            }
        }

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
                    generation => GenerationRollbackSnapshot.Capture(
                        generation,
                        context.Distribution,
                        context.Unit,
                        contracts,
                        statAccess),
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
                diagnostics.Rejected("Generation rollback capture failed.");
                logger.Exception("Capture current preview generation", exception);
                return;
            }

            diagnostics.Accepted(context.Reason + " " + sessionReason);
            RecordEvent(sessionReason + " " + BuildSessionFacts(session));

            if (session.Generation == 1 && session.IsPointBuyMode)
            {
                workflow.SetNewSessionStatus();
            }

            if (session.IsRestoringPointBuy)
            {
                RecordEvent(
                    "Observed a same-owner replacement during bounded point-buy restoration; roll staging is suppressed. " +
                    BuildSessionFacts(session));
                return;
            }

            if (session.IsPointBuyMode)
            {
                RecordEvent(
                    "Observed a same-owner preview while PointBuy mode is active; no array was generated or staged. " +
                    BuildSessionFacts(session));
                diagnostics.SetStatus("Point Buy is active; use the native Dice Roller panel to roll explicitly.");
                return;
            }

            if (session.IsApplied || session.IsStaged) return;

            string error;
            if (application.TryStageCurrentGeneration(session, contracts, out error))
            {
                RecordEvent(
                    "Staged the explicit rolled assignment on the accepted preview; awaiting live controller verification. " +
                    BuildSessionFacts(session));
                diagnostics.SetStatus("Rolled assignment is staged; awaiting live controller verification.");
            }
            else
            {
                FailApplication(session, "Rolled-array staging failed closed: " + error);
            }
        }

        public void OnDistributionStarted(object distribution, int pointBudget)
        {
            budgetTracker.Record(distribution, pointBudget);
            RollSession session;
            if (!sessions.TryGetByDistribution(distribution, out session)) return;
            KingmakerContracts contracts = contractsProvider();
            if (contracts == null) return;
            if (!session.IsRollMode && !session.IsEnteringRollMode) return;
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

        public void OnLevelUpAppliedToAuthoritativeUnit(
            object controller,
            object finalDescriptor)
        {
            RollSession session = sessions.Active;
            if (session == null ||
                session.CreationKind != SupportedCharacterCreationKind.Mercenary ||
                !ReferenceEquals(session.Controller, controller) ||
                !ReferenceEquals(session.StableOwner, finalDescriptor))
            {
                return;
            }

            KingmakerContracts contracts = contractsProvider();
            MercenaryFinalizationObservation observation;
            string error;
            if (mercenaryFinalization.TryApplyAuthoritativeAssignment(
                session,
                controller,
                finalDescriptor,
                contracts,
                out observation,
                out error))
            {
                return;
            }

            session.MarkFinalizationFailed();
            string detail = "Mercenary authoritative assignment failed before the native success callback: " +
                observation.BuildFacts();
            diagnostics.SetStatus(detail);
            if (diagnostics.Event("FINALIZATION APPLY FAILURE " + detail))
            {
                logger.Error(detail);
            }
        }

        public void OnLevelUpCommitCompleted(object controller)
        {
            RollSession session = sessions.Active;
            if (session == null ||
                session.CreationKind != SupportedCharacterCreationKind.Mercenary ||
                !ReferenceEquals(session.Controller, controller) ||
                !session.IsRollMode)
            {
                return;
            }

            KingmakerContracts contracts = contractsProvider();
            MercenaryFinalizationObservation observation;
            string error;
            bool passed = mercenaryFinalization.TryVerifyAfterSuccessCallback(
                session,
                controller,
                contracts,
                out observation,
                out error);
            if (!passed) session.MarkFinalizationFailed();

            try
            {
                session.Complete();
            }
            finally
            {
                sessions.Clear(session);
            }

            string detail = "Mercenary rolled-stat final verification: " + observation.BuildFacts();
            if (passed)
            {
                diagnostics.FinalizationVerified(detail);
                diagnostics.SetStatus("The final hired mercenary retained the verified rolled base values.");
                logger.Info(detail);
            }
            else
            {
                diagnostics.FinalizationFailed(detail);
                diagnostics.SetStatus(detail);
                logger.Error(detail);
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
                (!session.IsRollMode && !session.IsEnteringRollMode) ||
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

            FailApplication(session, "Live rolled-array verification failed closed: " + error + " " +
                live.BuildFacts(session, application.RefreshInProgress));
        }

        public bool TryRoll(out string error)
        {
            RollSession session = sessions.Active;
            if (session == null || !session.IsPointBuyMode)
            {
                error = "Roll is available only for an active new-character Point Buy session.";
                workflow.SetFailure(error);
                return false;
            }
            PointBuyOrigin origin;
            if (!TryCapturePointBuyOrigin(session, out origin, out error)) return false;
            RollCandidate candidate;
            if (!workflow.TryGenerate(out candidate, out error)) return false;
            return TryApplyUserAssignment(
                session,
                candidate.Assignment,
                origin,
                () => workflow.CommitGenerated(session, candidate, false),
                "Roll",
                out error);
        }

        public bool TryReroll(out string error)
        {
            RollSession session = sessions.Active;
            if (session == null || !session.IsRollMode)
            {
                error = "Reroll is available only while Roll Mode is active.";
                workflow.SetFailure(error);
                return false;
            }
            RollCandidate candidate;
            if (!workflow.TryGenerate(out candidate, out error)) return false;
            return TryApplyUserAssignment(
                session,
                candidate.Assignment,
                null,
                () => workflow.CommitGenerated(session, candidate, true),
                "Reroll",
                out error);
        }

        public bool TryMoveAssignment(AbilityScore ability, bool moveUp, out string error)
        {
            RollSession session = sessions.Active;
            if (session == null || !session.IsRollMode || session.Assignment == null)
            {
                error = "Assignment controls require an active verified Roll Mode array.";
                workflow.SetFailure(error);
                return false;
            }
            StatAssignment next = moveUp
                ? session.Assignment.MoveUp(ability)
                : session.Assignment.MoveDown(ability);
            if (ReferenceEquals(next, session.Assignment))
            {
                error = null;
                return true;
            }
            return TryApplyUserAssignment(
                session,
                next,
                null,
                () => workflow.CommitAssignment(session, next, "Assignment order verified on the live preview."),
                "Reassign",
                out error);
        }

        public void SelectPreviousHistory()
        {
            workflow.PreviousHistory(sessions.Active);
        }

        public void SelectNextHistory()
        {
            workflow.NextHistory(sessions.Active);
        }

        public bool TryUseSelectedHistory(out string error)
        {
            RollSession session = sessions.Active;
            RollHistoryEntry entry = session == null ? null : session.History.Selected;
            if (session == null || entry == null ||
                (!session.IsPointBuyMode && !session.IsRollMode))
            {
                error = "No usable history entry is selected.";
                workflow.SetFailure(error);
                return false;
            }
            StatAssignment next = entry.Assignment;
            PointBuyOrigin origin = null;
            if (session.IsPointBuyMode && !TryCapturePointBuyOrigin(session, out origin, out error))
            {
                return false;
            }
            return TryApplyUserAssignment(
                session,
                next,
                origin,
                () => workflow.CommitHistorySelection(session, entry),
                "Use history",
                out error);
        }

        public bool TryStoreCurrent(out string error)
        {
            try
            {
                workflow.StoreCurrent(sessions.Active);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                workflow.SetFailure(error);
                return false;
            }
        }

        public void SelectPreviousSaved()
        {
            workflow.PreviousSaved();
        }

        public void SelectNextSaved()
        {
            workflow.NextSaved();
        }

        public bool TryRecallSelectedSaved(out string error)
        {
            RollSession session = sessions.Active;
            SavedRollArrayRecord record = workflow.Saved.Selected;
            if (session == null || record == null ||
                (!session.IsPointBuyMode && !session.IsRollMode))
            {
                error = "No saved array can be recalled in the current session.";
                workflow.SetFailure(error);
                return false;
            }
            StatAssignment assignment;
            if (!record.TryCreateAssignment(out assignment, out error))
            {
                workflow.SetFailure("Saved array is invalid: " + error);
                return false;
            }
            PointBuyOrigin origin = null;
            if (session.IsPointBuyMode && !TryCapturePointBuyOrigin(session, out origin, out error))
            {
                return false;
            }
            return TryApplyUserAssignment(
                session,
                assignment,
                origin,
                () => workflow.CommitSavedRecall(session, record, assignment),
                "Recall",
                out error);
        }

        public bool DeleteSelectedSaved()
        {
            return workflow.DeleteSelectedSaved();
        }

        public void SetPreset(DiceRollPreset preset)
        {
            workflow.SetPreset(preset);
        }

        public void SetLowScorePolicy(LowScorePolicy policy)
        {
            workflow.SetLowScorePolicy(policy);
        }

        public void SetMinimumScore(int minimum)
        {
            workflow.SetMinimumScore(minimum);
        }

        public void SetCustomExpression(string expression)
        {
            workflow.SetCustomExpression(expression);
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
                    "The captured point-buy model and active native ability page are synchronized; rolled-array staging is suppressed.");
                logger.Info("Verified pristine point-buy model and native ability-page presentation. " + facts);
                workflow.SetPointBuyStatus();
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
            StatAssignment assignment = session.AssignmentForApplication;
            string detail = "Live controller state/preview verified for rolled array " + assignment.RolledArray + ". " +
                live.BuildFacts(session, application.RefreshInProgress);
            diagnostics.Applied(detail);
            diagnostics.SetStatus("Roll Mode is active on the verified live new-character preview.");
            logger.Info("Rolled-array application verified against the live controller preview. " + detail);
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

        private bool TryApplyUserAssignment(
            RollSession session,
            StatAssignment next,
            PointBuyOrigin capturedOrigin,
            Action commit,
            string commandName,
            out string error)
        {
            KingmakerContracts contracts = contractsProvider();
            if (contracts == null)
            {
                error = "Kingmaker contracts are unavailable.";
                workflow.SetFailure(error);
                return false;
            }
            if (!HasCurrentLiveBinding(session, contracts))
            {
                error = "The current controller preview is not safely bound to this character-roll session.";
                workflow.SetFailure(error);
                return false;
            }

            GenerationRollbackSnapshot rollback;
            try
            {
                rollback = GenerationRollbackSnapshot.Capture(
                    session.Generation,
                    session.Distribution,
                    session.Unit,
                    contracts,
                    statAccess);
                session.ReplaceGenerationRollback(rollback);
                if (capturedOrigin != null)
                {
                    session.BeginRollMode(capturedOrigin, next);
                }
                else
                {
                    session.BeginRollReplacement(next);
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
                workflow.SetFailure(error);
                logger.Exception(commandName + " preparation", exception);
                return false;
            }

            LivePreviewObservation live = null;
            RollPresentationObservation presentation = null;
            try
            {
                if (!application.TryStageCurrentGeneration(session, contracts, out error))
                {
                    throw new InvalidOperationException(error);
                }
                if (!application.TryMarkLiveVerified(session, contracts, out live, out error))
                {
                    throw new InvalidOperationException(error);
                }
                if (!pointBuyPresentation.TrySynchronizeRoll(
                    session,
                    contracts,
                    out presentation,
                    out error))
                {
                    throw new InvalidOperationException(error);
                }

                commit();
                string facts = live.BuildFacts(session, application.RefreshInProgress) + " " +
                    presentation.BuildFacts();
                diagnostics.Applied(commandName + " committed after live model, allocator, controls, and presentation verification. " + facts);
                diagnostics.SetStatus("Roll Mode is active; the rolled array owns the verified live ability preview.");
                logger.Info(commandName + " committed transactionally. " + facts);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                string primary = exception.Message;
                bool rollbackVerified = false;
                try
                {
                    rollback.Restore(session.Distribution, session.Unit, contracts, statAccess);
                    rollbackVerified = rollback.Values.DistributionValues.SequenceEqual(
                            statAccess.ReadDistributionValues(session.Distribution, contracts)) &&
                        rollback.Values.UnitValues.SequenceEqual(
                            statAccess.ReadUnitBaseValues(session.Unit, contracts)) &&
                        statAccess.ReadDistributionAvailable(session.Distribution, contracts) == rollback.AllocatorAvailable &&
                        statAccess.ReadDistributionPoints(session.Distribution, contracts) == rollback.RemainingPoints &&
                        statAccess.ReadDistributionTotalPoints(session.Distribution, contracts) == rollback.TotalPoints;
                    if (!rollbackVerified)
                    {
                        throw new InvalidOperationException("The command rollback did not verify on the current live preview.");
                    }
                    session.AbortPendingRoll();
                    string ignored;
                    pointBuyPresentation.TryRefreshCurrentAbilityPhase(contracts, out ignored);
                }
                catch (Exception rollbackException)
                {
                    logger.Exception(commandName + " rollback", rollbackException);
                    primary += " Recovery also failed: " + rollbackException.Message;
                }
                error = commandName + " failed without committing: " + primary;
                workflow.SetFailure(error);
                diagnostics.SetStatus(error);
                RecordEvent("FAIL " + error + " rollbackVerified=" + BooleanText(rollbackVerified) + ".");
                return false;
            }
        }

        private static bool HasCurrentLiveBinding(RollSession session, KingmakerContracts contracts)
        {
            object controller;
            object source;
            object state;
            object preview;
            if (!contracts.TryGetLevelUpControllerContext(out controller, out source, out state, out preview))
            {
                return false;
            }
            if (!session.OwnsStableOwner(controller, source) ||
                !session.OwnsState(state) ||
                !session.OwnsUnit(preview))
            {
                return false;
            }
            object distribution = ReflectionAccess.Read(contracts.LevelUpStateDistributionMember, state);
            return session.OwnsDistribution(distribution);
        }

        private static string BuildSessionFacts(RollSession session)
        {
            PointBuyOrigin origin = session.PointBuyOrigin;
            return "Facts: pointBuyOriginCaptured=" + BooleanText(session.PointBuyOriginCaptured) +
                ", creationKind=" + session.CreationKind +
                ", pointBuyOriginGeneration=" + (origin == null ? 0 : origin.CapturedGeneration) +
                ", currentGeneration=" + session.Generation +
                ", applicationGeneration=" + session.Generation +
                ", candidateBaselineContaminated=" + BooleanText(session.CandidateBaselineContaminated) +
                ", mode=" + session.Mode +
                ", allocatorBudget=" + (origin == null ? -1 : origin.AllocatorBudget) +
                ", pendingReplacementObserved=" + BooleanText(session.PendingReplacementObserved) +
                ", reboundPreview=" + BooleanText(session.ReboundPreview) +
                ", sameStableOwner=true" +
                ", rollSuppressedForStableOwner=" + BooleanText(session.RollSuppressedForStableOwner) + ".";
        }

        private PointBuyOrigin CapturePointBuyOrigin(
            RollSession session,
            KingmakerContracts contracts)
        {
            int budget;
            string budgetSource;
            if (!statAccess.ReadDistributionAvailable(session.Distribution, contracts))
            {
                throw new InvalidOperationException("Point Buy is not active; refusing to capture a roll origin.");
            }
            if (!budgetResolver.TryResolve(session.Distribution, contracts, out budget, out budgetSource))
            {
                throw new InvalidOperationException(
                    "Point-buy budget was not observed; refusing to enter non-restorable Roll mode.");
            }

            return PointBuyOrigin.Capture(
                session.Distribution,
                session.Unit,
                budget,
                budgetSource,
                session.Generation,
                contracts,
                statAccess);
        }

        private bool TryCapturePointBuyOrigin(
            RollSession session,
            out PointBuyOrigin origin,
            out string error)
        {
            origin = null;
            KingmakerContracts contracts = contractsProvider();
            if (contracts == null)
            {
                error = "Kingmaker contracts are unavailable.";
                workflow.SetFailure(error);
                return false;
            }
            if (!HasCurrentLiveBinding(session, contracts))
            {
                error = "The current point-buy preview is not safely bound to this character-roll session.";
                workflow.SetFailure(error);
                return false;
            }
            try
            {
                origin = CapturePointBuyOrigin(session, contracts);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                workflow.SetFailure(error);
                logger.Exception("Capture point-buy origin before explicit Roll Mode entry", exception);
                return false;
            }
        }

        private static string BooleanText(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
