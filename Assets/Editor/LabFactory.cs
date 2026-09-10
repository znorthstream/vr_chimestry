using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ChemLab.EditorTools
{
    /// <summary>
    /// Фабрика лабораторного оборудования: сосуды (стакан, пробирка, стаканчик),
    /// пипетка, термометр, электрод, весы, pH-метр, планшет, кнопки полки.
    /// Используются примитивы + URP материалы; XR-компоненты настраиваются кодом.
    /// </summary>
    public static class LabFactory
    {
        // ---------- низкоуровневые хелперы ----------

        public static GameObject Prim(string name, PrimitiveType type, Transform parent,
            Vector3 localPos, Vector3 scale, Material mat, Quaternion localRot = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot == default ? Quaternion.identity : localRot;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            return go;
        }

        /// <summary>Цилиндр по радиусу/высоте (mesh цилиндра: диаметр 1, высота 2).</summary>
        public static GameObject Cylinder(string name, Transform parent, Vector3 localPos, float radius, float height,
            Material mat, Quaternion localRot = default)
        {
            return Prim(name, PrimitiveType.Cylinder, parent, localPos, new Vector3(radius * 2f, height * 0.5f, radius * 2f), mat, localRot);
        }

        // ---------- сосуды ----------

        public class VesselSpec
        {
            public string name = "Beaker";
            public string kind = "beaker";
            public string displayName = "Стакан 250 мл";
            public float capacityMl = 250f;
            public float outerRadius = 0.036f;
            public float height = 0.095f;
            public float massG = 80f;
            public bool isGlass = true;
            public bool disposable = true;
            public int bubbleCount = 8;
        }

        /// <summary>Построить сосуд (стакан/пробирка/стаканчик) со всей химической начинкой.</summary>
        public static GameObject BuildVessel(VesselSpec spec)
        {
            var root = new GameObject(spec.name);

            var glass = AppearanceFactory.Get("glass");
            var body = Cylinder("Glass", root.transform, new Vector3(0, spec.height * 0.5f, 0), spec.outerRadius, spec.height, glass);
            body.GetComponent<Collider>().material = new PhysicsMaterial("Glass") { dynamicFriction = 0.4f, staticFriction = 0.45f, bounciness = 0.1f };

            float innerR = spec.outerRadius * 0.92f;
            float innerH = spec.height - 0.014f;
            float bottomY = 0.008f;

            // Жидкость
            var liquid = Cylinder("Liquid", root.transform, new Vector3(0, bottomY, 0), innerR, 0.001f, AppearanceFactory.Get("liquid"));
            Object.DestroyImmediate(liquid.GetComponent<Collider>());
            liquid.gameObject.SetActive(false);

            // Осадок
            var precip = Cylinder("Precipitate", root.transform, new Vector3(0, bottomY, 0), innerR * 0.96f, 0.001f, AppearanceFactory.Get("precipitate"));
            Object.DestroyImmediate(precip.GetComponent<Collider>());
            precip.gameObject.SetActive(false);

            // Пузырьки газа
            var bubblesRoot = new GameObject("Bubbles");
            bubblesRoot.transform.SetParent(root.transform, false);
            bubblesRoot.transform.localPosition = Vector3.zero;
            var bubbleMat = AppearanceFactory.Get("liquid");
            for (int i = 0; i < spec.bubbleCount; i++)
            {
                var b = Prim("Bubble" + i, PrimitiveType.Sphere, bubblesRoot.transform, Vector3.zero, Vector3.one * 0.006f, bubbleMat);
                Object.DestroyImmediate(b.GetComponent<Collider>());
            }
            bubblesRoot.SetActive(false);

            // Зона жидкости (триггер)
            var zone = new GameObject("LiquidZone");
            zone.transform.SetParent(root.transform, false);
            zone.transform.localPosition = new Vector3(0, spec.height * 0.5f, 0);
            var zoneCol = zone.AddComponent<CapsuleCollider>();
            zoneCol.isTrigger = true;
            zoneCol.radius = innerR + 0.004f;
            zoneCol.height = spec.height * 1.4f;
            int lz = LayerMask.NameToLayer("LiquidZone");
            if (lz >= 0) zone.layer = lz;
            zone.AddComponent<Equipment.ContainerZone>();

            // Химия
            var container = root.AddComponent<Chemistry.ChemicalContainer>();
            container.capacityMl = spec.capacityMl;

            var visuals = root.AddComponent<Chemistry.ContainerVisuals>();
            visuals.liquid = liquid.transform;
            visuals.precipitate = precip.transform;
            visuals.bubblesRoot = bubblesRoot.transform;
            visuals.innerHeight = innerH;
            visuals.bottomY = bottomY;
            visuals.innerRadius = innerR;
            visuals.liquidMaterial = AppearanceFactory.Get("liquid");
            visuals.precipitateMaterial = AppearanceFactory.Get("precipitate");

            // Физика и захват
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = Mathf.Max(0.02f, spec.massG / 1000f);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var lab = root.AddComponent<Core.LabObject>();
            lab.objectKind = spec.kind;
            lab.displayName = spec.displayName;
            lab.massG = spec.massG;
            lab.isGlass = spec.isGlass;
            lab.disposable = spec.disposable;

            root.AddComponent<Equipment.PourableGrab>();

            var grab = root.AddComponent<XRGrabInteractable>();
            ConfigureGrab(grab);
            return root;
        }

        public static void ConfigureGrab(XRGrabInteractable grab)
        {
            grab.movementType = XRGrabInteractable.MovementType.Kinematic;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;
            grab.smoothPosition = true;
            grab.smoothRotation = true;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        // ---------- пипетка ----------

        public static GameObject BuildPipette()
        {
            var root = new GameObject("Pipette_10ml");
            var glass = AppearanceFactory.Get("glass");
            var white = AppearanceFactory.Get("whiteplastic");

            var shaft = Cylinder("Shaft", root.transform, new Vector3(0, 0.09f, 0), 0.0065f, 0.14f, glass);
            var tipCone = Cylinder("TipCone", root.transform, new Vector3(0, 0.013f, 0), 0.0035f, 0.03f, glass);
            Object.DestroyImmediate(tipCone.GetComponent<Collider>());
            var bulb = Prim("Bulb", PrimitiveType.Sphere, root.transform, new Vector3(0, 0.172f, 0), Vector3.one * 0.026f, white);
            Object.DestroyImmediate(bulb.GetComponent<Collider>());

            var tip = new GameObject("Tip");
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(0, 0.002f, 0);

            var container = root.AddComponent<Chemistry.ChemicalContainer>();
            container.capacityMl = 10f;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.025f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var lab = root.AddComponent<Core.LabObject>();
            lab.objectKind = "pipette";
            lab.displayName = "Пипетка 10 мл";
            lab.massG = 25f;
            lab.isGlass = false;
            lab.disposable = false;

            var pipette = root.AddComponent<Equipment.Pipette>();
            pipette.tip = tip.transform;
            pipette.capacityMl = 10f;

            var grab = root.AddComponent<XRGrabInteractable>();
            ConfigureGrab(grab);
            int probesLayer = LayerMask.NameToLayer("Probes");
            if (probesLayer >= 0) SetLayerRecursive(root, probesLayer);
            return root;
        }

        // ---------- термометр ----------

        public static GameObject BuildThermometer()
        {
            var root = new GameObject("Thermometer");
            var white = AppearanceFactory.Get("whiteplastic");
            var accent = AppearanceFactory.Get("accent");

            var rod = Cylinder("Rod", root.transform, new Vector3(0, 0.06f, 0), 0.005f, 0.11f, white);
            var bulb = Prim("Bulb", PrimitiveType.Sphere, root.transform, new Vector3(0, 0.007f, 0), Vector3.one * 0.02f, accent);
            Object.DestroyImmediate(bulb.GetComponent<Collider>());

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.03f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var lab = root.AddComponent<Core.LabObject>();
            lab.objectKind = "thermometer";
            lab.displayName = "Термометр";
            lab.massG = 30f;
            lab.isGlass = true;
            lab.disposable = false;

            var thermo = root.AddComponent<Equipment.Thermometer>();
            thermo.bulb = bulb.transform;
            thermo.probeRadius = 0.05f;

            var grab = root.AddComponent<XRGrabInteractable>();
            ConfigureGrab(grab);
            int probesLayer2 = LayerMask.NameToLayer("Probes");
            if (probesLayer2 >= 0) SetLayerRecursive(root, probesLayer2);
            return root;
        }

        // ---------- электрод pH-метра ----------

        public static GameObject BuildElectrode()
        {
            var root = new GameObject("PH_Electrode");
            var metal = AppearanceFactory.Get("metal");
            var dark = AppearanceFactory.Get("darkplastic");

            var rod = Cylinder("Rod", root.transform, new Vector3(0, 0.07f, 0), 0.005f, 0.12f, metal);
            var handle = Cylinder("Handle", root.transform, new Vector3(0, 0.155f, 0), 0.012f, 0.05f, dark);
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            var tip = new GameObject("Tip");
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(0, 0.006f, 0);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.035f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var lab = root.AddComponent<Core.LabObject>();
            lab.objectKind = "electrode";
            lab.displayName = "Электрод pH-метра";
            lab.massG = 35f;
            lab.isGlass = true;
            lab.disposable = false;

            var grab = root.AddComponent<XRGrabInteractable>();
            ConfigureGrab(grab);
            int probesLayer3 = LayerMask.NameToLayer("Probes");
            if (probesLayer3 >= 0) SetLayerRecursive(root, probesLayer3);
            return root;
        }

        // ---------- штатив ----------

        public static GameObject BuildTestTubeRack(Material mat)
        {
            var root = new GameObject("TestTubeRack");
            Prim("Base", PrimitiveType.Cube, root.transform, new Vector3(0, 0.01f, 0), new Vector3(0.28f, 0.02f, 0.09f), mat);
            for (int i = 0; i < 4; i++)
            {
                float x = -0.105f + i * 0.07f;
                Prim("Post" + i, PrimitiveType.Cube, root.transform, new Vector3(x, 0.03f, 0), new Vector3(0.015f, 0.04f, 0.08f), mat);
            }
            return root;
        }

        // ---------- весы ----------

        public static GameObject BuildScale()
        {
            var root = new GameObject("DigitalScale");
            var device = AppearanceFactory.Get("device");
            var metal = AppearanceFactory.Get("metal");

            Prim("Base", PrimitiveType.Cube, root.transform, new Vector3(0, 0.02f, 0), new Vector3(0.17f, 0.04f, 0.15f), device);
            var plate = Cylinder("Plate", root.transform, new Vector3(0, 0.05f, 0), 0.055f, 0.012f, metal);

            var zone = new GameObject("PlateZone");
            zone.transform.SetParent(root.transform, false);
            zone.transform.localPosition = new Vector3(0, 0.10f, 0);
            var col = zone.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.13f, 0.10f, 0.12f);

            // Дисплей + кнопка тары
            var display = UiFactory.MakeDisplay("Display", root.transform, new Vector3(0, 0.045f, 0.078f), Quaternion.identity,
                new Vector2(420, 110), 3000f, 40);
            var tareBtn = UiFactory.AddButton(display.transform.parent, new Vector2(150, 0), new Vector2(90, 70), "ТАРА",
                new Color(0.15f, 0.3f, 0.5f), new Vector2(0.5f, 0.5f), 22);

            var scale = root.AddComponent<Equipment.DigitalScale>();
            scale.display = display;
            scale.tareButton = tareBtn;
            return root;
        }

        // ---------- pH-метр ----------

        public static GameObject BuildPHMeter()
        {
            var root = new GameObject("PHMeter");
            var device = AppearanceFactory.Get("device");

            Prim("Base", PrimitiveType.Cube, root.transform, new Vector3(0, 0.025f, 0), new Vector3(0.11f, 0.05f, 0.15f), device);

            var display = UiFactory.MakeDisplay("Display", root.transform, new Vector3(0, 0.032f, 0.078f), Quaternion.identity,
                new Vector2(380, 120), 3000f, 40);

            var meter = root.AddComponent<Equipment.PHMeter>();
            meter.display = display;
            meter.probeRadius = 0.055f;
            return root;
        }

        // ---------- планшет ----------

        public static GameObject BuildTablet()
        {
            var root = new GameObject("Tablet");
            var dark = AppearanceFactory.Get("darkplastic");

            Prim("Body", PrimitiveType.Cube, root.transform, new Vector3(0, 0.006f, 0), new Vector3(0.26f, 0.012f, 0.18f), dark);

            // Химической ёмкости нет, но LabObject нужен для grab-событий уроков
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            var lab = root.AddComponent<Core.LabObject>();
            lab.objectKind = "tablet";
            lab.displayName = "Планшет";
            lab.massG = 400f;
            lab.isGlass = false;
            lab.disposable = false;

            var grab = root.AddComponent<XRGrabInteractable>();
            ConfigureGrab(grab);

            BuildTabletUi(root.transform, ui);
            return root;
        }

        // ---------- кнопка полки с реактивом ----------

        public static GameObject BuildDispenser(string substanceId, float amountMl, GameObject spawnPrefab,
            Vector3 buttonPos, Vector3 spawnPos, Material buttonColor)
        {
            var root = new GameObject("Dispenser_" + substanceId);
            root.transform.position = buttonPos;
            root.transform.rotation = Quaternion.identity;

            var canvas = UiFactory.CreateCanvas("Canvas", root.transform, new Vector2(170, 80), 3000f);
            canvas.transform.localRotation = Quaternion.identity;
            var bg = UiFactory.AddPanel(canvas.transform, Vector2.zero, new Vector2(170, 80), new Color(0.1f, 0.16f, 0.24f, 0.95f), new Vector2(0.5f, 0.5f));
            bg.name = "Bg";
            var button = UiFactory.AddButton(canvas.transform, Vector2.zero, new Vector2(160, 72), substanceId.ToUpper(),
                buttonColor, new Vector2(0.5f, 0.5f), 24);

            var dispenser = root.AddComponent<Equipment.ReagentDispenser>();
            dispenser.substanceId = substanceId;
            dispenser.amountMl = amountMl;
            dispenser.spawnPrefab = spawnPrefab;
            dispenser.uiButton = button;

            var spawn = new GameObject("SpawnPoint");
            spawn.transform.position = spawnPos;
            dispenser.spawnPoint = spawn.transform;
            return root;
        }

        // ---------- UI планшета ----------

        private static void BuildTabletUi(Transform parent, UI.TabletUI ui)
        {
            var canvas = UiFactory.CreateCanvas("Screen", parent, new Vector2(660, 470), 3000f);
            canvas.transform.localPosition = new Vector3(0, 0.0135f, 0);
            canvas.transform.localRotation = Quaternion.Euler(-90f, 0, 0);

            UiFactory.AddPanel(canvas.transform, Vector2.zero, new Vector2(660, 470), new Color(0.05f, 0.07f, 0.11f), new Vector2(0.5f, 0.5f));
            UiFactory.AddText(canvas.transform, new Vector2(0, -22), new Vector2(660, 36),
                "ВИРТУАЛЬНАЯ ХИМИЧЕСКАЯ ЛАБОРАТОРИЯ", 24, new Color(0.75f, 0.85f, 1f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f));

            var titleAnchor = new Vector2(0.5f, 0f);
            var centerAnchor = new Vector2(0.5f, 0.5f);

            // --- Главная ---
            ui.homePage = new GameObject("HomePage");
            ui.homePage.transform.SetParent(canvas.transform, false);
            var hp = ui.homePage.AddComponent<RectTransform>();
            hp.sizeDelta = new Vector2(660, 470);
            hp.anchoredPosition = Vector2.zero;
            UiFactory.AddText(ui.homePage.transform, new Vector2(0, -165), new Vector2(660, 40), "ГЛАВНОЕ МЕНЮ", 30, Color.white, TextAnchor.MiddleCenter, titleAnchor);
            ui.btnLessons = UiFactory.AddButton(ui.homePage.transform, new Vector2(0, -220), new Vector2(320, 50), "УРОКИ", new Color(0.16f, 0.34f, 0.58f), centerAnchor);
            ui.btnFreeLab = UiFactory.AddButton(ui.homePage.transform, new Vector2(0, -280), new Vector2(320, 50), "СВОБОДНАЯ ЛАБОРАТОРИЯ", new Color(0.14f, 0.42f, 0.26f), centerAnchor, 22);
            ui.btnJournal = UiFactory.AddButton(ui.homePage.transform, new Vector2(0, -340), new Vector2(320, 50), "ЛАБОРАТОРНЫЙ ЖУРНАЛ", new Color(0.42f, 0.30f, 0.12f), centerAnchor, 22);
            ui.btnHelp = UiFactory.AddButton(ui.homePage.transform, new Vector2(0, -400), new Vector2(320, 50), "УПРАВЛЕНИЕ", new Color(0.30f, 0.24f, 0.44f), centerAnchor);

            // --- Список уроков ---
            ui.lessonsPage = MakePage(canvas.transform, "LessonsPage");
            UiFactory.AddText(ui.lessonsPage.transform, new Vector2(0, -165), new Vector2(660, 40), "УРОКИ", 30, Color.white, TextAnchor.MiddleCenter, titleAnchor);
            ui.lessonButtons = new Button[6];
            for (int i = 0; i < 6; i++)
                ui.lessonButtons[i] = UiFactory.AddButton(ui.lessonsPage.transform, new Vector2(0, -218 - i * 36), new Vector2(560, 34), "", new Color(0.16f, 0.30f, 0.46f), centerAnchor, 17);
            var back1 = UiFactory.AddButton(ui.lessonsPage.transform, new Vector2(255, -172), new Vector2(130, 36), "← Меню", new Color(0.3f, 0.3f, 0.34f), titleAnchor, 20);
            ui.btnLessonsBack = back1;

            // --- Страница урока ---
            ui.lessonPage = MakePage(canvas.transform, "LessonPage");
            ui.lessonTitle = UiFactory.AddText(ui.lessonPage.transform, new Vector2(0, -168), new Vector2(640, 34), "", 23, new Color(0.9f, 0.95f, 1f), TextAnchor.MiddleCenter, titleAnchor);
            ui.lessonBody = UiFactory.AddText(ui.lessonPage.transform, new Vector2(0, -285), new Vector2(630, 200), "", 21, Color.white, TextAnchor.UpperLeft, titleAnchor);
            ui.lessonStepInfo = UiFactory.AddText(ui.lessonPage.transform, new Vector2(0, -390), new Vector2(640, 28), "", 19, new Color(0.6f, 0.75f, 0.95f), TextAnchor.MiddleCenter, titleAnchor);
            ui.btnTheoryPrev = UiFactory.AddButton(ui.lessonPage.transform, new Vector2(-215, -425), new Vector2(130, 42), "← Назад", new Color(0.3f, 0.3f, 0.34f), titleAnchor, 20);
            ui.btnHint = UiFactory.AddButton(ui.lessonPage.transform, new Vector2(-78, -425), new Vector2(130, 42), "Подсказка", new Color(0.55f, 0.42f, 0.08f), titleAnchor, 19);
            ui.btnAbortLesson = UiFactory.AddButton(ui.lessonPage.transform, new Vector2(78, -425), new Vector2(130, 42), "Прервать", new Color(0.55f, 0.18f, 0.14f), titleAnchor, 20);
            ui.btnTheoryNext = UiFactory.AddButton(ui.lessonPage.transform, new Vector2(215, -425), new Vector2(130, 42), "Вперёд →", new Color(0.16f, 0.34f, 0.58f), titleAnchor, 20);

            // --- Тест ---
            ui.quizPage = MakePage(canvas.transform, "QuizPage");
            ui.quizQuestion = UiFactory.AddText(ui.quizPage.transform, new Vector2(0, -192), new Vector2(630, 70), "", 22, Color.white, TextAnchor.UpperLeft, titleAnchor);
            ui.quizOptions = new Button[3];
            for (int i = 0; i < 3; i++)
                ui.quizOptions[i] = UiFactory.AddButton(ui.quizPage.transform, new Vector2(0, -262 - i * 48), new Vector2(560, 42), "", new Color(0.16f, 0.30f, 0.46f), centerAnchor, 19);
            ui.quizExplanation = UiFactory.AddText(ui.quizPage.transform, new Vector2(0, -400), new Vector2(630, 44), "", 18, new Color(0.85f, 0.8f, 0.5f), TextAnchor.UpperLeft, titleAnchor);
            ui.btnQuizNext = UiFactory.AddButton(ui.quizPage.transform, new Vector2(0, -442), new Vector2(140, 36), "Далее →", new Color(0.16f, 0.34f, 0.58f), titleAnchor, 20);

            // --- Результаты ---
            ui.resultsPage = MakePage(canvas.transform, "ResultsPage");
            ui.resultsText = UiFactory.AddText(ui.resultsPage.transform, new Vector2(0, -290), new Vector2(620, 200), "", 23, Color.white, TextAnchor.UpperLeft, titleAnchor);
            ui.btnRetry = UiFactory.AddButton(ui.resultsPage.transform, new Vector2(-200, -425), new Vector2(150, 42), "Повторить", new Color(0.16f, 0.34f, 0.58f), titleAnchor, 20);
            ui.btnToLessons = UiFactory.AddButton(ui.resultsPage.transform, new Vector2(0, -425), new Vector2(150, 42), "К урокам", new Color(0.14f, 0.42f, 0.26f), titleAnchor, 20);
            ui.btnFreeFromResults = UiFactory.AddButton(ui.resultsPage.transform, new Vector2(200, -425), new Vector2(150, 42), "Свободно", new Color(0.3f, 0.3f, 0.34f), titleAnchor, 20);

            // --- Журнал ---
            ui.journalPage = MakePage(canvas.transform, "JournalPage");
            UiFactory.AddText(ui.journalPage.transform, new Vector2(0, -165), new Vector2(660, 40), "ЛАБОРАТОРНЫЙ ЖУРНАЛ", 28, Color.white, TextAnchor.MiddleCenter, titleAnchor);
            ui.journalText = UiFactory.AddText(ui.journalPage.transform, new Vector2(0, -300), new Vector2(620, 240), "", 16, new Color(0.85f, 0.88f, 0.92f), TextAnchor.UpperLeft, titleAnchor);
            ui.btnJournalBack = UiFactory.AddButton(ui.journalPage.transform, new Vector2(255, -172), new Vector2(130, 36), "← Меню", new Color(0.3f, 0.3f, 0.34f), titleAnchor, 20);

            // --- Справка ---
            ui.helpPage = MakePage(canvas.transform, "HelpPage");
            UiFactory.AddText(ui.helpPage.transform, new Vector2(0, -165), new Vector2(660, 40), "УПРАВЛЕНИЕ", 28, Color.white, TextAnchor.MiddleCenter, titleAnchor);
            UiFactory.AddText(ui.helpPage.transform, new Vector2(0, -300), new Vector2(620, 240),
                "Grip (боковая кнопка) — взять/отпустить предмет.\n" +
                "Триггер — активировать (пипетка: набрать/вылить) и нажимать кнопки.\n" +
                "Луч из контроллера — наведение на кнопки планшета и полки.\n" +
                "Левый стик — перемещение, правый стик — поворот.\n\n" +
                "Переливание: возьмите сосуд, наклоните его НАД другим сосудом.\n" +
                "Планшет можно взять в руку — кнопки работают и в руке.",
                18, new Color(0.85f, 0.88f, 0.92f), TextAnchor.UpperLeft, titleAnchor);
            ui.btnHelpBack = UiFactory.AddButton(ui.helpPage.transform, new Vector2(255, -172), new Vector2(130, 36), "← Меню", new Color(0.3f, 0.3f, 0.34f), titleAnchor, 20);

            // --- Баннер уведомлений (поверх страниц) ---
            ui.bannerRoot = new GameObject("Banner");
            ui.bannerRoot.transform.SetParent(canvas.transform, false);
            var br = ui.bannerRoot.AddComponent<RectTransform>();
            br.sizeDelta = new Vector2(640, 200);
            br.anchoredPosition = new Vector2(0, -130);
            ui.bannerBg = ui.bannerRoot.AddComponent<Image>();
            ui.bannerBg.color = new Color(0.13f, 0.30f, 0.55f, 0.97f);
            ui.bannerTitle = UiFactory.AddText(ui.bannerRoot.transform, new Vector2(0, -26), new Vector2(610, 34), "", 24, Color.white, TextAnchor.MiddleCenter, titleAnchor);
            ui.bannerBody = UiFactory.AddText(ui.bannerRoot.transform, new Vector2(0, -95), new Vector2(610, 100), "", 19, new Color(0.94f, 0.96f, 1f), TextAnchor.UpperLeft, titleAnchor);
            ui.btnBannerOk = UiFactory.AddButton(ui.bannerRoot.transform, new Vector2(0, -178), new Vector2(180, 38), "Понятно", new Color(0.25f, 0.25f, 0.3f), titleAnchor, 20);
            ui.bannerRoot.SetActive(false);
        }

        private static GameObject MakePage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(660, 470);
            rt.anchoredPosition = Vector2.zero;
            go.SetActive(false);
            return go;
        }
    }
}
