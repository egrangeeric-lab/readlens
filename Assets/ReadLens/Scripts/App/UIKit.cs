using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ReadLens.App
{
    // Small runtime UI toolkit: rounded panels, big high-contrast controls. 1 UI unit = 0.4 mm in the headset.
    public static class UIKit
    {
        public static readonly Color Ink = Hex("#14161A");
        public static readonly Color Muted = Hex("#4A5160");
        public static readonly Color Surface = Hex("#F7F5F0"); // warm off-white: less glare than pure white in the headset
        public static readonly Color Card = Hex("#E6E9EF");
        public static readonly Color Warning = Hex("#9A2A1F");
        public static readonly Color Accent = Hex("#1B3A8A");
        public static readonly Color OnAccent = Hex("#FFFFFF");
        public const float Touch = 96f; // 3.8 cm targets (1 unit = 0.4 mm): easy to poke, readable from afar

        public static TMP_FontAsset Font; // UI font (Atkinson Hyperlegible: designed for low vision)
        public static TMP_FontAsset FontBold;

        static readonly Dictionary<int, Sprite> rounded = new Dictionary<int, Sprite>();

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // 9-sliced rounded rectangle generated once per radius, anti-aliased edges.
        public static Sprite Rounded(int radius)
        {
            if (rounded.TryGetValue(radius, out var s)) return s;
            int size = radius * 2 + 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius), 0f);
                float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius), 0f);
                float a = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            rounded[radius] = s;
            return s;
        }

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer; // the window canvas renders only its own layer
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform r, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(left, bottom);
            r.offsetMax = new Vector2(-right, -top);
            return r;
        }

        public static Image Panel(Transform parent, Color color, int radius, string name = "Panel")
        {
            var img = Node(name, parent).gameObject.AddComponent<Image>();
            img.sprite = Rounded(radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color, bool bold = false,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var t = Node("Label", parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = bold ? FontBold : Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        public static LayoutElement Size(Component c, float width = -1, float height = -1, float flexWidth = -1)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (width >= 0) le.preferredWidth = le.minWidth = width;
            if (height >= 0) le.preferredHeight = le.minHeight = height;
            if (flexWidth >= 0) le.flexibleWidth = flexWidth;
            return le;
        }

        public static T Layout<T>(Component c, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.UpperLeft) where T : HorizontalOrVerticalLayoutGroup
        {
            var g = c.gameObject.AddComponent<T>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = typeof(T) == typeof(VerticalLayoutGroup);
            g.childForceExpandHeight = false;
            return g;
        }

        public static Button Button(Transform parent, string text, UnityAction onClick, float width = -1, float height = Touch, float textSize = 34)
        {
            var bg = Panel(parent, Card, 24, "Button: " + text);
            var b = bg.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var colors = b.colors;
            colors.highlightedColor = new Color(0.85f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.78f, 0.95f);
            b.colors = colors;
            b.onClick.AddListener(onClick);
            var label = Label(bg.transform, text, textSize, Ink, true, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 12, 0, 12, 0);
            Size(bg, width, height, width < 0 ? 1 : -1);
            return b;
        }

        public static void SetSelected(Button b, bool selected)
        {
            ((Image)b.targetGraphic).color = selected ? Accent : Card;
            b.GetComponentInChildren<TextMeshProUGUI>().color = selected ? OnAccent : Ink;
        }

        // Segmented choice: one option selected at a time. Returns a setter to refresh the visuals.
        public static Action<int> Choice(Transform parent, string[] options, Action<int> onPick, float textSize = 30)
        {
            var row = Layout<HorizontalLayoutGroup>(Node("Choice", parent), 12);
            row.childForceExpandWidth = true;
            var buttons = new Button[options.Length];
            void Select(int i) { for (int k = 0; k < buttons.Length; k++) SetSelected(buttons[k], k == i); }
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                buttons[i] = Button(row.transform, options[i], () => { Select(index); onPick(index); }, -1, Touch, textSize);
            }
            return Select;
        }

        // Colour swatches; the selected one gets a thick dark ring (a shape cue, not colour alone).
        public static Action<string> Swatches(Transform parent, string[] hexes, Action<string> onPick)
        {
            var row = Layout<HorizontalLayoutGroup>(Node("Swatches", parent), 16);
            var rings = new List<(string hex, Image ring, RectTransform dot)>();
            void Select(string hex)
            {
                foreach (var r in rings)
                {
                    bool on = string.Equals(r.hex, hex, StringComparison.OrdinalIgnoreCase);
                    r.ring.color = on ? Ink : new Color(0, 0, 0, 0.18f);
                    float inset = on ? 12 : 4;
                    Stretch(r.dot, inset, inset, inset, inset);
                }
            }
            foreach (var hex in hexes)
            {
                var ring = Panel(row.transform, Ink, 44, "Swatch " + hex);
                Size(ring, Touch, Touch);
                var dot = Panel(ring.transform, Hex(hex), 36, "Color");
                var b = ring.gameObject.AddComponent<Button>();
                b.targetGraphic = dot;
                string h = hex;
                b.onClick.AddListener(() => { Select(h); onPick(h); });
                rings.Add((hex, ring, dot.rectTransform));
            }
            return Select;
        }

        // [ − ]  value  [ + ]
        public static Action<string> Stepper(Transform parent, UnityAction minus, UnityAction plus)
        {
            var row = Layout<HorizontalLayoutGroup>(Node("Stepper", parent), 16, null, TextAnchor.MiddleLeft);
            Button(row.transform, "–", minus, Touch * 1.4f, Touch, 48);
            var value = Label(row.transform, "", 34, Ink, true, TextAlignmentOptions.Center);
            Size(value, 160, Touch);
            Button(row.transform, "+", plus, Touch * 1.4f, Touch, 48);
            return v => value.text = v;
        }

        // Vertical scroll area that hands can drag (poke or pinch-ray).
        public static RectTransform ScrollArea(Transform parent, out ScrollRect scroll)
        {
            var view = Node("Viewport", parent);
            view.gameObject.AddComponent<RectMask2D>();
            view.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // catches drags
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            var content = Node("Content", view);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = view;
            scroll.content = content;
            return content;
        }
    }
}
