using System;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class RollPanelDisclosureState
    {
        public RollPanelDisclosureState(
            bool advancedExpanded,
            bool historyExpanded,
            bool savedExpanded)
        {
            AdvancedExpanded = advancedExpanded;
            HistoryExpanded = historyExpanded;
            SavedExpanded = savedExpanded;
        }

        public bool AdvancedExpanded { get; }
        public bool HistoryExpanded { get; }
        public bool SavedExpanded { get; }

        public static RollPanelDisclosureState AllCollapsed { get; } =
            new RollPanelDisclosureState(false, false, false);
    }

    public sealed class NativeRollPanelState
    {
        private object controller;
        private object stableOwner;

        public bool IsAttached { get; private set; }
        public bool IsExpanded { get; private set; }
        public bool AdvancedExpanded { get; private set; }
        public bool HistoryExpanded { get; private set; }
        public bool SavedExpanded { get; private set; }
        public bool HasOwner => controller != null && stableOwner != null;

        public bool ExpandedSurfaceActive => IsAttached && IsExpanded;
        public bool ExpandedBackgroundActive => ExpandedSurfaceActive;
        public bool ExpandedContentActive => ExpandedSurfaceActive;
        public bool AccessTabActive => IsAttached && !IsExpanded;
        public bool ExpandedSurfaceBlocksRaycasts => ExpandedSurfaceActive;
        public bool AccessTabBlocksRaycasts => AccessTabActive;
        public bool OwnedRootBlocksRaycasts => false;

        public bool ObserveOwner(object nextController, object nextStableOwner)
        {
            if (nextController == null) throw new ArgumentNullException(nameof(nextController));
            if (nextStableOwner == null) throw new ArgumentNullException(nameof(nextStableOwner));
            if (ReferenceEquals(controller, nextController) &&
                ReferenceEquals(stableOwner, nextStableOwner))
            {
                return false;
            }

            controller = nextController;
            stableOwner = nextStableOwner;
            ResetViewChoice();
            return true;
        }

        public void AttachView()
        {
            if (!HasOwner)
            {
                throw new InvalidOperationException("A native panel cannot attach without a stable character-build owner.");
            }
            IsAttached = true;
        }

        public void DetachView()
        {
            IsAttached = false;
        }

        public void EndOwner()
        {
            controller = null;
            stableOwner = null;
            ResetViewChoice();
        }

        public void Open()
        {
            if (!IsAttached) throw new InvalidOperationException("A detached native panel cannot be opened.");
            IsExpanded = true;
        }

        public void Close()
        {
            IsExpanded = false;
        }

        public void ToggleAdvanced()
        {
            AdvancedExpanded = !AdvancedExpanded;
        }

        public void ToggleHistory()
        {
            HistoryExpanded = !HistoryExpanded;
        }

        public void ToggleSaved()
        {
            SavedExpanded = !SavedExpanded;
        }

        public RollPanelDisclosureState Disclosure => new RollPanelDisclosureState(
            AdvancedExpanded,
            HistoryExpanded,
            SavedExpanded);

        private void ResetViewChoice()
        {
            IsAttached = false;
            IsExpanded = false;
            AdvancedExpanded = false;
            HistoryExpanded = false;
            SavedExpanded = false;
        }
    }
}
