using System;
using UnityEngine;

namespace ChemLab.Core
{
    /// <summary>
    /// Центральная шина событий эксперимента.
    /// Всё, что происходит в лаборатории (захват объектов, реакции, измерения,
    /// нарушения техники безопасности), публикуется здесь и потребляется
    /// системой уроков, журналом, оценкой и UI.
    /// </summary>
    public static class ExperimentEvents
    {
        /// <summary>Пользователь взял объект. Аргумент — objectKind (beaker, testtube, pipette, ...).</summary>
        public static event Action<string> ObjectGrabbed;

        /// <summary>Пользователь отпустил объект.</summary>
        public static event Action<string> ObjectReleased;

        /// <summary>Объект поставлен на весы.</summary>
        public static event Action<string> ObjectPlacedOnScale;

        /// <summary>Прошла химическая реакция. Аргумент — id реакции.</summary>
        public static event Action<string, string> ReactionHappened; // (reactionId, containerInfo)

        /// <summary>Прибор снял показание ("ph", "temperature", "mass").</summary>
        public static event Action<string> InstrumentRead;

        /// <summary>Содержимое сосуда утилизировано (мусорное ведро).</summary>
        public static event Action WasteDisposed;

        /// <summary>Нарушение техники безопасности. Аргумент — id нарушения.</summary>
        public static event Action<string> SafetyViolationHappened;

        /// <summary>Изменилось содержимое любого контейнера (для живых проверок уроков).</summary>
        public static event Action ContentsChanged;

        public static void RaiseObjectGrabbed(string objectKind) => ObjectGrabbed?.Invoke(objectKind);
        public static void RaiseObjectReleased(string objectKind) => ObjectReleased?.Invoke(objectKind);
        public static void RaiseObjectPlacedOnScale(string objectKind) => ObjectPlacedOnScale?.Invoke(objectKind);
        public static void RaiseReactionHappened(string reactionId, string containerInfo) => ReactionHappened?.Invoke(reactionId, containerInfo);
        public static void RaiseInstrumentRead(string instrument) => InstrumentRead?.Invoke(instrument);
        public static void RaiseWasteDisposed() => WasteDisposed?.Invoke();
        public static void RaiseSafetyViolation(string violationId) => SafetyViolationHappened?.Invoke(violationId);
        public static void RaiseContentsChanged() => ContentsChanged?.Invoke();
    }

    /// <summary>Режимы работы приложения.</summary>
    public enum AppMode
    {
        Lessons,
        FreeLab
    }

    /// <summary>Важность уведомления (влияет на цвет баннера на планшете).</summary>
    public enum NotificationSeverity
    {
        Info,
        Success,
        Warning,
        Danger
    }
}
