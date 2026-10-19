using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingmakerDiceRoller.UI
{
    // Each owned control only reports pointer/focus ownership. The shared overlay
    // is outside the allocator's clipped rows and never receives raycasts.
    public sealed class InlineArrowTooltip : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        private InlineTooltipOverlay overlay;
        private string message;
        private Func<bool> canShow;
        private bool pointer;
        private bool selected;
        private bool keyboardSelection;
        private bool suppressedUntilReentry;

        public void Configure(InlineTooltipOverlay owner, string text, Func<bool> visibility = null)
        {
            overlay = owner;
            message = text;
            canShow = visibility;
        }

        public void OnPointerEnter(PointerEventData eventData) { pointer = true; Refresh(); }
        public void OnPointerExit(PointerEventData eventData)
        {
            pointer = false;
            suppressedUntilReentry = false;
            Refresh();
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            keyboardSelection = false;
            Refresh();
        }
        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            keyboardSelection = IsNavigationInput();
            if (keyboardSelection) suppressedUntilReentry = false;
            Refresh();
        }
        public void OnDeselect(BaseEventData eventData)
        {
            selected = keyboardSelection = false;
            Refresh();
        }
        public void OnSubmit(BaseEventData eventData)
        {
            if (!selected) return;
            keyboardSelection = true;
            suppressedUntilReentry = false;
            Refresh();
        }

        public bool WantsTooltip => isActiveAndEnabled &&
            (pointer || (selected && keyboardSelection)) &&
            !suppressedUntilReentry && (canShow == null || canShow());

        public void SuppressUntilReentry()
        {
            suppressedUntilReentry = true;
            keyboardSelection = false;
            Refresh();
        }

        private void Update()
        {
            if (!keyboardSelection || (!Input.GetMouseButtonDown(0) &&
                                       !Input.GetMouseButtonDown(1))) return;
            keyboardSelection = false;
            Refresh();
        }

        private void OnDisable()
        {
            pointer = selected = keyboardSelection = suppressedUntilReentry = false;
            if (overlay != null) overlay.Hide(transform as RectTransform);
        }

        private void OnDestroy()
        {
            if (overlay != null) overlay.Hide(transform as RectTransform);
        }

        private void Refresh()
        {
            if (overlay == null) return;
            RectTransform trigger = transform as RectTransform;
            if (WantsTooltip) overlay.Show(trigger, message);
            else overlay.Hide(trigger);
        }

        private static bool IsNavigationInput()
        {
            return !Input.GetMouseButton(0) &&
                (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.UpArrow) ||
                 Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftArrow) ||
                 Input.GetKeyDown(KeyCode.RightArrow));
        }
    }
}
