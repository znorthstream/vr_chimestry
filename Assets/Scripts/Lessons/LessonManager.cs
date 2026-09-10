using System;
using System.Collections.Generic;
using System.Text;
using ChemLab.Chemistry;
using ChemLab.Core;
using UnityEngine;

namespace ChemLab.Lessons
{
    /// <summary>
    /// Менеджер уроков: состояния "Теория → Задания → Тест → Результат",
    /// декларативные проверки шагов (CheckSpec), трёхуровневые подсказки,
    /// подсчёт оценки (Accuracy / Safety / Procedure) и запись в журнал.
    /// </summary>
    public class LessonManager : MonoBehaviour
    {
        public enum Phase { Idle, Theory, Task, Quiz, Results }

        public Phase CurrentPhase { get; private set; } = Phase.Idle;
        public LessonData CurrentLesson { get; private set; }

        public int CurrentTheoryPage { get; private set; }
        public int CurrentStepIndex { get; private set; }
        public int CurrentQuizIndex { get; private set; }

        public int TheoryPageCount => CurrentLesson?.theory?.Length ?? 0;
        public int StepsTotal => CurrentLesson?.steps?.Length ?? 0;

        public bool LastAnswerCorrect { get; private set; }
        public string QuizExplanation { get; private set; } = "";

        /// <summary>Уведомляет UI об изменении состояния урока.</summary>
        public event Action StateChanged;

        private GameManager _gm;
        private readonly HashSet<string> _eventFlags = new HashSet<string>();
        private int _violationCount;
        private int _hintUses;
        private float _accuracyValue = 100f;

        // --- Инициализация -------------------------------------------------

        public void Init(GameManager gm)
        {
            _gm = gm;
            ExperimentEvents.ObjectGrabbed += OnGrabbed;
            ExperimentEvents.ObjectPlacedOnScale += OnPlaced;
            ExperimentEvents.InstrumentRead += OnInstrumentRead;
            ExperimentEvents.ReactionHappened += OnReaction;
            ExperimentEvents.WasteDisposed += OnWaste;
            ExperimentEvents.SafetyViolationHappened += OnViolation;
        }

        void OnDestroy()
        {
            ExperimentEvents.ObjectGrabbed -= OnGrabbed;
            ExperimentEvents.ObjectPlacedOnScale -= OnPlaced;
            ExperimentEvents.InstrumentRead -= OnInstrumentRead;
            ExperimentEvents.ReactionHappened -= OnReaction;
            ExperimentEvents.WasteDisposed -= OnWaste;
            ExperimentEvents.SafetyViolationHappened -= OnViolation;
        }

        private void OnGrabbed(string kind) => _eventFlags.Add("grab:" + kind);
        private void OnPlaced(string kind) => _eventFlags.Add("scale:" + kind);
        private void OnInstrumentRead(string instrument) => _eventFlags.Add("read:" + instrument);
        private void OnReaction(string reactionId, string info) => _eventFlags.Add("reaction:" + reactionId);
        private void OnWaste() => _eventFlags.Add("waste");
        private void OnViolation(string id) => _violationCount++;

        // --- Управление уроком ----------------------------------------------

        public void StartLesson(LessonData lesson)
        {
            if (lesson == null) return;
            CurrentLesson = lesson;
            CurrentTheoryPage = 0;
            CurrentStepIndex = 0;
            CurrentQuizIndex = 0;
            _violationCount = 0;
            _hintUses = 0;
            _accuracyValue = 100f;
            _eventFlags.Clear();
            _gm?.SetMode(AppMode.Lessons);

            if (TheoryPageCount > 0)
            {
                CurrentPhase = Phase.Theory;
                _gm?.ShowNotification(lesson.title, "Сначала разберём теорию — почему мы делаем каждый шаг.", NotificationSeverity.Info);
            }
            else
                CurrentPhase = Phase.Task;

            RaiseStateChanged();
        }

        public void RestartLesson() => StartLesson(CurrentLesson);

        public void AbortLesson()
        {
            if (CurrentPhase == Phase.Idle) return;
            CurrentPhase = Phase.Idle;
            CurrentLesson = null;
            _gm?.ShowNotification("Урок прерван", "Вы можете запустить урок снова в любой момент.", NotificationSeverity.Info);
            RaiseStateChanged();
        }

        public void NextTheoryPage()
        {
            if (CurrentPhase != Phase.Theory || CurrentLesson == null) return;
            CurrentTheoryPage++;
            if (CurrentTheoryPage >= TheoryPageCount)
            {
                CurrentPhase = StepsTotal > 0 ? Phase.Task : Phase.Idle;
                if (CurrentPhase == Phase.Task)
                    _gm?.ShowNotification("Задание", CurrentLesson.steps[0].text, NotificationSeverity.Info);
            }
            RaiseStateChanged();
        }

        public void PrevTheoryPage()
        {
            if (CurrentPhase != Phase.Theory) return;
            CurrentTheoryPage = Mathf.Max(0, CurrentTheoryPage - 1);
            RaiseStateChanged();
        }

        /// <summary>Подсказка для текущего задания (до 3 уровней).</summary>
        public void RequestHint()
        {
            if (CurrentPhase != Phase.Task || CurrentLesson == null) return;
            var step = CurrentLesson.steps[CurrentStepIndex];
            if (step?.hints == null || step.hints.Length == 0)
            {
                _gm?.ShowNotification("Подсказка", "Попробуйте выполнить задание внимательнее — подсказок для этого шага нет.", NotificationSeverity.Info);
                return;
            }
            int level = Mathf.Min(_hintUses % step.hints.Length + 1, step.hints.Length);
            _hintUses++;
            _gm?.ShowNotification($"Подсказка {level} / {step.hints.Length}", step.hints[level - 1], NotificationSeverity.Info);
        }

        public void QuizAnswer(int index)
        {
            if (CurrentPhase != Phase.Quiz || CurrentLesson == null) return;
            var q = CurrentLesson.quiz[CurrentQuizIndex];
            LastAnswerCorrect = index == q.correctIndex;
            QuizExplanation = (LastAnswerCorrect ? "Верно! " : "Не совсем. ") + q.explanation;
            _gm?.ShowNotification(LastAnswerCorrect ? "Правильно!" : "Ошибка",
                QuizExplanation, LastAnswerCorrect ? NotificationSeverity.Success : NotificationSeverity.Warning);
            RaiseStateChanged();
        }

        public void QuizNext()
        {
            if (CurrentPhase != Phase.Quiz || CurrentLesson == null) return;
            CurrentQuizIndex++;
            if (CurrentQuizIndex >= (CurrentLesson.quiz?.Length ?? 0))
                FinishLesson();
            else
            {
                QuizExplanation = "";
                RaiseStateChanged();
            }
        }

        // --- Проверка заданий ------------------------------------------------

        void Update()
        {
            if (CurrentPhase != Phase.Task || CurrentLesson == null) return;
            if (CurrentStepIndex >= StepsTotal) return;

            var step = CurrentLesson.steps[CurrentStepIndex];
            if (step.type == "theory")
            {
                // Теория без проверки — передаётся как страница; пропускаем
                AdvanceStep();
                return;
            }

            if (step.check != null && IsCheckSatisfied(step.check))
                CompleteStep();
        }

        private bool IsCheckSatisfied(CheckSpec c)
        {
            switch (c.kind)
            {
                case "grab_object":
                    return _eventFlags.Contains("grab:" + c.objectType) || _eventFlags.Contains("grab:any");
                case "place_on_scale":
                    return _eventFlags.Contains("scale:" + c.objectType) || _eventFlags.Contains("scale:any");
                case "instrument_read":
                    return _eventFlags.Contains("read:" + c.instrument) || _eventFlags.Contains("read:any");
                case "reaction_occurred":
                    return string.IsNullOrEmpty(c.reactionId)
                        ? _eventFlags.Count > 0 && AnyFlagStartsWith("reaction:")
                        : _eventFlags.Contains("reaction:" + c.reactionId);
                case "dispose_waste":
                    return _eventFlags.Contains("waste");
                case "container_has":
                    return AnyContainer(cc => MatchesContainer(cc, c.containerType) && cc.Amount(c.substance) >= c.min
                                                          && (c.max <= 0f || cc.Amount(c.substance) <= c.max));
                case "pipette_has":
                    return AnyContainer(cc => cc.owner != null && cc.owner.objectKind == "pipette" && cc.TotalVolume >= c.min);
                case "ph_between":
                    return AnyContainer(cc => MatchesContainer(cc, c.containerType)
                                                          && cc.TotalVolume >= 10f
                                                          && cc.ComputePH() >= c.min && cc.ComputePH() <= c.max);
                case "temperature_between":
                    return AnyContainer(cc => MatchesContainer(cc, c.containerType)
                                                          && cc.TotalVolume >= 10f
                                                          && cc.temperatureC >= c.min && cc.temperatureC <= c.max);
                default:
                    Debug.LogWarning($"[LessonManager] Неизвестный тип проверки: {c.kind}");
                    return false;
            }
        }

        private bool AnyFlagStartsWith(string prefix)
        {
            foreach (var f in _eventFlags)
                if (f.StartsWith(prefix)) return true;
            return false;
        }

        private static bool MatchesContainer(ChemicalContainer cc, string containerType) =>
            string.IsNullOrEmpty(containerType) || (cc.owner != null && cc.owner.objectKind == containerType);

        private static bool AnyContainer(Func<ChemicalContainer, bool> predicate)
        {
            foreach (var cc in ChemicalContainer.All)
                if (cc != null && predicate(cc)) return true;
            return false;
        }

        private void CompleteStep()
        {
            var step = CurrentLesson.steps[CurrentStepIndex];
            _eventFlags.Clear(); // флаги одноразовые: следующий шаг требует новых действий

            // Если шаг был про pH — фиксируем точность (для итоговой оценки)
            if (step.check != null && step.check.kind == "ph_between")
                _accuracyValue = ComputeAccuracy(step.check);

            _gm?.ShowNotification("✓ Шаг выполнен", NextStepPreview(), NotificationSeverity.Success);
            AdvanceStep();
        }

        private string NextStepPreview()
        {
            int next = CurrentStepIndex + 1;
            if (next < StepsTotal) return "Далее: " + CurrentLesson.steps[next].text;
            if (CurrentLesson.quiz != null && CurrentLesson.quiz.Length > 0) return "Далее: проверочный тест.";
            return "Далее: подведение итогов.";
        }

        private void AdvanceStep()
        {
            CurrentStepIndex++;
            if (CurrentStepIndex < StepsTotal) { RaiseStateChanged(); return; }

            if (CurrentLesson.quiz != null && CurrentLesson.quiz.Length > 0)
            {
                CurrentPhase = Phase.Quiz;
                CurrentQuizIndex = 0;
                QuizExplanation = "";
            }
            else
                FinishLesson();
            RaiseStateChanged();
        }

        // --- Итоги и оценка ---------------------------------------------------

        private float ComputeAccuracy(CheckSpec phCheck)
        {
            // Ищем "лучший" контейнер: максимальный объём среди подходящих по типу
            ChemicalContainer best = null;
            foreach (var cc in ChemicalContainer.All)
            {
                if (cc == null || !MatchesContainer(cc, phCheck.containerType) || cc.TotalVolume < 10f) continue;
                if (best == null || cc.TotalVolume > best.TotalVolume) best = cc;
            }
            if (best == null || CurrentLesson.accuracy == null) return 100f;
            var spec = CurrentLesson.accuracy;
            float dev = Mathf.Abs(best.ComputePH() - spec.target) - spec.tolerance;
            if (dev <= 0f) return 100f;
            return Mathf.Clamp(100f - dev * spec.penaltyPerUnit, 0f, 100f);
        }

        private void FinishLesson()
        {
            float accuracy = _accuracyValue;
            float safety = Mathf.Max(0f, 100f - _violationCount * 15f);
            float procedure = Mathf.Max(40f, 100f - _hintUses * 4f);
            float score = Mathf.Round(0.4f * accuracy + 0.3f * safety + 0.3f * procedure);

            CurrentPhase = Phase.Results;
            RaiseStateChanged();

            var lines = new List<string>
            {
                $"Точность (Accuracy): {accuracy:0}%",
                $"Безопасность (Safety): {safety:0}%",
                $"Методика (Procedure): {procedure:0}%",
                "—",
                "Состояние сосудов на момент финиша:"
            };
            foreach (var cc in ChemicalContainer.All)
                if (cc != null && cc.TotalVolume > 0.5f && cc.owner != null)
                    lines.Add($"  {cc.owner.displayName}: {cc.DescribeContents()}, {cc.temperatureC:0.0} °C, pH {cc.ComputePH():0.00}");

            _gm?.Journal?.AddEntry($"Урок: {CurrentLesson.title}", lines, score >= 80f ? "SUCCESS" : "PARTIAL");

            var record = new Save.LessonScoreRecord
            {
                lessonId = CurrentLesson.id,
                lessonTitle = CurrentLesson.title,
                accuracy = accuracy,
                safety = safety,
                procedure = procedure,
                score = score,
                date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            };
            _gm?.Save?.RecordLessonResult(record);

            _gm?.ShowNotification("Урок завершён!",
                $"Итоговая оценка: {score:0}%\nПодробности на планшете.", NotificationSeverity.Success);
        }

        // --- Тексты для планшета ------------------------------------------------

        public string TheoryText()
        {
            if (CurrentLesson?.theory == null || TheoryPageCount == 0) return "";
            return CurrentLesson.theory[CurrentTheoryPage];
        }

        public string TaskText()
        {
            if (CurrentLesson == null || CurrentStepIndex >= StepsTotal) return "";
            var sb = new StringBuilder();
            sb.AppendLine(CurrentLesson.steps[CurrentStepIndex].text);
            return sb.ToString();
        }

        public string ResultsText()
        {
            if (_gm?.Save == null || CurrentLesson == null) return "";
            var rec = _gm.Save.progress.bestScores.Find(s => s.lessonId == CurrentLesson.id);
            if (rec == null) return "";
            var sb = new StringBuilder();
            sb.AppendLine("РЕЗУЛЬТАТ УРОКА");
            sb.AppendLine();
            sb.AppendLine($"Точность:      {rec.accuracy:0}%");
            sb.AppendLine($"Безопасность:  {rec.safety:0}%");
            sb.AppendLine($"Методика:      {rec.procedure:0}%");
            sb.AppendLine();
            sb.AppendLine($"ИТОГОВАЯ ОЦЕНКА: {rec.score:0}%");
            var violations = _violationCount;
            if (violations > 0) sb.AppendLine($"\nНарушений ТБ: {violations} — см. журнал.");
            return sb.ToString();
        }

        public string QuizQuestionText()
        {
            if (CurrentPhase != Phase.Quiz || CurrentLesson?.quiz == null) return "";
            var q = CurrentLesson.quiz[CurrentQuizIndex];
            return q.question;
        }

        public string[] QuizOptions()
        {
            if (CurrentPhase != Phase.Quiz || CurrentLesson?.quiz == null) return Array.Empty<string>();
            return CurrentLesson.quiz[CurrentQuizIndex].options;
        }

        private void RaiseStateChanged() => StateChanged?.Invoke();
    }
}
