using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace ChemLab.EditorTools
{
    /// <summary>
    /// Применяет настройки проекта под PICO 4 Ultra:
    /// Android/ARM64/IL2CPP, Linear color space, Input System, URP, OpenXR.
    /// ApplyCore() выполняет всю работу без диалогов — используется и из меню,
    /// и из CI-бутстрапа (ChemLabCiBootstrap) при сборке в GitHub Actions.
    /// </summary>
    public static class ChemLabProjectSetup
    {
        private static List<string> _manualSteps = new List<string>();

        [MenuItem("ChemLab/Setup/1. Apply Project Settings (Android/ARM64/OpenXR/URP)", priority = 1)]
        public static void Apply()
        {
            var manual = ApplyCore();
            AssetDatabase.SaveAssets();

            var report = "[Setup] Готово. Автоматически: Android, ARM64+IL2CPP, Linear, Input System, URP, слои, OpenXR.";
            if (manual.Count > 0)
            {
                report += "\n\nСДЕЛАЙТЕ ВРУЧНУЮ (см. docs/BUILD_APK.md):\n- " + string.Join("\n- ", manual);
                Debug.LogWarning(report);
                EditorUtility.DisplayDialog("ChemLab Setup",
                    "Настройки применены.\n\nРучные шаги:\n" + string.Join("\n", manual), "OK");
            }
            else
            {
                Debug.Log(report);
                EditorUtility.DisplayDialog("ChemLab Setup", "Настройки применены полностью.\nДалее: ChemLab → Setup → 2. Build Laboratory Scene", "OK");
            }
        }

        /// <summary>
        /// Применить все настройки без единого диалога. Возвращает список шагов,
        /// которые нужно сделать вручную (в CI — признак проблемы).
        /// Вызывается и из меню, и из ChemLabCiBootstrap (batch-режим).
        /// </summary>
        public static List<string> ApplyCore()
        {
            _manualSteps = new List<string>();

            // 1. Идентификатор приложения
            PlayerSettings.companyName = "VirtualChemLab";
            PlayerSettings.productName = "Virtual Chemistry Lab";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.virtuallab.chemistry");

            // 2. Цветовое пространство Linear
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
                if (!Application.isBatchMode)
                    Debug.LogWarning("[Setup] Color Space = Linear. ПЕРЕЗАПУСТИТЕ Unity в редакторе, чтобы применить.");
                else
                    Debug.Log("[Setup] Color Space = Linear (batch: применится к сборке).");
            }

            // 3. Скриптовый бэкенд и архитектура
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingBackend.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.Arm64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersion.Android10;
            Debug.Log("[Setup] IL2CPP + ARM64 + minSdk 29 (Android 10).");

            // 4. Active Input Handling = Input System (только новый ввод)
            try
            {
                var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (settings != null && settings.Length > 0)
                {
                    var so = new SerializedObject(settings[0]);
                    var prop = so.FindProperty("activeInputHandler");
                    if (prop != null && prop.intValue != 2)
                    {
                        prop.intValue = 2; // 2 = Input System Package (new)
                        so.ApplyModifiedProperties();
                        Debug.Log("[Setup] Active Input Handling = Input System.");
                    }
                }
            }
            catch (Exception e)
            {
                _manualSteps.Add("Project Settings → Player → Active Input Handling → Input System Package (" + e.Message + ")");
            }

            // 5. URP-ассет
            try
            {
                CreateAndAssignUrpAsset();
            }
            catch (Exception e)
            {
                _manualSteps.Add("Создать URP Asset и назначить в Graphics (" + e.Message + ")");
            }

            // 6. Слои: LiquidZone (зоны жидкостей), Glassware/Probes (прозрачные стенки для инструментов)
            try
            {
                AddLayer("LiquidZone");
                AddLayer("Glassware");
                AddLayer("Probes");
                Debug.Log("[Setup] Слои созданы: LiquidZone, Glassware, Probes.");
            }
            catch (Exception e)
            {
                _manualSteps.Add("Добавить слои LiquidZone, Glassware, Probes в Tag Manager (" + e.Message + ")");
            }

            // 7. XR Plug-in Management: OpenXR loader
            TrySetupXrManagement();

            // 8. OpenXR: режим рендера и профили контроллеров
            TrySetupOpenXr();

            return _manualSteps;
        }

        private static void CreateAndAssignUrpAsset()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset existing)
            {
                Debug.Log("[Setup] URP-ассет уже назначен: " + existing.name);
                return;
            }

            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.name = "ChemLab_Renderer";
            var urp = UniversalRenderPipelineAsset.Create(rendererData);
            urp.name = "ChemLab_URP";

            // VR-настройки качества
            urp.msaaSampleCount = 4;          // MSAA 4x — важно в VR
            urp.renderScale = 1.0f;
            urp.supportsHDR = false;          // экономим память и время кадра
            urp.shadowDistance = 12f;
            urp.shadowCascadeCount = 1;

            AssetDatabase.CreateAsset(rendererData, "Assets/Settings/ChemLab_Renderer.asset");
            AssetDatabase.CreateAsset(urp, "Assets/Settings/ChemLab_URP.asset");
            GraphicsSettings.defaultRenderPipeline = urp;
            Debug.Log("[Setup] URP-ассет создан и назначен (MSAA 4x, HDR off).");
        }

        private static void AddLayer(string layerName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var layers = so.FindProperty("layers");
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return;
            for (int i = 8; i < 32; i++)
            {
                var el = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(el.stringValue))
                {
                    el.stringValue = layerName;
                    so.ApplyModifiedProperties();
                    return;
                }
            }
        }

        private static void TrySetupXrManagement()
        {
            try
            {
                var general = XRGeneralSettings.Instance;
                if (general == null || general.Manager == null)
                {
                    _manualSteps.Add("Project Settings → XR Plug-in Management → Android → инициализировать и включить OpenXR");
                    return;
                }
                var manager = general.Manager;
                var openXrType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("UnityEngine.XR.OpenXR.OpenXRLoader"))
                    .FirstOrDefault(t => t != null);
                if (openXrType == null)
                {
                    _manualSteps.Add("Установить пакет OpenXR и включить его в XR Plug-in Management");
                    return;
                }
                bool has = manager.loaders != null && manager.loaders.Any(l => l != null && l.GetType() == openXrType);
                if (!has) manager.TryAddLoader(openXrType, 0);
                Debug.Log("[Setup] OpenXR loader добавлен в XR Plug-in Management (Android).");
            }
            catch (Exception e)
            {
                _manualSteps.Add("XR Plug-in Management → Android → включить OpenXR (" + e.Message + ")");
            }
        }

        private static void TrySetupOpenXr()
        {
            try
            {
                var oxr = OpenXRSettings.ActiveBuildTargetInstance;
                if (oxr == null)
                {
                    _manualSteps.Add("Project Settings → XR Plug-in Management → OpenXR (включить для Android)");
                    return;
                }
                oxr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                oxr.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth16Bit;
                Debug.Log("[Setup] OpenXR: Single Pass Instanced, Depth 16 bit.");

                // Профили контроллеров
                int added = 0;

                // 1) PICO-профили (если установлен PICO Unity Integration SDK)
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }
                    foreach (var t in types)
                    {
                        if (!typeof(OpenXRInteractionFeature).IsAssignableFrom(t) || t.IsAbstract) continue;
                        if (!t.Name.ToLower().Contains("pico")) continue;
                        try
                        {
                            var inst = ScriptableObject.CreateInstance(t) as OpenXRInteractionFeature;
                            inst.name = t.Name;
                            inst.enabled = true;
                            if (oxr.interactionProfiles == null || !oxr.interactionProfiles.Contains(inst))
                                oxr.interactionProfiles.Add(inst);
                            added++;
                            Debug.Log("[Setup] OpenXR: включён PICO-профиль " + t.Name);
                        }
                        catch { /* ignore */ }
                    }
                }

                // 2) Oculus Touch — универсальный профиль для PICO (работает без PICO SDK)
                var oculusType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("UnityEngine.XR.OpenXR.Features.OculusTouchControllerProfile"))
                    .FirstOrDefault(t => t != null);
                if (oculusType != null && added == 0)
                {
                    var inst = ScriptableObject.CreateInstance(oculusType) as OpenXRInteractionFeature;
                    inst.enabled = true;
                    if (oxr.interactionProfiles == null || !oxr.interactionProfiles.Contains(inst))
                        oxr.interactionProfiles.Add(inst);
                    Debug.Log("[Setup] OpenXR: включён Oculus Touch Controller Profile (совместим с PICO).");
                }
            }
            catch (Exception e)
            {
                _manualSteps.Add("Project Settings → XR Plug-in Management → OpenXR → Render Mode = Single Pass Instanced, включить профиль контроллеров (" + e.Message + ")");
            }
        }
    }
}
