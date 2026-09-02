using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DisasterReady.UI
{
    /// <summary>
    /// Transient banner shown briefly on a state change - either a routine
    /// "MISSION COMPLETE" (default gold accent) or a more urgent alert such as
    /// the emergency trigger (custom accent colour passed by the caller). Same
    /// fast fade/pop timing either way, so only the colour and text communicate
    /// which kind of event just happened - not a slower/different animation.
    /// </summary>
    public class MissionCompleteBanner : MonoBehaviour
    {
        public CanvasGroup Group;
        public Text Label;
        [Tooltip("Optional: recoloured to match the alert accent when Show(text, accentColor) is used. Left null-safe so this component still works if unassigned.")]
        public Image AccentBar;
        public float VisibleSeconds = 2.5f;
        public float FadeSeconds = 0.35f;

        public static readonly Color DefaultAccent = new Color(1f, 0.85f, 0.4f);

        [Header("Audio (optional, procedural - never required)")]
        public bool EnableChime = true;
        [Range(0f, 1f)] public float ChimeVolume = 0.22f;

        private Coroutine _routine;
        private RectTransform _rect;
        private AudioSource _chimeSource;
        private AudioClip _chimeClip;

        private void Awake()
        {
            if (Group != null) Group.alpha = 0f;
            _rect = Group != null ? Group.GetComponent<RectTransform>() : null;

            if (EnableChime)
            {
                _chimeSource = gameObject.AddComponent<AudioSource>();
                _chimeSource.playOnAwake = false;
                _chimeSource.spatialBlend = 0f;
                _chimeSource.volume = ChimeVolume;
                _chimeClip = BuildChimeClip();
            }
        }

        /// <summary>Routine banner (e.g. mission complete) using the default gold accent.</summary>
        public void Show(string text) => Show(text, DefaultAccent);

        /// <summary>Banner with a custom accent colour, e.g. a more urgent tone for the emergency trigger.</summary>
        public void Show(string text, Color accentColor)
        {
            if (Label != null)
            {
                Label.text = text;
                Label.color = accentColor;
            }
            if (AccentBar != null) AccentBar.color = accentColor;

            if (EnableChime && _chimeSource != null && _chimeClip != null)
            {
                _chimeSource.PlayOneShot(_chimeClip, ChimeVolume);
            }

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine());
        }

        /// <summary>
        /// Short, bright two-note "ding" (major third, quick decay) synthesized
        /// in code - zero dependency on an external audio asset, same approach
        /// as EmergencyScenarioController's siren. Deliberately soft so it
        /// reads as a positive confirmation cue, not an alert.
        /// </summary>
        private static AudioClip BuildChimeClip()
        {
            const int sampleRate = 22050;
            const float clipSeconds = 0.5f;
            const float noteA = 880f;   // A5
            const float noteB = 1108.7f; // C#6 - major third above, bright/friendly
            int sampleCount = Mathf.RoundToInt(sampleRate * clipSeconds);
            var data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 7f);
                float second = t > 0.08f ? Mathf.Sin(2f * Mathf.PI * noteB * t) * Mathf.Exp(-(t - 0.08f) * 7f) : 0f;
                float first = Mathf.Sin(2f * Mathf.PI * noteA * t) * envelope;
                data[i] = (first * 0.5f + second * 0.5f) * 0.6f;
            }
            var clip = AudioClip.Create("ProceduralMissionChime", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private IEnumerator ShowRoutine()
        {
            if (Group == null) yield break;

            float t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / FadeSeconds);
                Group.alpha = k;
                // Small, fast overshoot (scale 1 -> ~1.06 -> 1) so the banner reads
                // as an event happening, not just text fading in. Stays inside the
                // existing fade window, so it adds no extra time to the transition.
                if (_rect != null) _rect.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.06f);
                yield return null;
            }
            Group.alpha = 1f;
            if (_rect != null) _rect.localScale = Vector3.one;

            yield return new WaitForSeconds(VisibleSeconds);

            t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.deltaTime;
                Group.alpha = 1f - Mathf.Clamp01(t / FadeSeconds);
                yield return null;
            }
            Group.alpha = 0f;
        }
    }
}
