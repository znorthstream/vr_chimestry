using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ChemLab.EditorTools
{
    /// <summary>
    /// Установка/обновление пакетов. Вынесено в отдельный файл без ссылок на
    /// URP/XRI/OpenXR-типы, чтобы меню работало даже если пакеты ещё не установлены.
    /// Клиент Package Manager сам подберёт версию, совместимую с установленным редактором.
    /// </summary>
    public static class ChemLabPackages
    {
        private static readonly string[] RequiredPackages =
        {
            "com.unity.render-pipelines.universal",
            "com.unity.inputsystem",
            "com.unity.xr.interaction.toolkit",
            "com.unity.xr.management",
            "com.unity.xr.openxr"
        };

        private static AddRequest _request;
        private static Queue<string> _queue;
        private static Action _onComplete;

        [MenuItem("ChemLab/Setup/0. Install/Update Packages", priority = 0)]
        public static void InstallPackages()
        {
            if (_queue != null && _queue.Count > 0)
            {
                Debug.LogWarning("[ChemLab] Установка уже идёт, дождитесь завершения.");
                return;
            }
            _queue = new Queue<string>(RequiredPackages);
            _onComplete = () => Debug.Log("[ChemLab] Все пакеты установлены/обновлены.");
            Debug.Log("[ChemLab] Устанавливаю пакеты (версии подберёт Package Manager)...");
            EditorApplicationUpdate();
            EditorApplication.update += EditorApplicationUpdate;
        }

        private static void EditorApplicationUpdate()
        {
            if (_request != null && !_request.IsCompleted) return;

            if (_request != null && _request.IsCompleted)
            {
                if (_request.Status == StatusCode.Failure)
                    Debug.LogError($"[ChemLab] Не удалось установить {_request.Result?.packageId ?? "?"}: {_request.Error?.message}");
                else
                    Debug.Log($"[ChemLab] OK: {_request.Result?.packageId}");
                _request = null;
            }

            if (_queue.Count > 0)
            {
                var next = _queue.Dequeue();
                _request = Client.Add(next);
                return;
            }

            EditorApplication.update -= EditorApplicationUpdate;
            _onComplete?.Invoke();
        }
    }
}
