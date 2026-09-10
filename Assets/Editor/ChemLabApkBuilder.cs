using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ChemLab.EditorTools
{
    /// <summary>
    /// Сборка APK одним пунктом меню. Результат: Builds/VirtualChemistryLab.apk
    /// (подписывается отладочным ключом — достаточно для установки на PICO 4 Ultra).
    /// </summary>
    public static class ChemLabApkBuilder
    {
        public const string OutputPath = "Builds/VirtualChemistryLab.apk";

        [MenuItem("ChemLab/Build/Build Android APK", priority = 10)]
        public static void BuildApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUtility.DisplayDialog("ChemLab",
                    "Активная платформа — не Android.\nЗапустите ChemLab → Setup → 1. Apply Project Settings.", "OK");
                return;
            }

            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0 || !File.Exists(scenes[0].path))
            {
                EditorUtility.DisplayDialog("ChemLab",
                    "В Build Settings нет сцены.\nЗапустите ChemLab → Setup → 2. Build Laboratory Scene.", "OK");
                return;
            }

            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(scenes, s => s.path),
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                var fullPath = Path.GetFullPath(OutputPath);
                Debug.Log($"[ChemLab] APK собран: {fullPath} ({report.summary.totalSize / 1048576} MB)");
                EditorUtility.DisplayDialog("ChemLab",
                    $"APK собран:\n{fullPath}\n\nУстановка на шлем — см. docs/BUILD_APK.md", "OK");
            }
            else
            {
                Debug.LogError("[ChemLab] Сборка не удалась: " + report.summary.result);
                EditorUtility.DisplayDialog("ChemLab", "Сборка не удалась — смотрите Console.", "OK");
            }
        }

        [MenuItem("ChemLab/Build/Build And Run (на подключённом шлеме)", priority = 11)]
        public static void BuildAndRun()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUtility.DisplayDialog("ChemLab", "Активная платформа — не Android. Сначала Setup → 1.", "OK");
                return;
            }
            Directory.CreateDirectory("Builds");
            EditorUserBuildSettings.androidBuildAndRunDeployMode = AndroidDeployMode.DeviceViaUSB;
            BuildPipeline.BuildPlayer(
                System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
                OutputPath, BuildTarget.Android, BuildOptions.AutoRunPlayer);
        }
    }
}
