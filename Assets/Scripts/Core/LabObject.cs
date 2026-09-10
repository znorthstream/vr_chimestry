using ChemLab.Chemistry;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ChemLab.Core
{
    /// <summary>
    /// Базовый лабораторный объект: стакан, пробирка, пипетка, планшет и т.д.
    /// Отвечает за тип объекта (для проверок уроков и событий), массу
    /// (с учётом содержимого) и разбитие стеклянной посуды при падении.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class LabObject : MonoBehaviour
    {
        [Header("Идентификация")]
        [Tooltip("Тип объекта: beaker | testtube | pipette | thermometer | tablet | flask ...")]
        public string objectKind = "beaker";

        [Tooltip("Отображаемое имя (RU)")]
        public string displayName = "Стакан";

        [Header("Физика")]
        [Tooltip("Масса пустого объекта, г")]
        public float massG = 80f;

        [Tooltip("Стеклянный предмет — может разбиться при сильном ударе")]
        public bool isGlass = true;

        [Tooltip("Одноразовый (напр., стаканчик с реактивом) — удаляется при утилизации")]
        public bool disposable = true;

        [Header("Разбитие")]
        public float breakSpeed = 2.6f; // м/с относительной скорости

        public bool IsBroken { get; private set; }

        private Rigidbody _rb;
        private XRGrabInteractable _grab;
        private ContainerVisuals _visuals;
        private BillboardText _label;

        /// <summary>Полная масса (объект + содержимое), кг.</summary>
        public float TotalMassKg
        {
            get
            {
                float g = massG;
                var container = GetComponent<ChemicalContainer>();
                if (container != null) g += container.ContentsMassG;
                return g / 1000f;
            }
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.mass = Mathf.Max(0.02f, massG / 1000f);
            _grab = GetComponent<XRGrabInteractable>();
            _visuals = GetComponent<ContainerVisuals>();
            owner_setup();
        }

        private void owner_setup()
        {
            var container = GetComponent<ChemicalContainer>();
            if (container != null) container.owner = this;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!isGlass || IsBroken) return;
            if (collision.relativeVelocity.magnitude < breakSpeed) return;

            Break();
        }

        public void Break()
        {
            if (IsBroken) return;
            IsBroken = true;

            if (_grab != null) _grab.enabled = false;
            if (_visuals != null) _visuals.SetBroken(true);
            if (_label != null) _label.SetText("разбито!");

            // Упрощённая модель: содержимое "пропало" (пролилось)
            var container = GetComponent<ChemicalContainer>();
            if (container != null) container.Clear();

            Safety.SafetySystem.ReportGlassBroken(this);
        }

        /// <summary>Показать/скрыть плавающую метку с описанием (при захвате).</summary>
        public void ShowLabel(bool show)
        {
            if (_label == null) return;
            if (IsBroken) { _label.gameObject.SetActive(true); return; }
            _label.gameObject.SetActive(show);
        }

        /// <summary>Получить/создать плавающую метку над объектом.</summary>
        public BillboardText EnsureLabel()
        {
            if (_label != null) return _label;
            var go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            _label = go.AddComponent<BillboardText>();
            _label.SetText(displayName);
            go.SetActive(false);
            return _label;
        }
    }
}
