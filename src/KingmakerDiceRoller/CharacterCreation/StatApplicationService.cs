using System;
using System.Linq;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class StatApplicationService
    {
        private readonly KingmakerStatAccess statAccess;
        private readonly PreviewRefreshService preview;
        private readonly IModLogger logger;

        public StatApplicationService(KingmakerStatAccess statAccess, PreviewRefreshService preview, IModLogger logger)
        {
            this.statAccess = statAccess;
            this.preview = preview;
            this.logger = logger;
        }

        public bool TryApply(RollSession session, KingmakerContracts contracts, out string error)
        {
            AbilityValueSnapshot before = null;
            try
            {
                before = AbilityValueSnapshot.Capture(session.Distribution, session.Unit, contracts, statAccess);
                int[] values = session.Assignment.ToAssignedArray();
                statAccess.WriteDistributionValues(session.Distribution, values, contracts);
                statAccess.WriteUnitBaseValues(session.Unit, values, contracts);
                preview.Refresh(contracts);
                int[] distributionValues = statAccess.ReadDistributionValues(session.Distribution, contracts);
                int[] unitValues = statAccess.ReadUnitBaseValues(session.Unit, contracts);
                if (!values.SequenceEqual(distributionValues) || !values.SequenceEqual(unitValues))
                {
                    throw new InvalidOperationException("Post-refresh ability values do not match the owned rolled assignment.");
                }

                session.Lifecycle.MarkApplied();
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                logger.Exception("Apply rolled ability array", exception);
                try
                {
                    before?.Restore(session.Distribution, session.Unit, contracts, statAccess);
                    preview.Refresh(contracts);
                }
                catch (Exception rollbackException)
                {
                    logger.Exception("Rollback rolled ability array", rollbackException);
                }
                error = exception.Message;
                return false;
            }
        }
    }
}
