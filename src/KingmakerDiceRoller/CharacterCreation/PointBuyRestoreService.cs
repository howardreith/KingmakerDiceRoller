using System;
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

        public bool TryRestore(RollSession session, KingmakerContracts contracts, out string error)
        {
            if (session == null)
            {
                error = null;
                return true;
            }

            try
            {
                session.Lifecycle.BeginPointBuyRestore();

                // UpdatePreview may synchronously construct and bind a replacement preview. The
                // constructor postfix rebinds this session while it remains in RestoringPointBuy.
                preview.Refresh(contracts);

                PointBuyBaseline baseline = session.Baseline;
                LivePreviewObservation binding = livePreview.Observe(
                    session,
                    baseline.Values.DistributionValues,
                    baseline.Values.UnitValues,
                    contracts);
                if (!binding.HasCurrentLiveBinding)
                {
                    throw new InvalidOperationException(
                        "Point buy cannot be restored because the newest controller preview is not bound to the session. " +
                        binding.BuildFacts(session, preview.IsRefreshInProgress));
                }

                contracts.DistributionStartMethod.Invoke(
                    session.Distribution,
                    new object[] { baseline.Budget });
                baseline.Values.Restore(session.Distribution, session.Unit, contracts, statAccess);

                LivePreviewObservation restored = livePreview.Observe(
                    session,
                    baseline.Values.DistributionValues,
                    baseline.Values.UnitValues,
                    contracts);
                if (!restored.IsVerified)
                {
                    throw new InvalidOperationException(
                        "The newest live preview did not retain its captured point-buy baseline. " +
                        restored.BuildFacts(session, preview.IsRefreshInProgress));
                }

                session.Lifecycle.MarkPointBuyRestored();
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
    }
}
