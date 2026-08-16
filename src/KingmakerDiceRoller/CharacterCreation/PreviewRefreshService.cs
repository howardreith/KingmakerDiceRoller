using System;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class PreviewRefreshService
    {
        public void Refresh(KingmakerContracts contracts)
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
        }
    }
}
