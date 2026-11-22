using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PokerDemo.UI
{
    // Builds the few UI pieces that have to be created at runtime
    // (everything else is placed in the scene).
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

        public static Button CreateButton(string name, Transform parent, string text, UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.15f, 0.45f, 0.75f);

            buttonObject.GetComponent<LayoutElement>().preferredHeight = 48;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            CreateButtonText(buttonObject.transform, text);
            return button;
        }

        private static void CreateButtonText(Transform parent, string text)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = textObject.GetComponent<Text>();
            label.font = DefaultFont;
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;
        }
    }
}
