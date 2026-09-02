using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using DisasterReady.Missions;
using DisasterReady.UI;
using DisasterReady.Player;

namespace DisasterReady.Emergency
{
    /// <summary>
    /// Deterministic, scripted emergency scenario for the DisasterReady vertical
    /// slice.
    ///
    /// TRIGGER CONDITION: fires exactly once, the moment MissionManager reports
    /// that every preparedness mission is complete (MissionManager.OnAllMissionsComplete).
    /// There is no randomness anywhere in this class - the same playthrough always
    /// produces the same sequence at the same point, which is what "repeatable
    /// every time the scene is played" and "resets correctly on reload" require:
    /// on a scene reload this whole GameObject (and its _hasTriggered flag) is
    /// recreated fresh by Unity, so there is nothing to manually reset.
    ///
    /// This is a SIMULATED EMERGENCY — DEMONSTRATION ONLY. Nothing in this class
    /// reads, predicts, or is derived from real-world hazard, sensor, or weather
    /// data, and it never claims to be AI-predicted - it is a plain scripted state
    /// transition that exists to demonstrate the intended gameplay loop.
    /// </summary>
    public class EmergencyScenarioController : MonoBehaviour
    {
        public static EmergencyScenarioController Instance { get; private set; }

        [Header("Wiring (assigned by SceneBuilder)")]
        public MissionManager Missions;
        public HUDController Hud;
        public PlayerController Player;
        public SafeDirectionGuide Guide;
        public PreparednessResultPanel ResultPanel;
        public Text EmergencyStatusText;
        [Tooltip("Backing plate for EmergencyStatusText. When assigned, the whole plate is shown/hidden; falls back to toggling the text alone if left unassigned.")]
        public GameObject EmergencyStatusPlate;
        [Tooltip("World-space label on the HighestPointMarker/objective ('HIGHEST POINT' by default). Optional - if assigned, it's reworded while the emergency is active so the destination's purpose is obvious from a distance.")]
        public Text SummitLabelText;
        [Tooltip("Optional orbit camera. If assigned, the view eases a little further back for the emergency so the player can see more of the route/objective - purely a framing nudge, never takes control away from the player.")]
        public DisasterReady.CameraSystem.OrbitCameraController Camera;

        [Header("Atmosphere timing (seconds)")]
        public float AtmosphereInSeconds = 2.2f;
        public float AtmosphereOutSeconds = 1.5f;
        [Tooltip("Sustained overlay strength once the transition settles. Kept deliberately moderate so the emergency reads as urgent without becoming a full red-screen/horror effect.")]
        [Range(0f, 1f)] public float OverlayWeightTarget = 0.5f;
        [Tooltip("Brief overshoot right at the start of the transition - a quick 'something changed' pulse that then eases down to OverlayWeightTarget.")]
        [Range(0f, 1f)] public float OverlayPulseWeight = 0.85f;
        [Tooltip("Fraction of AtmosphereInSeconds spent on the initial pulse spike before easing down to the sustained target.")]
        [Range(0.05f, 0.6f)] public float PulseFraction = 0.25f;

        [Header("Camera")]
        [Tooltip("How much further back the camera eases during the emergency, on top of whatever distance the player already had.")]
        public float EmergencyZoomOutDistance = 4f;

        public bool IsActive { get; private set; }
        private bool _hasTriggered;

        private Volume _overlayVolume;
        private AudioSource _siren;
        private Coroutine _atmosphereRoutine;
        private Coroutine _statusPulseRoutine;

        private Color _baseFogColor;
        private Color _baseSunColor;
        private float _baseSunIntensity;
        private Color _statusBaseColor;
        private string _summitLabelBaseText;
        private float _cameraBaseDistance;
        private bool _cameraAdjusted;

        private static readonly Color AlertAccent = new Color(1f, 0.55f, 0.3f);

        private const string SummitLabelIdleText = "HIGHEST POINT";
        private const string SummitLabelEmergencyText = "EMERGENCY OBJECTIVE\nREACH HIGHER GROUND";

        private void Awake()
        {
            Instance = this;
            CaptureAtmosphereBaseline();
            BuildAtmosphereOverlay();
            BuildSiren();
            if (EmergencyStatusText != null) _statusBaseColor = EmergencyStatusText.color;
        }

        private void OnEnable()
        {
            if (Missions != null) Missions.OnAllMissionsComplete += HandleAllMissionsComplete;
        }

        private void OnDisable()
        {
            if (Missions != null) Missions.OnAllMissionsComplete -= HandleAllMissionsComplete;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void HandleAllMissionsComplete()
        {
            if (_hasTriggered) return; // guarantees a single, non-repeating trigger
            _hasTriggered = true;
            TriggerEmergency();
        }

        private void TriggerEmergency()
        {
            IsActive = true;

            if (Hud != null)
            {
                if (Hud.MissionTitleText != null) Hud.MissionTitleText.text = "EMERGENCY OBJECTIVE";
                if (Hud.MissionDescriptionText != null)
                    Hud.MissionDescriptionText.text = "REACH HIGHER TERRAIN\nFollow the demo safe-direction heuristic to safety.";
                if (Hud.CompleteBanner != null)
                    Hud.CompleteBanner.Show(
                        "SIMULATED EMERGENCY — DEMONSTRATION ONLY\nA scripted landslide-risk event has been triggered for this demo.",
                        AlertAccent);
            }

            if (EmergencyStatusText != null)
            {
                EmergencyStatusText.text = "SIMULATED EMERGENCY — DEMONSTRATION ONLY";
                EmergencyStatusText.color = _statusBaseColor;
            }
            if (EmergencyStatusPlate != null) EmergencyStatusPlate.SetActive(true);
            else if (EmergencyStatusText != null) EmergencyStatusText.gameObject.SetActive(true);

            if (SummitLabelText != null)
            {
                _summitLabelBaseText = SummitLabelText.text;
                SummitLabelText.text = SummitLabelEmergencyText;
            }

            if (Guide != null) Guide.Activate();
            if (_siren != null) _siren.Play();

            if (Camera != null && !_cameraAdjusted)
            {
                _cameraBaseDistance = Camera.Distance;
                Camera.Distance = Mathf.Clamp(_cameraBaseDistance + EmergencyZoomOutDistance, Camera.MinDistance, Camera.MaxDistance);
                _cameraAdjusted = true;
            }

            if (_statusPulseRoutine != null) StopCoroutine(_statusPulseRoutine);
            _statusPulseRoutine = StartCoroutine(PulseStatusText());

            RestartAtmosphereRoutine(toEmergency: true);
        }

        /// <summary>
        /// Called by EmergencyObjectiveTrigger when the player physically reaches
        /// the higher-terrain destination zone. A no-op unless an emergency is
        /// currently active, so the trigger can be wired up unconditionally and
        /// can never fire the win state twice.
        /// </summary>
        public void NotifyObjectiveReached()
        {
            if (!IsActive) return;
            IsActive = false;
            ResolveEmergency();
        }

        private void ResolveEmergency()
        {
            if (Guide != null) Guide.Deactivate();
            if (_siren != null) _siren.Stop();

            if (_statusPulseRoutine != null)
            {
                StopCoroutine(_statusPulseRoutine);
                _statusPulseRoutine = null;
            }
            if (EmergencyStatusText != null) EmergencyStatusText.color = _statusBaseColor;
            if (EmergencyStatusPlate != null) EmergencyStatusPlate.SetActive(false);
            else if (EmergencyStatusText != null) EmergencyStatusText.gameObject.SetActive(false);

            if (SummitLabelText != null) SummitLabelText.text = string.IsNullOrEmpty(_summitLabelBaseText) ? SummitLabelIdleText : _summitLabelBaseText;

            if (Camera != null && _cameraAdjusted)
            {
                Camera.Distance = Mathf.Clamp(_cameraBaseDistance, Camera.MinDistance, Camera.MaxDistance);
                _cameraAdjusted = false;
            }

            RestartAtmosphereRoutine(toEmergency: false);

            // Freeze the player once the demo has concluded, mirroring how
            // GameBootstrapper already freezes controls behind the title screen.
            if (Player != null) Player.ControlsEnabled = false;

            ShowResult();
        }

        private void ShowResult()
        {
            if (Missions == null || ResultPanel == null) return;

            int total = Missions.Missions.Count;
            int completed = 0;
            foreach (var m in Missions.Missions)
            {
                if (m.IsComplete) completed++;
            }

            int xpEarned = Missions.TotalXp;
            int xpMax = Missions.MaxXp;

            // Transparent, deterministic preparedness score - NOT a statistical or
            // ML-derived risk metric. Up to 70 points for the fraction of
            // preparedness missions actually completed, plus a flat 30 points for
            // successfully navigating the simulated emergency (only awarded here,
            // since ShowResult only runs after the player reaches the objective).
            // Under this vertical slice's current fully-gated flow (all missions
            // must complete before the emergency can even trigger, and this panel
            // only appears after the objective is reached) that means the score is
            // always 100/100 today - an honest property of a linear scripted demo,
            // not a bug. The formula is written so a future difficulty/failure
            // state would immediately start producing differentiated scores.
            float missionFraction = total > 0 ? (float)completed / total : 0f;
            int score = Mathf.Clamp(Mathf.RoundToInt(missionFraction * 70f) + 30, 0, 100);

            ResultPanel.Show(completed, total, xpEarned, xpMax, score);

            // Additive web-platform integration hook: hands the same result
            // values shown on the in-Unity result panel back to the hosting
            // web page (no-op outside WebGL builds). Does not affect the
            // panel, scoring, or emergency flow above.
            var launchParams = DisasterReady.Integration.WebGLBridge.GetLaunchParams();
            DisasterReady.Integration.WebGLBridge.SendResult(new DisasterReady.Integration.SimulatorResult
            {
                regionId = launchParams.regionId,
                scenarioId = launchParams.scenarioId,
                simulationCompleted = true,
                missionsCompleted = completed,
                missionsTotal = total,
                xpEarned = xpEarned,
                xpMax = xpMax,
                preparednessScore = score
            });
        }

        /// <summary>
        /// Slow, subtle alpha pulse on the persistent status tag while the
        /// emergency is active - purely a "this is still ongoing, pay attention"
        /// cue. Deliberately gentle (never fully transparent, ~1.1s period) so it
        /// stays readable and doesn't turn into a strobing/disorienting effect.
        /// </summary>
        private IEnumerator PulseStatusText()
        {
            if (EmergencyStatusText == null) yield break;
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(0.72f, 1f, 0.5f + 0.5f * Mathf.Sin(t * 3.4f));
                var c = _statusBaseColor;
                c.a = a;
                EmergencyStatusText.color = c;
                yield return null;
            }
        }

        // -----------------------------------------------------------------
        // Atmosphere: a runtime-only post-process overlay Volume + fog/sun tint.
        // Built and owned entirely by this component (never touches the scene's
        // shared post-process profile asset), so it is trivially reversible and
        // leaves nothing behind after ResolveEmergency runs.
        // -----------------------------------------------------------------
        private void CaptureAtmosphereBaseline()
        {
            _baseFogColor = RenderSettings.fogColor;
            if (RenderSettings.sun != null)
            {
                _baseSunColor = RenderSettings.sun.color;
                _baseSunIntensity = RenderSettings.sun.intensity;
            }
            else
            {
                _baseSunColor = Color.white;
                _baseSunIntensity = 1f;
            }
        }

        private void BuildAtmosphereOverlay()
        {
            var volGO = new GameObject("EmergencyAtmosphereOverlay");
            volGO.transform.SetParent(transform, false);

            _overlayVolume = volGO.AddComponent<Volume>();
            _overlayVolume.isGlobal = true;
            _overlayVolume.priority = 10f;
            _overlayVolume.weight = 0f; // invisible until TriggerEmergency lerps it up

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            // Softer than a straight "danger red" wash - warm amber-orange rather
            // than deep red, and a lower ceiling on saturation/vignette so the
            // scripted emergency reads as urgent without tipping into a horror-
            // movie red-screen effect. See OverlayWeightTarget/OverlayPulseWeight
            // for how strongly this ever actually gets applied.
            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.colorFilter.overrideState = true;
            colorAdj.colorFilter.value = new Color(1f, 0.64f, 0.42f);
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = 8f;
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = -0.03f;

            var vignette = profile.Add<Vignette>(true);
            vignette.color.overrideState = true;
            vignette.color.value = new Color(0.42f, 0.12f, 0.06f);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.3f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.75f;

            _overlayVolume.profile = profile;
        }

        private void BuildSiren()
        {
            _siren = gameObject.AddComponent<AudioSource>();
            _siren.playOnAwake = false;
            _siren.loop = true;
            _siren.volume = 0.35f;
            _siren.spatialBlend = 0f; // 2D - always audible, simplest placeholder-safe choice
            _siren.clip = BuildSirenClip();
        }

        /// <summary>
        /// Deterministic two-tone placeholder siren, synthesized in code so this
        /// demo has zero dependency on an external audio asset. Tone-switch and
        /// loop-wrap points are chosen so every boundary lands on an exact sine
        /// cycle for both frequencies (no clicks), and the clip loops seamlessly.
        /// </summary>
        private static AudioClip BuildSirenClip()
        {
            const int sampleRate = 22050;
            const float clipSeconds = 2f;
            const float toneA = 660f;
            const float toneB = 880f;

            int sampleCount = Mathf.RoundToInt(sampleRate * clipSeconds);
            var data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float freq = (Mathf.Repeat(t, 1f) < 0.5f) ? toneA : toneB;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f;
            }

            var clip = AudioClip.Create("SimulatedSirenPlaceholder", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void RestartAtmosphereRoutine(bool toEmergency)
        {
            if (!gameObject.activeInHierarchy) return;
            if (_atmosphereRoutine != null) StopCoroutine(_atmosphereRoutine);
            _atmosphereRoutine = StartCoroutine(LerpAtmosphere(toEmergency));
        }

        private IEnumerator LerpAtmosphere(bool toEmergency)
        {
            float duration = toEmergency ? AtmosphereInSeconds : AtmosphereOutSeconds;
            float startWeight = _overlayVolume != null ? _overlayVolume.weight : 0f;
            float targetWeight = toEmergency ? OverlayWeightTarget : 0f;
            float pulseFrac = Mathf.Clamp(PulseFraction, 0.05f, 0.95f);

            Color startFog = RenderSettings.fogColor;
            Color targetFog = toEmergency ? new Color(0.55f, 0.16f, 0.1f) : _baseFogColor;

            Light sun = RenderSettings.sun;
            Color startSunColor = sun != null ? sun.color : _baseSunColor;
            Color targetSunColor = toEmergency ? new Color(0.95f, 0.4f, 0.25f) : _baseSunColor;
            float startSunIntensity = sun != null ? sun.intensity : _baseSunIntensity;
            float targetSunIntensity = toEmergency ? _baseSunIntensity * 0.7f : _baseSunIntensity;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;

                if (_overlayVolume != null)
                {
                    float weight;
                    if (toEmergency)
                    {
                        // Quick spike up to OverlayPulseWeight (the "something just
                        // changed" beat), then ease back down to the sustained,
                        // deliberately-moderate OverlayWeightTarget.
                        weight = k < pulseFrac
                            ? Mathf.Lerp(startWeight, OverlayPulseWeight, k / pulseFrac)
                            : Mathf.Lerp(OverlayPulseWeight, targetWeight, (k - pulseFrac) / (1f - pulseFrac));
                    }
                    else
                    {
                        weight = Mathf.Lerp(startWeight, targetWeight, k);
                    }
                    _overlayVolume.weight = weight;
                }

                RenderSettings.fogColor = Color.Lerp(startFog, targetFog, k);
                if (sun != null)
                {
                    sun.color = Color.Lerp(startSunColor, targetSunColor, k);
                    sun.intensity = Mathf.Lerp(startSunIntensity, targetSunIntensity, k);
                }
                yield return null;
            }

            if (_overlayVolume != null) _overlayVolume.weight = targetWeight;
            RenderSettings.fogColor = targetFog;
            if (sun != null)
            {
                sun.color = targetSunColor;
                sun.intensity = targetSunIntensity;
            }
        }
    }
}
