using UnityEngine;

namespace DisasterReady.Player
{
    /// <summary>
    /// Fall-off-map safety net. Deliberately a separate, sibling component
    /// rather than an edit to PlayerController - it never reads input or
    /// writes movement, it only watches world-space Y and, on failure,
    /// performs a CharacterController-safe teleport back to the last known
    /// grounded position (never blindly to world origin).
    ///
    /// This is defence in depth alongside EnvironmentBuilder.BuildBoundaryWalls
    /// (the invisible walls around the terrain's true edge): the walls are
    /// meant to stop the player from ever reaching open space in the first
    /// place, but this guard is what recovers gameplay cleanly in case a
    /// player still ends up falling (a wall gap, being launched by physics,
    /// spawning outside bounds, etc.) instead of leaving them to fall forever.
    ///
    /// Fully WebGL-safe: no editor-only APIs, no physics changes, just
    /// Transform/CharacterController reads and one conditional reposition.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FallRecoveryGuard : MonoBehaviour
    {
        [Tooltip("World Y below which the player is considered to have fallen out of the playable area. Well below any valid terrain height (0..85 on this map).")]
        public float FallYThreshold = -15f;

        [Tooltip("How often (seconds) to refresh the remembered safe position while grounded, so tracking cost stays negligible.")]
        public float SafePositionSampleInterval = 0.25f;

        private CharacterController _controller;
        private Vector3 _lastSafePosition;
        private float _sampleTimer;
        private bool _haveSafePosition;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            // Whatever position EnvironmentBuilder spawned the player at is,
            // by construction, a valid on-terrain position - a safe fallback
            // even before the first grounded sample is taken.
            _lastSafePosition = transform.position;
            _haveSafePosition = true;
        }

        private void Update()
        {
            if (transform.position.y < FallYThreshold)
            {
                Recover();
                return;
            }

            _sampleTimer += Time.deltaTime;
            if (_sampleTimer >= SafePositionSampleInterval)
            {
                _sampleTimer = 0f;
                if (_controller != null && _controller.isGrounded)
                {
                    _lastSafePosition = transform.position;
                    _haveSafePosition = true;
                }
            }
        }

        private void Recover()
        {
            Vector3 target = _haveSafePosition ? _lastSafePosition : transform.position;
            // Small upward nudge so the CharacterController doesn't
            // immediately re-trigger whatever ground state caused the fall.
            target += Vector3.up * 0.25f;

            // Standard CharacterController-safe teleport: disable the
            // controller so it doesn't fight the direct transform write,
            // move, then re-enable it.
            if (_controller != null) _controller.enabled = false;
            transform.position = target;
            if (_controller != null) _controller.enabled = true;

            _sampleTimer = 0f;
        }
    }
}
