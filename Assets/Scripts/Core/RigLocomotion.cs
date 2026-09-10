using UnityEngine;
using UnityEngine.InputSystem;

namespace ChemLab.Core
{
    /// <summary>
    /// Простая локомоция: стик левого контроллера — перемещение,
    /// стик правого — плавный поворот на фиксированный угол (snap turn).
    /// Компонент вешается на корень XR Origin.
    /// </summary>
    public class RigLocomotion : MonoBehaviour
    {
        [Header("Действия (назначает редакторский билдер сцены)")]
        public InputActionProperty moveAction;
        public InputActionProperty turnAction;

        [Header("Параметры")]
        public float moveSpeed = 1.3f;
        public float turnAngle = 30f;
        public float turnCooldown = 0.35f;

        [Tooltip("Главная камера (для определения направления взгляда)")]
        public Transform cameraTransform;

        private float _turnTimer;

        void Update()
        {
            if (cameraTransform == null)
            {
                var cam = Camera.main;
                if (cam != null) cameraTransform = cam.transform;
                if (cameraTransform == null) return;
            }

            // Перемещение
            if (moveAction.action != null && moveAction.action.enabled)
            {
                var v = moveAction.action.ReadValue<Vector2>();
                if (v.sqrMagnitude > 0.01f)
                {
                    var fwd = cameraTransform.forward;
                    fwd.y = 0f;
                    fwd.Normalize();
                    var right = cameraTransform.right;
                    right.y = 0f;
                    right.Normalize();
                    var delta = (fwd * v.y + right * v.x) * (moveSpeed * Time.deltaTime);
                    transform.position += delta;
                }
            }

            // Snap-поворот
            _turnTimer -= Time.deltaTime;
            if (turnAction.action != null && turnAction.action.enabled && _turnTimer <= 0f)
            {
                var t = turnAction.action.ReadValue<Vector2>();
                if (Mathf.Abs(t.x) > 0.7f)
                {
                    var camPos = cameraTransform.position;
                    transform.RotateAround(camPos, Vector3.up, -turnAngle * Mathf.Sign(t.x));
                    _turnTimer = turnCooldown;
                }
            }
        }
    }
}
