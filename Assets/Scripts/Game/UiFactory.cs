using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftScape.Game
{
    public static class UiFactory
    {
        public static readonly Color Ink = new Color(0.14f, 0.2f, 0.26f);
        public static readonly Color Cream = new Color(1f, 0.98f, 0.93f);
        public static readonly Color Primary = new Color(0.93f, 0.52f, 0.28f);
        public static readonly Color Secondary = new Color(1f, 1f, 1f, 0.94f);
        public static readonly Color Dim = new Color(0.08f, 0.13f, 0.18f, 0.6f);
        public static readonly Color StarOn = new Color(1f, 0.79f, 0.2f);
        public static readonly Color StarOff = new Color(0.14f, 0.2f, 0.26f, 0.18f);

        // Fired for every button press, so the game can play one click sound for all of them.
        public static event Action AnyButtonClicked;

        static Font font;

        public static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image NewImage(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            var image = NewRect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        public static Text NewLabel(string name, Transform parent, string text, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var label = NewRect(name, parent).gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        public static Button NewButton(string name, Transform parent, string text, Color background, Color textColor, int fontSize, Action onClick)
        {
            Image image = NewImage(name, parent, UiSprites.RoundedRect, background, true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.5f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() => AnyButtonClicked?.Invoke());
            if (onClick != null) button.onClick.AddListener(() => onClick());
            if (!string.IsNullOrEmpty(text))
                NewLabel("Label", image.transform, text, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Stretch();
            return button;
        }

        public static Image[] NewStarRow(Transform parent, float starSize, float spacing)
        {
            var stars = new Image[StarCount];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = NewImage("Star", parent, UiSprites.Star, StarOff);
                stars[i].rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2((i - 1) * (starSize + spacing), 0f), new Vector2(starSize, starSize));
            }
            return stars;
        }

        public static void SetStars(Image[] stars, int count)
        {
            for (int i = 0; i < stars.Length; i++) stars[i].color = i < count ? StarOn : StarOff;
        }

        const int StarCount = 3;
    }
}
