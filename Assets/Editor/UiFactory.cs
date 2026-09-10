using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ChemLab.EditorTools
{
    /// <summary>
    /// Хелперы для построения world-space uGUI (планшет, кнопки полки, дисплеи приборов).
    /// Использует legacy uGUI Text + системный шрифт ОС — гарантированная кириллица
    /// на Android без импорта TMP-ассетов.
    /// </summary>
    public static class UiFactory
    {
        /// <summary>Создать world-space canvas в пиксельных координатах.</summary>
        public static Canvas CreateCanvas(string name, Transform parent, Vector2 sizePx, float pixelsPerMeter = 3000f)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
            var rt = go.GetComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            rt.sizeDelta = sizePx;
            float scale = 1f / pixelsPerMeter;
            go.transform.localScale = new Vector3(scale, scale, scale);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            return canvas;
        }

        public static RectTransform Rect(Transform parent, Vector2 anchorPoint, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Rect", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchorPoint;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image AddPanel(Transform parent, Vector2 pos, Vector2 size, Color color, Vector2 anchorPoint)
        {
            var rt = Rect(parent, anchorPoint, pos, size);
            rt.name = "Panel";
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text AddText(Transform parent, Vector2 pos, Vector2 size, string content, int sizePx,
            Color color, TextAnchor anchor, Vector2 anchorPoint)
        {
            var rt = Rect(parent, anchorPoint, pos, size);
            rt.name = "Text";
            var text = rt.gameObject.AddComponent<Text>();
            UI.VrTextFactory.Configure(text, content, sizePx, color, anchor);
            return text;
        }

        public static Button AddButton(Transform parent, Vector2 pos, Vector2 size, string label, Color bg,
            Vector2 anchorPoint, int fontSize = 26)
        {
            var rt = Rect(parent, anchorPoint, pos, size);
            rt.name = "Button_" + label;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            var button = rt.gameObject.AddComponent<Button>();
            var target = img;
            button.targetGraphic = target;
            var colors = button.colors;
            colors.highlightedColor = new Color(bg.r * 1.25f, bg.g * 1.25f, bg.b * 1.25f);
            colors.pressedColor = new Color(bg.r * 0.8f, bg.g * 0.8f, bg.b * 0.8f);
            button.colors = colors;

            var text = AddText(rt, Vector2.zero, size, label, fontSize, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
            text.name = "Label";
            return button;
        }

        /// <summary>Быстрый билдер маленького дисплея прибора: канвас + текст. Возвращает текст.</summary>
        public static Text MakeDisplay(string name, Transform parent, Vector3 localPos, Quaternion localRot,
            Vector2 sizePx, float pixelsPerMeter = 3000f, int fontSize = 34)
        {
            var canvas = CreateCanvas(name, parent, sizePx, pixelsPerMeter);
            canvas.transform.localPosition = localPos;
            canvas.transform.localRotation = localRot;
            var bg = AddPanel(canvas.transform, Vector2.zero, sizePx, new Color(0.02f, 0.06f, 0.03f, 0.95f), new Vector2(0.5f, 0.5f));
            bg.name = "DisplayBg";
            return AddText(canvas.transform, Vector2.zero, sizePx * 0.94f, "", fontSize,
                new Color(0.35f, 0.95f, 0.5f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        }
    }
}
