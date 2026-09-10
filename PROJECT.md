# PROJECT.md — карта проекта для разработчиков и агентов

> Этот файл — «инструктаж» для любого (человека или ИИ-агента), кто будет дорабатывать
> проект. Здесь: что где лежит, зачем, как всё связано и по каким правилам развивать код.

---

## 1. Что это за проект

**Virtual Chemistry Lab** — standalone VR-приложение для **PICO 4 Ultra**: виртуальная
химическая лаборатория для обучения. Пользователь **физически** выполняет операции
контроллерами (берёт, наклоняет, переливает, измеряет), а приложение моделирует химию,
объясняет происходящее, следит за безопасностью и выставляет оценку.

Главный принцип (из ТЗ): *пользователь не выбирает действие из меню — он физически
выполняет его в виртуальной лаборатории. Ошибки объясняются, а не просто «ERROR».*

Технологический стек:

| Слой | Технология |
|---|---|
| Движок | Unity 6 LTS (6000.3.x) |
| Язык | C# |
| Платформа | Android, ARM64, IL2CPP |
| XR | OpenXR + XR Plug-in Management (+ PICO Unity Integration SDK, опционально) |
| Взаимодействие | XR Interaction Toolkit 3.x |
| Ввод | Input System (`Assets/Settings/ChemLabInputActions.inputactions`) |
| Графика | URP (ассет `Assets/Settings/ChemLab_URP.asset`, создаётся Setup-ом) |
| Данные | JSON (TextAsset'ы) — вещества, реакции, уроки |

---

## 2. Важнейший факт: сцена и настройки генерируются кодом

В репозитории **нет** готовой `.unity`-сцены и大部分 настроек — они создаются редакторскими
скриптами. Это осознанное решение: сцена собирается из примитивов кодом, поэтому она
версионируется как исходник и легко пересобирается.

Точка входа — меню **ChemLab** в Unity:

```text
ChemLab/
├── Setup/0. Install/Update Packages       — переустановка пакетов под редактор
├── Setup/1. Apply Project Settings        — Android/ARM64/IL2CPP/Linear/Input System/URP/OpenXR/слои
├── Setup/2. Build Laboratory Scene        — генерация сцены + префабов + Build Settings
├── Build/Build Android APK                — сборка Builds/VirtualChemistryLab.apk
└── Build/Build And Run                    — собрать и запустить на подключённом шлеме
```

Соответствие меню и кода:

| Меню | Файл |
|---|---|
| Setup/0 | `Assets/Editor/ChemLabPackages.cs` |
| Setup/1 | `Assets/Editor/ChemLabProjectSetup.cs` |
| Setup/2 | `Assets/Editor/SceneBuilder.cs` (использует `LabFactory.cs`, `UiFactory.cs`, `AppearanceFactory.cs`) |
| Build/* | `Assets/Editor/ChemLabApkBuilder.cs` |

**Правило:** не править сцену руками. Менять `SceneBuilder.cs` / `LabFactory.cs` и пересобирать
сцену (Setup → 2). Ручные правки сцены будут потеряны при пересборке.

Порядок работы после `git clone` в свежем Unity: **Setup 0 (при проблемах) → Setup 1 →
перезапуск → Setup 2 → Build**. Полностью — в `docs/BUILD_APK.md`.

---

## 3. Карта репозитория

```text
vr_chimestry/
├── README.md                     — обзор проекта и быстрый старт
├── PROJECT.md                    — этот файл
├── docs/BUILD_APK.md             — сборка APK + установка на PICO 4 Ultra
├── Packages/manifest.json        — зависимости Unity (URP, XRI 3, OpenXR, Input System)
├── ProjectSettings/ProjectVersion.txt
│
└── Assets/
    ├── Editor/                   — РЕДАКТОРСКИЕ скрипты (меню ChemLab, генерация всего)
    │   ├── ChemLabPackages.cs        — установка/обновление пакетов
    │   ├── ChemLabProjectSetup.cs    — настройки платформы/графики/XR/слоёв
    │   ├── SceneBuilder.cs           — сборка сцены LaboratoryScene из кода
    │   ├── LabFactory.cs             — фабрика оборудования (сосуды, приборы, планшет, кнопки полки)
    │   ├── UiFactory.cs              — хелперы world-space uGUI (канвасы, кнопки, дисплеи)
    │   ├── AppearanceFactory.cs      — материалы URP (стекло, металл, жидкости…)
    │   └── ChemLabApkBuilder.cs      — сборка APK из меню
    │
    ├── Scripts/                  — RUNTIME-код (без UnityEditor!)
    │   ├── Core/
    │   │   ├── GameManager.cs        — singleton: связывает все подсистемы, режимы, уведомления
    │   │   ├── ExperimentEvents.cs   — шина событий (grab/reaction/instrument/safety/…), AppMode
    │   │   ├── LabObject.cs          — базовый объект лаборатории: тип, масса, разбитие стекла
    │   │   └── RigLocomotion.cs      — стики: перемещение + snap-поворот
    │   ├── Chemistry/
    │   │   ├── SubstanceData.cs      — модель вещества + JSON-формат
    │   │   ├── ReactionData.cs       — модель реакции + JSON-формат
    │   │   ├── ChemistryDatabase.cs  — загрузка всех JSON в словари
    │   │   ├── ChemicalContainer.cs  — СОСУД: содержимое, pH, цвет/индикатор, температура
    │   │   ├── ReactionEngine.cs     — движок: подбирает реакции, считает продукты/тепло
    │   │   └── ContainerVisuals.cs   — визуал: уровень жидкости, цвет, осадок, пузырьки газа
    │   ├── Equipment/
    │   │   ├── ContainerZone.cs      — триггер-зона жидкости; поиск сосудов в мире
    │   │   ├── PourableGrab.cs       — переливание наклоном (главная механика)
    │   │   ├── Pipette.cs            — пипетка: набор/выливание триггером, счётчик мл
    │   │   ├── DigitalScale.cs       — весы (масса объектов на платформе, кнопка ТАРА)
    │   │   ├── PHMeter.cs            — pH-метр + Термометр (датчики с зоной погружения)
    │   │   └── ReagentDispenser.cs   — кнопки полки реактивов; SinkMarker; WasteBin (утилизация)
    │   ├── Lessons/
    │   │   ├── LessonData.cs         — модель урока/шага/проверки/теста + JSON-формат
    │   │   └── LessonManager.cs      — стейт-машина урока, проверки, подсказки, оценка
    │   ├── Safety/
    │   │   └── SafetySystem.cs       — нарушения ТБ с объяснением «почему нельзя»
    │   ├── Save/
    │   │   ├── SaveManager.cs        — PlayerProgress → persistentDataPath/chemlab_save.json
    │   │   └── LabJournal.cs         — лабораторный журнал (записи обо всём)
    │   └── UI/
    │       ├── VrText.cs             — системный шрифт ОС (кириллица!) + BillboardText (3D-метки)
    │       └── TabletUI.cs           — планшет: страницы, кнопки, баннер уведомлений
    │
    ├── Chemistry/Data/           — ДАННЫЕ ХИМИИ (JSON, TextAsset)
    │   ├── Substances/substances_core.json   — 12 веществ
    │   └── Reactions/reactions_core.json     — 4 реакции
    ├── Lessons/                  — ДАННЫЕ УРОКОВ (JSON)
    │   ├── lesson_01_introduction.json
    │   └── lesson_02_neutralization.json
    ├── Settings/                 — ChemLabInputActions.inputactions (+ URP-ассеты после Setup 1)
    ├── Prefabs/Equipment/        — префабы оборудования (генерирует Setup → 2)
    ├── Scenes/                   — LaboratoryScene.unity (генерирует Setup → 2)
    └── Art/ Audio/ Materials/ UI/ — зарезервировано под будущий контент
```

---

## 4. Архитектура runtime: поток данных

```text
   Контроллер (OpenXR → Input System → XRI 3)
        │  Grip = захват,  Trigger = активация/кнопки
        ▼
   XR Origin + DirectInteractor/RayInteractor   (сцена, собирается SceneBuilder-ом)
        │
        ▼
   Физические действия пользователя
   ├── PourableGrab: наклонил сосуд >55° → переливание в сосуд под носиком
   ├── Pipette: триггер над жидкостью → набор, над сосудом → выливание
   ├── DigitalScale / PHMeter / Thermometer: зона → показание
   └── ReagentDispenser / WasteBin: выдача стаканчика / утилизация
        │
        ▼
   ChemicalContainer (у каждого сосуда)   ←— состояние: contents[вещество→мл], T°C, осадок
        │  NotifyChanged()
        ▼
   ReactionEngine (тик 4 Гц): ищет реакции, где есть все реагенты
        │  конвертирует реагенты → продукты (раствор/осадок/газ) + тепло
        ▼
   ExperimentEvents (шина событий)  ──►  ContainerVisuals (жидкость/осадок/пузырьки)
        │                          ──►  GameManager → TabletUI (объяснение реакции)
        │                          ──►  LessonManager (проверки шагов заданий)
        │                          ──►  LabJournal → SaveManager (журнал, прогресс)
        └─ SafetySystem (пролив/разбитие/слив в раковину/перелив → объяснение + штраф)
```

Ключевая идея: **логика химии не знает про XR и UI**, а XR и UI не знают про химию —
они общаются через `ChemicalContainer` и событие `ExperimentEvents`. Новое оборудование
достаточно «вставить» в эту схему, не трогая ядро.

---

## 5. Химическая модель (осознанные упрощения)

Полная CFD/стехиометрия не нужна — модель учебная:

- **Единица измерения — мл** (для твёрдых тоже: количество = объём, масса = мл × плотность).
  Так переливание, весы и концентрации считаются одними числами.
- **Contents** — список `(substanceId, ml)`. Продукты реакций «растворяются» в общий объём.
- **pH** — по балансу эквивалентов: у каждого вещества `phStrength` (−1 у 1M HCl, +1 у 1M NaOH).
  `pH = -log10(|кислота−щёлочь|/объём)` с нейтралью 7. Полуколичественно, но честно
  показывает сдвиг pH при титровании.
- **Индикаторы** — вещества с `isIndicator`: их наличие меняет цвет жидкости по pH
  (`universal` — 6 цветов шкалы, `phenolphthalein` — малиновый при pH ≥ 8.2).
- **Реакции** — декларативные: «если есть реагенты A,B → превратить в C,D с теплом».
  Осадок — отдельный счётчик `precipitateMl` (не смешивается, не переливается),
  газ — таймер пузырьков `gasTimer`.
- **Температура** — реакция даёт `heatPerUnitMl °C` на каждую единицу; затем пассивный
  дрейф к 21 °C. Нагревательных приборов в MVP нет (см. Roadmap).
- **Переливание** — доля от всего содержимого (`TakeProportional`): содержимое переносится
  пропорционально. «Осадок остаётся» — специально (фильтрация в будущих фазах).

Где расширять: `Assets/Chemistry/Data/*.json` (контент), `ReactionEngine.TryReact`
(условия типа температуры/катализатора), `ChemicalContainer` (новые вычисляемые свойства).

---

## 6. Данные отдельно от кода (как добавить контент)

### 6.1. Формат вещества (`Assets/Chemistry/Data/Substances/*.json`)

```json
{
  "substances": [
    {
      "id": "kmno4_solution",
      "name": "Перманганат калия",
      "formula": "KMnO₄ (р-р)",
      "state": "liquid",
      "colorHex": "#7A1FA2CC",
      "density": 1.03,
      "phStrength": 0.0,
      "molarMass": 158.03,
      "boilingPoint": 100.0,
      "meltingPoint": 0.0,
      "hazards": "Окислитель!",
      "description": "..."
    }
  ]
}
```

Поля описаны в `SubstanceData.cs`. **id уникален**, `colorHex` = `#RRGGBBAA`.

### 6.2. Формат реакции (`Assets/Chemistry/Data/Reactions/*.json`)

```json
{
  "reactions": [
    {
      "id": "hcl_plus_nahco3",
      "name": "Кислота + сода",
      "explanation": "Объяснение ДЛЯ ИГРОКА, почему идёт реакция",
      "reactants":  [ { "substance": "hcl_1m", "ratio": 1.0 },
                      { "substance": "nahco3_solid", "ratio": 1.0 } ],
      "products":   [ { "substance": "nacl_aq", "ratio": 1.0 },
                      { "substance": "water", "ratio": 1.0 },
                      { "substance": "co2", "ratio": 1.0, "gas": true } ],
      "heatPerUnitMl": 0.02,
      "hazardNote": "Если не пусто — показывается предупреждение",
      "requiresFumeHood": false
    }
  ]
}
```

`precipitate: true` — продукт падает осадком; `gas: true` — улетает с пузырьками.

### 6.3. Формат урока (`Assets/Lessons/*.json`)

Урок = `theory[]` + `steps[]` (type `theory`/`task`) + `quiz[]` + `accuracy`.
Проверка шага — декларативная `check`:

| `kind` | Поля | Что проверяет |
|---|---|---|
| `grab_object` | `objectType` | пользователь взял объект типа beaker/cup/pipette/… |
| `place_on_scale` | `objectType` | объект поставлен на весы |
| `instrument_read` | `instrument` | ph / temperature / mass — показание снято |
| `reaction_occurred` | `reactionId` | реакция произошла (id из реакций) |
| `container_has` | `substance`, `containerType?`, `min`, `max?` | в сосуде есть ≥min мл вещества |
| `pipette_has` | `min` | пипетка набрала ≥min мл |
| `ph_between` | `min`, `max`, `containerType?` | pH в диапазоне (итог титрования) |
| `temperature_between` | `min`, `max` | температура в диапазоне |
| `dispose_waste` | — | содержимое утилизировано в ведро |

Каждый `task` может иметь до 3 `hints` (уровни подсказок).

### 6.4. Подключение новых данных

**Ничего注册 не нужно**: `SceneBuilder` автоматически подхватывает **все** `t:TextAsset`
из `Assets/Chemistry/Data/Substances`, `Assets/Chemistry/Data/Reactions` и `Assets/Lessons`
(см. `SceneBuilder.BuildSystems → FindTextAssets`). Просто:
1. добавьте JSON-файл в нужную папку;
2. пересоберите сцену (Setup → 2) или вручную добавьте TextAsset в `ChemistryDatabase` на объекте `Systems`.

Но чтобы новое вещество можно было **взять на полке**, добавьте его кнопку-диспенсер
в `SceneBuilder.BuildReagentShelf` (массив `defs`).

---

## 7. XR: как устроен ввод

- Действия заданы в `Assets/Settings/ChemLabInputActions.inputactions`:
  `XRI HMD/Position|Rotation`, `XRI Left|RightHand Controller/…`, `XRI Left|RightHand
  Interaction/Select(Grip)|Activate(Trigger)|UI Press`, `XRI LeftHand Locomotion/Move`,
  `XRI RightHand Locomotion/Turn`.
- `SceneBuilder` назначает действия на интеракторы (`selectInput`, `activateInput`,
  `uiPressInput`) и `TrackedPoseDriver`. Привязки — стандартные `<XRController>{LeftHand}/…`,
  работают на PICO через OpenXR (профиль PICO или Oculus Touch).
- Захват: `XRGrabInteractable` (Kinematic, без броска, dynamic attach) — конфигурация в
  `LabFactory.ConfigureGrab`.
- UI планшета/полки: world-space uGUI + `TrackedDeviceGraphicRaycaster` + `XRUIInputModule`,
  нажимается лучом контроллера.
- **Слои физики** (создаёт Setup → 1): `Glassware` (сосуды) и `Probes` (пипетка/термометр/
  электрод) **не сталкиваются** между собой — иначе кончик пипетки физически не войти в сосуд
  (`GameManager.SetupPhysicsLayers`). `LiquidZone` — триггеры-зоны жидкости для приборов.

---

## 8. UI: почему без TextMeshPro

Шрифт берётся из системных (`Font.CreateDynamicFontFromOSFont`, Roboto на Android) —
это гарантирует **кириллицу** без импорта TMP-ассетов и настройки глифов.
Всё текстовое UI — legacy uGUI `Text` (`VrTextFactory.Configure`) и `TextMesh`
(`BillboardText` — плавающие метки над сосудами). Если будете менять UI — не
импортируйте TMP ради одного шрифта, это сломает простоту сборки.

---

## 9. Сохранения и журнал

- `SaveManager`: `Application.persistentDataPath/chemlab_save.json` — завершённые уроки,
  лучшие оценки, записи журнала (последние 60).
- `LabJournal.AddEntry(...)` — вызывается автоматически: реакция (GameManager), нарушение
  ТБ (SafetySystem), завершение урока с оценкой (LessonManager), утилизация.
- Просмотр: планшет → «ЛАБОРАТОРНЫЙ ЖУРНАЛ».

---

## 10. Конвенции и правила для агентов (важно)

1. **Процесс — маленькими вехами.** Каждая веха: код → компилируется → собирается APK →
   проверяется на шлеме → коммит. Не писать «100 файлов сразу» (правило из ТЗ §39).
2. **Данные отдельно от кода.** Новые вещества/реакции/уроки — только JSON. Ядро не меняется.
3. **Всё через события.** Новая механика публикует события в `ExperimentEvents`; уроки,
   журнал и UI подписываются. Не вызывайте UI напрямую из механики (исключение — GameManager.ShowNotification).
4. **Ошибки объясняются.** Любое нарушение/ошибка игрока → текст «почему так нельзя» + как
   правильно (`SafetySystem.Report` — образец). Просто «ERROR» — не по ТЗ.
5. **Сцена — только из кода.** Правки сцены делаются в `SceneBuilder`/`LabFactory`.
6. **Runtime-код без UnityEditor**; редакторский — только в `Assets/Editor/`.
7. **Никаких текстов в коде UI**, кроме служебных: учебные тексты — в JSON урока,
   сообщения систем — через `ShowNotification`/`SafetySystem`.
8. **Производительность PICO 4 Ultra**: примитивы, ≤1 realtime-свет, URP MSAA 4x, HDR off.
   Не добавлять пост-процессы/рефлекшены без нужды. Следить за числом реальных коллайдеров.
9. **Коммиты**: значимое изменение → коммит + push. Сообщения — по конвенции
   `feat|fix|refactor|docs(scope): суть`.
10. После изменений в редакторских генераторах всегда проверяйте, что полная цепочка
    `Setup 1 → Setup 2 → Build` выполняется с нуля (чистая сцена/чистые ассеты).

---

## 11. Текущий статус (Milestone 1 — MVP-вертикаль)

Реализовано (см. README «Что уже работает»):

- ✅ XR: захват/перенос (Grip), луч + кнопки (Trigger), локомоция, виброотклик
- ✅ Химия: 12 веществ, 4 реакции, pH-модель, индикаторы, температура, осадок, газ
- ✅ Оборудование: стакан 250 мл, пробирки + штатив, стаканчик 100 мл (одноразовый),
  пипетка 10 мл, весы (с тарой), pH-метр с электродом, термометр, полка реактивов,
  кран с водой, контейнер отходов, раковина (маркер нарушений)
- ✅ Уроки 1–2: теория → задания с автопроверкой → тест → оценка (Accuracy/Safety/Procedure)
- ✅ Подсказки 3 уровней, объяснение реакций и ошибок ТБ
- ✅ Планшет: меню, уроки, подсказки, результаты, журнал, справка
- ✅ Свободная лаборатория; сохранение прогресса (JSON)
- ✅ Инструменты: Setup/SceneBuilder/APK-меню, input actions

**Известные ограничения MVP** (осознанные, не баги):

- Вся графика — стилизованные примитивы (замена на модели — отдельная фаза, папка Art).
- Нагрева/охлаждения приборами нет (только тепло реакций), мешалки/бюретки/центрифуги нет.
- Проверка `ph_between` смотрит все подходящие сосуды (не конкретный) — для MVP достаточно.
- Локомоция без телепорта; без hands-tracking (только контроллеры).
- Аудио отсутствует (папка Audio зарезервирована).
- APK собирается локально в Unity (в CI/песочнице Unity недоступен).

## 12. Roadmap (фазы из ТЗ)

| Фаза | Что добавить | Где расширять |
|---|---|---|
| PHASE 7 | Бюретка + титрование, магнитная мешалка, горелка/горячая плита (нагрев), центрифуга, фильтрация (воронка+фильтр: осадок остаётся, фильтрат проходит), дистиллятор (температуры кипения уже есть в данных), спектрофотометр | новые классы в `Scripts/Equipment` + `SceneBuilder`/`LabFactory`; реакции — JSON |
| PHASE 6+ | Больше уроков (3–10 из ТЗ): растворы, pH трёх неизвестных, фильтрация, титрование, дистилляция, центрифуга, спектрофотометрия | только JSON в `Assets/Lessons` |
| PHASE 8 | Режим «Challenges» (задача без инструкции), разблокировка оборудования/реактивов | `AppMode`, `LessonManager`, данные |
| — | Вытяжной шкаф (проверка `requiresFumeHood` уже в модели реакций) | `SafetySystem` + SceneBuilder |
| — | Виртуальный преподаватель/голос, тесты с несколькими вопросами | `LessonManager`, UI |
| PHASE 9–10 | Оптимизация (baked light, LOD, пул объектов), финальное тестирование | — |

Расширяемость архитектуры уже заложена: `AppMode` (Lessons/FreeLab/…), декларативные
реакции и проверки, событийная шина, генерируемая сцена.
