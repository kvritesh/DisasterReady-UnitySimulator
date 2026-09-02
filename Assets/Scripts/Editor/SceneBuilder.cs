using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using DisasterReady.Terrain;
using DisasterReady.Missions;
using DisasterReady.UI;
using DisasterReady.Core;
using DisasterReady.Emergency;

namespace DisasterReady.EditorTools
{
    /// <summary>
    /// Editor-time scene assembly for the DisasterReady vertical slice demo.
    /// Run via menu: DisasterReady > Build Demo Scene.
    ///
    /// This builds the whole playable scene from code (terrain, lighting, trees,
    /// buildings, POIs, player, camera, UI) so the result is fully reproducible and
    /// none of it depends on hand-authored scene YAML.
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/DisasterReadyDemo.unity";
        public static readonly Vector3 TerrainSize = new Vector3(400f, 85f, 400f);
        public const int HeightmapResolution = 129;
        public const int Seed = 1337;

        [MenuItem("DisasterReady/Build Demo Scene")]
        public static void BuildDemoScene()
        {
            Debug.Log("[SceneBuilder] Starting DisasterReady demo scene build...");

            EnsureFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var terrainResult = BuildTerrainAndLighting();
            var envResult = EnvironmentBuilder.Populate(terrainResult);
            var uiResult = UIBuilder.Build();

            // ---- Missions ----
            var missionManagerGO = new GameObject("MissionManager");
            var missionManager = missionManagerGO.AddComponent<MissionManager>();
            missionManager.Missions = new List<MissionDefinition>
            {
                new MissionDefinition("find_highest", "Find the Highest Point", "Climb to the highest ridge in the valley.", 100),
                new MissionDefinition("find_shelter", "Locate the Emergency Shelter", "Make your way to the Emergency Shelter.", 100),
                new MissionDefinition("find_hospital", "Locate the Hospital", "Find the Hospital in the valley.", 100),
            };
            envResult.SummitTrigger.MissionId = "find_highest";
            envResult.ShelterTrigger.MissionId = "find_shelter";
            envResult.HospitalTrigger.MissionId = "find_hospital";

            // ---- UI wiring ----
            uiResult.Hud.Missions = missionManager;
            uiResult.Hud.CompleteBanner = uiResult.Banner;
            uiResult.TerrainInfo.TrackedTarget = envResult.Player.transform;

            // ---- Emergency scenario ----
            // Reuses the existing summit MissionZoneTrigger's GameObject (already an
            // "appropriately elevated" marked destination with a flagpole + label) as
            // the "reach higher terrain" objective, instead of authoring a second
            // elevated location. See EmergencyObjectiveTrigger / EmergencyScenarioController.
            var safeGuide = envResult.Player.AddComponent<SafeDirectionGuide>();
            safeGuide.StatusText = uiResult.SafeDirectionText;
            safeGuide.StatusPanel = uiResult.SafeDirectionPlate;
            var emergencyObjectiveTrigger = envResult.SummitTrigger.gameObject.AddComponent<EmergencyObjectiveTrigger>();
            // Point the guide at the exact same Transform the completion trigger
            // lives on, so the arrow can never point somewhere other than the
            // thing that actually finishes the emergency sequence.
            safeGuide.Objective = emergencyObjectiveTrigger.transform;

            var emergencyGO = new GameObject("EmergencySystem");
            // Deactivate before AddComponent so Awake()/OnEnable() don't fire until
            // SetActive(true) below - mirrors the same pattern UIBuilder already uses
            // for hudRoot (see BuildHudPanel), and matters here for the same reason:
            // OnEnable subscribes to Missions.OnAllMissionsComplete, and Missions is
            // only assigned on the next few lines. Without this, AddComponent would
            // run OnEnable synchronously while every field below is still null/unset,
            // and the subscription would silently never happen.
            emergencyGO.SetActive(false);
            var emergencyController = emergencyGO.AddComponent<EmergencyScenarioController>();
            emergencyController.Missions = missionManager;
            emergencyController.Hud = uiResult.Hud;
            emergencyController.Player = envResult.Player.GetComponent<DisasterReady.Player.PlayerController>();
            emergencyController.Guide = safeGuide;
            emergencyController.ResultPanel = uiResult.ResultPanel;
            emergencyController.EmergencyStatusText = uiResult.EmergencyStatusText;
            emergencyController.EmergencyStatusPlate = uiResult.EmergencyStatusPlate;
            emergencyController.SummitLabelText = envResult.SummitLabelText;
            emergencyController.Camera = envResult.MainCamera.GetComponent<DisasterReady.CameraSystem.OrbitCameraController>();
            emergencyGO.SetActive(true);

            // ---- Bootstrapper ----
            var bootstrapGO = new GameObject("GameBootstrapper");
            var bootstrapper = bootstrapGO.AddComponent<GameBootstrapper>();
            bootstrapper.TitleScreen = uiResult.Title;
            bootstrapper.Hud = uiResult.Hud;
            bootstrapper.Player = envResult.Player.GetComponent<DisasterReady.Player.PlayerController>();
            bootstrapper.OrbitCamera = envResult.MainCamera.GetComponent<DisasterReady.CameraSystem.OrbitCameraController>();

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AddSceneToBuildSettings(ScenePath, makeFirst: false);

            Debug.Log(saved
                ? $"[SceneBuilder] Demo scene built and saved to {ScenePath}"
                : "[SceneBuilder] Scene build finished but SaveScene reported failure - check console.");
        }

        [MenuItem("DisasterReady/Set DisasterReadyDemo As Default Scene")]
        public static void SetAsDefaultScene()
        {
            AddSceneToBuildSettings(ScenePath, makeFirst: true);
            Debug.Log("[SceneBuilder] DisasterReadyDemo.unity set as build index 0 (default scene).");
        }

        private static void AddSceneToBuildSettings(string path, bool makeFirst)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            var entry = new EditorBuildSettingsScene(path, true);
            if (makeFirst) scenes.Insert(0, entry);
            else scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "Scenes");
            CreateFolder("Assets", "Terrain");
            CreateFolder("Assets/Terrain", "Layers");
            CreateFolder("Assets", "Art");
            CreateFolder("Assets/Art", "Materials");
        }

        internal static void CreateFolder(string parent, string name)
        {
            string full = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        internal class TerrainBuildResult
        {
            public GameObject TerrainObject;
            public UnityEngine.Terrain Terrain;
            public TerrainData Data;
            public Vector3 Origin;
            public Vector3 Size;
            public ProceduralHeightSource Source;
        }

        private static TerrainBuildResult BuildTerrainAndLighting()
        {
            // ---- Terrain data ----
            var terrainData = new TerrainData();
            var source = new ProceduralHeightSource(Seed);
            TerrainGenerator.Apply(terrainData, source, HeightmapResolution, TerrainSize);

            BuildAndPaintLayers(terrainData);

            SharedAssetUtility.CreateOrReplaceAsset(terrainData, "Assets/Terrain/DisasterReadyTerrain.asset");

            var terrainGO = UnityEngine.Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = "AizawlMizoramTerrain";
            Vector3 origin = new Vector3(-TerrainSize.x * 0.5f, 0f, -TerrainSize.z * 0.5f);
            terrainGO.transform.position = origin;

            var terrain = terrainGO.GetComponent<UnityEngine.Terrain>();
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            var provider = terrainGO.AddComponent<ProceduralTerrainDataProvider>();
            provider.DemonstrationLabel = "DEMONSTRATION TERRAIN — PROTOTYPE DATA";

            // ---- Lighting ----
            var sunGO = new GameObject("Sun");
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.78f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sunGO.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.65f, 0.75f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.5f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.24f);

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var skyMat = new Material(skyShader);
                skyMat.SetColor("_SkyTint", new Color(0.85f, 0.82f, 0.95f));
                skyMat.SetColor("_GroundColor", new Color(0.55f, 0.5f, 0.42f));
                skyMat.SetFloat("_SunSize", 0.06f);
                skyMat.SetFloat("_AtmosphereThickness", 0.9f);
                skyMat.SetFloat("_Exposure", 1.15f);
                SharedAssetUtility.CreateOrReplaceAsset(skyMat, "Assets/Art/DemoSkybox.mat");
                RenderSettings.skybox = skyMat;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.86f, 0.83f, 0.78f);
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 420f;

            BuildPostProcessVolume();

            return new TerrainBuildResult
            {
                TerrainObject = terrainGO,
                Terrain = terrain,
                Data = terrainData,
                Origin = origin,
                Size = TerrainSize,
                Source = source,
            };
        }

        private static void BuildPostProcessVolume()
        {
            var volumeGO = new GameObject("Global Volume");
            var volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.saturation.overrideState = true; colorAdj.saturation.value = 10f;
            colorAdj.postExposure.overrideState = true; colorAdj.postExposure.value = 0.1f;
            colorAdj.colorFilter.overrideState = true; colorAdj.colorFilter.value = new Color(1f, 0.97f, 0.9f);

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.3f;
            bloom.threshold.overrideState = true; bloom.threshold.value = 1.05f;

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true; vignette.intensity.value = 0.2f;
            vignette.smoothness.overrideState = true; vignette.smoothness.value = 0.6f;

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.overrideState = true; tonemap.mode.value = TonemappingMode.ACES;

            SharedAssetUtility.CreateOrReplaceAsset(profile, "Assets/Art/DemoPostProcessProfile.asset");
            volume.sharedProfile = profile;
        }

        private static void BuildAndPaintLayers(TerrainData terrainData)
        {
            var grass = CreateTerrainLayer("Grass", new Color(0.42f, 0.55f, 0.28f), 45f);
            var rock = CreateTerrainLayer("Rock", new Color(0.5f, 0.47f, 0.44f), 30f);
            var snow = CreateTerrainLayer("Snow", new Color(0.92f, 0.93f, 0.95f), 25f);

            terrainData.terrainLayers = new[] { grass, rock, snow };

            int alphaRes = terrainData.alphamapResolution;
            var maps = new float[alphaRes, alphaRes, 3];
            for (int z = 0; z < alphaRes; z++)
            {
                float nz = (float)z / (alphaRes - 1);
                for (int x = 0; x < alphaRes; x++)
                {
                    float nx = (float)x / (alphaRes - 1);
                    float height01 = terrainData.GetInterpolatedHeight(nx, nz) / terrainData.size.y;
                    Vector3 normal = terrainData.GetInterpolatedNormal(nx, nz);
                    float slopeDeg = Vector3.Angle(normal, Vector3.up);

                    float rockW = Mathf.Clamp01((slopeDeg - 22f) / 20f);
                    float snowW = Mathf.Clamp01((height01 - 0.66f) / 0.14f) * (1f - rockW);
                    float grassW = Mathf.Clamp01(1f - rockW - snowW);

                    float sum = rockW + snowW + grassW;
                    if (sum < 0.0001f) { grassW = 1f; sum = 1f; }

                    maps[z, x, 0] = grassW / sum;
                    maps[z, x, 1] = rockW / sum;
                    maps[z, x, 2] = snowW / sum;
                }
            }
            terrainData.SetAlphamaps(0, 0, maps);
        }

        private static TerrainLayer CreateTerrainLayer(string name, Color color, float tileSize)
        {
            var tex = SharedAssetUtility.CreateSolidTexture(color, $"Assets/Terrain/Layers/{name}Tex.png");
            var layer = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(tileSize, tileSize) };
            SharedAssetUtility.CreateOrReplaceAsset(layer, $"Assets/Terrain/Layers/{name}.terrainlayer");
            return layer;
        }
    }
}
