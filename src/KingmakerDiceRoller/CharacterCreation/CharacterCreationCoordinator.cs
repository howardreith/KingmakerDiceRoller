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
            this.diagnostics = diagnostics;
            this.logger = logger;
            this.contractsProvider = contractsProvider;
            this.verboseProvider = verboseProvider;
        }

        public bool HasActiveSession => sessions.Active != null;

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

            int budget;
            string budgetSource;
            if (!budgetResolver.TryResolve(context.Distribution, contracts, out budget, out budgetSource))
            {
                diagnostics.Rejected("Point-buy budget was not captured; refusing to create a non-restorable session.");
                logger.Warning("Dice Roller refused character creation because the original point-buy budget is unavailable.");
                return;
            }

            PointBuyBaseline baseline;
            try
            {
                baseline = PointBuyBaseline.Capture(context.Distribution, context.Unit, budget, budgetSource, contracts, statAccess);
            }
            catch (Exception exception)
            {
                diagnostics.Rejected("Point-buy baseline capture failed.");
                logger.Exception("Capture point-buy baseline", exception);
                return;
            }

            RollSession session;
            string sessionReason;
            if (!sessions.TryOpenOrRebind(
                context,
                baseline,
                () => new StatAssignment(DiagnosticArrays.FixedPhaseTwoArray()),
                out session,
                out sessionReason))
            {
                diagnostics.Rejected(sessionReason);
                logger.Warning(sessionReason);
                return;
            }

            diagnostics.Accepted(context.Reason + " " + sessionReason + " Budget=" + budget + " via " + budgetSource + ".");
            RecordEvent(sessionReason + " applicationGeneration=" + session.Generation + ".");

            if (session.IsRestoringPointBuy)
            {
                RecordEvent(
                    "Observed a same-owner replacement during the bounded point-buy refresh; fixed-array staging is deferred. " +
                    BuildSessionFacts(session));
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
            if (session.IsRestoringPointBuy || session.IsApplied || session.IsStaged) return;
            KingmakerContracts contracts = contractsProvider();
            if (contracts == null) return;
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
                session.IsRestoringPointBuy ||
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
            if (!pointBuyRestore.TryRestore(session, contracts, out error)) return false;
            int budget = session.Baseline.Budget;
            string budgetSource = session.Baseline.BudgetSource;
            sessions.Clear(session);
            diagnostics.Restored("Newest live preview restored. Budget=" + budget + " via " + budgetSource + ".");
            diagnostics.SetStatus("Vanilla/modded point-buy allocator restored for the active live preview.");
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
            return "Facts: applicationGeneration=" + session.Generation +
                ", pendingReplacementObserved=" + BooleanText(session.PendingReplacementObserved) +
                ", reboundPreview=" + BooleanText(session.ReboundPreview) +
                ", sameStableOwner=true.";
        }

        private static string BooleanText(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
