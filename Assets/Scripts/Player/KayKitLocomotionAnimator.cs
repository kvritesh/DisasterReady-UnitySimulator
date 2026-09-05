using UnityEngine;

namespace DisasterReady.Player
{
    /// <summary>
    /// Drives the KayKit player rig's Animator from PlayerController's existing
    /// public speed API (CurrentSpeed01) - this is the only place that reads
    /// PlayerController for animation purposes; PlayerController itself is never
    /// modified. A single 1D blend-tree parameter ("Speed01") crossfades
    /// Idle -> Walk -> Run, matching how ProceduralCharacterAnimator already
    /// reads the same properties for the old procedural rig.
    ///
    /// Also defensively re-anchors a set of skeleton transforms (Rig_Medium
    /// itself and its "root" child) to their rest local position every frame.
    /// Confirmed via KayKitAnimDiagnostic that BOTH of these carry their own
    /// baked translation keys in the source clips (separate from every
    /// rotation curve that actually drives the limb-swing pose). This project
    /// supplies all world-space movement itself via CharacterController, so
    /// any such baked translation would otherwise make the visual mesh drift/
    /// float/glide out from under the capsule instead of staying planted
    /// under the player. Only position is touched here - rotation curves on
    /// these same transforms, and every curve on every other bone, play
    /// completely unaffected.
    /// </summary>
    [DisallowMultipleComponent]
    public class KayKitLocomotionAnimator : MonoBehaviour
    {
        [Header("Wiring (assigned by EnvironmentBuilder)")]
        public PlayerController Player;
        public Animator Animator;
        [Tooltip("Transforms whose baked-in local translation from the animation clips is neutralized every frame (rest position replayed), so all world-space movement comes from the CharacterController alone. Typically {Rig_Medium, Rig_Medium/root}.")]
        public Transform[] PositionAnchors;
        [Tooltip("The 'CharacterVisual' holder (parent of the instantiated Rogue_Hooded hierarchy). Used once, after the Animator has evaluated its first real pose, to measure the ACTUAL rendered feet position and correct any residual vertical offset - see ApplyOneTimeGroundingCorrection.")]
        public Transform VisualRoot;

        [Header("Blend")]
        [Tooltip("Animator.SetFloat damp time for the Speed01 parameter, so accel/decel doesn't pop between poses.")]
        public float SpeedDamping = 0.12f;

        private static readonly int Speed01Hash = Animator.StringToHash("Speed01");
        private Vector3[] _anchorRestLocalPos;

        // One-time (not per-frame) grounding correction state. The build-time
        // offset in KayKitIntegration.BuildPlayer is computed from the
        // imported mesh's STATIC/bind-pose bounds (KayKitInspectionReport.txt),
        // which do not necessarily match where the feet actually land once
        // the Idle animation clip's own baked hip/root translation is
        // playing. Rather than assume the static figure is right, this
        // measures the REAL combined renderer bounds after the Animator has
        // evaluated a few frames of Idle and nudges VisualRoot so the lowest
        // rendered point sits exactly on the CharacterController's feet
        // plane (the player root's own Y position - see BuildPlayer's
        // controller.center/height, whose bottom is at local Y=0). Applied
        // once and cached, per explicit instruction: no per-frame fake foot
        // positioning/IK.
        private int _groundingFrameCount;
        private bool _groundingApplied;

        private void Start()
        {
            if (PositionAnchors != null && PositionAnchors.Length > 0)
            {
                _anchorRestLocalPos = new Vector3[PositionAnchors.Length];
                for (int i = 0; i < PositionAnchors.Length; i++)
                {
                    if (PositionAnchors[i] != null) _anchorRestLocalPos[i] = PositionAnchors[i].localPosition;
                }
            }
        }

        private void Update()
        {
            if (Player == null || Animator == null) return;
            Animator.SetFloat(Speed01Hash, Player.CurrentSpeed01, SpeedDamping, Time.deltaTime);
        }

        private void LateUpdate()
        {
            // Runs after Animator has evaluated this frame's pose.
            if (PositionAnchors != null && _anchorRestLocalPos != null)
            {
                for (int i = 0; i < PositionAnchors.Length; i++)
                {
                    var t = PositionAnchors[i];
                    if (t == null) continue;
                    if (t.localPosition != _anchorRestLocalPos[i])
                    {
                        t.localPosition = _anchorRestLocalPos[i];
                    }
                }
            }

            if (!_groundingApplied && VisualRoot != null)
            {
                // Wait a couple of frames so the Animator has actually
                // evaluated a real Idle pose (frame 0 can still be the
                // import bind pose before the controller's state machine has
                // ticked) before measuring.
                _groundingFrameCount++;
                if (_groundingFrameCount >= 3)
                {
                    ApplyOneTimeGroundingCorrection();
                    _groundingApplied = true;
                }
            }
        }

        /// <summary>
        /// Measures the CharacterVisual hierarchy's ACTUAL combined renderer
        /// bounds (post-animation, not the static import-time figure the
        /// build-time offset in KayKitIntegration.BuildPlayer assumed) and
        /// shifts VisualRoot vertically so its lowest rendered point lands
        /// exactly on the player's feet plane. The CharacterController's
        /// center=(0,0.95,0)/height=1.9 puts its bottom at local Y=0, i.e.
        /// the player root's own world Y is the feet plane the visual must
        /// touch. One-shot correction only - never runs again after this.
        /// </summary>
        private void ApplyOneTimeGroundingCorrection()
        {
            var renderers = VisualRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) combined.Encapsulate(renderers[i].bounds);

            float actualFeetWorldY = combined.min.y;
            float desiredFeetWorldY = transform.position.y;
            float correction = desiredFeetWorldY - actualFeetWorldY;

            if (Mathf.Abs(correction) > 0.001f)
            {
                VisualRoot.position += new Vector3(0f, correction, 0f);
            }
        }
    }
}
