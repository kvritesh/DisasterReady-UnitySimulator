using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady.Missions
{
    /// <summary>
    /// Drives the 3-mission demo sequence: Find the Highest Point -> Locate the
    /// Emergency Shelter -> Locate the Hospital. Missions advance in order; each
    /// only "listens" for its own zone while it is current, so early entry into a
    /// later zone doesn't skip ahead.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        public List<MissionDefinition> Missions = new List<MissionDefinition>();
        public int CurrentIndex { get; private set; }
        public int TotalXp { get; private set; }
        public int MaxXp { get; private set; }

        public System.Action<MissionDefinition> OnMissionStarted;
        public System.Action<MissionDefinition> OnMissionCompleted;
        public System.Action<int, int> OnXpChanged; // (current, max)
        public System.Action OnAllMissionsComplete;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            MaxXp = 0;
            foreach (var m in Missions) MaxXp += m.XpReward;

            if (Missions.Count > 0)
            {
                OnMissionStarted?.Invoke(Missions[0]);
            }
            OnXpChanged?.Invoke(TotalXp, MaxXp);
        }

        public MissionDefinition CurrentMission => (CurrentIndex >= 0 && CurrentIndex < Missions.Count) ? Missions[CurrentIndex] : null;

        public void NotifyZoneEntered(string missionId)
        {
            var current = CurrentMission;
            if (current == null || current.IsComplete) return;
            if (current.Id != missionId) return; // not this mission's turn yet

            current.IsComplete = true;
            TotalXp += current.XpReward;
            OnXpChanged?.Invoke(TotalXp, MaxXp);
            OnMissionCompleted?.Invoke(current);

            CurrentIndex++;
            if (CurrentIndex < Missions.Count)
            {
                OnMissionStarted?.Invoke(Missions[CurrentIndex]);
            }
            else
            {
                OnAllMissionsComplete?.Invoke();
            }
        }
    }
}
