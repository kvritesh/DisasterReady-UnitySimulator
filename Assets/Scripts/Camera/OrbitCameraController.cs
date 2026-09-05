using UnityEngine;
using UnityEngine.InputSystem;

namespace DisasterReady.CameraSystem
{
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

        [Header("Auto-follow (final presentation pass)")]
        [Tooltip(
            "Locked-target auto-follow, redesigned to be mathematically incapable " +
            "of the old S-circling loop. The earlier approach re-derived the " +
            "camera's target yaw EVERY FRAME from something the camera's own " +
            "rotation had just influenced (the player's live facing / moveDir, " +
            "itself computed from this same camera's forward/right) - that closed " +
            "loop has a nonzero steady-state angular velocity for ANY non-forward " +
            "input, not just S (see the design note below). This version instead " +
            "reads raw WASD directly (never touches PlayerController) and LATCHES " +
            "a target yaw ONCE whenever the held-key combination changes, as " +
            "_yaw-at-that-instant plus a fixed offset for the new input direction. " +
            "Between changes the target is perfectly constant, so easing toward it " +
            "is an ordinary convergent first-order system that provably stops " +
            "rotating once it arrives - it is never re-fed by its own output. " +
            "Backward-ish input (S, and the S+A/S+D back-diagonals - anything " +
            "outside ForwardDominantAngle of straight ahead) is excluded from " +
            "engaging auto-follow at all, so holding S can never rotate the camera " +
            "by so much as one degree."
        )]
        public bool AutoFollowYaw = true;
        public float AutoFollowSpeed = 3.2f;
        [Tooltip("Seconds of no manual drag before auto-follow resumes.")]
        public float AutoFollowResumeDelay = 0.5f;
        [Tooltip("Only WASD input within this many degrees of straight-ahead (0) engages auto-follow. 90 = pure strafe still engages; anything back-of-strafe (S and the S+A/S+D diagonals) never does, so backward movement can never rotate the camera.")]
        public float ForwardDominantAngle = 100f;
        [Tooltip("Seconds of no WASD input before the camera latches once more onto the player's current facing (a single re-centre, not a continuous chase).")]
        public float IdleRecenterDelay = 0.6f;
        [Tooltip("Optional - if assigned, auto-follow is disabled while the player is not actually moving (e.g. blocked). Safe to leave unassigned.")]
        public DisasterReady.Player.PlayerController PlayerRef;

        private float _yaw;
        private float _pitch;
        private Vector3 _currentVelocity;
        private float _timeSinceManualDrag = 999f;

        // Locked-target auto-follow state.
        private float _targetYaw;
        private bool _hasTarget;
        private int _lastInputBitmask = -1;
        private float _timeSinceAnyInput = 999f;
        private bool _idleRecenterDone = true;

        public bool ControlsEnabled = true;

        private void Start() { _yaw = StartYaw; _pitch = StartPitch; }

        private void LateUpdate()
        {
            if (Target == null) return;
            if (ControlsEnabled)
            {
                var mouse = Mouse.current;
                bool dragging = false;
                if (mouse != null)
                {
                    dragging = mouse.leftButton.isPressed || mouse.rightButton.isPressed;
                    if (dragging)
                    {
                        Vector2 delta = mouse.delta.ReadValue();
                        _yaw += delta.x * YawSpeed * Time.deltaTime * 0.1f;
                        _pitch -= delta.y * PitchSpeed * Time.deltaTime * 0.1f;
                        _pitch = Mathf.Clamp(_pitch, MinPitch, MaxPitch);
                        _timeSinceManualDrag = 0f;
                        // A manual drag is the player explicitly taking over -
                        // drop any latched auto-follow target so it doesn't
                        // immediately fight the drag once AutoFollowResumeDelay
                        // elapses.
                        _hasTarget = false;
                        _lastInputBitmask = -1;
                    }
                    else
                    {
                        _timeSinceManualDrag += Time.deltaTime;
                    }

                    float scroll = mouse.scroll.ReadValue().y;
                    if (Mathf.Abs(scroll) > 0.01f)
                    {
                        Distance -= scroll * ZoomSpeed * 0.02f;
                        Distance = Mathf.Clamp(Distance, MinDistance, MaxDistance);
                    }
                }

                if (AutoFollowYaw && _timeSinceManualDrag > AutoFollowResumeDelay)
                {
                    UpdateAutoFollow();
                }
            }
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredPos = Target.position + TargetOffset - rot * Vector3.forward * Distance;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _currentVelocity, 1f / PositionSmoothing);
            transform.LookAt(Target.position + TargetOffset);
        }

        /// <summary>
        /// Reads raw WASD directly (deliberately independent of PlayerController
        /// - this file never needs to change if movement code changes) and
        /// latches a new, fixed camera target yaw only on the frame the held
        /// combination changes. See the class-level tooltip for why latching
        /// once (instead of continuously re-deriving the target) is what
        /// makes this immune to the old feedback loop.
        /// </summary>
        private void UpdateAutoFollow()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            bool w = kb.wKey.isPressed || kb.upArrowKey.isPressed;
            bool s = kb.sKey.isPressed || kb.downArrowKey.isPressed;
            bool a = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
            bool d = kb.dKey.isPressed || kb.rightArrowKey.isPressed;

            int bitmask = (w ? 1 : 0) | (a ? 2 : 0) | (s ? 4 : 0) | (d ? 8 : 0);
            bool anyInput = bitmask != 0;

            if (anyInput)
            {
                _timeSinceAnyInput = 0f;
                _idleRecenterDone = false;

                if (bitmask != _lastInputBitmask)
                {
                    // The held combination just changed - latch a new target
                    // ONCE, from the camera's current yaw right now. This is
                    // the crux: the target is never a function of anything
                    // that changes as a result of the camera moving, so it
                    // cannot feed back into itself.
                    float x = (d ? 1f : 0f) - (a ? 1f : 0f);
                    float y = (w ? 1f : 0f) - (s ? 1f : 0f);
                    float offset = Mathf.Atan2(x, y) * Mathf.Rad2Deg; // 0=fwd, +90=right, +-180=back
                    if (Mathf.Abs(offset) <= ForwardDominantAngle)
                    {
                        _targetYaw = _yaw + offset;
                        _hasTarget = true;
                    }
                    else
                    {
                        // Backward / back-diagonal - never engage auto-follow,
                        // so the camera cannot be rotated by holding S.
                        _hasTarget = false;
                    }
                }
            }
            else
            {
                _timeSinceAnyInput += Time.deltaTime;
                if (!_idleRecenterDone && _timeSinceAnyInput > IdleRecenterDelay)
                {
                    // Latch once more onto wherever the player ended up
                    // facing, then stop - this is a single re-centre, not a
                    // per-frame chase of the player's transform.
                    _targetYaw = Target.eulerAngles.y;
                    _hasTarget = true;
                    _idleRecenterDone = true;
                }
            }
            _lastInputBitmask = bitmask;

            if (_hasTarget)
            {
                _yaw = Mathf.LerpAngle(_yaw, _targetYaw, Time.deltaTime * AutoFollowSpeed);
            }
        }
    }
}
