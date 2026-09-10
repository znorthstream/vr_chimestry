using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChemLab.Save
{
    /// <summary>
    /// Лабораторный журнал: каждое значимое событие (реакция, измерение,
    /// завершение урока, нарушение безопасности) автоматически записывается.
    /// Просмотр — на планшете, хранение — в PlayerProgress (JSON).
    /// </summary>
    public class LabJournal : MonoBehaviour
    {
        private SaveManager _save;

        public void Init(SaveManager save) => _save = save;

        public void AddEntry(string title, List<string> lines, string result = "INFO")
        {
            if (_save == null) _save = SaveManager.Instance;
            if (_save == null) return;

            var entry = new JournalEntry
            {
                time = DateTime.Now.ToString("dd.MM HH:mm"),
                title = title,
                result = result,
                lines = lines ?? new List<string>()
            };
            _save.progress.journal.Add(entry);
            _save.Save();
        }

        /// <summary>Сформировать текст журнала для планшета (последние записи).</summary>
        public string FormatForTablet(int maxEntries = 10)
        {
            var sb = new StringBuilder();
            var list = _save != null ? _save.progress.journal : new List<JournalEntry>();
            int start = Mathf.Max(0, list.Count - maxEntries);
            if (list.Count == 0) return "Журнал пуст.\nПроведите эксперимент — записи появятся здесь автоматически.";

            for (int i = list.Count - 1; i >= start; i--)
            {
                var e = list[i];
                sb.AppendLine($"— {e.time} — {e.title}");
                foreach (var line in e.lines)
                    sb.AppendLine("   " + line);
                if (!string.IsNullOrEmpty(e.result) && e.result != "INFO")
                    sb.AppendLine($"   ИТОГ: {e.result}");
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
