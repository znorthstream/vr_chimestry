using ChemLab.Core;
using ChemLab.Equipment;
using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.Equipment
{
    /// <summary>
    /// pH-метр (раздел 19 ТЗ). Опустите электрод в раствор —
    /// на дисплее появится значение pH с двумя знаками после запятой.
    /// </summary>
    public class PHMeter : MonoBehaviour
    {
        [Header("pH-метр")]
        public Transform electrodeTip;
        public Text display;
        public float probeRadius = 0.055f;

        private bool _wasReading;

        void Update()
        {
            string text = "pH = ---";

            if (electrodeTip != null)
            {
                var zone = ContainerZone.FindAt(electrodeTip.position, probeRadius);
                if (zone != null && zone.container != null && zone.container.TotalVolume > 2f)
                {
                    float ph = zone.container.ComputePH();
                    text = $"pH = {ph:0.00}\n{zone.container.temperatureC:0.0} °C";
                    if (!_wasReading)
                        ExperimentEvents.RaiseInstrumentRead("ph");
                    _wasReading = true;
                }
                else _wasReading = false;
            }

            if (display != null && display.text != text) display.text = text;
        }
    }

    /// <summary>
    /// Термометр. Держите резервуаром в жидкости — показывает температуру раствора.
    /// Можно брать в руку (grabbable).
    /// </summary>
    public class Thermometer : MonoBehaviour
    {
        [Header("Термометр")]
        public Transform bulb;
        public float probeRadius = 0.05f;
        [Tooltip("Метка над термометром (создаётся автоматически при захвате)")]
        public BillboardText label;

        private bool _wasReading;

        void Update()
        {
            float temp = 21f;
            bool reading = false;

            if (bulb != null)
            {
                var zone = ContainerZone.FindAt(bulb.position, probeRadius);
                if (zone != null && zone.container != null && zone.container.TotalVolume > 2f)
                {
                    temp = zone.container.temperatureC;
                    reading = true;
                    if (!_wasReading)
                        ExperimentEvents.RaiseInstrumentRead("temperature");
                }
            }
            _wasReading = reading;

            if (label != null && label.gameObject.activeSelf)
                label.SetText($"{temp:0.0} °C");
        }
    }
}
