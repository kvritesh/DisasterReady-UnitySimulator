using UnityEngine;
using UnityEngine.InputSystem;

namespace DisasterReady.Player
{
    /// <summary>
    /// Simple, reliable third-person exploration controller.
    /// Editor/desktop fallback: WASD to move (relative to camera facing), gravity via CharacterController.
    /// Reads the new Input System directly (Keyboard.current) so no .inputactions asset wiring is required.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float MoveSpeed = 6.5f;
        public float SprintSpeed = 10.5f;
        public float Acceleration = 14f;
        public float Deceleration = 20f;
        public float TurnSmoothing = 12f;
        public float Gravity = -18f;

        [Header("Jump")]
        [Tooltip("Approximate peak jump height in meters.")]
        public float JumpHeight = 2f;

        [Header("References")]
        public Transform CameraPivot;

        /// <summary>Set false by GameBootstrapper until the player has clicked past the title screen.</summary>
        public bool ControlsEnabled = true;

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _currentGroundSpeed;
        private Vector3 _lastMoveDir = Vector3.forward;

        public System.Action<Vector3> OnMoved;

        /// <summary>
        /// 0..1 normalized ground-speed readout for the current frame (0 = idle, ~0.62 = walk, 1.0 = sprint).
        /// Used by animation systems (both ProceduralCharacterAnimator and Animator blend trees).
        /// </summary>
        public float CurrentSpeed01 { get; private set; }

        /// <summary>Current ground speed in m/s.</summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>True while the player is actively moving on the ground this frame.</summary>
        public bool IsMoving { get; private set; }

        /// <summary>True while the sprint key is held and the player is moving.</summary>
        public bool IsSprinting { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            if (CameraPivot == null && Camera.main != null)
            {
                CameraPivot = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (!ControlsEnabled)
            {
                CurrentSpeed01 = 0f;
                CurrentSpeed = 0f;
                IsMoving = false;
                IsSprinting = false;
                _currentGroundSpeed = 0f;
                return;
            }

            Vector2 input = ReadMoveInput();
            bool sprintPressed = ReadSprintInput();

            Transform cam = CameraPivot;
            if (cam == null && Camera.main != null) cam = Camera.main.transform;

            Vector3 camForward = cam != null ? cam.forward : Vector3.forward;
            Vector3 camRight = cam != null ? cam.right : Vector3.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * input.y + camRight * input.x;
            float inputMag = Mathf.Clamp01(moveDir.magnitude);

            if (inputMag > 0.001f)
            {
                _lastMoveDir = moveDir.normalized;
                Quaternion targetRot = Quaternion.LookRotation(_lastMoveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * TurnSmoothing);
            }

            bool isGrounded = _controller.isGrounded;
            if (isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2.5f;
            }

            if (isGrounded && ReadJumpInput())
            {
                _verticalVelocity = Mathf.Sqrt(2f * JumpHeight * -Gravity);
            }

            _verticalVelocity += Gravity * Time.deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, -35f);

            float maxTargetSpeed = sprintPressed ? SprintSpeed : MoveSpeed;
            float targetSpeed = maxTargetSpeed * inputMag;
            float rate = targetSpeed > _currentGroundSpeed ? Acceleration : Deceleration;
            _currentGroundSpeed = Mathf.MoveTowards(_currentGroundSpeed, targetSpeed, rate * Time.deltaTime);
            if (inputMag < 0.001f && _currentGroundSpeed < 0.05f) _currentGroundSpeed = 0f;

            Vector3 velocity = _lastMoveDir * _currentGroundSpeed;
            velocity.y = _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);

            IsMoving = _currentGroundSpeed > 0.1f && inputMag > 0.001f;
            IsSprinting = IsMoving && sprintPressed;
            CurrentSpeed = _currentGroundSpeed;
            CurrentSpeed01 = Mathf.Clamp01(_currentGroundSpeed / SprintSpeed);

            if (IsMoving)
            {
                OnMoved?.Invoke(transform.position);
            }
        }

        private Vector2 ReadMoveInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            float x = 0f, y = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;

            Vector2 v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        private bool ReadSprintInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            return kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        }

        private bool ReadJumpInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            return kb.spaceKey.wasPressedThisFrame;
        }
    }
}
