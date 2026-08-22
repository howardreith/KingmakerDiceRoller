using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.CharacterCreation
{
    public interface IRollUiCommandTarget
    {
        RollUiSnapshot UiSnapshot { get; }
        RollSession ActiveSession { get; }
        bool TryRoll(out string error);
        bool TryReroll(out string error);
        bool TryMoveAssignment(AbilityScore ability, bool moveUp, out string error);
        void SelectPreviousHistory();
        void SelectNextHistory();
        bool TryUseSelectedHistory(out string error);
        bool TryStoreCurrent(out string error);
        void SelectPreviousSaved();
        void SelectNextSaved();
        bool TryRecallSelectedSaved(out string error);
        bool DeleteSelectedSaved();
        bool TryRestorePointBuy(out string error);
        void SetPreset(DiceRollPreset preset);
        void SetLowScorePolicy(LowScorePolicy policy);
        void SetMinimumScore(int minimum);
        void SetCustomExpression(string expression);
    }
}
