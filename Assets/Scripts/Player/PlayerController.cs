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
        public float TurnSmoothing = 12f;
        public float Gravity = -18f;

        [Header("References")]
        public Transform CameraPivot;

        /// <summary>Set false by GameBootstrapper until the player has clicked past the title screen.</summary>
        public bool ControlsEnabled = true;

        private CharacterController _controller;
        private float _verticalVelocity;

        public System.Action<Vector3> OnMoved;

        /// <summary>
        /// 0..1 normalized ground-speed readout for the current frame, purely for
        /// presentation (e.g. ProceduralCharacterAnimator's walk-cycle blend).
        /// Does not feed back into movement in any way.
        /// </summary>
        public float CurrentSpeed01 { get; private set; }

        /// <summary>True while the player is actively moving on the ground this frame. Presentation-only, same as CurrentSpeed01.</summary>
        public bool IsMoving { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!ControlsEnabled)
            {
                CurrentSpeed01 = 0f;
                IsMoving = false;
                return;
            }

            Vector2 input = ReadMoveInput();
            Vector3 camForward = CameraPivot != null ? CameraPivot.forward : Vector3.forward;
            Vector3 camRight = CameraPivot != null ? CameraPivot.right : Vector3.right;
            camForward.y = 0f; camForward.Normalize();
            camRight.y = 0f; camRight.Normalize();

            Vector3 moveDir = camForward * input.y + camRight * input.x;

            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * TurnSmoothing);
            }

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -1f;
            }
            _verticalVelocity += Gravity * Time.deltaTime;

            Vector3 velocity = moveDir * MoveSpeed;
            velocity.y = _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);

            IsMoving = moveDir.sqrMagnitude > 0.001f;
            CurrentSpeed01 = Mathf.Clamp01(moveDir.magnitude);

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
    }
}
