using System;
using System.Linq;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class PointBuyRestoreService
    {
        private readonly KingmakerStatAccess statAccess;
        private readonly LivePreviewInspector livePreview;
        private readonly PreviewRefreshService preview;
        private readonly IModLogger logger;

        public PointBuyRestoreService(
            KingmakerStatAccess statAccess,
            LivePreviewInspector livePreview,
            PreviewRefreshService preview,
            IModLogger logger)
        {
            this.statAccess = statAccess ?? throw new ArgumentNullException(nameof(statAccess));
            this.livePreview = livePreview ?? throw new ArgumentNullException(nameof(livePreview));
            this.preview = preview ?? throw new ArgumentNullException(nameof(preview));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool TryRestore(
            RollSession session,
            KingmakerContracts contracts,
            out PointBuyRestoreObservation observation,
            out string error)
        {
            observation = null;
            if (session == null)
            {
                error = null;
                return true;
            }

            if (session.IsPointBuyMode)
            {
                observation = ObserveRestoration(session, contracts);
                error = observation.IsVerified
                    ? null
                    : "The durable point-buy session is no longer verified on the live preview.";
                return observation.IsVerified;
            }

            if (!session.IsRollMode)
            {
                error = "The session is already restoring point buy.";
                return false;
            }

            try
            {
                session.BeginPointBuyRestore();

                // UpdatePreview may synchronously construct and bind a replacement preview. The
                // constructor postfix rebinds this session while it remains in RestoringPointBuy.
                preview.Refresh(contracts);

                PointBuyOrigin pristine = session.PointBuyOrigin;
                LivePreviewObservation binding = livePreview.Observe(
                    session,
                    pristine.Values.DistributionValues,
                    pristine.Values.UnitValues,
                    contracts);
                if (!binding.HasCurrentLiveBinding)
                {
                    throw new InvalidOperationException(
                        "Point buy cannot be restored because the newest controller preview is not bound to the session. " +
                        binding.BuildFacts(session, preview.IsRefreshInProgress));
                }

                contracts.DistributionStartMethod.Invoke(
                    session.Distribution,
                    new object[] { pristine.AllocatorBudget });

                binding = livePreview.Observe(
                    session,
                    pristine.Values.DistributionValues,
                    pristine.Values.UnitValues,
                    contracts);
                if (!binding.HasCurrentLiveBinding)
                {
                    throw new InvalidOperationException(
                        "Point buy cannot be restored because allocator restart left the session detached. " +
                        binding.BuildFacts(session, preview.IsRefreshInProgress));
                }

                pristine.Restore(session.Distribution, session.Unit, contracts, statAccess);

                observation = ObserveRestoration(session, contracts);
                if (!observation.IsVerified)
                {
                    throw new InvalidOperationException(
                        observation.HybridStateDetected
                            ? "Restoration refused an illegal rolled-values-plus-full-budget hybrid. " +
                                observation.BuildFacts(session, preview.IsRefreshInProgress)
                            : "The newest live preview did not retain its pristine point-buy state. " +
                                observation.BuildFacts(session, preview.IsRefreshInProgress));
                }

                session.MarkPointBuyRestored(session.Generation);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                logger.Exception("Restore point-buy allocator on the newest live preview", exception);
                try
                {
                    int generation = session.Generation;
                    int[] values = session.Assignment.ToAssignedArray();
                    statAccess.WriteDistributionValues(session.Distribution, values, contracts);
                    statAccess.WriteUnitBaseValues(session.Unit, values, contracts);
                    statAccess.DisablePointBuyAllocator(session.Distribution, contracts);
                    LivePreviewObservation rollback = livePreview.Observe(session, values, contracts);
                    if (!rollback.IsVerified)
                    {
                        throw new InvalidOperationException(
                            "Failed restoration could not be rolled back on the newest live preview. " +
                            rollback.BuildFacts(session, preview.IsRefreshInProgress));
                    }
                    session.MarkPointBuyRollbackVerified(generation);
                }
                catch (Exception rollbackException)
                {
                    logger.Exception("Rollback failed point-buy restoration on the live preview", rollbackException);
                }
                error = exception.Message;
                return false;
            }
        }

        private PointBuyRestoreObservation ObserveRestoration(
            RollSession session,
            KingmakerContracts contracts)
        {
            PointBuyOrigin pristine = session.PointBuyOrigin;
            LivePreviewObservation live = livePreview.Observe(
                session,
                pristine.Values.DistributionValues,
                pristine.Values.UnitValues,
                pristine.AllocatorAvailable,
                pristine.RemainingPoints,
                pristine.TotalPoints,
                contracts);

            int[] assignment = session.Assignment.ToAssignedArray();
            bool rolledDistributionMatches = assignment.SequenceEqual(
                statAccess.ReadDistributionValues(session.Distribution, contracts));
            bool rolledUnitMatches = assignment.SequenceEqual(
                statAccess.ReadUnitBaseValues(session.Unit, contracts));
            bool available = statAccess.ReadDistributionAvailable(session.Distribution, contracts);
            int remaining = statAccess.ReadDistributionPoints(session.Distribution, contracts);
            int total = statAccess.ReadDistributionTotalPoints(session.Distribution, contracts);
            // A zero-point allocator has no spendable pool to layer onto a roll.
            // Treat only a positive, completely unspent pool as the illegal hybrid.
            bool fullBudget = available && total > 0 && remaining == total;
            return new PointBuyRestoreObservation(
                live,
                rolledDistributionMatches,
                rolledUnitMatches,
                fullBudget);
        }
    }
}
