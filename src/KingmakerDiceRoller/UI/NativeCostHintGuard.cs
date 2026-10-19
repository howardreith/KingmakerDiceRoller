using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingmakerDiceRoller.UI
{
    // Appended to an exact native score row. Kingmaker's own pointer handler
    // still raises ability highlighting; this hides only its cost animator after
    // that handler has had a chance to reveal it in the same event dispatch.
    public sealed class NativeCostHintGuard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private Action conceal;

        public void Configure(Action action)
        {
            conceal = action;
            enabled = false;
        }

        public void OnPointerEnter(PointerEventData eventData) { conceal?.Invoke(); }
        public void OnPointerExit(PointerEventData eventData) { conceal?.Invoke(); }
        public void OnSelect(BaseEventData eventData) { conceal?.Invoke(); }
        public void OnDeselect(BaseEventData eventData) { conceal?.Invoke(); }
        private void LateUpdate() { conceal?.Invoke(); }
    }
}
