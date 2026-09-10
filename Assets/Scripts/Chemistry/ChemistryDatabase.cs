using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChemLab.Chemistry
{
    /// <summary>
    /// База данных химии. Загружает JSON-файлы веществ, реакций и уроков
    /// из TextAsset'ов (ссылки задаёт редакторский билдер сцены).
    /// Контент добавляется данными — код ядра менять не нужно.
    /// </summary>
    public class ChemistryDatabase : MonoBehaviour
    {
        [Header("Данные (TextAsset)")]
        public TextAsset[] substanceFiles;
        public TextAsset[] reactionFiles;
        public TextAsset[] lessonFiles;

        private readonly Dictionary<string, SubstanceData> _substances = new Dictionary<string, SubstanceData>();
        private readonly Dictionary<string, ReactionData> _reactions = new Dictionary<string, ReactionData>();
        private readonly List<ReactionData> _reactionList = new List<ReactionData>();
        private readonly List<LessonData> _lessons = new List<LessonData>();

        public IReadOnlyList<ReactionData> Reactions => _reactionList;
        public IReadOnlyList<LessonData> Lessons => _lessons;

        public void LoadAll()
        {
            _substances.Clear();
            _reactions.Clear();
            _reactionList.Clear();
            _lessons.Clear();

            if (substanceFiles != null)
                foreach (var file in substanceFiles)
                {
                    if (file == null) continue;
                    var parsed = JsonUtility.FromJson<SubstanceFile>(file.text);
                    if (parsed?.substances == null) continue;
                    foreach (var s in parsed.substances)
                        if (!string.IsNullOrEmpty(s.id)) _substances[s.id] = s;
                }

            if (reactionFiles != null)
                foreach (var file in reactionFiles)
                {
                    if (file == null) continue;
                    var parsed = JsonUtility.FromJson<ReactionFile>(file.text);
                    if (parsed?.reactions == null) continue;
                    foreach (var r in parsed.reactions)
                        if (!string.IsNullOrEmpty(r.id) && !_reactions.ContainsKey(r.id))
                        {
                            _reactions[r.id] = r;
                            _reactionList.Add(r);
                        }
                }

            if (lessonFiles != null)
                foreach (var file in lessonFiles)
                {
                    if (file == null) continue;
                    var parsed = JsonUtility.FromJson<LessonFile>(file.text);
                    if (parsed?.lessons == null) continue;
                    foreach (var l in parsed.lessons)
                        if (!string.IsNullOrEmpty(l.id)) _lessons.Add(l);
                }

            _lessons.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            Debug.Log($"[ChemistryDatabase] Загружено: веществ={_substances.Count}, реакций={_reactionList.Count}, уроков={_lessons.Count}");
        }

        public SubstanceData GetSubstance(string id)
        {
            if (id != null && _substances.TryGetValue(id, out var s)) return s;
            Debug.LogWarning($"[ChemistryDatabase] Неизвестное вещество: {id}");
            return null;
        }

        public bool TryGetSubstance(string id, out SubstanceData data) => _substances.TryGetValue(id, out data);

        public ReactionData GetReaction(string id)
        {
            if (id != null && _reactions.TryGetValue(id, out var r)) return r;
            return null;
        }

        public LessonData GetLesson(string id)
        {
            foreach (var l in _lessons) if (l.id == id) return l;
            return null;
        }
    }
}
