using System;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class PointBuyRestoreService
    {
        private readonly KingmakerStatAccess statAccess;
        private readonly PreviewRefreshService preview;
        private readonly IModLogger logger;

        public PointBuyRestoreService(KingmakerStatAccess statAccess, PreviewRefreshService preview, IModLogger logger)
        {
            this.statAccess = statAccess;
            this.preview = preview;
            this.logger = logger;
        }

        public bool TryRestore(RollSession session, KingmakerContracts contracts, out string error)
        {
            if (session == null)
            {
                error = null;
                return true;
            }

            AbilityValueSnapshot rolledSnapshot = null;
            try
            {
                rolledSnapshot = AbilityValueSnapshot.Capture(session.Distribution, session.Unit, contracts, statAccess);
                session.Lifecycle.BeginPointBuyRestore();
                contracts.DistributionStartMethod.Invoke(session.Distribution, new object[] { session.Baseline.Budget });
                int[] pointBuyValues = statAccess.ReadDistributionValues(session.Distribution, contracts);
                for (int index = 0; index < pointBuyValues.Length; index++)
                {
                    if (pointBuyValues[index] < 1 || pointBuyValues[index] > 20)
                    {
                        throw new InvalidOperationException("Restored point-buy value at index " + index + " is outside 1-20.");
                    }
                }
                statAccess.WriteUnitBaseValues(session.Unit, pointBuyValues, contracts);
                preview.Refresh(contracts);
                session.Lifecycle.MarkPointBuyRestored();
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                logger.Exception("Restore point-buy allocator", exception);
                try
                {
                    rolledSnapshot?.Restore(session.Distribution, session.Unit, contracts, statAccess);
                    preview.Refresh(contracts);
                    if (session.Lifecycle.State == Domain.RollSessionState.RestoringPointBuy)
                    {
                        session.Lifecycle.AbortPointBuyRestore();
                    }
                }
                catch (Exception rollbackException)
                {
                    logger.Exception("Rollback failed point-buy restoration", rollbackException);
                }
                error = exception.Message;
                return false;
            }
        }
    }
}
