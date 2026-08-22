using System;
using System.Linq;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class StatApplicationService
    {
        private readonly KingmakerStatAccess statAccess;
        private readonly LivePreviewInspector livePreview;
        private readonly PreviewRefreshService previewRefresh;
        private readonly IModLogger logger;

        public StatApplicationService(
            KingmakerStatAccess statAccess,
            LivePreviewInspector livePreview,
            PreviewRefreshService previewRefresh,
            IModLogger logger)
        {
            this.statAccess = statAccess ?? throw new ArgumentNullException(nameof(statAccess));
            this.livePreview = livePreview ?? throw new ArgumentNullException(nameof(livePreview));
            this.previewRefresh = previewRefresh ?? throw new ArgumentNullException(nameof(previewRefresh));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool TryStageCurrentGeneration(RollSession session, KingmakerContracts contracts, out string error)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));

            if (!session.TryBeginApplicationAttempt(out error)) return false;

            int generation = session.Generation;
            object distribution = session.Distribution;
            object unit = session.Unit;
            AbilityValueSnapshot before = null;
            try
            {
                before = AbilityValueSnapshot.Capture(distribution, unit, contracts, statAccess);
                int[] values = session.Assignment.ToAssignedArray();
                statAccess.WriteDistributionValues(distribution, values, contracts);
                statAccess.WriteUnitBaseValues(unit, values, contracts);
                if (generation != session.Generation ||
                    !ReferenceEquals(distribution, session.Distribution) ||
                    !ReferenceEquals(unit, session.Unit))
                {
                    throw new InvalidOperationException("The preview generation changed while the fixed array was being staged.");
                }

                if (!values.SequenceEqual(statAccess.ReadDistributionValues(distribution, contracts)) ||
                    !values.SequenceEqual(statAccess.ReadUnitBaseValues(unit, contracts)))
                {
                    throw new InvalidOperationException("The staged preview objects did not retain the fixed assignment.");
                }

                session.MarkApplicationStaged(generation);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                logger.Exception("Stage rolled ability array for preview generation " + generation, exception);
                try
                {
                    before?.Restore(distribution, unit, contracts, statAccess);
                }
                catch (Exception rollbackException)
                {
                    logger.Exception("Rollback failed fixed-array staging", rollbackException);
                }
                error = exception.Message;
                return false;
            }
        }

        public LivePreviewObservation InspectLive(RollSession session, KingmakerContracts contracts)
        {
            return livePreview.Observe(session, session.Assignment.ToAssignedArray(), contracts);
        }

        public bool TryMarkLiveVerified(
            RollSession session,
            KingmakerContracts contracts,
            out LivePreviewObservation observation,
            out string error)
        {
            observation = InspectLive(session, contracts);
            if (!observation.IsVerified)
            {
                error = observation.Failure ?? "The live controller preview does not yet contain the staged fixed array.";
                return false;
            }

            try
            {
                session.MarkLiveApplicationVerified(session.Generation);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                logger.Exception("Mark live fixed-array application verified", exception);
                error = exception.Message;
                return false;
            }
        }

        public bool IsCurrentLiveDistribution(
            RollSession session,
            object distribution,
            KingmakerContracts contracts)
        {
            if (session == null || !session.IsApplied || !session.OwnsDistribution(distribution)) return false;
            LivePreviewObservation observation = InspectLive(session, contracts);
            return observation.IsVerified;
        }

        public bool RefreshInProgress => previewRefresh.IsRefreshInProgress;
    }
}
