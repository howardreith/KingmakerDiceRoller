using System;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class RollUiCommandRouter
    {
        private readonly IRollUiCommandTarget target;

        public RollUiCommandRouter(IRollUiCommandTarget target)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public RollUiSnapshot Snapshot => target.UiSnapshot;
        public RollSession ActiveSession => target.ActiveSession;
        public bool CanAttachNativePanel => target.CanAttachNativePanel;

        public bool Execute(
            RollUiCommand command,
            AbilityScore ability,
            out string error)
        {
            try
            {
                switch (command)
                {
                    case RollUiCommand.Roll: return target.TryRoll(out error);
                    case RollUiCommand.Reroll: return target.TryReroll(out error);
                    case RollUiCommand.ReturnToPointBuy: return target.TryRestorePointBuy(out error);
                    case RollUiCommand.MoveUp: return target.TryMoveAssignment(ability, true, out error);
                    case RollUiCommand.MoveDown: return target.TryMoveAssignment(ability, false, out error);
                    case RollUiCommand.PreviousHistory:
                        target.SelectPreviousHistory();
                        break;
                    case RollUiCommand.NextHistory:
                        target.SelectNextHistory();
                        break;
                    case RollUiCommand.UseHistory: return target.TryUseSelectedHistory(out error);
                    case RollUiCommand.StoreCurrent: return target.TryStoreCurrent(out error);
                    case RollUiCommand.PreviousSaved:
                        target.SelectPreviousSaved();
                        break;
                    case RollUiCommand.NextSaved:
                        target.SelectNextSaved();
                        break;
                    case RollUiCommand.RecallSaved: return target.TryRecallSelectedSaved(out error);
                    case RollUiCommand.DeleteSaved:
                        error = target.DeleteSelectedSaved() ? null : "No saved array is selected.";
                        return error == null;
                    case RollUiCommand.PreviousPreset:
                        target.SetPreset(CyclePreset(target.UiSnapshot.Configuration.Preset, -1));
                        break;
                    case RollUiCommand.NextPreset:
                        target.SetPreset(CyclePreset(target.UiSnapshot.Configuration.Preset, 1));
                        break;
                    case RollUiCommand.PreviousPolicy:
                        target.SetLowScorePolicy(CyclePolicy(target.UiSnapshot.Configuration.LowScorePolicy, -1));
                        break;
                    case RollUiCommand.NextPolicy:
                        target.SetLowScorePolicy(CyclePolicy(target.UiSnapshot.Configuration.LowScorePolicy, 1));
                        break;
                    case RollUiCommand.DecreaseMinimum:
                        target.SetMinimumScore(Math.Max(
                            RolledStatArray.MinimumScore,
                            target.UiSnapshot.Configuration.MinimumScore - 1));
                        break;
                    case RollUiCommand.IncreaseMinimum:
                        target.SetMinimumScore(Math.Min(
                            RolledStatArray.MaximumScore,
                            target.UiSnapshot.Configuration.MinimumScore + 1));
                        break;
                    default:
                        error = "Unsupported Dice Roller command.";
                        return false;
                }
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public void SetCustomExpression(string expression)
        {
            target.SetCustomExpression(expression ?? string.Empty);
        }

        private static DiceRollPreset CyclePreset(DiceRollPreset current, int direction)
        {
            int count = Enum.GetValues(typeof(DiceRollPreset)).Length;
            int value = ((int)current + direction + count) % count;
            return (DiceRollPreset)value;
        }

        private static LowScorePolicy CyclePolicy(LowScorePolicy current, int direction)
        {
            int count = Enum.GetValues(typeof(LowScorePolicy)).Length;
            int value = ((int)current + direction + count) % count;
            return (LowScorePolicy)value;
        }
    }
}
