using System.Collections.Generic;
using ChemLab.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.Equipment
{
    /// <summary>
    /// Электронные весы (раздел 19 ТЗ). Суммирует массу всех объектов на платформе
    /// (с учётом содержимого сосудов), показывает значение с точностью до миллиграмма.
    /// Есть кнопка "Тара" для обнуления.
    /// </summary>
    public class DigitalScale : MonoBehaviour
    {
        [Header("Весы")]
        public Text display;

        [Tooltip("Кнопка обнуления (создаёт билдер сцены)")]
        public Button tareButton;

        private readonly HashSet<LabObject> _objects = new HashSet<LabObject>();
        private float _tareG;
        private string _lastReading = "";

        void Awake()
        {
            if (tareButton != null) tareButton.onClick.AddListener(Tare);
        }

        public void Tare()
        {
            _tareG = CurrentMassG();
            UpdateDisplay(true);
        }

        private float CurrentMassG()
        {
            float g = 0f;
            foreach (var o in _objects)
                if (o != null) g += o.TotalMassKg * 1000f;
            return g;
        }

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;
            var lab = rb.GetComponent<LabObject>();
            if (lab == null) return;
            if (_objects.Add(lab))
                ExperimentEvents.RaiseObjectPlacedOnScale(lab.objectKind);
        }

        void OnTriggerExit(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;
            var lab = rb.GetComponent<LabObject>();
            if (lab != null) _objects.Remove(lab);
        }

        void Update() => UpdateDisplay(false);

        private void UpdateDisplay(bool force)
        {
            float g = Mathf.Max(0f, CurrentMassG() - _tareG);
            string reading = $"{g:0.000} г";
            if (reading != _lastReading || force)
            {
                _lastReading = reading;
                if (display != null) display.text = reading;
            }
        }

        /// <summary>Текущее показание в граммах (для уроков/журнала).</summary>
        public float ReadingG => Mathf.Max(0f, CurrentMassG() - _tareG);
    }
}
