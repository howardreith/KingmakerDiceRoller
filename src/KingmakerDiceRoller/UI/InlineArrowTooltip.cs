using UnityEngine;
using UnityEngine.EventSystems;

namespace KingmakerDiceRoller.UI
{
    // Each owned control only reports pointer/focus ownership. The shared overlay
    // is outside the allocator's clipped rows and never receives raycasts.
    public sealed class InlineArrowTooltip : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private InlineTooltipOverlay overlay;
        private string message;
        private bool pointer;
        private bool selected;

        public void Configure(InlineTooltipOverlay owner, string text)
        {
            overlay = owner;
            message = text;
        }

        public void OnPointerEnter(PointerEventData eventData) { pointer = true; Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { pointer = false; Refresh(); }
        public void OnSelect(BaseEventData eventData) { selected = true; Refresh(); }
        public void OnDeselect(BaseEventData eventData) { selected = false; Refresh(); }

        private void OnDisable()
        {
            pointer = selected = false;
            if (overlay != null) overlay.Hide(transform as RectTransform);
        }

        private void Refresh()
        {
            if (overlay == null) return;
            RectTransform trigger = transform as RectTransform;
            if (pointer || selected) overlay.Show(trigger, message);
            else overlay.Hide(trigger);
        }
    }
}
