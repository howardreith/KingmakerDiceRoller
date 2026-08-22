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

            var assignment = new StatAssignment(DiagnosticArrays.FixedPhaseTwoArray());
            RollSession session;
            string sessionReason;
            if (!sessions.TryOpenOrRebind(context, baseline, assignment, out session, out sessionReason))
            {
                diagnostics.Rejected(sessionReason);
                logger.Warning(sessionReason);
                return;
            }

            diagnostics.Accepted(context.Reason + " " + sessionReason + " Budget=" + budget + " via " + budgetSource + ".");
            string error;
            if (application.TryApply(session, contracts, out error))
            {
                diagnostics.Applied("Fixed array " + session.Assignment.RolledArray + ".");
                diagnostics.SetStatus("Fixed diagnostic array is active for the owned new-character session.");
            }
            else
            {
                session.Lifecycle.Abandon();
                sessions.Clear(session);
                diagnostics.SetStatus("Fixed-array application failed closed: " + error);
            }
        }

        public void OnDistributionStarted(object distribution, int pointBudget)
        {
            budgetTracker.Record(distribution, pointBudget);
            RollSession session;
            if (!sessions.TryGetByDistribution(distribution, out session)) return;
            if (session.IsRestoringPointBuy) return;
            KingmakerContracts contracts = contractsProvider();
            if (contracts == null) return;
            string error;
            if (!application.TryApply(session, contracts, out error))
            {
                session.Lifecycle.Abandon();
                sessions.Clear(session);
                diagnostics.SetStatus("Owned allocator restart failed closed and released completion ownership: " + error);
            }
        }

        public void OnDistributionIsComplete(object distribution, ref bool result)
        {
            RollSession session;
            if (sessions.TryGetByDistribution(distribution, out session) && session.IsApplied)
            {
                result = true;
            }
        }

        public void Update(float deltaTime)
        {
            if (!HasActiveSession) return;
            KingmakerContracts contracts = contractsProvider();
            if (contracts == null) return;

            object currentState;
            bool observationSucceeded = contracts.TryGetCurrentLevelUpState(out currentState);
            RollSession released;
            if (!sessions.ReleaseIfStale(currentState, observationSucceeded, deltaTime, out released)) return;

            diagnostics.Released("The active LevelUpState left the controller; stale session ownership was cleared.");
            diagnostics.SetStatus("Canceled or completed character-creation session released; waiting for a new exact context.");
            logger.Info("Released stale Kingmaker Dice Roller session after the LevelUpController moved away from its owned state.");
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
            sessions.Clear(session);
            diagnostics.Restored("Budget=" + session.Baseline.Budget + " via " + session.Baseline.BudgetSource + ".");
            diagnostics.SetStatus("Vanilla/modded point-buy allocator restored for the active session.");
            return true;
        }
    }
}
