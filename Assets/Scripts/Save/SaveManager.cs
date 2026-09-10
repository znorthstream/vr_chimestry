using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChemLab.Save
{
    /// <summary>Запись лабораторного журнала.</summary>
    [Serializable]
    public class JournalEntry
    {
        public string time;
        public string title;
        public string result;
        public List<string> lines = new List<string>();
    }

    /// <summary>Результат прохождения урока.</summary>
    [Serializable]
    public class LessonScoreRecord
    {
        public string lessonId;
        public string lessonTitle;
        public float accuracy;
        public float safety;
        public float procedure;
        public float score;
        public string date;
    }

    /// <summary>Прогресс игрока — сериализуется в JSON.</summary>
    [Serializable]
    public class PlayerProgress
    {
        public List<string> completedLessons = new List<string>();
        public List<LessonScoreRecord> bestScores = new List<LessonScoreRecord>();
        public List<JournalEntry> journal = new List<JournalEntry>();
        public int journalCounter;
    }

    /// <summary>
    /// Сохранение прогресса: Application.persistentDataPath/chemlab_save.json.
    /// Формат — JSON (по плану проекта, раздел 34).
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [HideInInspector] public PlayerProgress progress = new PlayerProgress();

        private string Path => System.IO.Path.Combine(Application.persistentDataPath, "chemlab_save.json");

        void Awake()
        {
            Instance = this;
            Load();
        }

        public void Load()
        {
            try
            {
                if (System.IO.File.Exists(Path))
                {
                    var json = System.IO.File.ReadAllText(Path);
                    var p = JsonUtility.FromJson<PlayerProgress>(json);
                    if (p != null) progress = p;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Не удалось загрузить сохранение: {e.Message}");
            }
        }

        public void Save()
        {
            try
            {
                // Ограничиваем журнал, чтобы файл не рос бесконечно
                if (progress.journal.Count > 60)
                    progress.journal.RemoveRange(0, progress.journal.Count - 60);
                System.IO.File.WriteAllText(Path, JsonUtility.ToJson(progress, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Не удалось сохранить прогресс: {e.Message}");
            }
        }

        public void RecordLessonResult(LessonScoreRecord record)
        {
            if (!progress.completedLessons.Contains(record.lessonId))
                progress.completedLessons.Add(record.lessonId);

            var existing = progress.bestScores.Find(s => s.lessonId == record.lessonId);
            if (existing == null || record.score > existing.score)
            {
                progress.bestScores.RemoveAll(s => s.lessonId == record.lessonId);
                progress.bestScores.Add(record);
            }
            Save();
        }

        public bool IsLessonCompleted(string lessonId) => progress.completedLessons.Contains(lessonId);
    }
}
