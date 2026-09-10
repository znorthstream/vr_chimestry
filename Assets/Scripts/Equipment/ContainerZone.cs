using UnityEngine;

namespace ChemLab.Equipment
{
    /// <summary>
    /// Зона жидкости внутри сосуда (триггер-коллайдер).
    /// По ней пипетка/приборы/переливание находят контейнер в мире.
    /// </summary>
    public class ContainerZone : MonoBehaviour
    {
        public ChemLab.Chemistry.ChemicalContainer container;

        private static int _zoneMask = -1;

        /// <summary>Найти зону жидкости рядом с точкой (слой LiquidZone, с фолбэком на все слои).</summary>
        public static ContainerZone FindAt(Vector3 point, float radius)
        {
            if (_zoneMask == -1)
            {
                _zoneMask = LayerMask.GetMask("LiquidZone");
                if (_zoneMask == 0) _zoneMask = ~0; // слой не настроен — ищем по всем
            }
            var hits = Physics.OverlapSphere(point, radius, _zoneMask, QueryTriggerInteraction.Collide);
            ContainerZone best = null;
            float bestDist = float.MaxValue;
            foreach (var hit in hits)
            {
                var zone = hit.GetComponentInParent<ContainerZone>();
                if (zone == null) continue;
                float d = Vector3.SqrMagnitude(hit.ClosestPoint(point) - point);
                if (d < bestDist) { bestDist = d; best = zone; }
            }
            return best;
        }

        /// <summary>Луч вниз по вертикали — найти сосуд-приёмник под точкой.
        /// Стенки других сосудов "прозрачны" для луча, сплошная мебель — нет.</summary>
        public static ContainerZone FindBelow(Vector3 origin, float maxDistance)
        {
            var hits = Physics.RaycastAll(origin, Vector3.down, maxDistance, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var zone = hit.collider.GetComponentInParent<ContainerZone>();
                if (zone != null) return zone;
                // Стенка чужого сосуда — смотрим глубже (можно лить по краю)
                if (hit.collider.GetComponentInParent<ChemLab.Core.LabObject>() != null) continue;
                return null; // стол / пол / раковина — приёмника нет
            }
            return null;
        }
    }
}
