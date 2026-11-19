using System;
using Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Demo
{
    public static class DemoUi
    {
        public static RectTransform Panel(Transform parent, string name, Vector2 size)
        {
            var instance = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(VerticalLayoutGroup));
            var rect = (RectTransform)instance.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            instance.GetComponent<UnityEngine.UI.Image>().color = new Color(0.06f, 0.09f, 0.14f, 0.97f);
            var layout = instance.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        public static TextMeshProUGUI Label(Transform parent, string value, float height = 36)
        {
            var instance = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            instance.transform.SetParent(parent, false);
            var text = instance.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = 21;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            instance.GetComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        public static UnityEngine.UI.Button Button(Transform parent, string caption, Action click)
        {
            var instance = new GameObject(caption, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(LayoutElement));
            instance.transform.SetParent(parent, false);
            instance.GetComponent<UnityEngine.UI.Image>().color = new Color(0.16f, 0.32f, 0.5f);
            instance.GetComponent<LayoutElement>().preferredHeight = 44;
            var button = instance.GetComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(() => click());
            var text = Label(instance.transform, caption);
            text.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)text.transform);
            return button;
        }

        public static GamepadInputField Field(Transform parent, string value)
        {
            var instance = new GameObject("TextInput", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(GamepadInputField), typeof(LayoutElement));
            instance.transform.SetParent(parent, false);
            instance.GetComponent<LayoutElement>().preferredHeight = 44;
            instance.GetComponent<UnityEngine.UI.Image>().color = new Color(0.12f, 0.17f, 0.23f);
            var text = Label(instance.transform, string.Empty);
            Stretch((RectTransform)text.transform);
            var field = instance.GetComponent<GamepadInputField>();
            field.textViewport = (RectTransform)instance.transform;
            field.textComponent = text;
            field.text = value;
            field.characterLimit = 96;
            return field;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
