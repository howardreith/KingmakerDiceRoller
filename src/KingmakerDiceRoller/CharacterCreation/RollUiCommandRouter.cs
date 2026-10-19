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
        public PointBuyReturnOutcome LastPointBuyReturn => target.LastPointBuyReturn;

        public void NotifyDrawerClosed()
        {
            target.OnRollDrawerClosed();
        }

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
                        return target.TryNavigateHistory(true, out error);
                    case RollUiCommand.NextHistory:
                        return target.TryNavigateHistory(false, out error);
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

        public bool SelectCustomExpression(string expression, out string error)
        {
            var snapshot = target.UiSnapshot;
            var candidate = new RollConfiguration(DiceRollPreset.CustomExpression,
                snapshot.Configuration.LowScorePolicy,
                snapshot.Configuration.MinimumScore,
                expression ?? string.Empty);
            DiceRollRule rule;
            if (!candidate.TryCreateRule(out rule, out error)) return false;
            try
            {
                target.SelectCustomExpression(expression);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public bool SelectPreset(DiceRollPreset preset, out string error)
        {
            if (!Enum.IsDefined(typeof(DiceRollPreset), preset))
            {
                error = "Choose an available roll method.";
                return false;
            }
            try
            {
                target.SetPreset(preset);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
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
