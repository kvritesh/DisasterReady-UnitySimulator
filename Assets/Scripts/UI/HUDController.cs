using UnityEngine;
using UnityEngine.UI;
using DisasterReady.Missions;

namespace DisasterReady.UI
{
    /// <summary>Drives the gameplay HUD: XP counter and current-mission panel.</summary>
    public class HUDController : MonoBehaviour
    {
        public GameObject RootPanel;
        public Text XpText;
        public Text MissionTitleText;
        public Text MissionDescriptionText;
        public MissionManager Missions;
        public MissionCompleteBanner CompleteBanner;

        private void OnEnable()
        {
            if (Missions == null) return;
            Missions.OnXpChanged += HandleXpChanged;
            Missions.OnMissionStarted += HandleMissionStarted;
            Missions.OnMissionCompleted += HandleMissionCompleted;
            Missions.OnAllMissionsComplete += HandleAllComplete;

            // MissionManager.Start() fires the very first OnMissionStarted/OnXpChanged
            // once, before this HUD is ever shown (hudRoot is built inactive and only
            // activates after the title screen is dismissed), so that first event is
            // otherwise missed permanently and the panel is stuck on its placeholder
            // text. Pull current state explicitly the moment we do subscribe, so a
            // subscriber that starts late (which, given the title-screen gate, is
            // always the case) still ends up showing the right thing.
            var current = Missions.CurrentMission;
            if (current != null) HandleMissionStarted(current);
            HandleXpChanged(Missions.TotalXp, Missions.MaxXp);
        }

        private void OnDisable()
        {
            if (Missions == null) return;
            Missions.OnXpChanged -= HandleXpChanged;
            Missions.OnMissionStarted -= HandleMissionStarted;
            Missions.OnMissionCompleted -= HandleMissionCompleted;
            Missions.OnAllMissionsComplete -= HandleAllComplete;
        }

        private void HandleXpChanged(int current, int max)
        {
            if (XpText != null) XpText.text = $"XP: {current} / {max}";
        }

        private void HandleMissionStarted(MissionDefinition mission)
        {
            if (MissionTitleText != null) MissionTitleText.text = "CURRENT MISSION";
            if (MissionDescriptionText != null) MissionDescriptionText.text = $"{mission.Title}\n{mission.Description}";
        }

        private void HandleMissionCompleted(MissionDefinition mission)
        {
            if (CompleteBanner != null) CompleteBanner.Show($"MISSION COMPLETE\n{mission.Title}  (+{mission.XpReward} XP)");
        }

        private void HandleAllComplete()
        {
            if (MissionDescriptionText != null) MissionDescriptionText.text = "All missions complete. Valley secured!";
        }

        public void Show()
        {
            if (RootPanel != null) RootPanel.SetActive(true);
        }

        public void Hide()
        {
            if (RootPanel != null) RootPanel.SetActive(false);
        }
    }
}
