using System.Collections.Generic;
using ChemLab.Core;
using UnityEngine;

namespace ChemLab.Safety
{
    /// <summary>
    /// Система безопасности. Отслеживает нарушения, объясняет ПОЧЕМУ действие
    /// опасно/неправильно и сообщает в UI и журнал. Штрафы учитываются системой оценки.
    /// </summary>
    public class SafetySystem : MonoBehaviour
    {
        public static SafetySystem Instance { get; private set; }

        [Tooltip("Кулдаун повторных сообщений одного типа, сек")]
        public float cooldownSeconds = 6f;

        private readonly Dictionary<string, float> _lastTime = new Dictionary<string, float>();
        private LabJournal _journal;

        void Awake() => Instance = this;

        public void Init(LabJournal journal) => _journal = journal;

        /// <summary>Разбита стеклянная посуда.</summary>
        public static void ReportGlassBroken(Core.LabObject obj)
        {
            Report("glass_broken",
                "Разбита посуда!",
                $"{obj.displayName} упал(о) и разбился(лось).\n\nПОЧЕМУ ЭТО ОПАСНО:\nОсколки стекла могут порезать руки, а остатки реактивов — попасть на кожу.\n\nКАК ПРАВИЛЬНО:\nСтеклянную посуду нужно ставить на стол аккуратно, не ронять и не бросать. Осколки убирают в специальный контейнер для стекла, не руками.",
                NotificationSeverity.Danger, 20);
        }

        /// <summary>Пролив реактива. intoSink=true — слили в раковину.</summary>
        public static void ReportSpill(bool intoSink, string what)
        {
            if (intoSink)
            {
                Report("spill_sink",
                    "Нарушение: реактивы в раковину",
                    "Вы вылили реактивы в раковину.\n\nПОЧЕМУ ЭТО НЕПРАВИЛЬНО:\nХимические вещества нельзя сливать в обычную раковину — они загрязняют воду и могут вступить в реакцию с остатками в трубах.\n\nКАК ПРАВИЛЬНО:\nОтработанные реактивы утилизируют в контейнер для химических отходов.",
                    NotificationSeverity.Danger, 15);
            }
            else
            {
                Report("spill_table",
                    "Пролил реактив",
                    $"Вы пролили: {what}.\n\nПОЧЕМУ ЭТО ПРОБЛЕМА:\nПролитые реактивы могут испортить оборудование и вызвать ожоги.\n\nСОВЕТ:\nНаклоняйте сосуд медленно и держите его НАД горловиной сосуда-приёмника.",
                    NotificationSeverity.Warning, 8);
            }
        }

        /// <summary>Сосуд переполнен.</summary>
        public static void ReportOverflow(string what)
        {
            Report("overflow",
                "Сосуд переполнен!",
                $"{what}\n\nПОЧЕМУ ЭТО ПРОБЛЕМА:\nЖидкость вылилась через край — часть вещества потеряна, объёмы для расчётов больше не точны.\n\nСОВЕТ:\nСледите за уровнем жидкости и не наливайте выше мерной отметки.",
                NotificationSeverity.Warning, 8);
        }

        /// <summary>Произвольное сообщение (например, предупреждение о реакции).</summary>
        public static void ReportCustom(string text, NotificationSeverity severity)
        {
            if (Instance == null) return;
            Core.GameManager.Instance?.ShowNotification("Внимание", text, severity);
        }

        /// <summary>Основной метод: объясняет нарушение, сообщает в UI, журнал и систему оценки.</summary>
        public static void Report(string id, string title, string explanation, NotificationSeverity severity, int scorePenalty)
        {
            if (Instance == null) return;
            float now = Time.time;
            Instance._lastTime.TryGetValue(id, out var last);
            if (now - last < Instance.cooldownSeconds) return;
            Instance._lastTime[id] = now;

            ExperimentEvents.RaiseSafetyViolation(id);
            Core.GameManager.Instance?.ShowNotification(title, explanation, severity);
            Core.GameManager.Instance?.Journal?.AddEntry("⚠ " + title, new List<string> { explanation }, "SAFETY");
        }
    }
}
