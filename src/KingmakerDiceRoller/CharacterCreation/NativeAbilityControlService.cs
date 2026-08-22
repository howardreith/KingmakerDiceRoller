using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class NativeAbilityControlService
    {
        private readonly Dictionary<object, bool> originalStates =
            new Dictionary<object, bool>(ReferenceComparer.Instance);
        private readonly IModLogger logger;

        public NativeAbilityControlService(IModLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool TrySuppressForRoll(
            RollSession session,
            KingmakerContracts contracts,
            out string error)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));
            if (!session.IsRollMode && !session.IsEnteringRollMode)
            {
                error = "Native point-buy controls are suppressed only while entering or in Roll mode.";
                return false;
            }
            try
            {
                object allocator;
                if (!TryGetActiveAllocator(contracts, out allocator, out error)) return false;
                IList entries = contracts.AbilityAllocatorStatEntriesField.GetValue(allocator) as IList;
                if (entries == null || entries.Count != 6)
                {
                    error = "The active ability allocator does not expose exactly six score rows.";
                    return false;
                }
                for (int index = 0; index < entries.Count; index++)
                {
                    object entry = entries[index];
                    if (entry == null) throw new InvalidOperationException("A native score row is null.");
                    SuppressButton(contracts.ScoreEntryUpButtonField.GetValue(entry), contracts);
                    SuppressButton(contracts.ScoreEntryDownButtonField.GetValue(entry), contracts);
                }
                if (!AreSuppressed(allocator, contracts))
                {
                    error = "One or more native point-buy buttons remained interactable in Roll mode.";
                    return false;
                }
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                logger.Exception("Suppress native point-buy score controls", exception);
                error = exception.Message;
                return false;
            }
        }

        public bool AreSuppressed(object allocator, KingmakerContracts contracts)
        {
            if (allocator == null || contracts == null) return false;
            IList entries = contracts.AbilityAllocatorStatEntriesField.GetValue(allocator) as IList;
            if (entries == null || entries.Count != 6) return false;
            for (int index = 0; index < entries.Count; index++)
            {
                object entry = entries[index];
                if (entry == null ||
                    IsInteractable(contracts.ScoreEntryUpButtonField.GetValue(entry), contracts) ||
                    IsInteractable(contracts.ScoreEntryDownButtonField.GetValue(entry), contracts))
                {
                    return false;
                }
            }
            return true;
        }

        public void ReleaseAfterNativePointBuyRefresh()
        {
            originalStates.Clear();
        }

        public void RestoreOwnedStates(KingmakerContracts contracts)
        {
            if (contracts == null)
            {
                originalStates.Clear();
                return;
            }
            foreach (KeyValuePair<object, bool> pair in originalStates)
            {
                try
                {
                    contracts.SelectableInteractableProperty.SetValue(pair.Key, pair.Value, null);
                }
                catch (Exception exception)
                {
                    logger.Exception("Restore native point-buy control state", exception);
                }
            }
            originalStates.Clear();
        }

        private void SuppressButton(object button, KingmakerContracts contracts)
        {
            if (button == null) throw new InvalidOperationException("A native point-buy button is null.");
            if (!originalStates.ContainsKey(button))
            {
                originalStates.Add(button, IsInteractable(button, contracts));
            }
            contracts.SelectableInteractableProperty.SetValue(button, false, null);
        }

        private static bool IsInteractable(object button, KingmakerContracts contracts)
        {
            if (button == null) return false;
            return (bool)contracts.SelectableInteractableProperty.GetValue(button, null);
        }

        private static bool TryGetActiveAllocator(
            KingmakerContracts contracts,
            out object allocator,
            out string error)
        {
            object characterBuild;
            bool active;
            object phase;
            if (!contracts.TryGetAbilityPhasePresentationContext(
                out characterBuild,
                out active,
                out phase,
                out allocator) ||
                characterBuild == null || !active || phase == null || allocator == null)
            {
                error = "The native ability allocator is not active.";
                return false;
            }
            error = null;
            return true;
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object first, object second) => ReferenceEquals(first, second);
            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
