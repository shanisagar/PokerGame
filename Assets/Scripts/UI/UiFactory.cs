using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PokerDemo.UI
{
    // Small helpers so the scene UI can be put together in a few lines.
    public static class UiFactory
    {
        private static Font font;

        private static Font DefaultFont
        {
            get
            {
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return font;
            }
        }

        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }

        // A vertical stack of elements, centered on screen.
        public static RectTransform Column(string name, Transform parent, float width)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, 0);

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        public static Text Label(string name, Transform parent, string text, int size,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var label = go.GetComponent<Text>();
            label.font = DefaultFont;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = Color.white;
            label.text = text;
            return label;
        }

        public static Button CreateButton(string name, Transform parent, string text, UnityAction onClick)
        {
            RectTransform rect = Panel(name, parent, new Color(0.15f, 0.45f, 0.75f));
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(onClick);

            Text label = Label("Text", rect, text, 22);
            Stretch(label.rectTransform);
            return button;
        }

        public static InputField CreateInput(string name, Transform parent, string placeholder)
        {
            RectTransform rect = Panel(name, parent, new Color(1f, 1f, 1f, 0.95f));
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;

            Text text = Label("Text", rect, "", 22, TextAnchor.MiddleLeft);
            text.color = Color.black;
            Stretch(text.rectTransform, 12);

            Text hint = Label("Placeholder", rect, placeholder, 22, TextAnchor.MiddleLeft);
            hint.color = new Color(0.4f, 0.4f, 0.4f);
            Stretch(hint.rectTransform, 12);

            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = rect.GetComponent<Image>();
            input.textComponent = text;
            input.placeholder = hint;
            return input;
        }

        public static void Stretch(RectTransform rect, float margin = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, 0);
            rect.offsetMax = new Vector2(-margin, 0);
        }
    }
}
