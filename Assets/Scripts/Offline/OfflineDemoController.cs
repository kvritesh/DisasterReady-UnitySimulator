using UnityEngine;
using UnityEngine.UI;

namespace DisasterReady.Offline
{
    /// <summary>
    /// Demonstrates the *future* offline-first architecture. This does not represent
    /// that real geographic data has been downloaded yet - it simulates the UI state
    /// the app will show once offline terrain/mission packs exist. The 3D world and
    /// missions keep running unchanged after the toggle; nothing here touches network
    /// state because nothing in this vertical slice makes network calls to begin with.
    /// </summary>
    public class OfflineDemoController : MonoBehaviour
    {
        public Text StatusHeadline;
        public Text StatusLine1;
        public Text StatusLine2;
        public Text StatusLine3;
        public Text ReadyBadgeText;
        public Button SimulateButton;
        public Text SimulateButtonLabel;

        private bool _offlineModeActive;

        private void Awake()
        {
            if (SimulateButton != null)
            {
                SimulateButton.onClick.AddListener(ToggleOfflineSimulation);
            }
            SetOnlinePresentation();
        }

        public void ToggleOfflineSimulation()
        {
            _offlineModeActive = !_offlineModeActive;
            if (_offlineModeActive) SetOfflinePresentation();
            else SetOnlinePresentation();
        }

        private void SetOnlinePresentation()
        {
            if (ReadyBadgeText != null) ReadyBadgeText.text = "OFFLINE READY";
            if (StatusHeadline != null) StatusHeadline.text = "";
            if (StatusLine1 != null) StatusLine1.text = "";
            if (StatusLine2 != null) StatusLine2.text = "";
            if (StatusLine3 != null) StatusLine3.text = "";
            if (SimulateButtonLabel != null) SimulateButtonLabel.text = "SIMULATE NO INTERNET";
        }

        private void SetOfflinePresentation()
        {
            if (StatusHeadline != null) StatusHeadline.text = "OFFLINE MODE";
            if (StatusLine1 != null) StatusLine1.text = "Terrain data available locally";
            if (StatusLine2 != null) StatusLine2.text = "Missions available";
            if (StatusLine3 != null) StatusLine3.text = "Emergency POIs available";
            if (SimulateButtonLabel != null) SimulateButtonLabel.text = "BACK TO ONLINE VIEW";
        }
    }
}
