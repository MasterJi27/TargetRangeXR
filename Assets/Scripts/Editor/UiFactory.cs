using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TargetRange.Feedback;
using TargetRange.UI;

namespace TargetRange.EditorTools
{
    /// <summary>Small helpers that build uGUI hierarchies (the same objects you would make with GameObject > UI).</summary>
    internal static class UiFactory
    {
        public static readonly Color Panel = new Color(0.075f, 0.082f, 0.098f, 0.90f);
        public static readonly Color Tile = new Color(0.125f, 0.135f, 0.158f, 1f);
        public static readonly Color Text = new Color(0.96f, 0.95f, 0.93f);
        public static readonly Color Muted = new Color(0.64f, 0.66f, 0.70f);
        public static readonly Color Amber = new Color(0.98f, 0.74f, 0.22f);

        public static Sprite Spr(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{AssetPipeline.TexDir}/{file}");

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot ?? anchor;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        public static RectTransform Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        public static Image Img(string name, Transform parent, string sprite, Color color, bool raycast = false)
        {
            RectTransform rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            if (!string.IsNullOrEmpty(sprite))
            {
                img.sprite = Spr(sprite);
                img.type = Image.Type.Sliced;
            }
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, float spacing = 0f)
        {
            RectTransform rt = Rect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.characterSpacing = spacing;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        public static Button MakeButton(string name, Transform parent, string label, TMP_FontAsset font, Color fill, Color textColor,
            Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, AudioService audio)
        {
            Image img = Img(name, parent, "ui_pill.png", fill, true);
            Place(img.rectTransform, anchor, pos, size);

            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            ColorBlock c = button.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(1f, 0.97f, 0.9f);
            c.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            c.selectedColor = Color.white;
            c.fadeDuration = 0.08f;
            button.colors = c;

            TextMeshProUGUI text = Label("Label", img.transform, label, font, fontSize, textColor, TextAlignmentOptions.Center, 4f);
            Stretch(text.rectTransform);

            var feedback = img.gameObject.AddComponent<UiButtonFeedback>();
            Wire.Ref(feedback, "audioService", audio);
            return button;
        }

        public static CanvasGroup Group(GameObject go)
        {
            return go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
        }
    }
}
