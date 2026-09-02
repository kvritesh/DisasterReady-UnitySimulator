using UnityEngine;

namespace DisasterReady.Missions
{
    /// <summary>
    /// Physical trigger volume placed on a mission-relevant location (summit, shelter,
    /// hospital). Real collision-based detection - this is what actually resolves
    /// missions; the UI only ever reflects state that this component reports.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MissionZoneTrigger : MonoBehaviour
    {
        public string MissionId;
        public string PlayerTag = "Player";

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(PlayerTag)) return;
            if (MissionManager.Instance == null) return;
            MissionManager.Instance.NotifyZoneEntered(MissionId);
        }
    }
}
