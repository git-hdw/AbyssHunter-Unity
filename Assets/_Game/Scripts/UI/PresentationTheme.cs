using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AbyssHunter.UI
{
    // One reference-space, palette and bundled Chinese font for every screen.
    internal static class PresentationTheme
    {
        internal static readonly Color Ink = new(0.025f, 0.055f, 0.085f, 1);
        internal static readonly Color Panel = new(0.04f, 0.085f, 0.12f, .96f);
        internal static readonly Color White = new(.9f, .96f, .96f);
        internal static readonly Color Muted = new(.49f, .64f, .69f);
        internal static readonly Color Cyan = new(.25f, .89f, .84f);
        internal static readonly Color Gold = new(.96f, .75f, .39f);
        internal static readonly Color Red = new(1, .36f, .32f);
        private static Font font;

        internal static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        internal static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal static Text Label(Transform parent, string name, string value, float x, float y,
            float w, float h, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            if (font == null) font = GameFont.Chinese;
            var label = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = value;
            label.fontSize = Mathf.Max(16, size);
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        internal static Button Button(Transform parent, string name, string value, float x, float y,
            float w, UnityAction action, bool primary = false)
        {
            var bg = Box(parent, name, x, y, w, 56, primary ? Cyan : new Color(.08f,.15f,.19f));
            bg.raycastTarget = true;
            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.highlightedColor = new Color(.75f, 1, .96f);
            colors.pressedColor = new Color(.5f, .75f, .72f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(action);
            Label(bg.transform, "Label", value, 22, 0, w - 44, 56, 19, primary ? Ink : White);
            Label(bg.transform, "Arrow", ">", w - 38, 0, 25, 56, 22, primary ? Ink : Cyan);
            return button;
        }
    }
}
