using UnityEngine;
using UnityEngine.UI;

namespace DisasterReady.UI
{
    /// <summary>Controls the opening overlay (title/subtitle/tagline + Enter button).</summary>
    public class TitleScreenController : MonoBehaviour
    {
        public GameObject RootPanel;
        public Button EnterButton;

        public System.Action OnEnterPressed;

        private void Awake()
        {
            if (EnterButton != null)
            {
                EnterButton.onClick.AddListener(HandleEnterClicked);
            }
        }

        private void HandleEnterClicked()
        {
            OnEnterPressed?.Invoke();
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
