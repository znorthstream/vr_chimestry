using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChemLab.EditorTools
{
    /// <summary>
    /// Автоматический бутстрап для headless-сборок (GitHub Actions / командная строка).
    /// В batch-режиме Unity сам: применяет настройки (URP/OpenXR/слои/ARM64),
    /// генерирует сцену LaboratoryScene, если её нет, и добавляет её в Build Settings —
    /// чтобы `game-ci/unity-builder` мог собрать APK без ручных шагов.
    /// В обычном (интерактивном) редакторе ничего не делает — всё через меню ChemLab.
    /// </summary>
    [InitializeOnLoad]
    public static class ChemLabCiBootstrap
    {
        static ChemLabCiBootstrap()
        {
            if (!Application.isBatchMode) return; // работаем только в headless/CI
            EditorApplication.delayCall += Bootstrap;
        }

        private static void Bootstrap()
        {
            // Смена платформы вызывает domain reload — метод перезапустится сам
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[CI bootstrap] Switching build target → Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                return;
            }

            try
            {
                var manual = ChemLabProjectSetup.ApplyCore();
                AssetDatabase.SaveAssets();
                if (manual.Count > 0)
                    Debug.LogWarning("[CI bootstrap] Ручные шаги неприменимы в batch-режиме:\n- " + string.Join("\n- ", manual));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                if (!File.Exists(SceneBuilder.ScenePath))
                {
                    Debug.Log("[CI bootstrap] Сцена не найдена — генерирую LaboratoryScene...");
                    SceneBuilder.BuildCore(interactive: false);
                }
                else
                {
                    // Сцена есть — гарантируем, что она в Build Settings
                    bool inBuild = EditorBuildSettings.scenes != null &&
                                   System.Array.Exists(EditorBuildSettings.scenes, s => s.path == SceneBuilder.ScenePath && s.enabled);
                    if (!inBuild)
                        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SceneBuilder.ScenePath, true) };
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Debug.Log("[CI bootstrap] Готово: настройки применены, сцена в Build Settings.");
        }
    }
}
