using UnityEngine;

namespace ChemLab.UI
{
    /// <summary>
    /// Единая фабрика шрифтов. Использует системный шрифт ОС (Roboto на Android/PICO,
    /// Arial/Ubuntu в редакторе) — это гарантирует кириллицу без импорта TMP-ассетов.
    /// </summary>
    public static class VrTextFactory
    {
        private static Font _font;
        private static readonly string[] Candidates =
        {
            "Roboto", "Noto Sans", "Arial", "Helvetica Neue", "Droid Sans", "Ubuntu", "DejaVu Sans", "Liberation Sans"
        };

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Font.CreateDynamicFontFromOSFont(Candidates, 48);
                    if (_font == null)
                        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _font;
            }
        }

        /// <summary>Настроить uGUI Text (шрифт, размер, цвет).</summary>
        public static void Configure(Text text, string content, int size, Color color, TextAnchor anchor)
        {
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;
        }
    }

    /// <summary>
    /// Плавающий 3D-текст (TextMesh), всегда развёрнут к игроку.
    /// Используется для меток над сосудами и показаний пипетки.
    /// </summary>
    public class BillboardText : MonoBehaviour
    {
        public float characterSize = 0.0016f;
        public int fontSize = 56;
        public Color color = new Color(1f, 1f, 1f, 0.95f);

        private TextMesh _tm;
        private Transform _cam;

        public void SetText(string value)
        {
            if (_tm == null) Build();
            if (_tm != null) _tm.text = value;
        }

        private void Build()
        {
            _tm = GetComponent<TextMesh>();
            if (_tm == null) _tm = gameObject.AddComponent<TextMesh>();
            var font = VrTextFactory.Font;
            _tm.font = font;
            _tm.fontSize = fontSize;
            _tm.characterSize = characterSize;
            _tm.anchor = TextAnchor.LowerCenter;
            _tm.alignment = TextAlignment.Center;
            _tm.color = color;
            var mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        void LateUpdate()
        {
            if (_cam == null)
            {
                var c = Camera.main;
                if (c != null) _cam = c.transform;
            }
            if (_cam == null) return;
            var dir = transform.position - _cam.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
