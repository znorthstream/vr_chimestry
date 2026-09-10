using System;
using UnityEngine;

namespace ChemLab.Chemistry
{
    /// <summary>
    /// Описание вещества. Загружается из JSON (Assets/Chemistry/Data/Substances/*.json).
    /// Новые вещества добавляются данными, без изменения кода ядра.
    /// </summary>
    [Serializable]
    public class SubstanceData
    {
        [Tooltip("Уникальный идентификатор, например \"water\"")]
        public string id;

        [Tooltip("Отображаемое название (RU)")]
        public string name;

        [Tooltip("Химическая формула")]
        public string formula;

        [Tooltip("Агрегатное состояние: solid | liquid | gas")]
        public string state = "liquid";

        [Tooltip("Цвет в формате #RRGGBBAA")]
        public string colorHex = "#DDDDDDCC";

        [Tooltip("Плотность, г/мл (для твёрдых — г/см³)")]
        public float density = 1f;

        [Tooltip("Кислотно-щелочная сила: эквивалент/л. Отрицательная — кислота, положительная — основание, 0 — нейтральное вещество. Пример: 1M HCl = -1, 1M NaOH = +1.")]
        public float phStrength;

        [Tooltip("Растворимость, г на 100 мл воды (для твёрдых веществ)")]
        public float solubility = 100f;

        public float molarMass;

        [Tooltip("Температура кипения, °C")]
        public float boilingPoint = 100f;

        [Tooltip("Температура плавления, °C")]
        public float meltingPoint;

        [Tooltip("Является индикатором (окрашивает раствор по pH)")]
        public bool isIndicator;

        [Tooltip("Тип индикатора: universal | phenolphthalein")]
        public string indicatorType = "";

        [Tooltip("Опасность (текст)")]
        public string hazards = "";

        [Tooltip("Учебное описание (текст)")]
        public string description = "";

        public bool IsSolid => string.Equals(state, "solid", StringComparison.OrdinalIgnoreCase);
        public bool IsLiquid => string.Equals(state, "liquid", StringComparison.OrdinalIgnoreCase);
        public bool IsGas => string.Equals(state, "gas", StringComparison.OrdinalIgnoreCase);

        public Color Color
        {
            get
            {
                var hex = colorHex;
                if (!hex.StartsWith("#")) hex = "#" + hex;
                if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
                return new Color(0.85f, 0.85f, 0.85f, 0.6f);
            }
        }
    }

    /// <summary>Корневой формат JSON-файла веществ (JsonUtility не читает массивы в корне).</summary>
    [Serializable]
    public class SubstanceFile
    {
        public SubstanceData[] substances;
    }
}
