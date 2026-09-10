using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ChemLab.EditorTools
{
    /// <summary>
    /// Процедурная сборка сцены LaboratoryScene: комната, мебель, XR-риг (OpenXR + XRI),
    /// оборудование, полка реактивов, планшет, системы (химия, уроки, безопасность, сохранения).
    /// Сцена полностью генерируется кодом — правки делаются в этом файле и применяются повторной сборкой.
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/LaboratoryScene.unity";
        private const string PrefabDir = "Assets/Prefabs/Equipment";
        private const string InputActionsPath = "Assets/Settings/ChemLabInputActions.inputactions";

        [MenuItem("ChemLab/Setup/2. Build Laboratory Scene", priority = 2)]
        public static void Build()
        {
            if (!AppearanceFactory.ShaderAvailable)
            {
                EditorUtility.DisplayDialog("ChemLab",
                    $"Шейдер \"{AppearanceFactory.UrpLitShader}\" не найден.\n" +
                    "Установите URP (ChemLab → Setup → 0) и примените настройки (ChemLab → Setup → 1), затем повторите.", "OK");
                return;
            }

            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("ChemLab",
                    "Сцена уже существует. Пересобрать заново? Все ручные правки сцены будут потеряны.", "Пересобрать", "Отмена"))
                return;

            Directory.CreateDirectory(PrefabDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildRoom();
            var prefabs = BuildPrefabs();
            BuildFurniture(prefabs);
            BuildSystems();
            BuildXrRig();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log($"[SceneBuilder] Сцена собрана и сохранена: {ScenePath}. Она уже добавлена в Build Settings.");
            EditorUtility.DisplayDialog("ChemLab", "Сцена LaboratoryScene собрана.\nДалее: ChemLab → Build → Build Android APK", "OK");
        }

        // ================= ОСВЕЩЕНИЕ =================

        private static void BuildLighting()
        {
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.63f);
            RenderSettings.fog = false;
        }

        // ================= КОМНАТА =================

        private static void BuildRoom()
        {
            var room = new GameObject("Room");
            var floor = AppearanceFactory.Get("floor");
            var wall = AppearanceFactory.Get("wall");
            var ceil = AppearanceFactory.Get("ceiling");

            LabFactory.Prim("Floor", PrimitiveType.Cube, room.transform, new Vector3(0, -0.05f, 0), new Vector3(7.2f, 0.1f, 7.2f), floor);
            LabFactory.Prim("Ceiling", PrimitiveType.Cube, room.transform, new Vector3(0, 3.02f, 0), new Vector3(7.2f, 0.06f, 7.2f), ceil);
            LabFactory.Prim("Wall_Back", PrimitiveType.Cube, room.transform, new Vector3(0, 1.5f, -3.5f), new Vector3(7.2f, 3f, 0.1f), wall);
            LabFactory.Prim("Wall_Front", PrimitiveType.Cube, room.transform, new Vector3(0, 1.5f, 3.5f), new Vector3(7.2f, 3f, 0.1f), wall);
            LabFactory.Prim("Wall_Left", PrimitiveType.Cube, room.transform, new Vector3(-3.5f, 1.5f, 0), new Vector3(0.1f, 3f, 7.2f), wall);
            LabFactory.Prim("Wall_Right", PrimitiveType.Cube, room.transform, new Vector3(3.5f, 1.5f, 0), new Vector3(0.1f, 3f, 7.2f), wall);

            // Светильник на потолке
            LabFactory.Prim("CeilingLamp", PrimitiveType.Cube, room.transform, new Vector3(0, 2.97f, -0.6f), new Vector3(1.6f, 0.02f, 0.5f), AppearanceFactory.Get("emissive"));
        }

        // ================= МЕБЕЛЬ =================

        private static class Layout
        {
            public const float TableTopY = 0.95f;   // рабочая высота
            public static readonly Vector3 TableCenter = new Vector3(0f, 0f, -1.6f);
            public static readonly Vector3 CounterCenter = new Vector3(0.3f, 0f, -3.05f);
            public static readonly Vector3 RigPos = new Vector3(0f, 0f, -0.15f);
        }

        private static void BuildFurniture(PrefabSet prefabs)
        {
            var root = new GameObject("Furniture");
            var tableMat = AppearanceFactory.Get("table");
            var counterMat = AppearanceFactory.Get("counter");
            var metal = AppearanceFactory.Get("metal");

            // Рабочий стол
            var table = new GameObject("WorkTable");
            table.transform.SetParent(root.transform, false);
            table.transform.position = Layout.TableCenter;
            LabFactory.Prim("Top", PrimitiveType.Cube, table.transform, new Vector3(0, 0.925f, 0), new Vector3(1.9f, 0.05f, 0.85f), tableMat);
            foreach (var (dx, dz) in new[] { (-0.88f, -0.36f), (0.88f, -0.36f), (-0.88f, 0.36f), (0.88f, 0.36f) })
                LabFactory.Prim("Leg", PrimitiveType.Cube, table.transform, new Vector3(dx, 0.45f, dz), new Vector3(0.06f, 0.9f, 0.06f), counterMat);

            // Стол-остров у задней стены (под полкой)
            var counter = new GameObject("Counter");
            counter.transform.SetParent(root.transform, false);
            counter.transform.position = Layout.CounterCenter;
            LabFactory.Prim("Body", PrimitiveType.Cube, counter.transform, new Vector3(0, 0.45f, 0), new Vector3(2.6f, 0.9f, 0.7f), counterMat);
            LabFactory.Prim("Top", PrimitiveType.Cube, counter.transform, new Vector3(0, 0.925f, 0), new Vector3(2.6f, 0.05f, 0.7f), tableMat);

            // Полка с реактивами над столом-островом
            var shelf = new GameObject("ReagentShelf");
            shelf.transform.SetParent(root.transform, false);
            shelf.transform.position = new Vector3(0.3f, 0f, -3.32f);
            LabFactory.Prim("Board", PrimitiveType.Cube, shelf.transform, new Vector3(0, 1.35f, 0.2f), new Vector3(2.6f, 0.03f, 0.42f), tableMat);
            LabFactory.Prim("Board2", PrimitiveType.Cube, shelf.transform, new Vector3(0, 1.65f, 0.2f), new Vector3(2.6f, 0.03f, 0.42f), tableMat);
            LabFactory.Prim("BackPanel", PrimitiveType.Cube, shelf.transform, new Vector3(0, 1.5f, 0.02f), new Vector3(2.6f, 1.1f, 0.03f), counterMat);
            LabFactory.Prim("PostL", PrimitiveType.Cube, shelf.transform, new Vector3(-1.28f, 0.9f, 0.2f), new Vector3(0.04f, 1.8f, 0.04f), metal);
            LabFactory.Prim("PostR", PrimitiveType.Cube, shelf.transform, new Vector3(1.28f, 0.9f, 0.2f), new Vector3(0.04f, 1.8f, 0.04f), metal);

            // Раковина (левый край рабочего стола)
            BuildSink(root.transform);

            // Контейнер для отходов
            var bin = new GameObject("WasteBin");
            bin.transform.SetParent(root.transform, false);
            bin.transform.position = new Vector3(1.45f, 0f, -0.9f);
            LabFactory.Cylinder("Body", bin.transform, new Vector3(0, 0.26f, 0), 0.15f, 0.52f, AppearanceFactory.Get("darkplastic"));
            LabFactory.Cylinder("Rim", bin.transform, new Vector3(0, 0.53f, 0), 0.16f, 0.03f, AppearanceFactory.Get("accent"));
            // триггер-зона утилизации
            var trigger = new GameObject("DropZone");
            trigger.transform.SetParent(bin.transform, false);
            trigger.transform.localPosition = new Vector3(0, 0.45f, 0);
            var tc = trigger.AddComponent<CapsuleCollider>();
            tc.isTrigger = true;
            tc.radius = 0.17f;
            tc.height = 0.5f;
            bin.AddComponent<Equipment.WasteBin>();

            // Оборудование на рабочем столе
            PlaceEquipment(root.transform, prefabs);
        }

        private static void BuildSink(Transform parent)
        {
            var sink = new GameObject("Sink");
            sink.transform.SetParent(parent, false);
            var white = AppearanceFactory.Get("sink");
            var metal = AppearanceFactory.Get("metal");
            var dark = AppearanceFactory.Get("screen");

            float bx = -0.72f; // левый край стола (локально от Furniture!)
            // Sink привязан к мировым координатам стола:
            var tableCenter = Layout.TableCenter;
            sink.transform.position = tableCenter + new Vector3(bx, 0f, 0f);

            LabFactory.Prim("CounterBlock", PrimitiveType.Cube, sink.transform, new Vector3(0, 0.945f, 0), new Vector3(0.5f, 0.05f, 0.55f), white);
            var basin = LabFactory.Prim("Basin", PrimitiveType.Cube, sink.transform, new Vector3(0, 0.97f, 0), new Vector3(0.4f, 0.04f, 0.42f), dark);
            // маркер раковины — на дне чаши (по нему определяется слив реактивов)
            basin.AddComponent<Equipment.SinkMarker>();

            var tapV = LabFactory.Cylinder("TapV", sink.transform, new Vector3(0, 1.12f, -0.24f), 0.012f, 0.24f, metal);
            Object.DestroyImmediate(tapV.GetComponent<Collider>());
            var tapH = LabFactory.Cylinder("TapH", sink.transform, new Vector3(0, 1.23f, -0.18f), 0.011f, 0.14f, metal, Quaternion.Euler(90f, 0, 0));
            Object.DestroyImmediate(tapH.GetComponent<Collider>());
            var nozzle = LabFactory.Cylinder("Nozzle", sink.transform, new Vector3(0, 1.19f, -0.115f), 0.008f, 0.06f, metal);
            Object.DestroyImmediate(nozzle.GetComponent<Collider>());
        }

        private static void PlaceEquipment(Transform parent, PrefabSet prefabs)
        {
            float y = Layout.TableTopY + 0.002f;
            var t = Layout.TableCenter;

            // Стакан с водой
            var beaker = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Beaker);
            beaker.transform.SetParent(parent, false);
            beaker.transform.position = t + new Vector3(-0.15f, y, -0.1f);
            var bc = beaker.GetComponent<Chemistry.ChemicalContainer>();
            bc.Add("water", 100f);
            beaker.GetComponent<Chemistry.ContainerVisuals>().Refresh();

            // Пробирки + штатив
            var rack = LabFactory.BuildTestTubeRack(AppearanceFactory.Get("table"));
            rack.transform.SetParent(parent, false);
            rack.transform.position = t + new Vector3(0.45f, y, -0.12f);
            for (int i = 0; i < 3; i++)
            {
                var tube = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.TestTube);
                tube.transform.SetParent(parent, false);
                tube.transform.position = t + new Vector3(0.31f + i * 0.07f, y + 0.012f, -0.12f);
            }

            // Пустой стаканчик для практики
            var cup = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.ReagentCup);
            cup.transform.SetParent(parent, false);
            cup.transform.position = t + new Vector3(-0.42f, y, 0.16f);

            // Пипетка (лежит)
            var pipette = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Pipette);
            pipette.transform.SetParent(parent, false);
            pipette.transform.position = t + new Vector3(0.1f, y + 0.01f, 0.28f);
            pipette.transform.rotation = Quaternion.Euler(0f, 25f, 90f);

            // Термометр (лежит)
            var thermo = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Thermometer);
            thermo.transform.SetParent(parent, false);
            thermo.transform.position = t + new Vector3(0.32f, y + 0.01f, 0.28f);
            thermo.transform.rotation = Quaternion.Euler(0f, -15f, 90f);

            // Весы
            var scale = LabFactory.BuildScale();
            scale.transform.SetParent(parent, false);
            scale.transform.position = t + new Vector3(-0.55f, y, -0.12f);

            // pH-метр + электрод
            var phMeter = LabFactory.BuildPHMeter();
            phMeter.transform.SetParent(parent, false);
            phMeter.transform.position = t + new Vector3(0.1f, y, -0.32f);
            var electrode = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Electrode);
            electrode.transform.SetParent(parent, false);
            electrode.transform.position = t + new Vector3(0.1f, y + 0.005f, -0.15f);
            electrode.transform.rotation = Quaternion.Euler(0f, 0f, 8f);
            var tip = electrode.transform.Find("Tip");
            if (tip != null) phMeter.GetComponent<Equipment.PHMeter>().electrodeTip = tip;

            // Полка с реактивами (кнопки выдачи)
            BuildReagentShelf(parent, prefabs);

            // Планшет
            var tablet = LabFactory.BuildTablet();
            tablet.transform.SetParent(parent, false);
            tablet.transform.position = t + new Vector3(0.75f, y + 0.008f, 0.12f);
            tablet.transform.rotation = Quaternion.Euler(60f, 180f, 0f);
        }

        private static void BuildReagentShelf(Transform parent, PrefabSet prefabs)
        {
            float shelfY = 1.28f; // под нижней полкой
            float z = -2.86f;     // перед полкой
            float spawnY = Layout.TableTopY + 0.01f;
            var counter = Layout.CounterCenter;

            var defs = new (string id, float ml, float x, float y, Color color)[]
            {
                ("water", 100f, -0.85f, shelfY, new Color(0.16f, 0.30f, 0.52f)),
                ("nacl_solid", 4.6f, -0.45f, shelfY, new Color(0.45f, 0.42f, 0.34f)),
                ("hcl_1m", 50f, -0.05f, shelfY, new Color(0.55f, 0.28f, 0.10f)),
                ("naoh_1m", 50f, 0.35f, shelfY, new Color(0.14f, 0.42f, 0.30f)),
                ("cuso4_solution", 50f, -0.65f, shelfY + 0.16f, new Color(0.18f, 0.34f, 0.58f)),
                ("universal_indicator", 5f, -0.25f, shelfY + 0.16f, new Color(0.30f, 0.18f, 0.44f)),
                ("ethanol", 60f, 0.15f, shelfY + 0.16f, new Color(0.42f, 0.40f, 0.12f))
            };

            foreach (var (id, ml, x, yy, color) in defs)
            {
                var d = LabFactory.BuildDispenser(id, ml, prefabs.ReagentCup,
                    new Vector3(x, yy, z), new Vector3(x, spawnY, counter.z), color);
                d.transform.SetParent(parent, true);
                // небольшой поворот кнопок к игроку
                d.transform.rotation = Quaternion.identity;
            }

            // Кнопка "ВОДА" у крана над раковиной
            var tap = LabFactory.BuildDispenser("water", 100f, prefabs.ReagentCup,
                new Vector3(Layout.TableCenter.x - 0.72f, 1.28f, Layout.TableCenter.z - 0.28f),
                new Vector3(Layout.TableCenter.x - 0.72f, 1.05f, Layout.TableCenter.z - 0.02f),
                new Color(0.10f, 0.36f, 0.60f));
            tap.name = "WaterTap";
            tap.transform.SetParent(parent, true);
        }

        // ================= СИСТЕМЫ =================

        private static void BuildSystems()
        {
            var root = new GameObject("Systems");

            var db = root.AddComponent<Chemistry.ChemistryDatabase>();
            db.substanceFiles = FindTextAssets("Assets/Chemistry/Data/Substances");
            db.reactionFiles = FindTextAssets("Assets/Chemistry/Data/Reactions");
            db.lessonFiles = FindTextAssets("Assets/Lessons");

            root.AddComponent<Chemistry.ReactionEngine>();
            root.AddComponent<Safety.SafetySystem>();
            root.AddComponent<Save.LabJournal>();
            root.AddComponent<Save.SaveManager>();
            root.AddComponent<Lessons.LessonManager>();

            var gm = root.AddComponent<Core.GameManager>();
            gm.database = db;
            gm.reactionEngine = root.GetComponent<Chemistry.ReactionEngine>();
            gm.safety = root.GetComponent<Safety.SafetySystem>();
            gm.lessons = root.GetComponent<Lessons.LessonManager>();
            gm.journal = root.GetComponent<Save.LabJournal>();
            gm.save = root.GetComponent<Save.SaveManager>();
            // gm.tablet назначается после создания планшета
        }

        private static TextAsset[] FindTextAssets(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return Array.Empty<TextAsset>();
            var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { folder });
            var list = new List<TextAsset>();
            foreach (var g in guids)
                list.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(g)));
            return list.ToArray();
        }

        // ================= XR-РИГ =================

        private static void BuildXrRig()
        {
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputAsset == null)
                Debug.LogError($"[SceneBuilder] Не найден {InputActionsPath} — ввод контроллеров не будет работать.");

            // Interaction Manager
            var managerGo = new GameObject("XR Interaction Manager");
            managerGo.AddComponent<XRInteractionManager>();

            // EventSystem для uGUI через лучи контроллеров
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<XRUIInputModule>();

            // XR Origin
            var rig = new GameObject("XR Origin");
            rig.transform.position = Layout.RigPos;
            var origin = rig.AddComponent<Unity.XR.CoreUtils.XROrigin>();
            var locomotion = rig.AddComponent<Core.RigLocomotion>();

            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(rig.transform, false);

            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(offset.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 40f;
            cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();
            AddPoseDriver(camGo, inputAsset, "XRI HMD/Position", "XRI HMD/Rotation");

            var left = BuildHand(offset.transform, "LeftHand Controller", inputAsset, "LeftHand");
            var right = BuildHand(offset.transform, "RightHand Controller", inputAsset, "RightHand");

            origin.Camera = cam;
            origin.CameraFloorOffsetObject = offset;
            origin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;

            locomotion.cameraTransform = cam.transform;
            if (inputAsset != null)
            {
                locomotion.moveAction = new InputActionProperty(inputAsset.FindAction("XRI LeftHand Locomotion/Move"));
                locomotion.turnAction = new InputActionProperty(inputAsset.FindAction("XRI RightHand Locomotion/Turn"));
            }

            // Планшет в GameManager
            var tablet = GameObject.Find("Furniture")?.GetComponentInChildren<UI.TabletUI>();
            var gm = FindFirstObjectByType<Core.GameManager>();
            if (gm != null) gm.tablet = tablet;
        }

        private static GameObject BuildHand(Transform parent, string name, InputActionAsset asset, string hand)
        {
            var handGo = new GameObject(name);
            handGo.transform.SetParent(parent, false);
            AddPoseDriver(handGo, asset, $"XRI {hand} Controller/Position", $"XRI {hand} Controller/Rotation");

            // Группа взаимодействий: direct + ray не конфликтуют
            var group = handGo.AddComponent<XRInteractionGroup>();

            // Прямой захват (внутри сферы)
            var direct = handGo.AddComponent<XRDirectInteractor>();
            var sphere = handGo.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.035f;
            if (asset != null)
                direct.selectInput = new InputActionProperty(asset.FindAction($"XRI {hand} Interaction/Select"));

            // Луч (наведение на объекты и UI)
            var ray = handGo.AddComponent<XRRayInteractor>();
            ray.maxRaycastDistance = 3.5f;
            if (asset != null)
            {
                ray.selectInput = new InputActionProperty(asset.FindAction($"XRI {hand} Interaction/Select"));
                ray.activateInput = new InputActionProperty(asset.FindAction($"XRI {hand} Interaction/Activate"));
                ray.uiPressInput = new InputActionProperty(asset.FindAction($"XRI {hand} Interaction/UI Press"));
            }

            var line = handGo.AddComponent<LineRenderer>();
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            var lineVisual = handGo.AddComponent<XRInteractorLineVisual>();
            lineVisual.lineWidth = 0.0035f;

            group.AddGroupMember(direct);
            group.AddGroupMember(ray);
            return handGo;
        }

        private static void AddPoseDriver(GameObject go, InputActionAsset asset, string posAction, string rotAction)
        {
            var tpd = go.AddComponent<TrackedPoseDriver>();
            if (asset != null)
            {
                tpd.positionInput = new InputActionProperty(asset.FindAction(posAction));
                tpd.rotationInput = new InputActionProperty(asset.FindAction(rotAction));
            }
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        }

        // ================= ПРЕФАБЫ =================

        public class PrefabSet
        {
            public GameObject Beaker;
            public GameObject TestTube;
            public GameObject ReagentCup;
            public GameObject Pipette;
            public GameObject Thermometer;
            public GameObject Electrode;
        }

        private static PrefabSet BuildPrefabs()
        {
            var set = new PrefabSet();

            set.Beaker = SaveAsPrefab(LabFactory.BuildVessel(new LabFactory.VesselSpec
            {
                name = "Beaker_250ml", kind = "beaker", displayName = "Стакан 250 мл",
                capacityMl = 250f, outerRadius = 0.036f, height = 0.095f, massG = 80f,
                isGlass = true, disposable = false
            }), $"{PrefabDir}/Beaker_250ml.prefab");

            set.TestTube = SaveAsPrefab(LabFactory.BuildVessel(new LabFactory.VesselSpec
            {
                name = "TestTube_30ml", kind = "testtube", displayName = "Пробирка 30 мл",
                capacityMl = 30f, outerRadius = 0.013f, height = 0.105f, massG = 20f,
                isGlass = true, disposable = false, bubbleCount = 5
            }), $"{PrefabDir}/TestTube_30ml.prefab");

            set.ReagentCup = SaveAsPrefab(LabFactory.BuildVessel(new LabFactory.VesselSpec
            {
                name = "ReagentCup_100ml", kind = "cup", displayName = "Стаканчик 100 мл",
                capacityMl = 100f, outerRadius = 0.026f, height = 0.075f, massG = 40f,
                isGlass = true, disposable = true, bubbleCount = 6
            }), $"{PrefabDir}/ReagentCup_100ml.prefab");

            set.Pipette = SaveAsPrefab(LabFactory.BuildPipette(), $"{PrefabDir}/Pipette_10ml.prefab");
            set.Thermometer = SaveAsPrefab(LabFactory.BuildThermometer(), $"{PrefabDir}/Thermometer.prefab");
            set.Electrode = SaveAsPrefab(LabFactory.BuildElectrode(), $"{PrefabDir}/PH_Electrode.prefab");

            return set;
        }

        private static GameObject SaveAsPrefab(GameObject temp, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            UnityEngine.Object.DestroyImmediate(temp);
            Debug.Log("[SceneBuilder] Префаб сохранён: " + path);
            return prefab;
        }
    }
}
