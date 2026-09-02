using UnityEngine;
using DisasterReady.UI;
using DisasterReady.Player;
using DisasterReady.CameraSystem;

namespace DisasterReady.Core
{
    /// <summary>
    /// Scene entry point: shows the title overlay first, then reveals the 3D world +
    /// HUD and hands control to the player once "ENTER AIZAWL, MIZORAM" is pressed.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        public TitleScreenController TitleScreen;
        public HUDController Hud;
        public PlayerController Player;
        public OrbitCameraController OrbitCamera;

        private void Start()
        {
            if (Player != null) Player.ControlsEnabled = false;
            if (OrbitCamera != null) OrbitCamera.ControlsEnabled = false;

            if (Hud != null) Hud.Hide();
            if (TitleScreen != null)
            {
                TitleScreen.Show();
                TitleScreen.OnEnterPressed += HandleEnterPressed;
            }
        }

        private void HandleEnterPressed()
        {
            if (TitleScreen != null) TitleScreen.Hide();
            if (Hud != null) Hud.Show();
            if (Player != null) Player.ControlsEnabled = true;
            if (OrbitCamera != null) OrbitCamera.ControlsEnabled = true;
        }
    }
}
