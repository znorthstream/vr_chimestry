using System.Collections;
using ChemLab.Chemistry;
using ChemLab.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.UI
{
    /// <summary>
    /// Виртуальный планшет (разделы 31-33 ТЗ): учебник, список уроков, журнал,
    /// подсказки, результаты, уведомления (баннер). UI только на планшете —
    /// постоянный HUD перед глазами отсутствует.
    /// </summary>
    public class TabletUI : MonoBehaviour
    {
        [Header("Страницы")]
        public GameObject homePage;
        public GameObject lessonsPage;
        public GameObject lessonPage;
        public GameObject quizPage;
        public GameObject resultsPage;
        public GameObject journalPage;
        public GameObject helpPage;

        [Header("Главная")]
        public Button btnLessons;
        public Button btnFreeLab;
        public Button btnJournal;
        public Button btnHelp;

        [Header("Список уроков (до 6)")]
        public Button[] lessonButtons;
        public Button btnLessonsBack;

        [Header("Страница урока")]
        public Text lessonTitle;
        public Text lessonBody;
        public Text lessonStepInfo;
        public Button btnTheoryPrev;
        public Button btnTheoryNext;
        public Button btnHint;
        public Button btnAbortLesson;

        [Header("Тест")]
        public Text quizQuestion;
        public Text quizExplanation;
        public Button[] quizOptions;
        public Button btnQuizNext;

        [Header("Результаты")]
        public Text resultsText;
        public Button btnRetry;
        public Button btnToLessons;
        public Button btnFreeFromResults;

        [Header("Журнал")]
        public Text journalText;
        public Button btnJournalBack;

        [Header("Справка")]
        public Button btnHelpBack;

        [Header("Баннер уведомлений")]
        public GameObject bannerRoot;
        public Image bannerBg;
        public Text bannerTitle;
        public Text bannerBody;
        public Button btnBannerOk;

        private GameManager _gm;
        private Coroutine _hideRoutine;
        private static readonly Color InfoColor = new Color(0.13f, 0.30f, 0.55f, 0.95f);
        private static readonly Color SuccessColor = new Color(0.10f, 0.45f, 0.20f, 0.95f);
        private static readonly Color WarningColor = new Color(0.70f, 0.42f, 0.03f, 0.95f);
        private static readonly Color DangerColor = new Color(0.62f, 0.10f, 0.10f, 0.95f);

        public void Init(GameManager gm)
        {
            _gm = gm;
            if (gm.lessons != null) gm.lessons.StateChanged += OnLessonStateChanged;

            SafeClick(btnLessons, ShowLessonsList);
            SafeClick(btnFreeLab, () => _gm.SetMode(AppMode.FreeLab));
            SafeClick(btnJournal, ShowJournal);
            SafeClick(btnHelp, () => ShowPage(helpPage));
            SafeClick(btnLessonsBack, ShowHome);
            SafeClick(btnTheoryPrev, () => _gm.lessons.PrevTheoryPage());
            SafeClick(btnTheoryNext, () => _gm.lessons.NextTheoryPage());
            SafeClick(btnHint, () => _gm.lessons.RequestHint());
            SafeClick(btnAbortLesson, () => { _gm.lessons.AbortLesson(); ShowHome(); });
            SafeClick(btnQuizNext, () => _gm.lessons.QuizNext());
            SafeClick(btnRetry, () => { _gm.lessons.RestartLesson(); });
            SafeClick(btnToLessons, ShowLessonsList);
            SafeClick(btnFreeFromResults, () => _gm.SetMode(AppMode.FreeLab));
            SafeClick(btnJournalBack, ShowHome);
            SafeClick(btnHelpBack, ShowHome);
            SafeClick(btnBannerOk, HideBanner);

            for (int i = 0; i < quizOptions?.Length; i++)
            {
                int idx = i;
                SafeClick(quizOptions[i], () => _gm.lessons.QuizAnswer(idx));
            }
            for (int i = 0; i < lessonButtons?.Length; i++)
            {
                int idx = i;
                SafeClick(lessonButtons[i], () => OnLessonClick(idx));
            }

            if (bannerRoot != null) bannerRoot.SetActive(false);
            ShowHome();
        }

        private static void SafeClick(Button b, UnityEngine.Events.UnityAction action)
        {
            if (b != null) b.onClick.AddListener(action);
        }

        private void OnLessonClick(int index)
        {
            var lessons = _gm.database != null ? _gm.database.Lessons : null;
            if (lessons == null || index >= lessons.Count) return;
            _gm.lessons.StartLesson(lessons[index]);
        }

        // --- Страницы -------------------------------------------------------

        private void ShowPage(GameObject page)
        {
            foreach (var p in new[] { homePage, lessonsPage, lessonPage, quizPage, resultsPage, journalPage, helpPage })
                if (p != null) p.SetActive(p == page);
        }

        public void ShowHome() => ShowPage(homePage);

        public void ShowLessonsList()
        {
            var lessons = _gm.database != null ? _gm.database.Lessons : null;
            for (int i = 0; i < lessonButtons?.Length; i++)
            {
                var b = lessonButtons[i];
                if (b == null) continue;
                bool has = lessons != null && i < lessons.Count;
                b.gameObject.SetActive(has);
                if (!has) continue;
                var txt = b.GetComponentInChildren<Text>(true);
                var l = lessons[i];
                var save = _gm.Save;
                string mark = save != null && save.IsLessonCompleted(l.id) ? "✓ " : "";
                if (txt != null) txt.text = $"{mark}{l.title}\n<size=20>{l.summary}</size>";
            }
            ShowPage(lessonsPage);
        }

        private void ShowJournal()
        {
            if (journalText != null)
                journalText.text = _gm.Journal != null ? _gm.Journal.FormatForTablet() : "Журнал недоступен.";
            ShowPage(journalPage);
        }

        // --- Реакция на состояние урока ---------------------------------------

        private void OnLessonStateChanged()
        {
            if (_gm?.lessons == null) return;
            switch (_gm.lessons.CurrentPhase)
            {
                case Lessons.LessonManager.Phase.Idle:
                    ShowHome();
                    break;
                case Lessons.LessonManager.Phase.Theory:
                case Lessons.LessonManager.Phase.Task:
                    RefreshLessonPage();
                    ShowPage(lessonPage);
                    break;
                case Lessons.LessonManager.Phase.Quiz:
                    RefreshQuizPage();
                    ShowPage(quizPage);
                    break;
                case Lessons.LessonManager.Phase.Results:
                    if (resultsText != null) resultsText.text = _gm.lessons.ResultsText();
                    ShowPage(resultsPage);
                    break;
            }
        }

        private void RefreshLessonPage()
        {
            var lm = _gm.lessons;
            if (lessonTitle != null) lessonTitle.text = lm.CurrentLesson?.title ?? "";
            bool theory = lm.CurrentPhase == Lessons.LessonManager.Phase.Theory;

            if (lessonBody != null)
                lessonBody.text = theory ? lm.TheoryText() : lm.TaskText();

            if (lessonStepInfo != null)
                lessonStepInfo.text = theory
                    ? $"Теория {lm.CurrentTheoryPage + 1} / {Mathf.Max(1, lm.TheoryPageCount)}"
                    : $"Задание {lm.CurrentStepIndex + 1} / {Mathf.Max(1, lm.StepsTotal)}";

            if (btnTheoryPrev != null) btnTheoryPrev.gameObject.SetActive(theory);
            if (btnTheoryNext != null) btnTheoryNext.gameObject.SetActive(theory);
            if (btnHint != null) btnHint.gameObject.SetActive(!theory);
        }

        private void RefreshQuizPage()
        {
            var lm = _gm.lessons;
            if (quizQuestion != null) quizQuestion.text = lm.QuizQuestionText();
            if (quizExplanation != null) quizExplanation.text = lm.QuizExplanation;

            var options = lm.QuizOptions();
            bool answered = !string.IsNullOrEmpty(lm.QuizExplanation);
            for (int i = 0; i < quizOptions?.Length; i++)
            {
                var b = quizOptions[i];
                if (b == null) continue;
                bool has = i < options.Length;
                b.gameObject.SetActive(has && !answered);
                if (has)
                {
                    var txt = b.GetComponentInChildren<Text>(true);
                    if (txt != null) txt.text = options[i];
                }
            }
            if (btnQuizNext != null) btnQuizNext.gameObject.SetActive(answered);
        }

        // --- Баннер уведомлений ---------------------------------------------

        public void PushBanner(string title, string body, NotificationSeverity severity, bool sticky = false)
        {
            if (bannerRoot == null) return;
            bannerRoot.SetActive(true);
            if (bannerTitle != null) bannerTitle.text = title;
            if (bannerBody != null) bannerBody.text = body;
            if (bannerBg != null)
            {
                switch (severity)
                {
                    case NotificationSeverity.Success: bannerBg.color = SuccessColor; break;
                    case NotificationSeverity.Warning: bannerBg.color = WarningColor; break;
                    case NotificationSeverity.Danger: bannerBg.color = DangerColor; break;
                    default: bannerBg.color = InfoColor; break;
                }
            }
            if (btnBannerOk != null) btnBannerOk.gameObject.SetActive(sticky || severity == NotificationSeverity.Danger);

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            if (!sticky && severity != NotificationSeverity.Danger)
                _hideRoutine = StartCoroutine(HideBannerLater(9f));
        }

        private IEnumerator HideBannerLater(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            HideBanner();
        }

        public void HideBanner()
        {
            if (_hideRoutine != null) { StopCoroutine(_hideRoutine); _hideRoutine = null; }
            if (bannerRoot != null) bannerRoot.SetActive(false);
        }
    }
}
