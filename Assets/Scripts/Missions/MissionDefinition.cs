using System;

namespace DisasterReady.Missions
{
    /// <summary>Plain data describing one mission. Completion detection lives in MissionManager/MissionZoneTrigger, not here.</summary>
    [Serializable]
    public class MissionDefinition
    {
        public string Id;
        public string Title;
        public string Description;
        public int XpReward = 100;
        public bool IsComplete;

        public MissionDefinition(string id, string title, string description, int xpReward)
        {
            Id = id;
            Title = title;
            Description = description;
            XpReward = xpReward;
        }
    }
}
