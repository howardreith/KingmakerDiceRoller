using System;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class PreviewRefreshService
    {
        public void Refresh(KingmakerContracts contracts)
        {
            object game = ReflectionAccess.Read(contracts.GameInstanceMember, null);
            if (game == null) throw new InvalidOperationException("Game.Instance is unavailable.");
            object controller = ReflectionAccess.Read(contracts.GameLevelUpControllerMember, game);
            if (controller == null) throw new InvalidOperationException("Game.LevelUpController is unavailable.");
            contracts.PreviewRecalculateField.SetValue(controller, true);
            contracts.PreviewUpdateMethod.Invoke(controller, null);
        }
    }
}
