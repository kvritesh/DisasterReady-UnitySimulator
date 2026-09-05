using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DisasterReady.Core;
using DisasterReady.CameraSystem;
using DisasterReady.Emergency;
using DisasterReady.Missions;
using DisasterReady.Player;
using DisasterReady.POI;
using DisasterReady.UI;

namespace DisasterReady.EditorTools
{
    /// <summary>
    /// One-click transplanter: seamlessly hooks all DisasterReady gameplay systems (Player, Camera,
    /// Missions, POIs, UI, Emergency Landslide Evacuation, Safe-Direction Compass, Bootstrapper)
    /// into the Synty PolygonStarter demo scene or any active environment.
    /// </summary>
    public static class DisasterReadyTransplanter
    {
        private const string SyntyDemoScenePath = "Assets/Synty/PolygonStarter/Scenes/Demo.unity";

        [MenuItem("DisasterReady/Setup Synty Demo Scene (Recommended)")]
        public static void SetupSyntyDemoScene()
        {
            if (!File.Exists(SyntyDemoScenePath))
            {
                EditorUtility.DisplayDialog("Scene Not Found", $"Could not find {SyntyDemoScenePath}.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(SyntyDemoScenePath, OpenSceneMode.Single);
            Debug.Log($"[DisasterReadyTransplanter] Opened Synty demo scene: {scene.path}");

            // Add colliders to environment geometry so player doesn't fall through
            AddCollidersToEnvironment();

            // Run core transplant
            TransplantInternal(scene, isSyntyLevel: true);

            // Add to build settings as Scene 0
            SetSceneAsBuildIndexZero(SyntyDemoScenePath);

            EditorUtility.DisplayDialog(
                "DisasterReady Transplant Complete!",
                "SUCCESS! The entire DisasterReady game has been transplanted into the Synty environment:\n\n" +
                "• All grounds, ramps, and mountains have colliders added.\n" +
                "• Player with KayKit animations spawned.\n" +
                "• Hospital, Shelter, and Mountain Summit POIs placed.\n" +
                "• Landslide Emergency System and Safe-Direction Compass hooked up.\n" +
                "• Full UI Canvas & Mission HUD built.\n\n" +
                "Click OK, then press Play in the Editor to test your game!",
                "Let's Go!");
        }

        [MenuItem("DisasterReady/Transplant to Active Scene")]
        public static void TransplantToActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            AddCollidersToEnvironment();
            TransplantInternal(activeScene, isSyntyLevel: false);

            EditorUtility.DisplayDialog(
                "Transplant Complete!",
                "DisasterReady mechanics have been transplanted into the active scene!\n\n" +
                "Hit Play to test the full game loop!",
                "Awesome");
        }

        private static void AddCollidersToEnvironment()
        {
            int count = 0;
            var demoObjects = GameObject.Find("Demo_Objects");
            var filters = demoObjects != null 
                ? demoObjects.GetComponentsInChildren<MeshFilter>(true)
                : Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;
                if (mf.GetComponent<Collider>() != null) continue;

                string n = mf.gameObject.name.ToLower();
                // Skip sky, icons, floating markers
                if (n.Contains("sky") || n.Contains("cloud") || n.Contains("icon") || n.Contains("target") || n.Contains("ring"))
                    continue;

                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                count++;
            }

            Debug.Log($"[DisasterReadyTransplanter] Ensured {count} environment meshes have MeshColliders.");
        }

        private static void TransplantInternal(Scene scene, bool isSyntyLevel)
        {
            Debug.Log($"[DisasterReadyTransplanter] Starting transplant into {scene.name}...");

            // 1. Clean up old DisasterReady objects if present
            GameObject drRoot = GameObject.Find("DisasterReady_Gameplay");
            if (drRoot != null) Object.DestroyImmediate(drRoot);

            GameObject oldUI = GameObject.Find("UI_Root");
            if (oldUI != null) Object.DestroyImmediate(oldUI);

            drRoot = new GameObject("DisasterReady_Gameplay");
            Undo.RegisterCreatedObjectUndo(drRoot, "Transplant DisasterReady");

            // 2. Determine player spawn and POI positions
            Vector3 spawnPos = new Vector3(6f, 3.5f, 2f);
            Vector3 hospPos = new Vector3(-8f, 3.5f, 15f);
            Vector3 shelterPos = new Vector3(20f, 3.5f, -8f);
            Vector3 summitPos = new Vector3(0f, 18f, 50f);

            if (isSyntyLevel)
            {
                // Find highest mountain for summit
                float maxMountainY = -999f;
                Vector3 mountainPeak = summitPos;
                var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                foreach (var r in renderers)
                {
                    string rn = r.gameObject.name.ToLower();
                    if (rn.Contains("mountain") && r.bounds.max.y > maxMountainY)
                    {
                        maxMountainY = r.bounds.max.y;
                        mountainPeak = new Vector3(r.bounds.center.x, r.bounds.max.y + 0.5f, r.bounds.center.z);
                    }
                }
                if (maxMountainY > -900f) summitPos = mountainPeak;
            }

            // Raycast down to find ground for spawn
            if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f))
            {
                spawnPos = hit.point + Vector3.up * 0.1f;
            }

            // 3. Build Player
            GameObject player = KayKitIntegration.BuildPlayer(drRoot.transform, spawnPos);
            player.name = "DisasterReady_Player";

            // 4. Setup Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.transform.SetParent(drRoot.transform);
                mainCam = camGO.AddComponent<Camera>();
                mainCam.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }

            mainCam.fieldOfView = 55f;
            mainCam.nearClipPlane = 0.3f;
            mainCam.farClipPlane = 600f;

            var orbitCam = mainCam.GetComponent<OrbitCameraController>();
            if (orbitCam == null) orbitCam = mainCam.gameObject.AddComponent<OrbitCameraController>();
            orbitCam.Target = player.transform;
            orbitCam.StartYaw = 0f;
            orbitCam.StartPitch = 30f;
            orbitCam.Distance = 11.5f;
            orbitCam.TargetOffset = new Vector3(0f, 1.55f, 0f);
            orbitCam.PlayerRef = player.GetComponent<PlayerController>();

            // 5. Create POIs
            var poiRoot = new GameObject("POIs");
            poiRoot.transform.SetParent(drRoot.transform);

            var hospTrigger = CreatePoiMarker(poiRoot.transform, "POI_Hospital", hospPos, PoiType.Hospital, "HOSPITAL", new Color(0.2f, 0.85f, 0.3f));
            hospTrigger.MissionId = "find_hospital";

            var shelterTrigger = CreatePoiMarker(poiRoot.transform, "POI_Shelter", shelterPos, PoiType.EmergencyShelter, "EMERGENCY SHELTER", new Color(0.2f, 0.5f, 0.95f));
            shelterTrigger.MissionId = "find_shelter";

            var summitTrigger = CreatePoiMarker(poiRoot.transform, "POI_Summit", summitPos, PoiType.Summit, "HIGHEST POINT (SUMMIT)", new Color(1f, 0.85f, 0.1f), out Text summitLabel);
            summitTrigger.MissionId = "find_highest";
            var emergencyObjectiveTrigger = summitTrigger.gameObject.AddComponent<EmergencyObjectiveTrigger>();

            // 6. Build UI Canvas
            var uiResult = UIBuilder.Build();

            // 7. Mission Manager
            var missionManagerGO = new GameObject("MissionManager");
            missionManagerGO.transform.SetParent(drRoot.transform);
            var missionManager = missionManagerGO.AddComponent<MissionManager>();
            missionManager.Missions = new List<MissionDefinition>
            {
                new MissionDefinition("find_highest", "Find the Highest Point", "Climb to the highest peak in the valley.", 100),
                new MissionDefinition("find_shelter", "Locate the Emergency Shelter", "Make your way to the designated emergency shelter.", 100),
                new MissionDefinition("find_hospital", "Locate the Hospital", "Find the medical facility in the valley.", 100),
            };

            uiResult.Hud.Missions = missionManager;
            uiResult.Hud.CompleteBanner = uiResult.Banner;
            uiResult.TerrainInfo.TrackedTarget = player.transform;

            // 8. Emergency System (Landslide scenario + Safe Direction Guide)
            var safeGuide = player.AddComponent<SafeDirectionGuide>();
            safeGuide.StatusText = uiResult.SafeDirectionText;
            safeGuide.StatusPanel = uiResult.SafeDirectionPlate;
            safeGuide.Objective = emergencyObjectiveTrigger.transform;

            var emergencyGO = new GameObject("EmergencySystem");
            emergencyGO.transform.SetParent(drRoot.transform);
            emergencyGO.SetActive(false);
            var emergencyController = emergencyGO.AddComponent<EmergencyScenarioController>();
            emergencyController.Missions = missionManager;
            emergencyController.Hud = uiResult.Hud;
            emergencyController.Player = player.GetComponent<PlayerController>();
            emergencyController.Guide = safeGuide;
            emergencyController.ResultPanel = uiResult.ResultPanel;
            emergencyController.EmergencyStatusText = uiResult.EmergencyStatusText;
            emergencyController.EmergencyStatusPlate = uiResult.EmergencyStatusPlate;
            emergencyController.SummitLabelText = summitLabel;
            emergencyController.Camera = orbitCam;
            emergencyGO.SetActive(true);

            // 9. Bootstrapper
            var bootstrapGO = new GameObject("GameBootstrapper");
            bootstrapGO.transform.SetParent(drRoot.transform);
            var bootstrapper = bootstrapGO.AddComponent<GameBootstrapper>();
            bootstrapper.TitleScreen = uiResult.Title;
            bootstrapper.Hud = uiResult.Hud;
            bootstrapper.Player = player.GetComponent<PlayerController>();
            bootstrapper.OrbitCamera = orbitCam;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DisasterReadyTransplanter] SUCCESS! DisasterReady gameplay systems saved in {scene.name}.");
        }

        private static MissionZoneTrigger CreatePoiMarker(Transform parent, string name, Vector3 pos, PoiType type, string labelText, Color markerColor)
        {
            return CreatePoiMarker(parent, name, pos, type, labelText, markerColor, out _);
        }

        private static MissionZoneTrigger CreatePoiMarker(Transform parent, string name, Vector3 pos, PoiType type, string labelText, Color markerColor, out Text labelComp)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 8f;
            col.center = new Vector3(0f, 2f, 0f);

            var trigger = go.AddComponent<MissionZoneTrigger>();
            var poi = go.AddComponent<PoiMarker>();
            poi.Type = type;

            // Visual beacon cylinder
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "MarkerPole";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(0.35f, 4f, 0.35f);
            visual.transform.localPosition = new Vector3(0f, 4f, 0f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = markerColor;
            visual.GetComponent<Renderer>().sharedMaterial = mat;

            // 3D Billboard Label
            var canvasGO = new GameObject("WorldCanvas");
            canvasGO.transform.SetParent(go.transform, false);
            canvasGO.transform.localPosition = new Vector3(0f, 9.5f, 0f);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<BillboardLabel>();

            var textGO = new GameObject("LabelText");
            textGO.transform.SetParent(canvasGO.transform, false);
            var text = textGO.AddComponent<Text>();
            text.text = labelText;
            text.font = UIBuilder.GetDefaultFont();
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = markerColor;
            text.rectTransform.sizeDelta = new Vector2(350f, 65f);
            textGO.transform.localScale = Vector3.one * 0.04f;

            labelComp = text;
            return trigger;
        }

        private static void SetSceneAsBuildIndexZero(string scenePath)
        {
            var currentScenes = EditorBuildSettings.scenes;
            var list = new List<EditorBuildSettingsScene>(currentScenes);
            list.RemoveAll(s => s.path == scenePath);
            list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[DisasterReadyTransplanter] Set {scenePath} as Scene 0 in Build Settings.");
        }
    }
}
