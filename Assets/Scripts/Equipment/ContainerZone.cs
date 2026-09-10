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

        /// <summary>Луч вниз по вертикали — найти сосуд-приёмник под точкой.</summary>
        public static ContainerZone FindBelow(Vector3 origin, float maxDistance)
        {
            if (Physics.Raycast(origin, Vector3.down, out var hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
                return hit.collider.GetComponentInParent<ContainerZone>();
            return null;
        }
    }
}
