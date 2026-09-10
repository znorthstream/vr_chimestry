using System;
using System.Collections.Generic;
using System.Text;
using ChemLab.Core;
using UnityEngine;

namespace ChemLab.Chemistry
{
    /// <summary>Одна компонента содержимого: вещество + объём (мл).</summary>
    [Serializable]
    public class ContentUnit
    {
        public string substanceId;
        public float ml;

        public ContentUnit() { }
        public ContentUnit(string id, float amount) { substanceId = id; ml = amount; }
    }

    /// <summary>
    /// Химический контейнер: стакан, пробирка, пипетка, колба и т.д.
    /// Хранит содержимое, температуру, вычисляет pH и цвет, обрабатывает переливание.
    /// Физическая модель упрощённая (без CFD): состояние переносится между контейнерами.
    /// </summary>
    public class ChemicalContainer : MonoBehaviour
    {
        [Header("Ёмкость")]
        public float capacityMl = 250f;

        [Header("Состояние")]
        public float temperatureC = 21f;

        [Tooltip("Содержимое (вещество + объём, мл)")]
        public List<ContentUnit> contents = new List<ContentUnit>();

        [Header("Эффекты реакций")]
        [Tooltip("Осадок на дне, мл")]
        public float precipitateMl;

        [Tooltip("Вещество осадка")]
        public string precipitateSubstanceId = "";

        [HideInInspector] public float gasTimer;         // >0 — выделяется газ (пузырьки)
        [HideInInspector] public float lastReactionTime; // анти-спам для движка реакций

        [HideInInspector] public LabObject owner;        // объект-владелец (стакан, пробирка...)

        /// <summary>Событие вызывается при любом изменении содержимого.</summary>
        public event Action<ChemicalContainer> Changed;

        public static readonly IReadOnlyList<ChemicalContainer> All => _all;
        private static readonly List<ChemicalContainer> _all = new List<ChemicalContainer>();

        public float TotalVolume
        {
            get
            {
                float v = 0f;
                foreach (var c in contents) v += c.ml;
                return v;
            }
        }

        public float FillFraction => capacityMl > 0f ? Mathf.Clamp01(TotalVolume / capacityMl) : 0f;

        /// <summary>Масса содержимого, г (сумма объём × плотность + осадок).</summary>
        public float ContentsMassG
        {
            get
            {
                float m = precipitateMl;
                var db = ChemistryDatabaseHolder.Instance;
                foreach (var c in contents)
                {
                    var s = db != null ? db.GetSubstance(c.substanceId) : null;
                    m += c.ml * (s != null ? s.density : 1f);
                }
                return m;
            }
        }

        void OnEnable() => _all.Add(this);
        void OnDisable() => _all.Remove(this);

        void Update()
        {
            // Пассивное остывание/нагрев до комнатной температуры
            temperatureC += (21f - temperatureC) * Mathf.Clamp01(0.02f * Time.deltaTime);
            if (gasTimer > 0f) gasTimer -= Time.deltaTime;
        }

        /// <summary>Добавить вещество. Возвращает фактически добавленный объём (перелив теряется).</summary>
        public float Add(string substanceId, float ml)
        {
            if (ml <= 0f) return 0f;
            float free = capacityMl - TotalVolume;
            float accepted = Mathf.Min(ml, free);
            if (accepted <= 0f)
            {
                Core.GameManager.Instance?.NotifyOverflow(this);
                return 0f;
            }
            if (ml > accepted + 0.01f)
                Core.GameManager.Instance?.NotifyOverflow(this);

            var unit = contents.Find(c => c.substanceId == substanceId);
            if (unit != null) unit.ml += accepted;
            else contents.Add(new ContentUnit(substanceId, accepted));

            NotifyChanged();
            return accepted;
        }

        /// <summary>Забрать часть содержимого пропорционально (переливание "всего, что есть").</summary>
        public List<ContentUnit> TakeProportional(float ml)
        {
            var result = new List<ContentUnit>();
            float total = TotalVolume;
            if (total <= 0f || ml <= 0f) return result;
            ml = Mathf.Min(ml, total);
            float frac = ml / total;

            for (int i = contents.Count - 1; i >= 0; i--)
            {
                var c = contents[i];
                float take = c.ml * frac;
                c.ml -= take;
                if (take > 0.0001f) result.Add(new ContentUnit(c.substanceId, take));
                if (c.ml <= 0.0001f) contents.RemoveAt(i);
            }

            // осадок переливается только если достаточно жидкости (упрощение)
            NotifyChanged();
            return result;
        }

        /// <summary>Забрать конкретное вещество (для движка реакций).</summary>
        public float Take(string substanceId, float ml)
        {
            var unit = contents.Find(c => c.substanceId == substanceId);
            if (unit == null) return 0f;
            float take = Mathf.Min(unit.ml, ml);
            unit.ml -= take;
            if (unit.ml <= 0.0001f) contents.Remove(unit);
            NotifyChanged();
            return take;
        }

        public bool Has(string substanceId, float minMl = 0.01f)
        {
            foreach (var c in contents)
                if (c.substanceId == substanceId && c.ml >= minMl) return true;
            return false;
        }

        public float Amount(string substanceId)
        {
            foreach (var c in contents)
                if (c.substanceId == substanceId) return c.ml;
            return 0f;
        }

        public void Clear()
        {
            contents.Clear();
            precipitateMl = 0f;
            precipitateSubstanceId = "";
            gasTimer = 0f;
            NotifyChanged();
        }

        /// <summary>
        /// Расчёт pH по упрощённой кислотно-щелочной модели:
        /// баланс эквивалентов кислот/оснований на литр раствора.
        /// c &gt; 0 (избыток основания) → pH = 14 + log10(c);
        /// c &lt; 0 (избыток кислоты)    → pH = -log10(-c);
        /// c ≈ 0                        → pH = 7.
        /// </summary>
        public float ComputePH()
        {
            float totalL = TotalVolume / 1000f;
            if (totalL < 1e-5f) return 7f;

            var db = ChemistryDatabaseHolder.Instance;
            float balance = 0f; // + основание, - кислота (эквиваленты)
            foreach (var c in contents)
            {
                var s = db != null ? db.GetSubstance(c.substanceId) : null;
                if (s == null) continue;
                balance += (c.ml / 1000f) * s.phStrength;
            }

            float cNet = balance / totalL;
            float ph;
            if (cNet > 1e-4f) ph = 14f + Mathf.Log10(cNet);
            else if (cNet < -1e-4f) ph = -Mathf.Log10(-cNet);
            else ph = 7f;
            return Mathf.Clamp(ph, 0f, 14f);
        }

        /// <summary>Есть ли в содержимом pH-индикатор.</summary>
        public string GetIndicatorType()
        {
            var db = ChemistryDatabaseHolder.Instance;
            foreach (var c in contents)
            {
                var s = db != null ? db.GetSubstance(c.substanceId) : null;
                if (s != null && s.isIndicator) return s.indicatorType;
            }
            return null;
        }

        /// <summary>Цвет индикатора при данном pH.</summary>
        public static Color IndicatorColor(string indicatorType, float ph)
        {
            if (indicatorType == "phenolphthalein")
                return ph >= 8.2f ? new Color(0.95f, 0.35f, 0.60f, 0.55f) : new Color(0.96f, 0.97f, 1.00f, 0.25f);

            // универсальный индикатор: красный → оранжевый → жёлтый → зелёный → синий → фиолетовый
            Color c;
            if (ph < 3f) c = new Color(0.85f, 0.10f, 0.10f);
            else if (ph < 5f) c = new Color(0.90f, 0.45f, 0.10f);
            else if (ph < 6.5f) c = new Color(0.92f, 0.80f, 0.15f);
            else if (ph < 7.6f) c = new Color(0.25f, 0.75f, 0.30f);
            else if (ph < 10f) c = new Color(0.15f, 0.45f, 0.90f);
            else c = new Color(0.50f, 0.20f, 0.75f);
            return new Color(c.r, c.g, c.b, 0.5f);
        }

        /// <summary>Итоговый цвет жидкости: цвет индикатора > смесь цветов содержимого.</summary>
        public Color GetLiquidColor()
        {
            var indicator = GetIndicatorType();
            if (indicator != null) return IndicatorColor(indicator, ComputePH());

            if (contents.Count == 0) return new Color(1f, 1f, 1f, 0f);

            var db = ChemistryDatabaseHolder.Instance;
            float total = TotalVolume;
            var c = new Color(0f, 0f, 0f, 0f);
            float alphaWeight = 0f;
            foreach (var unit in contents)
            {
                var s = db != null ? db.GetSubstance(unit.substanceId) : null;
                var col = s != null ? s.Color : new Color(0.9f, 0.9f, 0.9f, 0.4f);
                float w = total > 0f ? unit.ml / total : 0f;
                c += col * w;
                alphaWeight += col.a * w;
            }
            if (total > 0f) c /= Mathf.Max(total, 0.0001f);
            c.a = Mathf.Clamp01(alphaWeight + precipitateMl * 0.02f + 0.15f);
            return c;
        }

        /// <summary>Текстовое описание содержимого (для меток, журнала, уведомлений).</summary>
        public string DescribeContents()
        {
            var sb = new StringBuilder();
            var db = ChemistryDatabaseHolder.Instance;
            bool first = true;
            foreach (var c in contents)
            {
                if (c.ml < 0.05f) continue;
                if (!first) sb.Append(", ");
                var s = db != null ? db.GetSubstance(c.substanceId) : null;
                sb.Append(s != null ? s.name : c.substanceId);
                sb.Append($" {c.ml:0.#} мл");
                first = false;
            }
            if (precipitateMl > 0.05f)
            {
                if (!first) sb.Append(", ");
                var s = db != null ? db.GetSubstance(precipitateSubstanceId) : null;
                sb.Append($"осадок ({(s != null ? s.name : precipitateSubstanceId)}) {precipitateMl:0.#} мл");
            }
            if (first) return "пусто";
            return sb.ToString();
        }

        public void NotifyChanged()
        {
            Changed?.Invoke(this);
            ExperimentEvents.RaiseContentsChanged();
        }
    }

    /// <summary>
    /// Простой доступ к ChemistryDatabase из любых систем (заполняется GameManager-ом).
    /// </summary>
    public static class ChemistryDatabaseHolder
    {
        public static ChemistryDatabase Instance;
    }
}
