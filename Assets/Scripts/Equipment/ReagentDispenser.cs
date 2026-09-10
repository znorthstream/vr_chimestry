using System.Collections.Generic;
using ChemLab.Chemistry;
using ChemLab.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ChemLab.Equipment
{
    /// <summary>
    /// Полка с реактивами: кнопка на полке выдаёт стаканчик с веществом.
    /// Кнопки — uGUI, нажимаются лучом контроллера (триггер).
    /// </summary>
    public class ReagentDispenser : MonoBehaviour
    {
        [Header("Реактив")]
        [Tooltip("id вещества из базы (например \"hcl_1m\")")]
        public string substanceId = "water";

        [Tooltip("Сколько выдавать, мл (твёрдые — считается через плотность)")]
        public float amountMl = 50f;

        [Header("Выдача")]
        [Tooltip("Префаб стаканчика (создаёт билдер сцены)")]
        public GameObject spawnPrefab;

        public Transform spawnPoint;

        [Tooltip("Кнопка на полке")]
        public Button uiButton;

        [Tooltip("Подпись кнопки (создаётся автоматически из базы веществ)")]
        public Text label;

        private static readonly List<GameObject> Spawned = new List<GameObject>();
        private const int MaxSpawned = 14;

        void Awake()
        {
            if (uiButton != null) uiButton.onClick.AddListener(Dispense);
        }

        void Start()
        {
            // В Start база веществ уже загружена (GameManager.Awake)
            RefreshLabel();
        }

        public void RefreshLabel()
        {
            var db = ChemistryDatabaseHolder.Instance;
            var s = db != null ? db.GetSubstance(substanceId) : null;
            if (label != null && s != null) label.text = s.name;
        }

        public void Dispense()
        {
            if (spawnPrefab == null)
            {
                Debug.LogWarning($"[ReagentDispenser] Не назначен префаб для {substanceId}");
                return;
            }

            Spawned.RemoveAll(o => o == null);
            if (Spawned.Count >= MaxSpawned)
            {
                GameManager.Instance?.ShowNotification("Порядок на столе",
                    "Слишком много посуды в лаборатории.\nУтилизируйте лишние стаканчики в контейнер для отходов.",
                    NotificationSeverity.Warning);
                return;
            }

            var pos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.down * 0.1f;
            var go = Instantiate(spawnPrefab, pos, Quaternion.identity);
            go.name = "Reagent_" + substanceId;
            Spawned.Add(go);

            var container = go.GetComponent<ChemicalContainer>();
            if (container != null) container.Add(substanceId, amountMl);

            var visuals = go.GetComponent<ContainerVisuals>();
            if (visuals != null) visuals.Refresh();

            if (go.TryGetComponent<Rigidbody>(out var rb) && spawnPoint != null)
                rb.position = pos;
        }
    }

    /// <summary>
    /// Маркер раковины: слив реактивов в раковину — нарушение техники безопасности.
    /// </summary>
    public class SinkMarker : MonoBehaviour { }

    /// <summary>
    /// Контейнер для отходов. Утилизация содержимого — ПРАВИЛЬНОЕ действие
    /// (система безопасности поощряет). Одноразовая посуда удаляется.
    /// </summary>
    public class WasteBin : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;
            var lab = rb.GetComponent<LabObject>();
            if (lab == null) return;

            var container = rb.GetComponent<ChemicalContainer>();
            bool hadContents = false;
            string what = "";

            if (container != null && (container.TotalVolume > 0.1f || container.precipitateMl > 0.1f))
            {
                what = container.DescribeContents();
                container.Clear();
                hadContents = true;
            }

            if (hadContents || lab.IsBroken)
            {
                ExperimentEvents.RaiseWasteDisposed();
                GameManager.Instance?.ShowNotification("Утилизация",
                    lab.IsBroken
                        ? "Битое стекло утилизировано правильно."
                        : $"Отработанные реактивы утилизированы правильно ({what}).\nТак и нужно поступать вместо слива в раковину.",
                    NotificationSeverity.Success);
            }

            // Одноразовую посуду и осколки убираем, если её не держат
            var grab = rb.GetComponent<XRGrabInteractableProxy>();
            bool isHeld = grab != null && grab.IsSelected;
            if ((lab.disposable || lab.IsBroken) && !isHeld)
                Destroy(rb.gameObject, 0.4f);
        }
    }
}
