using UnityEngine;
using UnityEngine.EventSystems;

namespace KingmakerDiceRoller.UI
{
    // Owned hover/focus help for the vertical assignment arrows. It has no
    // character-state authority and disappears with its owned button.
    public sealed class InlineArrowTooltip : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private GameObject tooltip;

        public void Configure(GameObject value) { tooltip = value; }
        public void OnPointerEnter(PointerEventData eventData) { Show(true); }
        public void OnPointerExit(PointerEventData eventData) { Show(false); }
        public void OnSelect(BaseEventData eventData) { Show(true); }
        public void OnDeselect(BaseEventData eventData) { Show(false); }
        private void OnDisable() { Show(false); }

        private void Show(bool visible)
        {
            if (tooltip != null && tooltip.activeSelf != visible)
                tooltip.SetActive(visible);
        }
    }
}
