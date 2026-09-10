using System;

namespace ChemLab.Lessons
{
    /// <summary>Спецификация проверки шага задания. Декларативная — из JSON.</summary>
    [Serializable]
    public class CheckSpec
    {
        [Tooltip("kind: grab_object | place_on_scale | container_has | pipette_has | ph_between | temperature_between | reaction_occurred | instrument_read | dispose_waste")]
        public string kind;

        [Tooltip("Тип объекта (для grab_object, place_on_scale): beaker, testtube, pipette, ...")]
        public string objectType;

        [Tooltip("id вещества (для container_has)")]
        public string substance;

        [Tooltip("Тип сосуда-ограничитель (для container_has/ph_between): beaker, testtube, ...")]
        public string containerType;

        [Tooltip("Минимум (мл / значение pH / °C)")]
        public float min;

        [Tooltip("Максимум (0 = без ограничения)")]
        public float max;

        [Tooltip("Прибор (для instrument_read): ph | temperature | mass")]
        public string instrument;

        [Tooltip("id реакции (для reaction_occurred)")]
        public string reactionId;
    }

    /// <summary>Шаг урока: страница теории или задание с проверкой.</summary>
    [Serializable]
    public class LessonStep
    {
        [Tooltip("type: theory | task")]
        public string type;

        [Tooltip("Текст теории или задания")]
        public string text;

        [Tooltip("Проверка выполнения (только для type=task)")]
        public CheckSpec check;

        [Tooltip("Подсказки по уровням (до 3)")]
        public string[] hints;
    }

    /// <summary>Вопрос теста в конце урока.</summary>
    [Serializable]
    public class QuizQuestion
    {
        public string question;
        public string[] options;
        public int correctIndex;
        public string explanation;
    }

    /// <summary>Спецификация точности (для оценки Accuracy).</summary>
    [Serializable]
    public class AccuracySpec
    {
        [Tooltip("metric: none | ph | mass")]
        public string metric = "none";

        [Tooltip("Целевое значение")]
        public float target = 7f;

        [Tooltip("Допуск ±")]
        public float tolerance = 0.5f;

        [Tooltip("Штраф за каждую единицу сверх допуска")]
        public float penaltyPerUnit = 20f;
    }

    /// <summary>Урок: теория → задания → тест → результат (раздел 23 ТЗ).</summary>
    [Serializable]
    public class LessonData
    {
        public string id;
        public string title;
        [Tooltip("Краткое описание для списка уроков")]
        public string summary;
        [Tooltip("Абзацы теории")]
        public string[] theory;
        public LessonStep[] steps;
        public QuizQuestion[] quiz;
        public AccuracySpec accuracy;
    }

    /// <summary>Корневой формат JSON-файла уроков.</summary>
    [Serializable]
    public class LessonFile
    {
        public LessonData[] lessons;
    }
}
