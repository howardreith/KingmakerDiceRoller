using UnityEngine;

namespace KingmakerDiceRoller.UI
{
    // The overlay is a sibling, so hiding the native button graphic cannot hide
    // it. Follow only the exact donor RectTransform driven by Kingmaker's layout.
    public sealed class InlineArrowAnchor : MonoBehaviour
    {
        private RectTransform source;
        private RectTransform owned;

        public void Bind(RectTransform value)
        {
            source = value;
            owned = transform as RectTransform;
            Copy();
        }

        private void LateUpdate() { Copy(); }

        private void Copy()
        {
            if (source == null || owned == null) return;
            owned.anchorMin = source.anchorMin;
            owned.anchorMax = source.anchorMax;
            owned.pivot = source.pivot;
            owned.anchoredPosition = source.anchoredPosition;
            owned.sizeDelta = source.sizeDelta;
            owned.localRotation = source.localRotation;
        }
    }
}
