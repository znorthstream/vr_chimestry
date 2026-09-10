# Сборка APK и установка на PICO 4 Ultra

Полная инструкция «от нуля до работающего приложения в шлеме». Первый раз займёт ~30–40 минут,
повторные сборки — 2–3 минуты.

---

## ⚡ Сборка в облаке через GitHub Actions (Unity на ПК не нужен)

В репозитории уже есть воркфлоу: `.github/workflows/build-apk.yml` и
`.github/workflows/acquire-unity-license.yml`. CI сам ставит Unity, применяет все
настройки (headless-бутстрап `Assets/Editor/ChemLabCiBootstrap.cs`), генерирует сцену
и собирает APK.

### Шаг 1. Лицензионный файл (5 минут, один раз)

1. Заведите бесплатный аккаунт Unity: https://id.unity.com (Personal-лицензия бесплатна).
2. GitHub репозитория → **Actions** → слева **«Acquire Unity Activation File»** → **Run workflow**.
3. Через ~1 минуту скачайте артефакт **Unity_ActivationFile** (файл `.alf`).
4. Откройте https://license.unity3d.com/manual , войдите, загрузите `.alf`,
   выберите **Personal license** → вам отдадут файл `Unity_vXXXX.ulf`.

### Шаг 2. Секреты (один раз)

GitHub → **Settings → Secrets and variables → Actions → New repository secret**:

| Секрет | Значение |
|---|---|
| `UNITY_LICENSE` | **полное содержимое** файла `Unity_vXXXX.ulf` |
| `UNITY_EMAIL` | email вашего аккаунта Unity |
| `UNITY_PASSWORD` | пароль аккаунта (лучше без спецсимволов — рекомендация GameCI) |

> Владельцам Pro/Plus: вместо `UNITY_LICENSE` добавьте `UNITY_SERIAL`.

### Шаг 3. Сборка

**Actions → «Build APK (Android / PICO 4 Ultra)» → Run workflow → Run.**
Через ~15–30 минут в завершившемся запуске скачайте артефакт
**VirtualChemistryLab-APK** — внутри `VirtualChemistryLab.apk` (debug-подпись,
ставится на шлем без танцев). Каждый push в `main`/`arena/**` пересобирает APK.

> Если job упал с ошибкой про «image not found» — запустите снова, указав вручную
> вход `unityVersion` (например `6000.0.50f1`): образ GameCI для вашей версии Unity
> мог ещё не выйти.

Установка APK на шлем — раздел 8 ниже. Локальная сборка — разделы 2–7 ниже.

---

## 1. Что понадобится

| Что | Где взять |
|---|---|
| ПК: Windows 10/11 (macOS/Linux тоже подходят) | — |
| **Unity Hub** | https://unity.com/download |
| **Unity 6 LTS** (6000.3.x, проект создавался на 6000.3.8f1) | через Unity Hub → Installs |
| Модуль **Android Build Support** (+ OpenJDK + Android SDK & NDK Tools) | Unity Hub → Installs → Add modules |
| **PICO 4 Ultra** + кабель USB-C (передача данных, не только зарядка) | — |
| (опционально) ADB platform-tools | https://developer.android.com/tools/releases/platform-tools |
| (опционально, рекомендуется) PICO Unity Integration SDK | https://developer.picoxr.com/resources |

> В песочнице/CI собрать APK нельзя: сборка требует Unity Editor с Android-модулем.
> Этап сборки выполняется на локальной машине разработчика.

---

## 2. Установка Unity

1. Установите **Unity Hub**, войдите под лицензией (Personal — бесплатно).
2. **Installs → Install Editor → Unity 6 LTS** (любой 6000.3.x или новее 6000.x).
3. На шаге модулей обязательно отметьте:
   - ✅ **Android Build Support**
   - ✅ **OpenJDK**
   - ✅ **Android SDK & NDK Tools**

---

## 3. Открытие проекта

1. Склонируйте репозиторий:
   ```bash
   git clone https://github.com/znorthstream/vr_chimestry.git
   cd vr_chimestry
   ```
2. Unity Hub → **Add → Add project from disk** → выберите папку `vr_chimestry`.
3. Откройте проект выбранной версией Unity 6. Если Hub предупреждает о другой версии —
   согласитесь открыть имеющейся (проект не привязан жёстко).
4. Дождитесь импорта пакетов (первый раз 5–10 минут).

**Если Package Manager ругается на версии пакетов:**
меню **ChemLab → Setup → 0. Install/Update Packages** — переустановит
URP, Input System, XR Interaction Toolkit, OpenXR под вашу версию Unity.

---

## 4. Настройка проекта (одна кнопка)

Меню **ChemLab → Setup → 1. Apply Project Settings (Android/ARM64/OpenXR/URP)**.

Скрипт автоматически:
- переключит платформу на **Android**;
- выставит **IL2CPP + ARM64**, minSdk Android 10;
- включит **Linear color space** (⚠ после этого Unity попросит перезапуск — перезапустите);
- включит **Input System** как обработчик ввода;
- создаст и назначит **URP-ассет** (MSAA 4x, HDR off);
- включит **OpenXR** в XR Plug-in Management, Single Pass Instanced, Depth 16 bit;
- включит профиль контроллеров (PICO-профиль, если стоит PICO SDK, иначе Oculus Touch);
- создаст слои `LiquidZone`, `Glassware`, `Probes`.

**Ручной чеклист, если какой-то пункт не применился** (Project Settings):

| Раздел | Настройка |
|---|---|
| Player → Other Settings → Rendering | Color Space = **Linear**, затем перезапуск Unity |
| Player → Other Settings → Configuration | Scripting Backend = **IL2CPP**, Target Architectures = **ARM64** (галка ARMv7 снята) |
| Player → Other Settings → Configuration | Active Input Handling = **Input System Package (New)** |
| XR Plug-in Management | Установить, вкладка **Android**: включить **OpenXR** |
| XR Plug-in Management → OpenXR | Render Mode = **Single Pass Instanced**, Depth Submission = 16 bit |
| XR Plug-in Management → OpenXR → Interaction Profiles | **PICO Controller Profile** (при PICO SDK) или **Oculus Touch Controller Profile** |
| Tags and Layers | слои `LiquidZone`, `Glassware`, `Probes` |

---

## 5. (Рекомендуется) PICO Unity Integration SDK

Без PICO SDK приложение собирается и работает на PICO 4 Ultra (универсальный OpenXR +
профиль Oculus Touch). PICO SDK добавляет родные профили контроллеров, 90/120 Гц,
hand-tracking и др.

1. Зайдите на https://developer.picoxr.com/resources → скачайте **PICO Unity Integration SDK** (файл `.tgz`).
2. Unity → **Window → Package Manager → + → Install package from tarball** → выберите скачанный файл.
3. **Project Settings → XR Plug-in Management → OpenXR → Interaction Profiles** → включите
   **PICO 4 Controller Profile** (и другие PICO-профили, если нужны).
4. Перезапустите редактор.

---

## 6. Генерация сцены

Меню **ChemLab → Setup → 2. Build Laboratory Scene**.

Скрипт соберёт `Assets/Scenes/LaboratoryScene.unity` (комната, столы, полка реактивов,
оборудование, XR-риг, планшет, системы) и добавит её в Build Settings.
Повторный запуск пересоздаёт сцену с нуля — правьте билдер (`Assets/Editor/SceneBuilder.cs`),
а не сцену руками.

---

## 7. Сборка APK

Меню **ChemLab → Build → Build Android APK**.

Результат: `Builds/VirtualChemistryLab.apk` (подписан отладочным ключом — для установки на шлем достаточно).

Если нужны логи — сборка видна в Console; при ошибке Gradle проверьте, что JDK/SDK
стоят из Unity Hub (Preferences → External Tools → все галки «Installed with Unity»).

---

## 8. Установка на PICO 4 Ultra

### 8.1. Включить режим разработчика на шлеме

1. Наденьте шлем: **Настройки → Общие → О устройстве (Информация)**.
2. **Быстро тапните 7–8 раз по пункту «Версия ПО»** — появится сообщение «Вы стали разработчиком».
3. В настройках появится раздел **«Параметры разработчика»** → включите **«Отладка по USB»**.

### 8.2. Вариант А — через ADB (удобно для разработки)

1. Установите [platform-tools](https://developer.android.com/tools/releases/platform-tools) (содержит `adb`).
2. Подключите шлем кабелем к ПК. В шлеме появится запрос «Разрешить отладку USB?» → **Разрешить**.
3. Проверьте и установите:
   ```bash
   adb devices          # в списке должно быть устройство
   adb install -r Builds/VirtualChemistryLab.apk
   ```
   (`-r` — переустановка поверх, сохраняя данные сохранений)
4. Полезное:
   ```bash
   adb shell am start -n com.virtuallab.chemistry/com.unity3d.player.UnityPlayerActivity  # запустить
   adb logcat -s Unity    # логи Unity во время работы
   adb uninstall com.virtuallab.chemistry
   ```

### 8.3. Вариант Б — без ADB (файлом)

1. Скопируйте APK на шлем одним из способов:
   - по USB как на обычный Android-телефон (режим передачи файлов);
   - отправьте файл себе и откройте **браузером шлема** (скачается в загрузки);
   - загрузите на облако и скачайте из браузера шлема.
2. На шлеме: **Диспетчер файлов** → найдите APK (вкладка **APK**) → тапните → **Установить**.
3. Приложение появится в **Библиотеке** (секция «Неизвестные источники»).

### 8.4. Проверка MVP-сценария (критерий готовности)

Запустите приложение в шлеме и пройдите по чеклисту:

- [ ] Запустилось, видно лабораторию, контроллеры двигают луч
- [ ] Взял пробирку/стакан (Grip), поставил на место
- [ ] Взял пипетку, набрал воду триггером, вылил в пробирку
- [ ] Нажал кнопку на полке реактивов (луч + триггер)
- [ ] Перелил реактив наклоном стаканчика в стакан
- [ ] Смешал HCl + NaOH → увидел объяснение реакции на планшете, раствор нагрелся
- [ ] Измерил pH электродом; универсальный индикатор сменил цвет
- [ ] На планшете запустил Урок 2, прошёл шаги, получил оценку
- [ ] Уронил стакан → получил объяснение ошибки; утилизировал реактив в ведро → похвала
- [ ] Перезапустил приложение → прогресс и журнал сохранились

---

## 9. Траблшутинг

| Симптом | Причина и решение |
|---|---|
| Чёрный экран в шлеме | Не включён OpenXR в XR Plug-in Management (вкладка Android) или не назначен профиль контроллеров |
| Контроллеры не двигают луч | Не назначен Interaction Profile; проверьте Active Input Handling = Input System |
| Объекты розовые | Не создан/не назначен URP-ассет (Setup → 1) или не установлен URP |
| Ошибка Gradle при сборке | Preferences → External Tools: JDK/SDK/NDK/Gradle — все «Installed with Unity» |
| Приложение не появляется в библиотеке | Дождитесь окончания установки; проверьте `adb shell pm list packages | grep virtuallab` |
| Ошибки компиляции при первом открытии | Дождитесь импорта пакетов; при необходимости Setup → 0 |
| Сильный нагрев/лаги на шлеме | Убедитесь, что рендер Single Pass Instanced, MSAA 4x (не 8x), HDR выключен |
| Сохранение «пропало» | Сохранения лежат в памяти приложения; удаление приложения стирает их (`adb uninstall`) |

## 10. Тестирование без шлема (в редакторе)

- **XR Interaction Simulator**: Window → Package Manager → XR Interaction Toolkit → Samples →
  импортируйте «XR Device Simulator» — управление мышью/клавиатурой имитирует контроллеры.
- Химия и уроки работают и без VR: сцена запускается обычным Play Mode (взаимодействие —
  симулятором, UI планшета — мышью).
