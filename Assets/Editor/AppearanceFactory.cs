using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChemLab.EditorTools
{
    /// <summary>
    /// Фабрика материалов для процедурной лаборатории (примитивы + URP Lit).
    /// </summary>
    public static class AppearanceFactory
    {
        public const string UrpLitShader = "Universal Render Pipeline/Lit";

        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        public static Shader LitShader => Shader.Find(UrpLitShader);

        public static bool ShaderAvailable => LitShader != null;

        public static Material Get(string key)
        {
            if (Cache.TryGetValue(key, out var m) && m != null) return m;

            if (!ShaderAvailable)
            {
                Debug.LogError($"[AppearanceFactory] Шейдер \"{UrpLitShader}\" не найден. Установите URP и запустите ChemLab → Setup → 1.");
                return null;
            }

            m = new Material(LitShader);
            switch (key)
            {
                case "glass":
                    MakeTransparent(m, new Color(0.78f, 0.88f, 0.94f, 0.30f), 0.9f, 0f);
                    break;
                case "liquid":
                    MakeTransparent(m, new Color(0.85f, 0.93f, 1f, 0.35f), 0.75f, 0f);
                    break;
                case "precipitate":
                    m.color = new Color(0.72f, 0.72f, 0.74f);
                    SetSmooth(m, 0.25f);
                    break;
                case "floor":
                    m.color = new Color(0.42f, 0.44f, 0.47f);
                    SetSmooth(m, 0.35f);
                    break;
                case "wall":
                    m.color = new Color(0.74f, 0.73f, 0.70f);
                    SetSmooth(m, 0.2f);
                    break;
                case "ceiling":
                    m.color = new Color(0.88f, 0.88f, 0.9f);
                    break;
                case "table":
                    m.color = new Color(0.52f, 0.38f, 0.26f);
                    SetSmooth(m, 0.45f);
                    break;
                case "counter":
                    m.color = new Color(0.60f, 0.60f, 0.62f);
                    SetSmooth(m, 0.5f);
                    break;
                case "metal":
                    m.color = new Color(0.65f, 0.67f, 0.70f);
                    SetMetallic(m, 0.9f);
                    SetSmooth(m, 0.7f);
                    break;
                case "darkplastic":
                    m.color = new Color(0.16f, 0.17f, 0.19f);
                    SetSmooth(m, 0.55f);
                    break;
                case "whiteplastic":
                    m.color = new Color(0.88f, 0.89f, 0.9f);
                    SetSmooth(m, 0.45f);
                    break;
                case "device":
                    m.color = new Color(0.22f, 0.24f, 0.28f);
                    SetSmooth(m, 0.5f);
                    break;
                case "accent":
                    m.color = new Color(0.15f, 0.45f, 0.80f);
                    SetSmooth(m, 0.4f);
                    break;
                case "screen":
                    m.color = new Color(0.03f, 0.04f, 0.06f);
                    SetSmooth(m, 0.3f);
                    break;
                case "emissive":
                    m.color = new Color(1f, 1f, 1f);
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", new Color(1f, 1f, 0.95f) * 1.4f);
                    break;
                case "rubber":
                    m.color = new Color(0.10f, 0.10f, 0.11f);
                    SetSmooth(m, 0.15f);
                    break;
                case "sink":
                    m.color = new Color(0.70f, 0.71f, 0.73f);
                    SetMetallic(m, 0.6f);
                    SetSmooth(m, 0.6f);
                    break;
                default:
                    m.color = Color.magenta;
                    break;
            }

            Cache[key] = m;
            return m;
        }

        private static void SetSmooth(Material m, float s) => m.SetFloat("_Smoothness", s);
        private static void SetMetallic(Material m, float v) => m.SetFloat("_Metallic", v);

        /// <summary>Перевод URP Lit в прозрачный режим (Surface Type = Transparent, Alpha blend).</summary>
        public static void MakeTransparent(Material m, Color color, float smoothness, float metallic)
        {
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);

            m.SetFloat("_Surface", 1f);                       // Transparent
            m.SetFloat("_Blend", 0f);                         // Alpha
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.SetInt("_Cull", (int)CullMode.Back);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
