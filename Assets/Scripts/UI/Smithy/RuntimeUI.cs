using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.UI.Smithy
{
    /// <summary>
    /// Tiny helpers for panels that build list rows at runtime, so the scene
    /// generator only has to create containers, not every row. All created text
    /// uses the supplied font so Cyrillic renders correctly.
    /// </summary>
    public static class RuntimeUI
    {
        public static TMP_Text MakeText(Transform parent, TMP_FontAsset font, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = size + 10f;
            le.preferredHeight = size + 10f;
            return t;
        }

        public static Button MakeButtonRow(Transform parent, TMP_FontAsset font, string label, float height,
            Color bg, Color textColor, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height; le.preferredHeight = height;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)labelGo.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 0); rt.offsetMax = new Vector2(-12, 0);
            var t = labelGo.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = label;
            t.fontSize = 22;
            t.color = textColor;
            t.alignment = TextAlignmentOptions.Left;
            t.raycastTarget = false;

            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }
}
