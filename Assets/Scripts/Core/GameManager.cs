using System.Collections.Generic;
using ChemLab.Chemistry;
using ChemLab.Lessons;
using ChemLab.Save;
using ChemLab.Safety;
using ChemLab.UI;
using UnityEngine;

namespace ChemLab.Core
{
    /// <summary>
    /// Главный менеджер приложения: связывает подсистемы (химия, уроки, безопасность,
    /// журнал, сохранения, UI), управляет режимом (Уроки / Свободная лаборатория),
    /// маршрутизирует уведомления игроку.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Подсистемы (назначает редакторский билдер сцены)")]
        public ChemistryDatabase database;
        public ReactionEngine reactionEngine;
        public SafetySystem safety;
        public LessonManager lessons;
        public LabJournal journal;
        public SaveManager save;
        public TabletUI tablet;

        [Header("Режим")]
        public AppMode mode = AppMode.FreeLab;

        public LabJournal Journal => journal;
        public SaveManager Save => save;

        private readonly Dictionary<ChemicalContainer, float> _overflowTimes = new Dictionary<ChemicalContainer, float>();

        void Awake()
        {
            Instance = this;

            if (database == null) database = FindObjectOfType<ChemistryDatabase>();
            ChemistryDatabaseHolder.Instance = database;
            if (database != null) database.LoadAll();

            if (safety == null) safety = FindObjectOfType<SafetySystem>();
            if (journal == null) journal = FindObjectOfType<LabJournal>();
            if (save == null) save = FindObjectOfType<SaveManager>();
            if (lessons == null) lessons = FindObjectOfType<LessonManager>();
            if (tablet == null) tablet = FindObjectOfType<TabletUI>();

            safety?.Init(journal);
            lessons?.Init(this);
            tablet?.Init(this);
        }

        void Start()
        {
            // Приложение стартует в свободном режиме; урок запускается с планшета
            SetMode(AppMode.FreeLab);
            ShowNotification("Виртуальная химическая лаборатория",
                "Возьмите планшет на столе и выберите режим.\nНаведите луч и нажмите триггер, чтобы нажимать кнопки.",
                NotificationSeverity.Info);
        }

        void OnEnable()
        {
            ExperimentEvents.ReactionHappened += OnReactionHappened;
        }

        void OnDisable()
        {
            ExperimentEvents.ReactionHappened -= OnReactionHappened;
        }

        public void SetMode(AppMode newMode)
        {
            mode = newMode;
            if (newMode == AppMode.FreeLab) lessons?.AbortLesson();
            tablet?.ShowHome();
        }

        /// <summary>Показать уведомление на планшете.</summary>
        public void ShowNotification(string title, string body, NotificationSeverity severity, bool sticky = false)
        {
            tablet?.PushBanner(title, body, severity, sticky);
        }

        /// <summary>Переполнение сосуда (вызывается из ChemicalContainer).</summary>
        public void NotifyOverflow(ChemicalContainer container)
        {
            _overflowTimes.TryGetValue(container, out var last);
            if (Time.time - last < 6f) return;
            _overflowTimes[container] = Time.time;
            SafetySystem.ReportOverflow($"{container.owner?.displayName ?? "Сосуд"} переполнен ({container.capacityMl:0} мл).");
        }

        private void OnReactionHappened(string reactionId, string containerInfo)
        {
            var reaction = database != null ? database.GetReaction(reactionId) : null;
            if (reaction == null) return;

            var lines = new List<string>
            {
                $"Сосуд: {containerInfo}",
                reaction.explanation
            };
            journal?.AddEntry("Реакция: " + reaction.name, lines, "EXPERIMENT");

            ShowNotification(reaction.name, reaction.explanation, NotificationSeverity.Info);
        }
    }
}
