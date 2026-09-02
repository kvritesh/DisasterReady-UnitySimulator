using UnityEngine;

namespace DisasterReady.Emergency
{
    /// <summary>
    /// Physical trigger volume on the "reach higher terrain" destination. Mirrors
    /// the existing MissionZoneTrigger pattern (dumb collider -> singleton locator),
    /// so it stays consistent with how mission zones already work in this project.
    ///
    /// Deliberately does not gate on emergency state itself - EmergencyScenarioController
    /// owns that decision (NotifyObjectiveReached is a no-op unless an emergency is
    /// currently active), the same way MissionManager - not MissionZoneTrigger - decides
    /// whether a zone entry actually matters.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EmergencyObjectiveTrigger : MonoBehaviour
    {
        public string PlayerTag = "Player";

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(PlayerTag)) return;
            EmergencyScenarioController.Instance?.NotifyObjectiveReached();
        }
    }
}
