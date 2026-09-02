using UnityEngine;

namespace DisasterReady.Player
{
    /// <summary>
    /// Lightweight, fully procedural walk/idle animation for the low-poly player
    /// character built by EnvironmentBuilder. No animation clips, no Animator
    /// Controller, no rig import - just four limb pivots swung on a sine wave in
    /// code, plus a subtle idle bob/sway on the visual root. Kept intentionally
    /// simple and WebGL-safe: a handful of transform.localRotation writes per
    /// frame, nothing else.
    ///
    /// Reads PlayerController.CurrentSpeed01/IsMoving for presentation only -
    /// this component never touches movement, input, or collision in any way.
    /// </summary>
    public class ProceduralCharacterAnimator : MonoBehaviour
    {
        [Header("Wiring (assigned by EnvironmentBuilder)")]
        public PlayerController Player;
        public Transform VisualRoot;
        public Transform LeftArm;
        public Transform RightArm;
        public Transform LeftLeg;
        public Transform RightLeg;

        [Header("Walk cycle")]
        public float StepsPerSecondAtFullSpeed = 2.4f;
        [Tooltip("Max swing angle (degrees) for legs at full speed.")]
        public float LegSwingDegrees = 32f;
        [Tooltip("Max swing angle (degrees) for arms at full speed.")]
        public float ArmSwingDegrees = 24f;
        [Tooltip("How quickly the walk-cycle amplitude eases toward the target speed, so starting/stopping doesn't snap.")]
        public float AmplitudeSmoothing = 8f;

        [Header("Idle")]
        public float IdleBobHeight = 0.035f;
        public float IdleBobSpeed = 1.4f;
        public float IdleSwaySpeed = 0.9f;
        public float IdleSwayDegrees = 1.5f;

        [Header("Footstep audio (optional, procedural - never required)")]
        public bool EnableFootstepAudio = true;
        [Range(0f, 1f)] public float FootstepVolume = 0.16f;

        private float _phase;
        private float _amplitude01;
        private Quaternion _leftArmRest, _rightArmRest, _leftLegRest, _rightLegRest;
        private Vector3 _visualRootRestLocalPos;
        private AudioSource _footstepSource;
        private AudioClip _footstepClip;
        private bool _lastStepWasLeft;
        private float _idleT;

        private void Awake()
        {
            if (LeftArm != null) _leftArmRest = LeftArm.localRotation;
            if (RightArm != null) _rightArmRest = RightArm.localRotation;
            if (LeftLeg != null) _leftLegRest = LeftLeg.localRotation;
            if (RightLeg != null) _rightLegRest = RightLeg.localRotation;
            if (VisualRoot != null) _visualRootRestLocalPos = VisualRoot.localPosition;

            if (EnableFootstepAudio)
            {
                _footstepSource = gameObject.AddComponent<AudioSource>();
                _footstepSource.playOnAwake = false;
                _footstepSource.loop = false;
                _footstepSource.spatialBlend = 1f;
                _footstepSource.minDistance = 3f;
                _footstepSource.maxDistance = 30f;
                _footstepSource.volume = FootstepVolume;
                _footstepClip = BuildFootstepClip();
            }
        }

        private void Update()
        {
            bool moving = Player != null && Player.IsMoving;
            float speed01 = Player != null ? Player.CurrentSpeed01 : 0f;

            _amplitude01 = Mathf.MoveTowards(_amplitude01, moving ? Mathf.Max(0.35f, speed01) : 0f, Time.deltaTime * AmplitudeSmoothing);

            if (_amplitude01 > 0.001f)
            {
                float stepsPerSecond = StepsPerSecondAtFullSpeed * Mathf.Max(0.35f, speed01);
                float previousPhase = _phase;
                _phase += Time.deltaTime * stepsPerSecond * Mathf.PI * 2f;
                if (_phase > Mathf.PI * 2f) _phase -= Mathf.PI * 2f;

                float legAngle = Mathf.Sin(_phase) * LegSwingDegrees * _amplitude01;
                float armAngle = Mathf.Sin(_phase) * ArmSwingDegrees * _amplitude01;

                if (LeftLeg != null) LeftLeg.localRotation = _leftLegRest * Quaternion.Euler(legAngle, 0f, 0f);
                if (RightLeg != null) RightLeg.localRotation = _rightLegRest * Quaternion.Euler(-legAngle, 0f, 0f);
                // Arms swing opposite their same-side leg, as in a natural walk.
                if (LeftArm != null) LeftArm.localRotation = _leftArmRest * Quaternion.Euler(-armAngle, 0f, 0f);
                if (RightArm != null) RightArm.localRotation = _rightArmRest * Quaternion.Euler(armAngle, 0f, 0f);

                if (VisualRoot != null)
                {
                    float bob = Mathf.Abs(Mathf.Sin(_phase)) * 0.06f * _amplitude01;
                    VisualRoot.localPosition = _visualRootRestLocalPos + new Vector3(0f, bob, 0f);
                }

                // A footfall happens each time the swing crosses zero - twice per
                // full sine cycle. Fire on the descending-through-zero crossings
                // only, alternating feet, so each step gets exactly one tick.
                bool crossedZero = (previousPhase < Mathf.PI && _phase >= Mathf.PI) || (previousPhase > _phase);
                if (crossedZero && EnableFootstepAudio && _footstepSource != null && _footstepClip != null)
                {
                    PlayFootstep();
                }
            }
            else
            {
                // Idle: gentle breathing bob + a very small sway, no footsteps.
                _idleT += Time.deltaTime;
                if (LeftLeg != null) LeftLeg.localRotation = _leftLegRest;
                if (RightLeg != null) RightLeg.localRotation = _rightLegRest;

                float armSway = Mathf.Sin(_idleT * IdleSwaySpeed) * IdleSwayDegrees;
                if (LeftArm != null) LeftArm.localRotation = _leftArmRest * Quaternion.Euler(armSway * 0.5f, 0f, 0f);
                if (RightArm != null) RightArm.localRotation = _rightArmRest * Quaternion.Euler(-armSway * 0.5f, 0f, 0f);

                if (VisualRoot != null)
                {
                    float bob = Mathf.Sin(_idleT * IdleBobSpeed) * IdleBobHeight;
                    VisualRoot.localPosition = _visualRootRestLocalPos + new Vector3(0f, bob, 0f);
                }
            }
        }

        private void PlayFootstep()
        {
            _lastStepWasLeft = !_lastStepWasLeft;
            // Tiny pitch alternation between feet so steps don't sound identically robotic.
            _footstepSource.pitch = _lastStepWasLeft ? 0.97f : 1.03f;
            _footstepSource.PlayOneShot(_footstepClip, FootstepVolume);
        }

        /// <summary>
        /// Very short, soft filtered-noise "thud" synthesized in code - deliberately
        /// muted and brief so it reads as a footstep cue, not a percussive sound
        /// effect. Zero external asset dependency, same approach as the emergency
        /// siren elsewhere in this project.
        /// </summary>
        private static AudioClip BuildFootstepClip()
        {
            const int sampleRate = 22050;
            const float clipSeconds = 0.09f;
            int sampleCount = Mathf.RoundToInt(sampleRate * clipSeconds);
            var data = new float[sampleCount];
            float prev = 0f;
            var rng = new System.Random(1337);
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = (1f - t) * (1f - t); // fast decay
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                // One-pole low-pass to turn hiss into a soft thump.
                prev = prev + (noise - prev) * 0.18f;
                data[i] = prev * envelope * 0.6f;
            }
            var clip = AudioClip.Create("ProceduralFootstep", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
