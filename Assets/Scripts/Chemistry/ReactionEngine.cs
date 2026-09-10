using System.Collections.Generic;
using ChemLab.Core;
using UnityEngine;

namespace ChemLab.Chemistry
{
    /// <summary>
    /// Движок химических реакций.
    /// Периодически обходит все контейнеры и ищет применимые реакции:
    /// если в контейнере есть все реагенты — преобразует их в продукты
    /// (в раствор, осадок или газ), применяет тепловой эффект и сообщает UI.
    /// Реакции описаны данными (ReactionData) — ядро не знает конкретной химии.
    /// </summary>
    public class ReactionEngine : MonoBehaviour
    {
        [Header("Настройки")]
        [Tooltip("Минимальный объём реакции за один такт, мл")]
        public float minUnitsPerTick = 0.5f;

        [Tooltip("Максимальный объём реакции за один такт, мл (чтобы реакция была видна во времени)")]
        public float maxUnitsPerTick = 6f;

        [Tooltip("Интервал обработки, сек")]
        public float tickInterval = 0.25f;

        [Tooltip("Максимальный нагрев/охлаждение за такт, °C")]
        public float maxDeltaTempPerTick = 2.5f;

        private float _timer;

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < tickInterval) return;
            _timer = 0f;

            var db = ChemistryDatabaseHolder.Instance;
            if (db == null) return;

            // Копия списка: контейнеры могут появляться/удаляться во время обработки
            var containers = new List<ChemicalContainer>(ChemicalContainer.All);
            foreach (var container in containers)
                if (container != null) ProcessContainer(db, container);
        }

        private void ProcessContainer(ChemistryDatabase db, ChemicalContainer container)
        {
            if (container.TotalVolume < 0.05f && container.precipitateMl < 0.05f) return;
            if (Time.time - container.lastReactionTime < tickInterval * 0.9f) return;

            bool anyReacted = false;

            foreach (var reaction in db.Reactions)
            {
                int guard = 0;
                while (guard++ < 24 && TryReact(db, container, reaction))
                {
                    anyReacted = true;
                    container.lastReactionTime = Time.time;
                }
            }

            if (anyReacted) container.NotifyChanged();
        }

        private bool TryReact(ChemistryDatabase db, ChemicalContainer container, ReactionData reaction)
        {
            if (reaction.reactants == null || reaction.reactants.Length == 0) return false;
            if (reaction.products == null || reaction.products.Length == 0) return false;

            // Сколько "единиц" реакции можно провести
            float units = float.MaxValue;
            foreach (var r in reaction.reactants)
                units = Mathf.Min(units, container.Amount(r.substance) / Mathf.Max(r.ratio, 0.0001f));

            if (units < minUnitsPerTick) return false;
            units = Mathf.Min(units, maxUnitsPerTick);

            // Поглощаем реагенты
            foreach (var r in reaction.reactants)
                container.Take(r.substance, units * r.ratio);

            // Выдаём продукты
            foreach (var p in reaction.products)
            {
                float amount = units * p.ratio;
                if (amount < 0.01f) continue;

                if (p.gas)
                {
                    // Газ улетает — визуально пузырьки
                    container.gasTimer = Mathf.Max(container.gasTimer, 4f);
                }
                else if (p.precipitate)
                {
                    if (string.IsNullOrEmpty(container.precipitateSubstanceId))
                        container.precipitateSubstanceId = p.substance;
                    container.precipitateMl += amount;
                }
                else
                {
                    container.Add(p.substance, amount);
                }
            }

            // Тепловой эффект
            if (Mathf.Abs(reaction.heatPerUnitMl) > 0.0001f)
            {
                float dT = Mathf.Clamp(reaction.heatPerUnitMl * units, -maxDeltaTempPerTick, maxDeltaTempPerTick);
                container.temperatureC = Mathf.Clamp(container.temperatureC + dT, -20f, 120f);
            }

            // Сообщаем миру о реакции (UI-объяснение, журнал, уроки)
            string info = $"{container.owner?.displayName ?? "сосуд"}: {container.DescribeContents()}";
            ExperimentEvents.RaiseReactionHappened(reaction.id, info);

            if (!string.IsNullOrEmpty(reaction.hazardNote))
                Safety.SafetySystem.ReportCustom(reaction.hazardNote, NotificationSeverity.Warning);

            return true;
        }
    }
}
