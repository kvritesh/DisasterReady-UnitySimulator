using System.Linq;
using UnityEditor;
using UnityEngine;
using DisasterReady.Missions;
using DisasterReady.Player;
using DisasterReady.Emergency;
using DisasterReady.CameraSystem;
using DisasterReady.Core;

namespace DisasterReady.EditorTools
{
    /// <summary>
    /// Read-only sanity check for the currently open scene. Does NOT modify anything --
    /// it only logs to the Console. Written as a small, standalone tool per the "small
    /// editor utilities that save time" request: this is the kind of check that's easy
    /// to forget after a manual environment tweak (e.g. accidentally duplicating the
    /// MissionManager singleton, or leaving a POI's MissionId blank), and expensive to
    /// diagnose live in Play Mode.
    ///
    /// Run it any time via DisasterReady/Validate Scene References, ideally right after
    /// AizawlValleySceneBuilder.BuildScene() and again after any manual scene edit you
    /// intend to keep.
    /// </summary>
    public static class DisasterReadySceneValidator
    {
        [MenuItem("DisasterReady/Validate Scene References")]
        public static void Validate()
        {
            int errors = 0;
            int warnings = 0;

            Debug.Log("[SceneValidator] ==============================================");
            Debug.Log("[SceneValidator] Validating current scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            Debug.Log("[SceneValidator] ==============================================");

            // --- Player ---
            var players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            if (players.Length == 0)
            {
                Debug.LogError("[SceneValidator] FAIL: no PlayerController found in the scene.");
                errors++;
            }
            else if (players.Length > 1)
            {
                Debug.LogError($"[SceneValidator] FAIL: {players.Length} PlayerController instances found (expected exactly 1).");
                errors++;
            }
            else
            {
                var player = players[0];
                if (!player.CompareTag("Player"))
                {
                    Debug.LogError($"[SceneValidator] FAIL: Player GameObject '{player.name}' does not have the 'Player' tag -- mission/emergency triggers that check PlayerTag will never fire for it.");
                    errors++;
                }
                if (player.GetComponent<CharacterController>() == null)
                {
                    Debug.LogError($"[SceneValidator] FAIL: Player GameObject '{player.name}' has no CharacterController.");
                    errors++;
                }
                Debug.Log($"[SceneValidator] OK: Player found ('{player.name}', tag={player.tag}).");
            }

            // --- Camera ---
            var cams = Object.FindObjectsByType<OrbitCameraController>(FindObjectsSortMode.None);
            if (cams.Length == 0)
            {
                Debug.LogError("[SceneValidator] FAIL: no OrbitCameraController found in the scene.");
                errors++;
            }
            else if (cams.Length > 1)
            {
                Debug.LogWarning($"[SceneValidator] WARN: {cams.Length} OrbitCameraController instances found (expected exactly 1).");
                warnings++;
            }
            else if (cams[0].Target == null)
            {
                Debug.LogError("[SceneValidator] FAIL: OrbitCameraController.Target is not assigned.");
                errors++;
            }
            else
            {
                Debug.Log("[SceneValidator] OK: OrbitCameraController found with a Target assigned.");
            }

            // --- MissionManager ---
            var missionManagers = Object.FindObjectsByType<MissionManager>(FindObjectsSortMode.None);
            if (missionManagers.Length == 0)
            {
                Debug.LogError("[SceneValidator] FAIL: no MissionManager found in the scene.");
                errors++;
            }
            else if (missionManagers.Length > 1)
            {
                Debug.LogError($"[SceneValidator] FAIL: {missionManagers.Length} MissionManager instances found -- this is a singleton (Instance), duplicates will silently fight over it.");
                errors++;
            }
            else
            {
                var mm = missionManagers[0];
                if (mm.Missions == null || mm.Missions.Count == 0)
                {
                    Debug.LogError("[SceneValidator] FAIL: MissionManager has no missions assigned.");
                    errors++;
                }
                else
                {
                    var allTriggers = Object.FindObjectsByType<MissionZoneTrigger>(FindObjectsSortMode.None);
                    foreach (var mission in mm.Missions)
                    {
                        if (string.IsNullOrEmpty(mission.Id))
                        {
                            Debug.LogError("[SceneValidator] FAIL: a MissionDefinition has a blank Id -- it can never be completed (MissionZoneTrigger.MissionId comparisons will never match).");
                            errors++;
                            continue;
                        }
                        bool hasTrigger = allTriggers.Any(t => t.MissionId == mission.Id);
                        if (!hasTrigger)
                        {
                            Debug.LogError($"[SceneValidator] FAIL: mission '{mission.Id}' ({mission.Title}) has no MissionZoneTrigger in the scene with a matching MissionId -- it can never be completed.");
                            errors++;
                        }
                    }
                    Debug.Log($"[SceneValidator] OK: MissionManager found with {mm.Missions.Count} mission(s), {allTriggers.Length} MissionZoneTrigger(s) in scene.");
                }
            }

            // --- Emergency system ---
            var emergencyControllers = Object.FindObjectsByType<EmergencyScenarioController>(FindObjectsSortMode.None);
            if (emergencyControllers.Length == 0)
            {
                Debug.LogWarning("[SceneValidator] WARN: no EmergencyScenarioController found -- the scripted emergency sequence will never trigger in this scene.");
                warnings++;
            }
            else if (emergencyControllers.Length > 1)
            {
                Debug.LogError($"[SceneValidator] FAIL: {emergencyControllers.Length} EmergencyScenarioController instances found -- this is a singleton (Instance), duplicates will silently fight over it.");
                errors++;
            }
            else
            {
                var ec = emergencyControllers[0];
                CheckField(ec.Missions != null, "EmergencyScenarioController.Missions", ref errors);
                CheckField(ec.Hud != null, "EmergencyScenarioController.Hud", ref errors);
                CheckField(ec.Player != null, "EmergencyScenarioController.Player", ref errors);
                CheckField(ec.Guide != null, "EmergencyScenarioController.Guide", ref errors);
                CheckField(ec.ResultPanel != null, "EmergencyScenarioController.ResultPanel", ref errors);

                var objectiveTriggers = Object.FindObjectsByType<EmergencyObjectiveTrigger>(FindObjectsSortMode.None);
                if (objectiveTriggers.Length == 0)
                {
                    Debug.LogError("[SceneValidator] FAIL: no EmergencyObjectiveTrigger found -- the emergency, once triggered, can never be resolved (no way to call NotifyObjectiveReached).");
                    errors++;
                }
                else
                {
                    Debug.Log($"[SceneValidator] OK: EmergencyScenarioController found, {objectiveTriggers.Length} EmergencyObjectiveTrigger(s) in scene.");
                }
            }

            // --- Bootstrapper ---
            var bootstrappers = Object.FindObjectsByType<GameBootstrapper>(FindObjectsSortMode.None);
            if (bootstrappers.Length == 0)
            {
                Debug.LogWarning("[SceneValidator] WARN: no GameBootstrapper found -- player/camera controls will default to enabled at scene start (no title-screen gate).");
                warnings++;
            }
            else
            {
                Debug.Log("[SceneValidator] OK: GameBootstrapper found.");
            }

            // --- POI markers with an empty label or missing collider ---
            var pois = Object.FindObjectsByType<DisasterReady.POI.PoiMarker>(FindObjectsSortMode.None);
            foreach (var poi in pois)
            {
                var trigger = poi.GetComponent<MissionZoneTrigger>();
                var col = poi.GetComponent<Collider>();
                if (col == null)
                {
                    Debug.LogError($"[SceneValidator] FAIL: POI '{poi.name}' has no Collider -- it can never be entered.");
                    errors++;
                }
                else if (!col.isTrigger)
                {
                    Debug.LogWarning($"[SceneValidator] WARN: POI '{poi.name}' Collider is not marked isTrigger -- it will physically block the player instead of registering entry.");
                    warnings++;
                }
                if (trigger != null && string.IsNullOrEmpty(trigger.MissionId))
                {
                    Debug.LogWarning($"[SceneValidator] WARN: POI '{poi.name}' has an empty MissionId -- entering it will never complete anything.");
                    warnings++;
                }
            }

            Debug.Log("[SceneValidator] ==============================================");
            Debug.Log($"[SceneValidator] DONE. {errors} error(s), {warnings} warning(s).");
            Debug.Log("[SceneValidator] ==============================================");

            if (errors == 0 && warnings == 0)
            {
                EditorUtility.DisplayDialog("Scene Validator", "All checks passed with no errors or warnings.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Scene Validator", $"{errors} error(s), {warnings} warning(s). See Console for details.", "OK");
            }
        }

        private static void CheckField(bool isAssigned, string fieldName, ref int errors)
        {
            if (!isAssigned)
            {
                Debug.LogError($"[SceneValidator] FAIL: {fieldName} is not assigned.");
                errors++;
            }
        }
    }
}
