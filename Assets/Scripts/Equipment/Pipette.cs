using ChemLab.Chemistry;
using ChemLab.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ChemLab.Equipment
{
    /// <summary>
    /// Пипетка (раздел 12 ТЗ). Взять пипетку, зажать триггер (Activate):
    ///  - кончик в жидкости — набирает;
    ///  - кончик над сосудом — выливает.
    /// Объём отображается плавающей меткой "X / 10 мл".
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(ChemicalContainer))]
    public class Pipette : MonoBehaviour
    {
        [Header("Пипетка")]
        public float capacityMl = 10f;
        public float rateMlPerSec = 5f;

        [Tooltip("Кончик пипетки (создаёт редакторский билдер сцены)")]
        public Transform tip;

        [Tooltip("Радиус поиска жидкости вокруг кончика, м")]
        public float tipRadius = 0.05f;

        private XRGrabInteractable _grab;
        private ChemicalContainer _container;
        private LabObject _labObject;
        private BillboardText _label;
        private bool _activating;
        private bool _wasReading;

        public float VolumeMl => _container != null ? _container.TotalVolume : 0f;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _container = GetComponent<ChemicalContainer>();
            _container.capacityMl = capacityMl;
            _labObject = GetComponent<LabObject>();
        }

        void OnEnable()
        {
            _grab.activated.AddListener(OnActivated);
            _grab.deactivated.AddListener(OnDeactivated);
            _grab.selectEntered.AddListener(OnGrab);
            _grab.selectExited.AddListener(OnRelease);
        }

        void OnDisable()
        {
            _grab.activated.RemoveListener(OnActivated);
            _grab.deactivated.RemoveListener(OnDeactivated);
            _grab.selectEntered.RemoveListener(OnGrab);
            _grab.selectExited.RemoveListener(OnRelease);
        }

        private void OnActivated(ActivateEventArgs args) => _activating = true;
        private void OnDeactivated(ActivateEventArgs args) => _activating = false;

        private void OnGrab(SelectEnterEventArgs args)
        {
            Experiment.CoreEventsHelper.Grabbed(_labObject);
            if (args.interactorObject is XRBaseInputInteractor input)
                input.SendHapticImpulse(0.3f, 0.04f);
            EnsureLabel();
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            Experiment.CoreEventsHelper.Released(_labObject);
        }

        private void EnsureLabel()
        {
            if (_label == null)
            {
                if (_labObject != null) _label = _labObject.EnsureLabel();
                else
                {
                    var go = new GameObject("PipetteLabel");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                    _label = go.AddComponent<BillboardText>();
                }
            }
            _label.gameObject.SetActive(true);
        }

        void Update()
        {
            if (_label != null && _label.gameObject.activeSelf)
                _label.SetText($"Пипетка {_container.TotalVolume:0.#} / {capacityMl:0} мл");

            if (!_activating || tip == null || _container == null) return;
            if (_labObject != null && _labObject.IsBroken) return;

            float dt = Time.deltaTime;

            // 1) Набор: кончик внутри жидкости
            var zone = ContainerZone.FindAt(tip.position, tipRadius);
            if (zone != null && zone.container != _container && zone.container.TotalVolume > 0.1f)
            {
                float free = capacityMl - _container.TotalVolume;
                if (free > 0.01f)
                {
                    float want = Mathf.Min(rateMlPerSec * dt, free);
                    var taken = zone.container.TakeProportional(want);
                    foreach (var unit in taken)
                        _container.Add(unit.substanceId, unit.ml);
                }
                return;
            }

            // 2) Выливание: кончик над сосудом-приёмником
            if (_container.TotalVolume > 0.01f)
            {
                var dest = ContainerZone.FindBelow(tip.position, 0.5f);
                if (dest != null && dest.container != _container)
                {
                    var given = _container.TakeProportional(Mathf.Min(rateMlPerSec * dt, _container.TotalVolume));
                    foreach (var unit in given)
                        dest.container.Add(unit.substanceId, unit.ml);
                }
            }
        }
    }
}
