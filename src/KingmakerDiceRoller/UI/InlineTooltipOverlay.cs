using TMPro;
using KingmakerDiceRoller.CharacterCreation;
using UnityEngine;

namespace KingmakerDiceRoller.UI
{
    // One measured, clamped overlay for all inline controls. Its geometry follows
    // the current trigger after native layout moves, scale changes and rebuilds.
    public sealed class InlineTooltipOverlay : MonoBehaviour
    {
        private const float Inset = 16f;
        private const float Gap = 7f;
        private RectTransform bounds;
        private RectTransform panel;
        private RectTransform trigger;
        private TextMeshProUGUI label;
        private readonly Vector3[] corners = new Vector3[4];
        private float measuredWidth = -1f;
        private float measuredHeight = -1f;
        private float measuredScale = -1f;

        public void Configure(RectTransform owner, RectTransform frame, TextMeshProUGUI text)
        {
            bounds = owner;
            panel = frame;
            label = text;
            panel.gameObject.SetActive(false);
        }

        public void Show(RectTransform source, string message)
        {
            if (source == null || bounds == null || panel == null || label == null) return;
            trigger = source;
            label.text = message ?? string.Empty;
            Measure();
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            Position();
        }

        public void Hide(RectTransform source)
        {
            if (trigger != source) return;
            trigger = null;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        public void HideAll()
        {
            trigger = null;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        public void Remeasure()
        {
            if (panel == null || !panel.gameObject.activeSelf) return;
            Measure();
            Position();
        }

        private void LateUpdate()
        {
            if (trigger == null || !trigger.gameObject.activeInHierarchy)
            {
                if (panel != null) panel.gameObject.SetActive(false);
                trigger = null;
                return;
            }
            if (Mathf.Abs(bounds.rect.width - measuredWidth) > 0.5f ||
                Mathf.Abs(bounds.rect.height - measuredHeight) > 0.5f ||
                Mathf.Abs(bounds.lossyScale.y - measuredScale) > 0.001f)
                Measure();
            Position();
        }

        private void Measure()
        {
            float maximumWidth = Mathf.Max(120f, Mathf.Min(390f, bounds.rect.width - 2f * Inset));
            Vector2 oneLine = label.GetPreferredValues(label.text);
            float width = Mathf.Clamp(oneLine.x + 28f, 160f, maximumWidth);
            Vector2 wrapped = label.GetPreferredValues(label.text, width - 28f, 0f);
            float height = Mathf.Clamp(wrapped.y + 26f, 38f,
                Mathf.Max(38f, bounds.rect.height - 2f * Inset));
            panel.sizeDelta = new Vector2(width, height);
            measuredWidth = bounds.rect.width;
            measuredHeight = bounds.rect.height;
            measuredScale = bounds.lossyScale.y;
        }

        private void Position()
        {
            if (trigger == null || bounds == null || panel == null) return;
            trigger.GetWorldCorners(corners);
            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            float bottom = float.PositiveInfinity;
            float top = float.NegativeInfinity;
            for (int index = 0; index < corners.Length; index++)
            {
                Vector3 point = bounds.InverseTransformPoint(corners[index]);
                left = Mathf.Min(left, point.x);
                right = Mathf.Max(right, point.x);
                bottom = Mathf.Min(bottom, point.y);
                top = Mathf.Max(top, point.y);
            }
            Rect area = bounds.rect;
            LocalLayoutRect placement = InlineTooltipPlacement.Place(
                new LocalLayoutRect(area.xMin, area.yMin, area.xMax, area.yMax),
                new LocalLayoutRect(left, bottom, right, top),
                panel.rect.width, panel.rect.height, Inset, Gap);
            // The panel anchors to the root's bottom-left, while Place returns
            // local coordinates relative to the root pivot.
            panel.anchoredPosition = new Vector2(
                placement.XMin - area.xMin, placement.YMin - area.yMin);
        }
    }
}
