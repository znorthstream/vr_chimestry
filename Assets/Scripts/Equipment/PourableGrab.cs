using ChemLab.Chemistry;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ChemLab.Equipment
{
    /// <summary>
    /// Сосуд, который можно взять и наклонить, чтобы перелить содержимое.
    /// При наклоне больше порога жидкость перетекает в сосуд-приёмник под носиком
    /// (упрощённая модель переливания, раздел 6 ТЗ).
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(ChemicalContainer))]
    public class PourableGrab : MonoBehaviour
    {
        [Header("Переливание")]
        [Tooltip("Угол наклона, при котором начинается переливание, градусы")]
        public float pourAngle = 55f;

        [Tooltip("Скорость переливания при полном наклоне, мл/с")]
        public float maxPourRate = 45f;

        [Tooltip("Высота точки выливания (носик) над центром объекта")]
        public float spoutHeight = 0.04f;

        private XRGrabInteractable _grab;
        private ChemicalContainer _container;
        private LabObject _labObject;
        private BillboardText _label;
        private float _lastSpillReport;

        public bool IsGrabbed { get; private set; }

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _container = GetComponent<ChemicalContainer>();
            _labObject = GetComponent<LabObject>();
        }

        void OnEnable()
        {
            _grab.selectEntered.AddListener(OnGrab);
            _grab.selectExited.AddListener(OnRelease);
        }

        void OnDisable()
        {
            _grab.selectEntered.RemoveListener(OnGrab);
            _grab.selectExited.RemoveListener(OnRelease);
        }

        private void OnGrab(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
        {
            IsGrabbed = true;
            Experiment.CoreEventsHelper.Grabbed(_labObject);

            // Лёгкая вибрация контроллера как отклик на захват
            if (args.interactorObject is XRBaseInputInteractor input)
                input.SendHapticImpulse(0.35f, 0.04f);

            if (_labObject != null && _label == null)
                _label = _labObject.EnsureLabel();
            if (_label != null) _label.gameObject.SetActive(true);
        }

        private void OnRelease(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
        {
            IsGrabbed = false;
            Experiment.CoreEventsHelper.Released(_labObject);
            if (_label != null && _container.TotalVolume <= 0f) _label.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!IsGrabbed || _container == null || _container.TotalVolume <= 0f) return;

            float tilt = Vector3.Angle(transform.up, Vector3.up);
            if (tilt < pourAngle) return;

            float rate = maxPourRate * Mathf.Clamp01((tilt - pourAngle) / 35f);
            float amount = rate * Time.deltaTime;
            var spoutPos = transform.position + transform.up * spoutHeight;

            var destZone = ContainerZone.FindBelow(spoutPos, 0.4f);
            if (destZone != null && destZone.container != _container)
            {
                PourInto(destZone.container, amount);
                return;
            }

            // Мимо приёмника — проверяем раковину и проливаем
            if (Physics.Raycast(spoutPos, Vector3.down, out var hit, 0.45f, ~0, QueryTriggerInteraction.Collide))
            {
                bool isSink = hit.collider.GetComponentInParent<SinkMarker>() != null;
                var taken = _container.TakeProportional(amount);
                if (taken.Count > 0 && Time.time - _lastSpillReport > 5f)
                {
                    _lastSpillReport = Time.time;
                    string what = _container.DescribeContents();
                    Safety.SafetySystem.ReportSpill(isSink, what);
                }
            }
        }

        private void PourInto(ChemicalContainer dest, float amount)
        {
            var taken = _container.TakeProportional(amount);
            foreach (var unit in taken)
                dest.Add(unit.substanceId, unit.ml);
        }
    }
}

namespace ChemLab.Experiment
{
    /// <summary>Вспомогательные переходники для событий (чтобы не тянуть лишние using).</summary>
    public static class CoreEventsHelper
    {
        public static void Grabbed(Core.LabObject obj) => Core.ExperimentEvents.RaiseObjectGrabbed(obj != null ? obj.objectKind : "unknown");
        public static void Released(Core.LabObject obj) => Core.ExperimentEvents.RaiseObjectReleased(obj != null ? obj.objectKind : "unknown");
    }
}
