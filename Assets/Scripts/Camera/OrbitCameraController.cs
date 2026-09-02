using UnityEngine;
using UnityEngine.InputSystem;

namespace DisasterReady.CameraSystem
{
    /// <summary>
    /// Simple, reliable orbit/follow camera.
    /// Editor fallback: hold the left or right mouse button and drag to orbit, scroll to zoom.
    /// </summary>
    public class OrbitCameraController : MonoBehaviour
    {
        [Header("Target")]
        public Transform Target;
        public Vector3 TargetOffset = new Vector3(0f, 1.6f, 0f);

        [Header("Orbit")]
        public float YawSpeed = 150f;
        public float PitchSpeed = 120f;
        public float MinPitch = 12f;
        public float MaxPitch = 75f;
        public float StartYaw = 0f;
        public float StartPitch = 35f;

        [Header("Zoom")]
        public float Distance = 12f;
        public float MinDistance = 4f;
        public float MaxDistance = 26f;
        public float ZoomSpeed = 4f;

        [Header("Feel")]
        public float PositionSmoothing = 10f;

        private float _yaw;
        private float _pitch;
        private Vector3 _currentVelocity;

        public bool ControlsEnabled = true;

        private void Start()
        {
            _yaw = StartYaw;
            _pitch = StartPitch;
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            if (ControlsEnabled)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    bool dragging = mouse.leftButton.isPressed || mouse.rightButton.isPressed;
                    if (dragging)
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

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredPos = Target.position + TargetOffset - rot * Vector3.forward * Distance;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _currentVelocity, 1f / PositionSmoothing);
            transform.LookAt(Target.position + TargetOffset);
        }
    }
}
