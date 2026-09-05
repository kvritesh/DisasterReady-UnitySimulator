using UnityEngine;
using UnityEngine.InputSystem;

namespace DisasterReady.CameraSystem
{
    public class OrbitCameraController : MonoBehaviour
    {
        [Header("Target")]
        public Transform Target;
        public Vector3 TargetOffset = new Vector3(0f, 1.8f, 0f);

        [Header("Orbit")]
        public float YawSpeed = 140f;
        public float PitchSpeed = 100f;
        public float MinPitch = -10f;
        public float MaxPitch = 65f;
        public float StartYaw = 0f;
        public float StartPitch = 18f;

        [Header("Zoom")]
        public float Distance = 6f;
        public float MinDistance = 2.5f;
        public float MaxDistance = 12f;
        public float ZoomSpeed = 3.5f;

        [Header("Feel")]
        public float PositionSmoothing = 14f;

        [Header("Collision")]
        public bool EnableCollision = true;
        public float CollisionRadius = 0.25f;
        public LayerMask CollisionMask = ~0;

        [Header("Legacy compatibility")]
        public bool AutoFollowYaw = false;
        public DisasterReady.Player.PlayerController PlayerRef;

        private float _yaw;
        private float _pitch;
        private Vector3 _currentVelocity;
        private float _currentCollisionDistance;

        public bool ControlsEnabled = true;

        private void Start()
        {
            _yaw = StartYaw;
            _pitch = StartPitch;
            _currentCollisionDistance = Distance;
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            if (ControlsEnabled)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    bool canOrbit = mouse.rightButton.isPressed || mouse.leftButton.isPressed || Cursor.lockState == CursorLockMode.Locked;
                    if (canOrbit)
                    {
                        Vector2 delta = mouse.delta.ReadValue();
                        _yaw += delta.x * YawSpeed * Time.deltaTime * 0.1f;
                        _pitch -= delta.y * PitchSpeed * Time.deltaTime * 0.1f;
                        _pitch = Mathf.Clamp(_pitch, MinPitch, MaxPitch);
                    }

                    float scroll = mouse.scroll.ReadValue().y;
                    if (Mathf.Abs(scroll) > 0.01f)
                    {
                        Distance -= scroll * ZoomSpeed * 0.02f;
                        Distance = Mathf.Clamp(Distance, MinDistance, MaxDistance);
                    }
                }
            }

            Vector3 focusPoint = Target.position + TargetOffset;
            Quaternion targetRot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 backDir = -(targetRot * Vector3.forward);

            float targetDistance = Distance;

            if (EnableCollision)
            {
                int layerMask = CollisionMask & ~(1 << LayerMask.NameToLayer("UI"));
                if (Physics.SphereCast(focusPoint, CollisionRadius, backDir, out RaycastHit hit, Distance, layerMask, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.transform.IsChildOf(Target))
                    {
                        targetDistance = Mathf.Clamp(hit.distance - 0.15f, MinDistance * 0.5f, Distance);
                    }
                }
            }

            _currentCollisionDistance = Mathf.MoveTowards(_currentCollisionDistance, targetDistance, Time.deltaTime * 25f);
            Vector3 desiredPos = focusPoint + backDir * _currentCollisionDistance;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _currentVelocity, 1f / PositionSmoothing);
            transform.rotation = targetRot;
        }
    }
}
