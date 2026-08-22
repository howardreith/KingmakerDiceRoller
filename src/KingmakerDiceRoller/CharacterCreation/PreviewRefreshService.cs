using System;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class PreviewRefreshService
    {
        private bool refreshInProgress;

        public bool IsRefreshInProgress => refreshInProgress;
        public int RefreshCount { get; private set; }

        public void Refresh(KingmakerContracts contracts)
        {
            if (refreshInProgress)
            {
                throw new InvalidOperationException("A preview refresh is already in progress; nested refresh was refused.");
            }

            refreshInProgress = true;
            try
            {
                object controller;
                if (!contracts.TryGetLevelUpController(out controller))
                {
                    throw new InvalidOperationException("Game.Instance.UI.CharacterBuildController.LevelUpController lookup failed.");
                }
                if (controller == null)
                {
                    throw new InvalidOperationException("Game.Instance.UI.CharacterBuildController.LevelUpController is unavailable.");
                }
                contracts.PreviewRecalculateField.SetValue(controller, true);
                contracts.PreviewUpdateMethod.Invoke(controller, null);
                RefreshCount++;
            }
            finally
            {
                refreshInProgress = false;
            }
        }
    }
}
