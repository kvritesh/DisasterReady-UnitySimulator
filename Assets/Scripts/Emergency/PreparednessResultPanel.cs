using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DisasterReady.Emergency
{
    /// <summary>
    /// Final "preparedness simulation complete" result screen for the vertical
    /// slice. Fades in and stays visible (unlike MissionCompleteBanner, this does
    /// not auto-hide) - this is the end state of the demo run.
    ///
    /// All values shown here come from the caller (EmergencyScenarioController),
    /// which derives them from actual MissionManager state; this component only
    /// displays them, matching the existing "UI reflects state, does not own it"
    /// pattern already used by HUDController/MissionCompleteBanner.
    /// </summary>
    public class PreparednessResultPanel : MonoBehaviour
    {
        public GameObject RootPanel;
        public CanvasGroup Group;
        public Text HeadlineText;
        public Text ScoreText;
        public Text MissionsText;
        public Text XpText;
        public Text EmergencyText;
        public Text DisclaimerText;
        public float FadeSeconds = 0.6f;

        [Header("Audio (optional, procedural - never required)")]
        public bool EnableFanfare = true;
        [Range(0f, 1f)] public float FanfareVolume = 0.25f;

        private Coroutine _routine;
        private AudioSource _fanfareSource;
        private AudioClip _fanfareClip;

        private void Awake()
        {
            if (EnableFanfare)
            {
                _fanfareSource = gameObject.AddComponent<AudioSource>();
                _fanfareSource.playOnAwake = false;
                _fanfareSource.spatialBlend = 0f;
                _fanfareSource.volume = FanfareVolume;
                _fanfareClip = BuildFanfareClip();
            }
        }

        public void Show(int missionsCompleted, int missionsTotal, int xpEarned, int xpMax, int preparednessScore)
        {
            if (RootPanel != null) RootPanel.SetActive(true);

            if (HeadlineText != null) HeadlineText.text = "PREPAREDNESS SIMULATION COMPLETE";
            if (ScoreText != null) ScoreText.text = $"Preparedness score: {preparednessScore} / 100";
            if (MissionsText != null) MissionsText.text = $"Missions completed: {missionsCompleted} / {missionsTotal}";
            if (XpText != null) XpText.text = $"XP earned: {xpEarned} / {xpMax}";
            if (EmergencyText != null) EmergencyText.text = "Emergency successfully navigated (simulated)";
            if (DisclaimerText != null)
                DisclaimerText.text =
                    "This is a scripted gameplay simulation, not a real emergency response or a validated evacuation system.";

            if (EnableFanfare && _fanfareSource != null && _fanfareClip != null)
            {
                _fanfareSource.PlayOneShot(_fanfareClip, FanfareVolume);
            }

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FadeIn());
        }

        /// <summary>
        /// Short three-note ascending major arpeggio (root-third-fifth), synthesized
        /// in code with zero external asset dependency - a small "well done"
        /// flourish for the run's final beat, not a long musical sting.
        /// </summary>
        private static AudioClip BuildFanfareClip()
        {
            const int sampleRate = 22050;
            const float clipSeconds = 1.1f;
            float[] notes = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5
            float[] starts = { 0f, 0.14f, 0.28f };
            int sampleCount = Mathf.RoundToInt(sampleRate * clipSeconds);
            var data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float sample = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float noteT = t - starts[n];
                    if (noteT < 0f) continue;
                    float env = Mathf.Exp(-noteT * 3.2f);
                    sample += Mathf.Sin(2f * Mathf.PI * notes[n] * noteT) * env;
                }
                data[i] = Mathf.Clamp(sample * 0.3f, -1f, 1f);
            }
            var clip = AudioClip.Create("ProceduralResultFanfare", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void Hide()
        {
            if (_routine != null) StopCoroutine(_routine);
            if (Group != null)
            {
                Group.alpha = 0f;
                Group.blocksRaycasts = false;
            }
            if (RootPanel != null) RootPanel.SetActive(false);
        }

        private IEnumerator FadeIn()
        {
            if (Group == null) yield break;
            var rect = Group.GetComponent<RectTransform>();

            float t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / FadeSeconds);
                Group.alpha = k;
                // Small pop-in (scale ~0.94 -> 1) so the completion screen feels
                // like a deliberate final beat rather than text just fading in.
                // Still finishes inside FadeSeconds - no slow reveal to sit through.
                if (rect != null) rect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, k);
                yield return null;
            }
            Group.alpha = 1f;
            if (rect != null) rect.localScale = Vector3.one;
            Group.blocksRaycasts = true;
        }
    }
}
