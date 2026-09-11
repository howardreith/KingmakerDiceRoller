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
        private readonly DerivedStateRefreshService derivedRefresh;
        private readonly SkillsPhaseSynchronizationService skillsSync;
        private readonly MercenaryFinalizationService mercenaryFinalization;
        private readonly RespecLifecycleService respec;
        public RespecLifecycleService Respec => respec;
        public MercenaryFinalizationService MercenaryFinalization => mercenaryFinalization;
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
            DerivedStateRefreshService derivedRefresh,
            SkillsPhaseSynchronizationService skillsSync,
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
                derivedRefresh,
                skillsSync,
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
            DerivedStateRefreshService derivedRefresh,
            SkillsPhaseSynchronizationService skillsSync,
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
            this.derivedRefresh = derivedRefresh ?? throw new ArgumentNullException(nameof(derivedRefresh));
            this.skillsSync = skillsSync ?? throw new ArgumentNullException(nameof(skillsSync));
            mercenaryFinalization = new MercenaryFinalizationService(statAccess);
            respec = new RespecLifecycleService(statAccess);
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
            // The one-use authoritative tickets apply before native action checks, while
            // first admission waits for HandleLevelUpStart to finish binding the controller.
            respec.BeforeReplay(state, unit, contracts);
            TryBeginMercenaryCommitReplay(state, unit, contracts);
            CharacterCreationContextDecision context = contextPolicy.Evaluate(state, unit, mode, contracts,
                respec.Active, sessions.Active != null && (sessions.Active.IsRollMode || sessions.Active.IsRestoringPointBuy));
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
                bool stagedOnAuthoritativeSource = ReferenceEquals(session.Unit, session.StableOwner);
                RecordEvent(
                    (stagedOnAuthoritativeSource
                        ? "Staged the explicit rolled assignment on the authoritative stable owner before native action replay. "
                        : "Staged the explicit rolled assignment on the accepted preview; awaiting live controller verification. ") +
                    BuildSessionFacts(session));
                if (!stagedOnAuthoritativeSource)
                {
                    diagnostics.SetStatus("Rolled assignment is staged; awaiting live controller verification.");
                }
            }
            else
            {
                FailApplication(session, "Rolled-array staging failed closed: " + error);
            }
        }

        private void TryBeginMercenaryCommitReplay(object state, object unit, KingmakerContracts contracts)
        {
            RollSession session = sessions.Active;
            if (session == null || session.CreationKind != SupportedCharacterCreationKind.Mercenary)
            {
                return;
            }
            object activeController;
            if (!contracts.TryGetLevelUpController(out activeController) ||
                !ReferenceEquals(activeController, session.Controller))
            {
                return;
            }
            string error;
            if (mercenaryFinalization.TryBeginReplay(session, session.Controller, state, unit, contracts, out error))
            {
                RecordEvent(
                    "Staged the verified rolled assignment on the exact mercenary stable owner before native commit replay. " +
                    BuildSessionFacts(session));
                return;
            }
            if (error != null)
            {
                diagnostics.FinalizationFailed("Mercenary pre-replay staging failed: " + error);
                logger.Error("Mercenary pre-replay staging failed: " + error);
            }
        }

        public void OnRespecBound(RespecOwnership owner)
        {
            if (owner == null || sessions.Active != null || !respec.Bind(owner)) return;
            ObserveRespecPreview(owner.Controller);
            if (sessions.Active == null) respec.Abort(owner.Controller, "Respec admission rejected.");
        }

        public void ObserveRespecPreview(object controller)
        {
            KingmakerContracts contracts = contractsProvider();
            RespecOwnership owner = respec.Active;
            if (owner == null || !owner.Owns(controller, owner.Source) || contracts == null) return;
            object state = ReflectionAccess.Read(contracts.LevelUpControllerStateMember, controller);
            object preview = ReflectionAccess.Read(contracts.LevelUpControllerPreviewMember, controller);
            if (state == null || !ReferenceEquals(ReflectionAccess.Read(contracts.LevelUpStateUnitMember, state), preview)) return;
            OnLevelUpStateConstructed(state, preview, ReflectionAccess.Read(contracts.LevelUpStateModeMember, state));
        }

        public void OnRespecCommitStarted(object controller, Func<bool> inProgress)
        {
            RollSession session = sessions.Active;
            if (!respec.BeginCommit(session, controller, contractsProvider(), inProgress)) return;
            sessions.Clear(session);
            session.Complete();
        }

        public void OnBuildCanceled(object controller)
        {
            respec.Abort(controller, "Canceled or interrupted native build.");
            mercenaryFinalization.Abort(controller, "Canceled or interrupted native build.", contractsProvider());
            RollSession session = sessions.Active;
            if (session != null && session.CreationKind == SupportedCharacterCreationKind.Respec && ReferenceEquals(session.Controller, controller))
            { session.Lifecycle.Abandon(); sessions.Clear(session); }
        }

        public void ReportRespecContract(string detail)
        {
            if (diagnostics.Event(detail)) logger.Info(detail);
        }

        public void ReportRespecExclusion(string reason)
        {
            if (diagnostics.Rejected(reason)) logger.Info("Respec panel unavailable: " + reason);
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

        // Exact 2.1.7b IL proves every forward phase route funnels through
        // CharacterBuildController.SetPhase(Type); this guard may only veto, never permit.
        public bool AllowForwardPhaseTransition(object characterBuildController, int targetPhaseValue)
        {
            try
            {
                KingmakerContracts contracts = contractsProvider();
                RollSession session = sessions.Active;
                if (contracts == null || session == null || session.IsRestoringPointBuy) return true;
                object boundController = ReflectionAccess.Read(
                    contracts.CharacterBuildLevelUpControllerMember,
                    characterBuildController);
                if (!ReferenceEquals(boundController, session.Controller)) return true;

                object currentPhase = ReflectionAccess.Read(
                    contracts.CharacterBuildCurrentPhaseMember,
                    characterBuildController);
                if (currentPhase == null) return true;
                int skillsPhaseValue;
                try { skillsPhaseValue = Convert.ToInt32(contracts.SkillsPhaseValue); }
                catch { return true; }
                int currentPhaseValue = Convert.ToInt32(currentPhase);
                // Veto only forward movement beyond Skills (Next or a later-phase jump);
                // Back and same-phase navigation always stay available.
                if (targetPhaseValue <= skillsPhaseValue || currentPhaseValue > skillsPhaseValue) return true;

                object state = ReflectionAccess.Read(contracts.LevelUpControllerStateMember, session.Controller);
                if (state == null) return true;
                object complete = contracts.LevelUpStateIsSkillPointsCompleteMethod.Invoke(state, null);
                if (!(complete is bool)) return true;
                if ((bool)complete) return true;

                // Veto: the live native model says the Skills allocation is invalid. Refresh
                // the stale presentation, show the native attention marks, and explain why.
                int remaining = ReadSkillPointsRemaining(state, contracts);
                SynchronizeSkillsPresentation(session, "blocked forward transition");
                object skillsPhase = ReflectionAccess.Read(
                    contracts.CharacterBuildSkillsPhaseMember,
                    characterBuildController);
                if (skillsPhase != null)
                {
                    try { contracts.SkillsPhaseBlinkMarksMethod.Invoke(skillsPhase, null); }
                    catch (Exception blinkException)
                    {
                        logger.Exception("Blink native skills marks after a blocked transition", blinkException);
                    }
                }
                string reason = remaining > 0
                    ? "Spend " + remaining + " remaining skill point(s) on Skills before continuing."
                    : remaining < 0
                        ? "Remove " + (-remaining) + " excess skill rank(s) on Skills before continuing."
                        : "Resolve the skill allocation on Skills before continuing.";
                diagnostics.SetStatus(reason);
                if (diagnostics.Event("BLOCKED FORWARD " + reason + " " + BuildSessionFacts(session)))
                {
                    logger.Info("Blocked an invalid forward phase transition out of Skills. " + reason);
                }
                return false;
            }
            catch (Exception exception)
            {
                logger.Exception("Forward phase-transition guard", exception);
                return true;
            }
        }

        public void OnRollDrawerClosed()
        {
            RollSession session = sessions.Active;
            if (session == null) return;
            SynchronizeSkillsPresentation(session, "roll drawer close");
        }

        private void SynchronizeSkillsPresentation(RollSession session, string context)
        {
            if (session == null) return;
            if (!skillsSync.IsSynchronizationPending(session)) return;
            int revision;
            string error;
            if (skillsSync.TrySynchronize(session, contractsProvider(), out revision, out error))
            {
                RecordEvent(
                    "Synchronized the native skills page (remaining-points badge, phase completion, navigation) via " +
                    SkillsPhaseSynchronizationService.NativeRefreshPath + "; revision=" + revision +
                    " (" + context + ").");
                return;
            }
            string detail = "The native skills page did not refresh after " + context + ": " + error;
            diagnostics.SetStatus(detail);
            if (diagnostics.Event("FAIL " + detail)) logger.Error(detail);
        }

        private static int ReadSkillPointsRemaining(object state, KingmakerContracts contracts)
        {
            try
            {
                object value = ReflectionAccess.Read(contracts.LevelUpStateSkillPointsRemainingMember, state);
                return value is int ? (int)value : 0;
            }
            catch
            {
                return 0;
            }
        }

        public void OnLevelUpAppliedToAuthoritativeUnit(
            object controller,
            object finalDescriptor,
            System.Collections.IList survivingActions = null)
        {
            KingmakerContracts contracts = contractsProvider();
            if (respec.HasPendingCommit) respec.AfterReplay(controller, finalDescriptor, contracts);
            if (respec.Active != null) ObserveRespecPreview(controller);
            RollSession session = sessions.Active;
            if (session == null ||
                session.CreationKind != SupportedCharacterCreationKind.Mercenary ||
                !ReferenceEquals(session.Controller, controller) ||
                !ReferenceEquals(session.StableOwner, finalDescriptor))
            {
                return;
            }

            if (mercenaryFinalization.HasPendingCommit)
            {
                bool replayVerified = mercenaryFinalization.AfterReplay(
                    controller,
                    finalDescriptor,
                    survivingActions,
                    contracts);
                if (replayVerified)
                {
                    session.MarkAuthoritativeFinalizationApplied(controller, finalDescriptor);
                    return;
                }
                session.MarkFinalizationFailed();
                string stagingFailure = "Mercenary authoritative replay did not retain the staged rolled assignment. " +
                    BuildSessionFacts(session);
                diagnostics.SetStatus(stagingFailure);
                if (diagnostics.Event("REPLAY FAIL " + stagingFailure))
                {
                    logger.Error(stagingFailure);
                }
            }
        }

        public void OnRespecCopyCompleted(object context) { respec.ObserveCopyCompleted(context); }

        public void OnLevelUpCommitCompleted(object controller)
        {
            KingmakerContracts contracts = contractsProvider();
            if (respec.Complete(controller, contracts))
            {
                diagnostics.SetStatus(respec.LastResult);
                if (respec.LastPassed == true) { diagnostics.FinalizationVerified(respec.LastResult); logger.Info(respec.LastResult); }
                else { diagnostics.FinalizationFailed(respec.LastResult); logger.Error(respec.LastResult); }
            }

            RollSession session = sessions.Active;
            if (session == null || !ReferenceEquals(session.Controller, controller))
            {
                mercenaryFinalization.Complete(controller, contracts);
                return;
            }
            if (session.CreationKind == SupportedCharacterCreationKind.Mercenary && session.IsRollMode)
            {
                CompleteMercenaryCommit(session, controller, contracts);
                return;
            }
            if (session.CreationKind == SupportedCharacterCreationKind.NewMainCharacter && session.IsRollMode)
            {
                CompleteNewMainCommit(session, controller, contracts);
                return;
            }
            mercenaryFinalization.Complete(controller, contracts);
        }

        private void CompleteMercenaryCommit(
            RollSession session,
            object controller,
            KingmakerContracts contracts)
        {
            MercenaryFinalizationObservation observation;
            string error;
            bool passed = mercenaryFinalization.TryVerifyAfterSuccessCallback(
                session,
                controller,
                contracts,
                out observation,
                out error);
            if (!passed) session.MarkFinalizationFailed();
            mercenaryFinalization.Complete(controller, contracts);

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

        private void CompleteNewMainCommit(
            RollSession session,
            object controller,
            KingmakerContracts contracts)
        {
            // The commit-time constructor rebind stages the verified assignment on the stable
            // owner before native replay consumes it; this seam only verifies and closes.
            int[] expected = session.Assignment == null ? null : session.Assignment.ToAssignedArray();
            bool passed = false;
            string detail;
            try
            {
                int[] observed = expected == null
                    ? null
                    : statAccess.ReadUnitBaseValues(session.StableOwner, contracts);
                passed = expected != null && observed != null && expected.SequenceEqual(observed);
                detail = "New-main rolled-stat final verification: passed=" + BooleanText(passed) +
                    ", creationKind=" + session.CreationKind +
                    ", expectedBase=[" + DescribeValues(expected) + "]" +
                    ", observedFinalBase=[" + DescribeValues(observed) + "]" +
                    ", " + BuildSessionFacts(session);
            }
            catch (Exception exception)
            {
                detail = "New-main rolled-stat final verification failed with " +
                    exception.GetType().Name + ": " + exception.Message + " " + BuildSessionFacts(session);
            }

            try
            {
                session.Complete();
            }
            finally
            {
                sessions.Clear(session);
            }

            if (passed)
            {
                diagnostics.FinalizationVerified(detail);
                diagnostics.SetStatus("The completed main character retained the verified rolled base values.");
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
            KingmakerContracts contracts = contractsProvider();
            if (respec.ExpireInterruptedCommit())
            { diagnostics.FinalizationFailed(respec.LastResult); logger.Error(respec.LastResult); }
            if (mercenaryFinalization.ExpireInterruptedCommit(contracts))
            {
                diagnostics.FinalizationFailed("Mercenary commit interrupted before completion; the exact pre-commit state was restored and no late writes were made.");
                logger.Error("Mercenary commit interrupted before completion; the exact pre-commit state was restored and no late writes were made.");
            }
            if (respec.Active != null && !respec.Active.IsCurrent)
                OnBuildCanceled(respec.Active.Controller);
            RollSession session = sessions.Active;
            if (session == null) return;
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
                respec.Abort(released.Controller, "Exact source owner disappeared.");
                diagnostics.Released("The active character-build controller/source owner disappeared; session ownership was cleared.");
                diagnostics.SetStatus("Canceled or completed character-creation session released; waiting for a new exact context.");
                logger.Info("Released the Kingmaker Dice Roller session after its stable controller/source owner left character creation.");
                return;
            }

            session = sessions.Active;
            SynchronizeSkillsPresentation(sessions.Active, "update");
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
            // If that replay overwrote the staged values, restage this already-live generation once
            // and redo the native derived allowances so checks and counters agree again.
            if (session.ApplicationAttempts < RollSession.MaximumApplicationAttemptsPerGeneration)
            {
                RecordEvent(
                    "The live replacement overwrote its constructor-stage values; performing one bounded live restage. " +
                    live.BuildFacts(session, application.RefreshInProgress));
                if (application.TryStageCurrentGeneration(session, contracts, out error) &&
                    RefreshDerivedAllowances(session, contracts, "bounded restage", out DerivedAllowanceSnapshot ignoredSnapshot) &&
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
            if (session.CreationKind == SupportedCharacterCreationKind.Respec && !HasCurrentLiveBinding(session, contracts))
            { error = "Respec ownership/generation was lost; restoration cannot retarget a character."; return false; }
            PointBuyRestoreObservation restored;
            if (!pointBuyRestore.TryRestore(session, contracts, out restored, out error))
            {
                string detail = "Point-buy restoration failed closed: " + error + " " + BuildSessionFacts(session);
                diagnostics.SetStatus(detail);
                RecordEvent(detail);
                return false;
            }

            // The restored point-buy scores must also drive the live derived allowances; a
            // failed refresh is reported instead of claiming a coherent Return to Point Buy.
            DerivedAllowanceSnapshot derivedPrevious;
            if (!RefreshDerivedAllowances(session, contracts, "Return to Point Buy", out derivedPrevious))
            {
                if (derivedPrevious != null)
                {
                    RestoreDerivedAllowances(session, contracts, derivedPrevious, "Return to Point Buy");
                }
                error = "Point-buy restoration verified, but the native derived allowances did not refresh: " +
                    "restoration=" + restored.BuildFacts(session, application.RefreshInProgress) + " " +
                    BuildSessionFacts(session);
                diagnostics.SetStatus(error);
                RecordEvent("FAIL " + error);
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
            SynchronizeSkillsPresentation(session, "Return to Point Buy");
            error = null;
            return true;
        }

        public bool TryPrepareDisable(out string error)
        {
            if (sessions.Active != null && sessions.Active.CreationKind == SupportedCharacterCreationKind.Respec &&
                (respec.Active == null || !respec.Active.IsCurrent)) OnBuildCanceled(sessions.Active.Controller);
            if (sessions.Active == null) respec.Abort(null, "Disabled or unloaded.");
            RollSession session = sessions.Active;
            if (session == null)
            {
                mercenaryFinalization.Abort(null, "Disabled or unloaded.", contractsProvider());
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
            respec.Abort(null, "Disabled or unloaded.");
            mercenaryFinalization.Abort(null, "Disabled or unloaded.", contractsProvider());
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
            SynchronizeSkillsPresentation(session, "verified live generation");
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
            DerivedAllowanceSnapshot derivedPrevious = null;
            try
            {
                if (!application.TryStageCurrentGeneration(session, contracts, out error))
                {
                    throw new InvalidOperationException(error);
                }
                if (!RefreshDerivedAllowances(session, contracts, commandName, out derivedPrevious))
                {
                    throw new InvalidOperationException(
                        "The native derived allowances (skill points/spell slots) did not refresh from the staged scores.");
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
                SynchronizeSkillsPresentation(session, commandName);
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
                    // Rollback covers the semantic effect: derived allowances too, not only six numbers.
                    RestoreDerivedAllowances(session, contracts, derivedPrevious, commandName);
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

        private bool RefreshDerivedAllowances(
            RollSession session,
            KingmakerContracts contracts,
            string commandName,
            out DerivedAllowanceSnapshot previous)
        {
            int granted;
            string refreshError;
            if (derivedRefresh.TryRefresh(session.State, session.Unit, contracts, out previous, out granted, out refreshError))
            {
                RecordEvent(
                    "Refreshed native derived allowances from the staged scores via LevelUpState.OnApplyAction; " +
                    "intelligenceSkillPoints=" + granted + " (" + commandName + ").");
                return true;
            }
            RecordEvent("FAIL Native derived-allowance refresh failed during " + commandName + ": " + refreshError);
            logger.Error("Native derived-allowance refresh failed during " + commandName + ": " + refreshError);
            return false;
        }

        private void RestoreDerivedAllowances(
            RollSession session,
            KingmakerContracts contracts,
            DerivedAllowanceSnapshot previous,
            string commandName)
        {
            if (previous == null) return;
            string restoreError;
            if (derivedRefresh.TryRestore(previous, session.State, session.Unit, contracts, out restoreError)) return;
            logger.Exception(commandName + " derived-allowance rollback", new InvalidOperationException(restoreError));
        }

        private bool HasCurrentLiveBinding(RollSession session, KingmakerContracts contracts)
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
            if (session.CreationKind == SupportedCharacterCreationKind.Respec &&
                (respec.Active == null || !respec.Active.Owns(controller, source) ||
                 !Equals(ReflectionAccess.Read(contracts.LevelUpStateIsFirstLevelMember, state), true) ||
                 ReflectionAccess.Read(contracts.LevelUpStateModeMember, state).ToString() != "Respec")) return false;
            object distribution = ReflectionAccess.Read(contracts.LevelUpStateDistributionMember, state);
            if (session.CreationKind == SupportedCharacterCreationKind.Respec && session.IsPointBuyMode &&
                !statAccess.ReadDistributionAvailable(distribution, contracts)) return false;
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

        private static string DescribeValues(int[] values)
        {
            return values == null ? "unavailable" : string.Join(",", values);
        }
    }
}
