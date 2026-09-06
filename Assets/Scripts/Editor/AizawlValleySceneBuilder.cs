using System;
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
    /// Architectural world builder for the dedicated Aizawl Mountain Valley environment
    /// (Assets/Scenes/DisasterReady_AizawlValley.unity).
    /// </summary>
    [InitializeOnLoad]
    public static class AizawlValleySceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/DisasterReady_AizawlValley.unity";
        public const string ScreenshotsDir = @"C:\Users\kvrit\.gemini\antigravity\brain\5f86b6ff-d590-4484-867b-2dde9099cfe9";

        // Synty asset paths
        private const string GroundFlatPrefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Ground_Flat_01.prefab";
        private const string Ground01Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Ground_01.prefab";
        private const string Ground02Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Ground_02.prefab";
        private const string Ground03Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Ground_03.prefab";
        private const string Ground04Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Ground_04.prefab";

        private const string MountainPeakPrefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Mountains_Grass_02.prefab";
        private const string MountainRidgePrefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Mountains_Soft_01.prefab";

        private const string SyntyHousePrefab = "Assets/Synty/PolygonStarter/Prefabs/SM_PolygonApocalypse_Bld_House_01.prefab";
        private const string SyntyCarPrefab = "Assets/Synty/PolygonStarter/Prefabs/SM_PolygonCity_Veh_Car_Small_01.prefab";
        private const string SyntyConePrefab = "Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Prop_Cone_01.prefab";
        private const string SyntyCratePrefab = "Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Prop_Crate_03.prefab";
        private const string SyntySkyDomePrefab = "Assets/Synty/PolygonStarter/Prefabs/SM_SimpleSky_Dome_01.prefab";

        private const string SyntyTree01Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Tree_01.prefab";
        private const string SyntyTree02Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Tree_02.prefab";
        private const string SyntyTree03Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Tree_03.prefab";
        private const string SyntyTree04Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Tree_04.prefab";
        private const string SyntyTreeDeadPrefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_TreeDead_01.prefab";
        private const string SyntyRock01Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Small_Rocks_01.prefab";
        private const string SyntyRock02Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Small_Rocks_02.prefab";
        private const string SyntyRock03Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Small_Rocks_03.prefab";
        private const string SyntyRock04Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Small_Rocks_04.prefab";
        private const string SyntyRock05Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Small_Rocks_05.prefab";

        private const string SyntyRamp25Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Buildings_Ramp_25_1x1_01P.prefab";
        private const string SyntyStairsPrefab = "Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Buildings_Stairs_1x3_01P.prefab";
        private const string SyntyFloor5x5Prefab = "Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Buildings_Floor_5x5_01P.prefab";

        // KayKit asset paths
        private const string KayKitCityDir = "Assets/KayKit/CityBuilder/Assets/fbx (unity)";
        private const string KayKitForestDir = "Assets/KayKit/Forest/Assets/fbx(unity)";

        private static bool _autoBuildDone = false;

        [InitializeOnLoadMethod]
        private static void OnInit()
        {
            EditorApplication.delayCall += () =>
            {
                // Play Mode always wins: never force-exit Play Mode and never rebuild
                // while entering, inside, or exiting a Play Mode session. Auto-build
                // only runs during genuine Edit Mode initialization.
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }
                if (!_autoBuildDone)
                {
                    _autoBuildDone = true;
                    BuildScene();
                }
            };
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.delayCall += () =>
                {
                    if (!_autoBuildDone)
                    {
                        _autoBuildDone = true;
                        BuildScene();
                    }
                };
            }
        }

        [MenuItem("DisasterReady/Build Aizawl Valley Scene (Dedicated)")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[AizawlValleySceneBuilder] Exiting Play Mode first...");
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => BuildScene();
                return;
            }
            Debug.Log("[AizawlValleySceneBuilder] ==============================================");
            Debug.Log("[AizawlValleySceneBuilder] BUILDING AIZAWL VALLEY (TERRACED MOUNTAIN CITY)");
            Debug.Log("[AizawlValleySceneBuilder] ==============================================");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Root Hierarchy setup
            var envRoot = new GameObject("Environment");
            var infraRoot = new GameObject("Infrastructure");
            var navRoot = new GameObject("Navigation");
            var gpRoot = new GameObject("DisasterReady_Gameplay");

            // Sub-hierarchies
            var terrainFolder = CreateChild(envRoot.transform, "Terrain");
            var mountainsFolder = CreateChild(envRoot.transform, "Mountains");
            var envRoadsFolder = CreateChild(envRoot.transform, "Roads");
            var buildingsFolder = CreateChild(envRoot.transform, "Buildings");
            var vegetationFolder = CreateChild(envRoot.transform, "Vegetation");
            var rocksFolder = CreateChild(envRoot.transform, "Rocks");
            var propsFolder = CreateChild(envRoot.transform, "Props");
            var routeMarkersFolder = CreateChild(envRoot.transform, "RouteMarkers");

            var hospitalFolder = CreateChild(infraRoot.transform, "Hospital");
            var shelterFolder = CreateChild(infraRoot.transform, "EmergencyShelter");
            var infraRoadsFolder = CreateChild(infraRoot.transform, "Roads");
            var signsFolder = CreateChild(infraRoot.transform, "Signs");
            var barriersFolder = CreateChild(infraRoot.transform, "EmergencyBarriers");

            var mainRouteFolder = CreateChild(navRoot.transform, "MainRoute");
            var hospRouteFolder = CreateChild(navRoot.transform, "HospitalRoute");
            var shelterRouteFolder = CreateChild(navRoot.transform, "ShelterRoute");
            var summitRouteFolder = CreateChild(navRoot.transform, "SummitRoute");

            // 2. Lighting & Atmosphere
            SetupLighting(envRoot.transform);

            // 3. Terraced Valley Floor & Retaining Walls
            BuildTerracedValley(terrainFolder.transform, rocksFolder.transform);

            // 3b. Ramp-edge guardrail bollards (terrain readability)
            BuildRampGuardrails(terrainFolder.transform);

            // 4. Perimeter Mountain Amphitheater & High Summit
            Vector3 summitPeakPos = BuildMountains(mountainsFolder.transform, terrainFolder.transform);

            // 4b. Summit safe-zone edge marking & signage (visual polish)
            BuildSummitSafetyFeatures(mountainsFolder.transform, summitPeakPos);

            // 4c. Distant background settlement silhouette (macro composition)
            BuildBackgroundSettlementSilhouette(mountainsFolder.transform);

            // 5. Contoured Road Network (Flush on ground surface)
            BuildRoadNetwork(envRoadsFolder.transform, infraRoadsFolder.transform, terrainFolder.transform);

            // 6. Hospital Complex
            Vector3 hospitalPos = BuildHospitalComplex(hospitalFolder.transform, infraRoadsFolder.transform);

            // 6b. Hospital realism/readability detail pass
            BuildHospitalDetailPass(hospitalFolder.transform, hospitalPos);

            // 7. Emergency Shelter Complex
            Vector3 shelterPos = BuildShelterComplex(shelterFolder.transform, infraRoadsFolder.transform);

            // 8. Disaster Storytelling (Landslide Hazard Zone)
            BuildLandslideHazardZone(barriersFolder.transform, rocksFolder.transform, signsFolder.transform, envRoadsFolder.transform);

            // 8b. Landslide storytelling detail pass (hazard tape, rubble, damaged wall)
            BuildLandslideDetailPass(barriersFolder.transform, rocksFolder.transform, signsFolder.transform);

            // 8c. Landslide hero set piece upgrade (vertical slice pass)
            BuildLandslideHeroUpgrade(barriersFolder.transform, rocksFolder.transform, signsFolder.transform);

            // 9. Terraced Residential Settlement
            BuildResidentialSettlement(buildingsFolder.transform, propsFolder.transform);

            // 9b. Hillside residential density fill (vertical slice pass)
            BuildHillsideResidentialClusters(buildingsFolder.transform, propsFolder.transform, vegetationFolder.transform);

            // 10. Vegetation & Mountain Trees
            BuildVegetationAndRocks(vegetationFolder.transform, rocksFolder.transform);

            // 11. Navigation Waypoint Anchors
            Vector3 spawnPos = new Vector3(0f, 0.52f, -32f);
            BuildNavigationAnchors(mainRouteFolder.transform, hospRouteFolder.transform, shelterRouteFolder.transform, summitRouteFolder.transform, spawnPos, hospitalPos, shelterPos, summitPeakPos);

            // 11b. In-world evacuation route markers (environmental, not screen UI)
            BuildRouteMarkers(routeMarkersFolder.transform);

            // 12. DisasterReady Gameplay Systems Integration
            var gpResult = IntegrateGameplay(gpRoot.transform, spawnPos, hospitalPos, shelterPos, summitPeakPos);

            // 13. Save Scene & Register as Scene 0
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            SetSceneAsBuildIndexZero(ScenePath);

            Debug.Log("[AizawlValleySceneBuilder] ==============================================");
            Debug.Log($"[AizawlValleySceneBuilder] SUCCESS! Aizawl Valley scene saved to {ScenePath}");
            Debug.Log("[AizawlValleySceneBuilder] ==============================================");

            // 14. Capture Inspection Screenshots
            CaptureInspectionScreenshots(gpResult.Player, gpResult.MainCamera, hospitalPos, shelterPos, summitPeakPos);
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetupLighting(Transform envParent)
        {
            var sunGO = new GameObject("Sun_DirectionalLight");
            sunGO.transform.SetParent(envParent, false);
            var light = sunGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.7f;
            sunGO.transform.rotation = Quaternion.Euler(48f, 32f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.82f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.60f, 0.70f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.35f, 0.30f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0028f;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.90f);

            var skyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntySkyDomePrefab);
            if (skyPrefab != null)
            {
                var sky = (GameObject)PrefabUtility.InstantiatePrefab(skyPrefab, envParent);
                sky.name = "SkyDome";
                sky.transform.position = new Vector3(0f, -10f, 15f);
                sky.transform.localScale = new Vector3(12f, 12f, 12f);
                StripColliders(sky);
            }
        }
        private static void BuildTerracedValley(Transform terrainParent, Transform rocksFolder)
        {
            var flatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GroundFlatPrefab);
            var rampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyRamp25Prefab);
            var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyRock02Prefab);

            // Foundational Valley Underlay (ensures zero skybox/void gaps anywhere in the valley basin)
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_Underlay_L0", new Vector3(0f, -0.3f, -30f), new Vector3(4.0f, 1f, 3.2f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_Underlay_L1", new Vector3(0f,  1.4f,   6f), new Vector3(4.5f, 1f, 3.2f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_Underlay_L2", new Vector3(0f,  4.2f,  36f), new Vector3(4.0f, 1f, 2.5f));

            // Level 0: Lower Valley & Entrance Basin (Ground pos Y = 0f, Top surface Y = 0.46f)
            // 6 overlapping tiles covering X = [-30, 30], Z = [-44, -12]
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L0_SW", new Vector3(-12f, 0f, -32f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L0_SC", new Vector3(  0f, 0f, -32f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L0_SE", new Vector3( 12f, 0f, -32f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L0_NW", new Vector3(-12f, 0f, -18f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L0_NC", new Vector3(  0f, 0f, -18f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L0_NE", new Vector3( 12f, 0f, -18f), new Vector3(1.5f, 1f, 1.5f));

            // Earthen Terrace Embankment Berm under retaining rocks (fills vertical riser, eliminates white/sky gap)
            var bermMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bermMat.color = new Color(0.38f, 0.44f, 0.32f); // Earthy olive hill slope

            var bermL0W = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bermL0W.name = "TerraceBerm_L0_L1_West";
            bermL0W.transform.SetParent(terrainParent, false);
            bermL0W.transform.position = new Vector3(-17.5f, 1.25f, -13.5f);
            bermL0W.transform.localScale = new Vector3(27f, 2.5f, 2.5f);
            EnsureBoxCollider(bermL0W, Vector3.one, Vector3.zero);
            SetStaticFlags(bermL0W);
            bermL0W.GetComponent<Renderer>().sharedMaterial = bermMat;

            var bermL0E = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bermL0E.name = "TerraceBerm_L0_L1_East";
            bermL0E.transform.SetParent(terrainParent, false);
            bermL0E.transform.position = new Vector3(17.5f, 1.25f, -13.5f);
            bermL0E.transform.localScale = new Vector3(27f, 2.5f, 2.5f);
            EnsureBoxCollider(bermL0E, Vector3.one, Vector3.zero);
            SetStaticFlags(bermL0E);
            bermL0E.GetComponent<Renderer>().sharedMaterial = bermMat;

            // Retaining Embankment Rocks along Level 0 to Level 1 step (Z = -13.5f)
            if (rockPrefab != null)
            {
                for (float x = -30f; x <= 30f; x += 3.5f)
                {
                    if (Mathf.Abs(x) < 3.5f) continue; // Gap for roadway ramp
                    var r = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, rocksFolder);
                    r.name = $"RetainingWall_L0_L1_{x}";
                    r.transform.position = new Vector3(x, 1.3f, -13.5f);
                    r.transform.localScale = new Vector3(2.5f, 2.2f, 2.0f);
                    r.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    EnsureMeshCollider(r);
                    SetStaticFlags(r);
                }
            }

            // Ramp connecting Level 0 (surface Y=0.46) to Level 1 (surface Y=2.96)
            if (rampPrefab != null)
            {
                var ramp = (GameObject)PrefabUtility.InstantiatePrefab(rampPrefab, terrainParent);
                ramp.name = "Ramp_L0_to_L1";
                ramp.transform.position = new Vector3(0f, 0.46f, -14f);
                ramp.transform.localScale = new Vector3(4.0f, 2.5f, 4.5f);
                EnsureMeshCollider(ramp);
                SetStaticFlags(ramp);
                var rampMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rampMat.color = new Color(0.24f, 0.25f, 0.26f); // Asphalt color
                foreach (var rend in ramp.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = rampMat;
            }

            // Level 1: Mid Valley Settlement & Civic Center (Ground pos Y = 1.85f, Top surface Y = 2.31f)
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L1_C1", new Vector3(  0f, 1.85f, -4f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L1_C2", new Vector3(  0f, 1.85f,  8f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L1_W1", new Vector3(-18f, 1.80f, -4f), new Vector3(1.6f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L1_W2", new Vector3(-18f, 1.80f,  8f), new Vector3(1.6f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L1_E1", new Vector3( 18f, 1.80f, -4f), new Vector3(1.6f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L1_E2", new Vector3( 18f, 1.80f,  8f), new Vector3(1.6f, 1f, 1.5f));

            // Level 1 to Level 2 Berm
            var bermL1W = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bermL1W.name = "TerraceBerm_L1_L2_West";
            bermL1W.transform.SetParent(terrainParent, false);
            bermL1W.transform.position = new Vector3(-15f, 4.25f, 15.5f);
            bermL1W.transform.localScale = new Vector3(24f, 3.0f, 2.5f);
            EnsureBoxCollider(bermL1W, Vector3.one, Vector3.zero);
            SetStaticFlags(bermL1W);
            bermL1W.GetComponent<Renderer>().sharedMaterial = bermMat;

            var bermL1E = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bermL1E.name = "TerraceBerm_L1_L2_East";
            bermL1E.transform.SetParent(terrainParent, false);
            bermL1E.transform.position = new Vector3(15f, 4.25f, 15.5f);
            bermL1E.transform.localScale = new Vector3(24f, 3.0f, 2.5f);
            EnsureBoxCollider(bermL1E, Vector3.one, Vector3.zero);
            SetStaticFlags(bermL1E);
            bermL1E.GetComponent<Renderer>().sharedMaterial = bermMat;

            // Retaining Embankment Rocks along Level 1 to Level 2 step (Z = 15.5f)
            if (rockPrefab != null)
            {
                for (float x = -26f; x <= 26f; x += 3.5f)
                {
                    if (Mathf.Abs(x) < 3.5f) continue;
                    var r = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, rocksFolder);
                    r.name = $"RetainingWall_L1_L2_{x}";
                    r.transform.position = new Vector3(x, 4.0f, 15.5f);
                    r.transform.localScale = new Vector3(2.5f, 2.2f, 2.0f);
                    r.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    EnsureMeshCollider(r);
                    SetStaticFlags(r);
                }
            }

            // Ramp connecting Level 1 (Y=2.96) to Level 2 (Y=5.96)
            if (rampPrefab != null)
            {
                var ramp2 = (GameObject)PrefabUtility.InstantiatePrefab(rampPrefab, terrainParent);
                ramp2.name = "Ramp_L1_to_L2";
                ramp2.transform.position = new Vector3(0f, 2.96f, 15.0f);
                ramp2.transform.localScale = new Vector3(4.0f, 3.0f, 5.0f);
                EnsureMeshCollider(ramp2);
                SetStaticFlags(ramp2);
                var rampMat2 = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rampMat2.color = new Color(0.24f, 0.25f, 0.26f); // Asphalt color
                foreach (var rend in ramp2.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = rampMat2;
            }

            // Level 2: Upper Residential Ridge (Ground pos Y = 5.5f, Top surface Y = 5.96f)
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L2_W1", new Vector3(-12f, 5.5f, 27f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L2_C1", new Vector3(  0f, 5.5f, 27f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L2_E1", new Vector3( 12f, 5.5f, 27f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L2_W2", new Vector3(-12f, 5.5f, 39f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L2_C2", new Vector3(  0f, 5.5f, 39f), new Vector3(1.5f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L2_E2", new Vector3( 12f, 5.5f, 39f), new Vector3(1.5f, 1f, 1.5f));

            // Level 3: Trailhead & Foothills Base (Ground pos Y = 8.5f, Top surface Y = 8.96f)
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L3_Trail_1", new Vector3( 0f, 8.5f, 48f), new Vector3(1.6f, 1f, 1.5f));
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_L3_Trail_2", new Vector3( 0f, 8.5f, 62f), new Vector3(1.6f, 1f, 1.5f));
            if (rampPrefab != null)
            {
                var ramp3 = (GameObject)PrefabUtility.InstantiatePrefab(rampPrefab, terrainParent);
                ramp3.name = "Ramp_L2_to_L3";
                ramp3.transform.position = new Vector3(0f, 5.96f, 41.5f);
                ramp3.transform.localScale = new Vector3(4.0f, 3.0f, 5.0f);
                EnsureMeshCollider(ramp3);
                SetStaticFlags(ramp3);
                var rampMat3 = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rampMat3.color = new Color(0.48f, 0.46f, 0.40f);
                foreach (var rend in ramp3.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = rampMat3;
            }
        }

        private static void PlaceFlatGround(GameObject prefab, Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            EnsureMeshCollider(go);
            SetStaticFlags(go);
        }

        private static Vector3 BuildMountains(Transform mountainsParent, Transform terrainParent)
        {
            var peakPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MountainPeakPrefab);
            var ridgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MountainRidgePrefab);
            var stairsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyStairsPrefab);

            // 1. THE SUMMIT - Safe Zone High Lookout Deck at (0, 13.5, 62)
            Vector3 summitLookoutPos = new Vector3(0f, 13.5f, 62f);

            // Summit Mountain Platform (elevated ground under the deck)
            var flatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GroundFlatPrefab);
            PlaceFlatGround(flatPrefab, terrainParent, "Ground_SummitDeck_Base", new Vector3(0f, 12.8f, 62f), new Vector3(1.2f, 1f, 1.2f));

            // Stairs climbing from Level 3 (surface Y=8.96, Z=54) to Summit Lookout (Y=13.5, Z=62)
            if (stairsPrefab != null)
            {
                var st1 = (GameObject)PrefabUtility.InstantiatePrefab(stairsPrefab, terrainParent);
                st1.name = "SummitAscent_Stairs_Lower";
                st1.transform.position = new Vector3(0f, 8.6f, 54f);
                st1.transform.localScale = new Vector3(2.5f, 2.3f, 2.5f);
                EnsureMeshCollider(st1);
                SetStaticFlags(st1);
                var stMat1 = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                stMat1.color = new Color(0.50f, 0.49f, 0.45f);
                foreach (var rend in st1.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = stMat1;

                var st2 = (GameObject)PrefabUtility.InstantiatePrefab(stairsPrefab, terrainParent);
                st2.name = "SummitAscent_Stairs_Upper";
                st2.transform.position = new Vector3(0f, 10.8f, 58f);
                st2.transform.localScale = new Vector3(2.5f, 2.3f, 2.5f);
                EnsureMeshCollider(st2);
                SetStaticFlags(st2);
                var stMat2 = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                stMat2.color = new Color(0.50f, 0.49f, 0.45f);
                foreach (var rend in st2.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = stMat2;
            }

            // Summit Evacuee Observation Bench
            string benchFbx = $"{KayKitCityDir}/bench.fbx";
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(benchFbx);
            if (benchPrefab != null)
            {
                var sBench = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, mountainsParent);
                sBench.name = "Summit_Evacuee_Bench";
                sBench.transform.position = new Vector3(1.2f, 13.26f, 60.5f);
                sBench.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
                sBench.transform.localScale = Vector3.one * 1.6f;
                StripColliders(sBench);
            }

            // Summit Emergency Communication Mast
            var mastGO = new GameObject("Summit_Emergency_CommsMast");
            mastGO.transform.SetParent(mountainsParent, false);
            mastGO.transform.position = new Vector3(-2.2f, 13.26f, 61.5f);
            var mastPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mastPole.name = "MastPole";
            mastPole.transform.SetParent(mastGO.transform, false);
            mastPole.transform.localScale = new Vector3(0.18f, 3.5f, 0.18f);
            mastPole.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            UnityEngine.Object.DestroyImmediate(mastPole.GetComponent<Collider>());
            var mastMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mastMat.color = new Color(0.85f, 0.2f, 0.2f);
            mastPole.GetComponent<Renderer>().sharedMaterial = mastMat;

            var mastLight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mastLight.name = "WarningBeaconSphere";
            mastLight.transform.SetParent(mastGO.transform, false);
            mastLight.transform.localScale = Vector3.one * 0.5f;
            mastLight.transform.localPosition = new Vector3(0f, 7.1f, 0f);
            UnityEngine.Object.DestroyImmediate(mastLight.GetComponent<Collider>());
            var beaconMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            beaconMat.color = new Color(1.0f, 0.9f, 0.1f);
            mastLight.GetComponent<Renderer>().sharedMaterial = beaconMat;

            // Mountain Massif rising behind the observation deck
            if (peakPrefab != null)
            {
                var massif = (GameObject)PrefabUtility.InstantiatePrefab(peakPrefab, mountainsParent);
                massif.name = "Mountain_SummitMassif_Backdrop";
                massif.transform.position = new Vector3(0f, 6.0f, 74f);
                massif.transform.localScale = new Vector3(4.0f, 2.8f, 3.5f);
                EnsureMeshCollider(massif);
                SetStaticFlags(massif);

                // West and East shoulders
                var leftPeak = (GameObject)PrefabUtility.InstantiatePrefab(peakPrefab, mountainsParent);
                leftPeak.name = "Mountain_Summit_WestShoulder";
                leftPeak.transform.position = new Vector3(-28f, 5.0f, 68f);
                leftPeak.transform.localScale = new Vector3(3.2f, 2.4f, 3.0f);
                EnsureMeshCollider(leftPeak);
                SetStaticFlags(leftPeak);

                var rightPeak = (GameObject)PrefabUtility.InstantiatePrefab(peakPrefab, mountainsParent);
                rightPeak.name = "Mountain_Summit_EastShoulder";
                rightPeak.transform.position = new Vector3(28f, 5.0f, 68f);
                rightPeak.transform.localScale = new Vector3(3.2f, 2.4f, 3.0f);
                EnsureMeshCollider(rightPeak);
                SetStaticFlags(rightPeak);
            }

            // 2. Surrounding Perimeter Mountain Wall Enclosure
            if (ridgePrefab != null)
            {
                // North Backdrop Ridge
                PlaceRidge(ridgePrefab, mountainsParent, "Mountain_NorthWall_1", new Vector3(-42f, 6f, 80f), Quaternion.Euler(0f, 20f, 0f), new Vector3(2.8f, 3.2f, 2.4f));
                PlaceRidge(ridgePrefab, mountainsParent, "Mountain_NorthWall_2", new Vector3(  0f, 8f, 82f), Quaternion.Euler(0f,  0f, 0f), new Vector3(3.0f, 3.6f, 2.4f));
                PlaceRidge(ridgePrefab, mountainsParent, "Mountain_NorthWall_3", new Vector3( 42f, 6f, 80f), Quaternion.Euler(0f, -20f, 0f), new Vector3(2.8f, 3.2f, 2.4f));

                // West Mountain Wall (Positioned cleanly behind hospital without foothills intruding)
                PlaceRidge(ridgePrefab, mountainsParent, "Mountain_WestWall_Upper", new Vector3(-54f, 5.0f, 42f), Quaternion.Euler(0f, 85f, 0f), new Vector3(2.2f, 3.2f, 1.8f));
                PlaceRidge(ridgePrefab, mountainsParent, "Mountain_WestWall_Mid",   new Vector3(-68f, 2.5f,  4f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2.0f, 3.2f, 1.8f));
                PlaceRidge(ridgePrefab, mountainsParent, "Mountain_WestWall_Lower", new Vector3(-62f, 0.5f, -32f), Quaternion.Euler(0f, 80f, 0f), new Vector3(2.0f, 2.8f, 1.8f));

                // East Mountain Wall (Positioned cleanly behind shelter without foothills intruding)
                PlaceRidge(ridgePrefab, mountainsParent, "Mountain_EastWall_Upper", new Vector3( 54f, 5.0f, 42f), Quaternion.Euler(0f, -85f, 0f), new Vector3(2.2f, 3.2f, 1.8f));
                PlaceRidge(ridgePrefab, mountainsParent, "Mountain_EastWall_Mid",   new Vector3( 68f, 2.5f,  4f), Quaternion.Euler(0f, -90f, 0f), new Vector3(2.0f, 3.2f, 1.8f));
                PlaceRidge(ridgePrefab, mountainsParent, "Mountain_EastWall_Lower", new Vector3( 62f, 0.5f, -32f), Quaternion.Euler(0f, -80f, 0f), new Vector3(2.0f, 2.8f, 1.8f));

                // South Valley Entrance Enclosure
                PlaceRidge(ridgePrefab, mountainsParent, "Mountain_SouthWall_W", new Vector3(-38f, -0.5f, -54f), Quaternion.Euler(0f, 120f, 0f), new Vector3(2.4f, 2.6f, 2.0f));
                PlaceRidge(ridgePrefab, mountainsParent, "Mountain_SouthWall_E", new Vector3( 38f, -0.5f, -54f), Quaternion.Euler(0f, -120f, 0f), new Vector3(2.4f, 2.6f, 2.0f));
            }

            return summitLookoutPos;
        }

        private static void PlaceRidge(GameObject prefab, Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;
            EnsureMeshCollider(go);
            SetStaticFlags(go);
        }

        private static void BuildRoadNetwork(Transform envRoadsParent, Transform infraRoadsParent, Transform terrainParent)
        {
            string straightFbx = $"{KayKitCityDir}/road_straight.fbx";
            string curveFbx = $"{KayKitCityDir}/road_corner_curved.fbx";
            string junctionFbx = $"{KayKitCityDir}/road_junction.fbx";

            // 1. Level 0 Road (Flush on grass surface at Y = 0.49f)
            for (float z = -34f; z <= -16f; z += 2.0f)
            {
                PlaceRoadStraight(straightFbx, envRoadsParent, $"Road_L0_Spine_{z}", new Vector3(0f, 0.49f, z), 90f);
            }

            // 2. Level 1 Town Center Roads (Flush on ground surface at Y = 2.99f)
            // Starts directly at ramp exit Z = -11.5f
            for (float z = -11.5f; z <= -9.5f; z += 2.0f)
            {
                PlaceRoadStraight(straightFbx, envRoadsParent, $"Road_L1_Approach_{z}", new Vector3(0f, 2.99f, z), 90f);
            }

            // 4-Way Junction at (0, 2.99, -8)
            PlaceRoadTile(junctionFbx, infraRoadsParent, "Road_CentralJunction", new Vector3(0f, 2.99f, -8f), Quaternion.Euler(0f, 0f, 0f));

            // West Spur: Road toward Hospital (from X = -2 to -18 at Z = -8)
            for (float x = -2f; x >= -18f; x -= 2.0f)
            {
                PlaceRoadStraight(straightFbx, infraRoadsParent, $"Road_HospBranch_{x}", new Vector3(x, 2.99f, -8f), 0f);
            }
            // Curved corner turning North into Hospital Apron at (-20, 2.99, -8)
            PlaceRoadTile(curveFbx, infraRoadsParent, "Road_HospCurve", new Vector3(-20f, 2.99f, -8f), Quaternion.Euler(0f, 90f, 0f));
            for (float z = -6f; z <= 6f; z += 2.0f)
            {
                PlaceRoadStraight(straightFbx, infraRoadsParent, $"Road_HospApronRoad_{z}", new Vector3(-20f, 2.99f, z), 90f);
            }

            // East Spur: Road toward Emergency Shelter (from X = 2 to 18 at Z = -8)
            for (float x = 2f; x <= 18f; x += 2.0f)
            {
                PlaceRoadStraight(straightFbx, infraRoadsParent, $"Road_ShelterBranch_{x}", new Vector3(x, 2.99f, -8f), 0f);
            }
            // Curved corner turning North into Shelter Courtyard at (20, 2.99, -8)
            PlaceRoadTile(curveFbx, infraRoadsParent, "Road_ShelterCurve", new Vector3(20f, 2.99f, -8f), Quaternion.Euler(0f, 0f, 0f));
            for (float z = -6f; z <= 6f; z += 2.0f)
            {
                PlaceRoadStraight(straightFbx, infraRoadsParent, $"Road_ShelterCourtyardRoad_{z}", new Vector3(20f, 2.99f, z), 90f);
            }

            // North Continuation: Town Street through Level 1 (from Z = -6 to 13 at X = 0)
            for (float z = -6f; z <= 13f; z += 2.0f)
            {
                PlaceRoadStraight(straightFbx, envRoadsParent, $"Road_TownStreet_{z}", new Vector3(0f, 2.99f, z), 90f);
            }

            // 3. Level 2 Residential Ridge Road (Flush at Y = 5.99f) from Z = 17.5 to 37.5
            for (float z = 17.5f; z <= 37.5f; z += 2.0f)
            {
                PlaceRoadStraight(straightFbx, envRoadsParent, $"Road_L2_Spine_{z}", new Vector3(0f, 5.99f, z), 90f);
            }
        }

        private static void PlaceRoadStraight(string fbxPath, Transform parent, string name, Vector3 pos, float yaw, float pitch = 0f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            EnsureBoxCollider(go, new Vector3(2.0f, 0.15f, 2.0f), new Vector3(0f, 0.05f, 0f));
            SetStaticFlags(go);
        }

        private static void PlaceRoadTile(string fbxPath, Transform parent, string name, Vector3 pos, Quaternion rot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            EnsureBoxCollider(go, new Vector3(2.0f, 0.15f, 2.0f), new Vector3(0f, 0.05f, 0f));
            SetStaticFlags(go);
        }
        private enum SignType { Warning, Medical, Shelter, Welcome }

        private static Vector3 BuildHospitalComplex(Transform hospitalFolder, Transform roadsParent)
        {
            Vector3 centerPos = new Vector3(-20f, 3.0f, 9f);
            var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyFloor5x5Prefab);

            // Paved Hospital Apron (Clean concrete/asphalt apron flush with road at Y = 3.00f)
            if (floorPrefab != null)
            {
                var apron = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab, hospitalFolder);
                apron.name = "Hospital_CourtyardApron";
                apron.transform.position = new Vector3(-18f, 3.00f, 6f);
                apron.transform.localScale = new Vector3(2.5f, 0.1f, 2.5f);
                EnsureBoxCollider(apron, new Vector3(5f, 0.2f, 5f), new Vector3(0f, 0f, 0f));
                SetStaticFlags(apron);
                var aspMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                aspMat.color = new Color(0.24f, 0.25f, 0.26f);
                foreach (var rend in apron.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = aspMat;
            }

            // Main Medical Building (KayKit Building C - Dignified Multi-Story Hospital)
            string bldFbx = $"{KayKitCityDir}/building_C.fbx";
            var bldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bldFbx);
            if (bldPrefab != null)
            {
                var mainHosp = (GameObject)PrefabUtility.InstantiatePrefab(bldPrefab, hospitalFolder);
                mainHosp.name = "Hospital_MainFacility";
                mainHosp.transform.position = centerPos;
                mainHosp.transform.localScale = new Vector3(2.6f, 2.6f, 2.6f);
                mainHosp.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                EnsureBoxCollider(mainHosp, new Vector3(2f, 3.5f, 2f), new Vector3(0f, 1.75f, 0f));
                SetStaticFlags(mainHosp);

                // Emergency Ward Annex (Building F)
                string annexFbx = $"{KayKitCityDir}/building_F.fbx";
                var annexPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(annexFbx);
                if (annexPrefab != null)
                {
                    var annex = (GameObject)PrefabUtility.InstantiatePrefab(annexPrefab, hospitalFolder);
                    annex.name = "Hospital_EmergencyWard_Annex";
                    annex.transform.position = centerPos + new Vector3(-5.5f, 0f, 0f);
                    annex.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
                    annex.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    EnsureBoxCollider(annex, new Vector3(2f, 3f, 2f), new Vector3(0f, 1.5f, 0f));
                    SetStaticFlags(annex);
                }
            }

            // Medical Red Cross Insignia prominently mounted above the front entrance
            CreateMedicalCrossSign(hospitalFolder, centerPos + new Vector3(0f, 4.2f, -2.6f));

            // Emergency Ambulance parked in marked ambulance bay
            string carFbx = $"{KayKitCityDir}/car_stationwagon.fbx";
            var carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(carFbx);
            if (carPrefab != null)
            {
                var amb = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab, hospitalFolder);
                amb.name = "Hospital_Ambulance_Vehicle";
                amb.transform.position = new Vector3(-15f, 3.02f, 5.5f);
                amb.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
                amb.transform.localScale = Vector3.one * 1.3f;
                EnsureBoxCollider(amb, new Vector3(2f, 1.5f, 4f), new Vector3(0f, 0.8f, 0f));
                var ambMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                ambMat.color = new Color(0.96f, 0.96f, 0.96f);
                foreach (var rend in amb.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = ambMat;
            }

            // Streetlights
            PlaceStreetlight(hospitalFolder, new Vector3(-13f, 3.02f, 2f), 180f);
            PlaceStreetlight(hospitalFolder, new Vector3(-23f, 3.02f, 2f), 0f);

            // Hospital Entrance Signpost (Medical Green Plate at courtyard entrance)
            CreateSignpost(hospitalFolder, new Vector3(-13.5f, 3.02f, 3.0f), "AIZAWL DISTRICT HOSPITAL\nEMERGENCY SERVICES", -40f, SignType.Medical);

            // Return POI beacon anchor position at the hospital entrance
            return new Vector3(-20f, 3.02f, 6.8f);
        }

        private static void CreateMedicalCrossSign(Transform parent, Vector3 pos)
        {
            var crossGO = new GameObject("Hospital_RedCross_Insignia");
            crossGO.transform.SetParent(parent, false);
            crossGO.transform.position = pos;

            var vBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vBar.name = "Cross_V";
            vBar.transform.SetParent(crossGO.transform, false);
            vBar.transform.localScale = new Vector3(0.4f, 1.4f, 0.12f);
            UnityEngine.Object.DestroyImmediate(vBar.GetComponent<Collider>());

            var hBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hBar.name = "Cross_H";
            hBar.transform.SetParent(crossGO.transform, false);
            hBar.transform.localScale = new Vector3(1.4f, 0.4f, 0.12f);
            UnityEngine.Object.DestroyImmediate(hBar.GetComponent<Collider>());

            var redMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            redMat.color = new Color(0.92f, 0.08f, 0.08f);
            vBar.GetComponent<Renderer>().sharedMaterial = redMat;
            hBar.GetComponent<Renderer>().sharedMaterial = redMat;
        }

        private static Vector3 BuildShelterComplex(Transform shelterFolder, Transform roadsParent)
        {
            Vector3 centerPos = new Vector3(20f, 3.0f, 9f);
            var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyFloor5x5Prefab);

            // Paved Assembly Courtyard
            if (floorPrefab != null)
            {
                var apron = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab, shelterFolder);
                apron.name = "Shelter_CourtyardApron";
                apron.transform.position = new Vector3(18f, 3.00f, 6f);
                apron.transform.localScale = new Vector3(2.5f, 0.1f, 2.5f);
                EnsureBoxCollider(apron, new Vector3(5f, 0.2f, 5f), new Vector3(0f, 0f, 0f));
                SetStaticFlags(apron);
                var aspMat2 = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                aspMat2.color = new Color(0.24f, 0.25f, 0.26f);
                foreach (var rend in apron.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = aspMat2;
            }

            // Main Community Shelter Facility (KayKit Building G)
            string bldFbx = $"{KayKitCityDir}/building_G.fbx";
            var bldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bldFbx);
            if (bldPrefab != null)
            {
                var mainShelter = (GameObject)PrefabUtility.InstantiatePrefab(bldPrefab, shelterFolder);
                mainShelter.name = "Shelter_CommunityCenter_Main";
                mainShelter.transform.position = centerPos;
                mainShelter.transform.localScale = new Vector3(2.6f, 2.6f, 2.6f);
                mainShelter.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                EnsureBoxCollider(mainShelter, new Vector3(2f, 3f, 2f), new Vector3(0f, 1.5f, 0f));
                SetStaticFlags(mainShelter);

                // Supply Storage Hall (Building H)
                string storageFbx = $"{KayKitCityDir}/building_H.fbx";
                var storagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(storageFbx);
                if (storagePrefab != null)
                {
                    var store = (GameObject)PrefabUtility.InstantiatePrefab(storagePrefab, shelterFolder);
                    store.name = "Shelter_SupplyStorage_Hall";
                    store.transform.position = centerPos + new Vector3(5.5f, 0f, 0f);
                    store.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
                    store.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    EnsureBoxCollider(store, new Vector3(2f, 3f, 2f), new Vector3(0f, 1.5f, 0f));
                    SetStaticFlags(store);
                }
            }

            // Stacked Emergency Relief Crates
            var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyCratePrefab);
            if (cratePrefab != null)
            {
                Vector3[] crateOffsets = {
                    new Vector3(-4.0f, 0f, 1f),
                    new Vector3(-4.8f, 0f, 1f),
                    new Vector3(-4.4f, 0.8f, 1f),
                    new Vector3(-4.0f, 0f, 2.2f),
                    new Vector3(-4.8f, 0f, 2.2f),
                };
                for (int i = 0; i < crateOffsets.Length; i++)
                {
                    var crate = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, shelterFolder);
                    crate.name = $"Shelter_ReliefCrate_{i}";
                    crate.transform.position = centerPos + crateOffsets[i];
                    crate.transform.localScale = Vector3.one * 1.15f;
                    StripColliders(crate);
                }
            }

            // Assembly Benches
            string benchFbx = $"{KayKitCityDir}/bench.fbx";
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(benchFbx);
            if (benchPrefab != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    var bench = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, shelterFolder);
                    bench.name = $"Shelter_Bench_{i}";
                    bench.transform.position = centerPos + new Vector3(-2f - i * 2.5f, 0f, -4f);
                    bench.transform.localScale = Vector3.one * 1.2f;
                    StripColliders(bench);
                }
            }

            // Police Emergency Response Vehicle parked in response bay
            string policeFbx = $"{KayKitCityDir}/car_police.fbx";
            var policePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(policeFbx);
            if (policePrefab != null)
            {
                var pol = (GameObject)PrefabUtility.InstantiatePrefab(policePrefab, shelterFolder);
                pol.name = "Shelter_Police_Vehicle";
                pol.transform.position = new Vector3(14f, 3.02f, 6f);
                pol.transform.rotation = Quaternion.Euler(0f, 50f, 0f);
                pol.transform.localScale = Vector3.one * 1.3f;
                EnsureBoxCollider(pol, new Vector3(2f, 1.5f, 4f), new Vector3(0f, 0.8f, 0f));
            }

            // Streetlights
            PlaceStreetlight(shelterFolder, new Vector3(13f, 3.02f, 2f), 0f);
            PlaceStreetlight(shelterFolder, new Vector3(23f, 3.02f, 2f), 180f);

            // Shelter Entrance Signpost (Disaster Blue Plate with crisp white text)
            CreateSignpost(shelterFolder, new Vector3(14.0f, 3.02f, 1.5f), "COMMUNITY EVACUATION SHELTER\nDISTRICT RELIEF HQ", 30f, SignType.Shelter);

            // Return POI beacon anchor position (at entrance)
            return new Vector3(20f, 3.02f, 6.8f);
        }

        private static void BuildLandslideHazardZone(Transform barriersFolder, Transform rocksFolder, Transform signsFolder, Transform envRoadsParent)
        {
            Vector3 hazardCenter = new Vector3(-18f, 0.5f, -22f);

            // Damaged collapsed road section branching into the slide
            string straightFbx = $"{KayKitCityDir}/road_straight.fbx";
            PlaceRoadStraight(straightFbx, envRoadsParent, "Road_Landslide_Track", new Vector3(-8f, 0.49f, -22f), 0f);
            PlaceRoadStraight(straightFbx, envRoadsParent, "Road_Landslide_Collapsed", new Vector3(-12f, 0.35f, -22f), 15f, pitch: 20f);

            // Safety Orange Traffic Cones blocking the collapsed road
            var conePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyConePrefab);
            var orangeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            orangeMat.color = new Color(1.0f, 0.35f, 0.0f); // Bright safety orange

            if (conePrefab != null)
            {
                Vector3[] coneSpots = {
                    new Vector3(-6.0f, 0.49f, -20.5f),
                    new Vector3(-6.0f, 0.49f, -22.0f),
                    new Vector3(-6.0f, 0.49f, -23.5f),
                    new Vector3(-8.5f, 0.49f, -20.5f),
                    new Vector3(-8.5f, 0.49f, -23.5f),
                };
                for (int i = 0; i < coneSpots.Length; i++)
                {
                    var cone = (GameObject)PrefabUtility.InstantiatePrefab(conePrefab, barriersFolder);
                    cone.name = $"Landslide_SafetyCone_{i}";
                    cone.transform.position = coneSpots[i];
                    cone.transform.localScale = Vector3.one * 1.5f;
                    StripColliders(cone);
                    var rend = cone.GetComponentInChildren<Renderer>();
                    if (rend != null) rend.sharedMaterial = orangeMat;
                }
            }

            // Massive Rockfall Debris spilled over the slope and road
            string[] rockPrefabs = { SyntyRock01Prefab, SyntyRock02Prefab, SyntyRock03Prefab, SyntyRock04Prefab, SyntyRock05Prefab };
            Vector3[] debrisSpots = {
                hazardCenter + new Vector3( 2f, 0.2f, 0f),
                hazardCenter + new Vector3( 0f, 0.3f, 1.5f),
                hazardCenter + new Vector3(-2f, 0.5f, -1f),
                hazardCenter + new Vector3( 3f, 0.1f, -2f),
                hazardCenter + new Vector3(-1f, 0.4f, 3f),
                hazardCenter + new Vector3(-4f, 0.8f, 1f),
                hazardCenter + new Vector3( 1f, 0.2f, -3f),
            };

            for (int i = 0; i < debrisSpots.Length; i++)
            {
                var rPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rockPrefabs[i % rockPrefabs.Length]);
                if (rPrefab != null)
                {
                    var rock = (GameObject)PrefabUtility.InstantiatePrefab(rPrefab, rocksFolder);
                    rock.name = $"Landslide_RockfallDebris_{i}";
                    rock.transform.position = debrisSpots[i];
                    rock.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-25f, 25f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-25f, 25f));
                    rock.transform.localScale = Vector3.one * UnityEngine.Random.Range(2.0f, 3.8f);
                    EnsureMeshCollider(rock);
                    SetStaticFlags(rock);
                }
            }

            // Fallen Dead Tree trunk crushed in the slide
            var deadTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyTreeDeadPrefab);
            if (deadTreePrefab != null)
            {
                var dt = (GameObject)PrefabUtility.InstantiatePrefab(deadTreePrefab, barriersFolder);
                dt.name = "Landslide_CollapsedTree";
                dt.transform.position = hazardCenter + new Vector3(0f, 0.2f, 0f);
                dt.transform.rotation = Quaternion.Euler(75f, 40f, 15f);
                dt.transform.localScale = Vector3.one * 1.4f;
                StripColliders(dt);
            }

            // Prominent Warning Signboard directly facing oncoming player
            CreateSignpost(signsFolder, new Vector3(-4.5f, 0.49f, -20.5f), "CAUTION: LANDSLIDE HAZARD\nROAD CLOSED - EVACUATE TO HIGH GROUND", -90f, SignType.Warning);

            // Town Welcome Sign at Lower Entrance (on right curb)
            CreateSignpost(signsFolder, new Vector3(6.5f, 0.49f, -28.0f), "AIZAWL HILL DISTRICT\nELEVATION 1132m - EVAC ROUTE NORTH", 0f, SignType.Welcome);
        }

        private static void CreateSignpost(Transform parent, Vector3 pos, string textContent, float yaw, SignType type = SignType.Warning)
        {
            var signGO = new GameObject("Signpost");
            signGO.transform.SetParent(parent, false);
            signGO.transform.position = pos;
            signGO.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Left & Right Support Posts behind the board
            var postMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            postMat.color = new Color(0.28f, 0.22f, 0.16f); // Dark timber

            for (int p = -1; p <= 1; p += 2)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"Post_{(p < 0 ? "L" : "R")}";
                post.transform.SetParent(signGO.transform, false);
                post.transform.localScale = new Vector3(0.09f, 1.1f, 0.09f);
                post.transform.localPosition = new Vector3(p * 1.05f, 1.1f, 0.08f);
                UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
                post.GetComponent<Renderer>().sharedMaterial = postMat;
            }

            // Dark Backing Frame
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "BackingFrame";
            frame.transform.SetParent(signGO.transform, false);
            frame.transform.localScale = new Vector3(2.6f, 1.3f, 0.06f);
            frame.transform.localPosition = new Vector3(0f, 1.95f, 0.02f);
            UnityEngine.Object.DestroyImmediate(frame.GetComponent<Collider>());
            var frameMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            frameMat.color = new Color(0.18f, 0.18f, 0.18f); // Graphite border
            frame.GetComponent<Renderer>().sharedMaterial = frameMat;

            // Front Sign Plate
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "BoardPlate";
            board.transform.SetParent(signGO.transform, false);
            board.transform.localScale = new Vector3(2.45f, 1.15f, 0.02f);
            board.transform.localPosition = new Vector3(0f, 1.95f, -0.02f);
            UnityEngine.Object.DestroyImmediate(board.GetComponent<Collider>());

            var boardMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color textColor;
            switch (type)
            {
                case SignType.Warning:
                    boardMat.color = new Color(0.98f, 0.85f, 0.12f); // Safety Yellow
                    textColor = new Color(0.10f, 0.10f, 0.10f); // High-contrast Black
                    break;
                case SignType.Medical:
                    boardMat.color = new Color(0.12f, 0.55f, 0.28f); // Emergency Green
                    textColor = Color.white;
                    break;
                case SignType.Shelter:
                    boardMat.color = new Color(0.14f, 0.38f, 0.78f); // Disaster Blue
                    textColor = Color.white;
                    break;
                default:
                    boardMat.color = new Color(0.16f, 0.44f, 0.36f); // Highway Slate Green
                    textColor = Color.white;
                    break;
            }
            board.GetComponent<Renderer>().sharedMaterial = boardMat;

            // Single high-visibility Canvas cleanly padded inside plate (parented to signGO, NOT scaled board!)
            var canvasGO = new GameObject("SignCanvas");
            canvasGO.transform.SetParent(signGO.transform, false);
            canvasGO.transform.localPosition = new Vector3(0f, 1.95f, -0.05f);
            canvasGO.transform.localRotation = Quaternion.identity;
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(canvasGO.transform, false);
            var text = textGO.AddComponent<Text>();
            text.text = textContent;
            text.font = UIBuilder.GetDefaultFont();
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 20;
            text.lineSpacing = 1.15f;
            text.rectTransform.sizeDelta = new Vector2(230f, 95f);
            textGO.transform.localScale = Vector3.one * 0.01f;
        }

        private static void BuildResidentialSettlement(Transform buildingsFolder, Transform propsFolder)
        {
            var syntyHousePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyHousePrefab);
            string[] kaykitHouses = {
                $"{KayKitCityDir}/building_A.fbx",
                $"{KayKitCityDir}/building_B.fbx",
                $"{KayKitCityDir}/building_D.fbx",
                $"{KayKitCityDir}/building_F.fbx"
            };

            // 1. Level 0 Suburban Houses (flanking entrance road)
            if (syntyHousePrefab != null)
            {
                PlaceBuilding(syntyHousePrefab, buildingsFolder, "House_L0_West", new Vector3(-15f, 0.5f, -34f), Quaternion.Euler(0f, 85f, 0f), Vector3.one * 0.9f);
                PlaceBuilding(syntyHousePrefab, buildingsFolder, "House_L0_East", new Vector3( 15f, 0.5f, -34f), Quaternion.Euler(0f, -85f, 0f), Vector3.one * 0.9f);

                // Level 2 Upper Ridge Houses
                PlaceBuilding(syntyHousePrefab, buildingsFolder, "House_L2_West", new Vector3(-12f, 6.0f, 33f), Quaternion.Euler(0f, 85f, 0f), Vector3.one * 0.9f);
                PlaceBuilding(syntyHousePrefab, buildingsFolder, "House_L2_East", new Vector3( 12f, 6.0f, 33f), Quaternion.Euler(0f, -85f, 0f), Vector3.one * 0.9f);
            }

            // 2. Level 1 Town Center Houses (lining the town street)
            Vector3[] townHouseSpots = {
                new Vector3(-6.0f, 3.0f, -3f),
                new Vector3( 6.0f, 3.0f, -3f),
                new Vector3(-6.0f, 3.0f,  4f),
                new Vector3( 6.0f, 3.0f,  4f),
                new Vector3(-6.0f, 3.0f, 11f),
                new Vector3( 6.0f, 3.0f, 11f),
            };

            for (int i = 0; i < townHouseSpots.Length; i++)
            {
                string path = kaykitHouses[i % kaykitHouses.Length];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    float yaw = (townHouseSpots[i].x < 0) ? 80f : -80f;
                    PlaceBuilding(prefab, buildingsFolder, $"TownHouse_{i}", townHouseSpots[i], Quaternion.Euler(0f, yaw, 0f), Vector3.one * 2.2f);
                }
            }

            // 3. Streetlights along roads (placed on road curb)
            PlaceStreetlight(propsFolder, new Vector3(-1.8f, 0.49f, -30f), 0f);
            PlaceStreetlight(propsFolder, new Vector3( 1.8f, 0.49f, -22f), 180f);
            PlaceStreetlight(propsFolder, new Vector3(-1.8f, 2.99f,  -5f), 0f);
            PlaceStreetlight(propsFolder, new Vector3( 1.8f, 2.99f,   5f), 180f);
            PlaceStreetlight(propsFolder, new Vector3(-1.8f, 5.99f,  24f), 0f);
            PlaceStreetlight(propsFolder, new Vector3( 1.8f, 5.99f,  32f), 180f);
        }

        private static void PlaceBuilding(GameObject prefab, Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;
            EnsureBoxCollider(go, new Vector3(4f, 4f, 4f), new Vector3(0f, 2f, 0f));
            SetStaticFlags(go);
        }

        private static void PlaceVehicle(GameObject prefab, Transform parent, string name, Vector3 pos, Quaternion rot)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            EnsureBoxCollider(go, new Vector3(2f, 1.5f, 4f), new Vector3(0f, 0.75f, 0f));
            SetStaticFlags(go);
        }

        private static void PlaceStreetlight(Transform parent, Vector3 pos, float yaw)
        {
            string fbx = $"{KayKitCityDir}/streetlight.fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = $"Streetlight_{pos.z}";
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * 1.2f;
            StripColliders(go);
            SetStaticFlags(go);
        }
        private static void BuildVegetationAndRocks(Transform vegFolder, Transform rocksFolder)
        {
            string[] treePrefabs = { SyntyTree01Prefab, SyntyTree02Prefab, SyntyTree03Prefab, SyntyTree04Prefab };
            string[] kaykitPines = {
                $"{KayKitForestDir}/Tree_1_A_Color1.fbx",
                $"{KayKitForestDir}/Tree_2_A_Color1.fbx",
                $"{KayKitForestDir}/Tree_3_A_Color1.fbx",
                $"{KayKitForestDir}/Tree_4_A_Color1.fbx"
            };
            string[] bushFbx = {
                $"{KayKitForestDir}/Bush_1_A_Color1.fbx",
                $"{KayKitForestDir}/Bush_2_A_Color1.fbx",
                $"{KayKitForestDir}/Bush_4_A_Color1.fbx"
            };

            // 1. Mountain Pine Trees framing the valley boundaries
            Vector3[] pineSpots = {
                // East mountain flank
                new Vector3(24f, 0.1f, -32f), new Vector3(26f, 0.1f, -22f),
                new Vector3(32f, 2.6f,  -4f), new Vector3(32f, 2.6f,   8f),
                new Vector3(22f, 5.6f,  24f), new Vector3(24f, 5.6f,  36f),
                new Vector3(14f, 9.1f,  46f), new Vector3(12f, 15.6f, 60f),
                // West mountain flank (safe buffer away from hospital apron)
                new Vector3(-24f, 0.1f, -34f), new Vector3(-32f, 2.6f,  -4f),
                new Vector3(-32f, 2.6f,  12f), new Vector3(-22f, 5.6f,  24f),
                new Vector3(-24f, 5.6f,  36f), new Vector3(-14f, 9.1f,  46f),
                new Vector3(-12f, 15.6f, 60f),
                // Summit backdrop groves
                new Vector3(-4.5f, 13.26f, 63f), new Vector3(4.5f, 13.26f, 63f)
            };

            for (int i = 0; i < pineSpots.Length; i++)
            {
                GameObject prefab = (i % 2 == 0)
                    ? AssetDatabase.LoadAssetAtPath<GameObject>(treePrefabs[i % treePrefabs.Length])
                    : AssetDatabase.LoadAssetAtPath<GameObject>(kaykitPines[i % kaykitPines.Length]);

                if (prefab != null)
                {
                    var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, vegFolder);
                    tree.name = $"PineTree_{i:D2}";
                    tree.transform.position = pineSpots[i];
                    tree.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.2f, 1.8f);
                    StripColliders(tree);
                    SetStaticFlags(tree);
                }
            }

            // 2. Green Shrubs softening building edges and road corners
            Vector3[] shrubSpots = {
                new Vector3(-3f, 0.1f, -26f), new Vector3( 3f, 0.1f, -26f),
                new Vector3(-3f, 2.6f,  -6f), new Vector3( 3f, 2.6f,  -6f),
                new Vector3(-14f, 2.6f, -6f), new Vector3( 14f, 2.6f, -6f),
                new Vector3(-3f, 2.6f,   2f), new Vector3( 3f, 2.6f,   2f),
                new Vector3(-3f, 2.6f,  10f), new Vector3( 3f, 2.6f,  10f),
                new Vector3(-3f, 5.6f,  26f), new Vector3( 3f, 5.6f,  26f),
            };

            for (int i = 0; i < shrubSpots.Length; i++)
            {
                var bPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bushFbx[i % bushFbx.Length]);
                if (bPrefab != null)
                {
                    var shrub = (GameObject)PrefabUtility.InstantiatePrefab(bPrefab, vegFolder);
                    shrub.name = $"Shrub_{i:D2}";
                    shrub.transform.position = shrubSpots[i];
                    shrub.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    shrub.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.3f, 1.8f);
                    StripColliders(shrub);
                    SetStaticFlags(shrub);
                }
            }
        }

        private static void BuildNavigationAnchors(Transform mainRoute, Transform hospRoute, Transform shelterRoute, Transform summitRoute, Vector3 spawn, Vector3 hosp, Vector3 shelter, Vector3 summit)
        {
            // Main Route
            CreateWaypoint(mainRoute, "Waypoint_Spawn", spawn);
            CreateWaypoint(mainRoute, "Waypoint_L0_Spine", new Vector3(0f, 0.1f, -24f));
            CreateWaypoint(mainRoute, "Waypoint_CentralJunction", new Vector3(0f, 2.6f, -8f));
            CreateWaypoint(mainRoute, "Waypoint_TownStreet", new Vector3(0f, 2.6f, 6f));
            CreateWaypoint(mainRoute, "Waypoint_UpperRidge", new Vector3(0f, 5.6f, 28f));
            CreateWaypoint(mainRoute, "Waypoint_Trailhead", new Vector3(0f, 9.1f, 46f));

            // Hospital Route
            CreateWaypoint(hospRoute, "Waypoint_HospFork", new Vector3(-8f, 2.6f, -8f));
            CreateWaypoint(hospRoute, "Waypoint_HospCorner", new Vector3(-20f, 2.6f, -8f));
            CreateWaypoint(hospRoute, "Waypoint_HospApron", hosp);

            // Shelter Route
            CreateWaypoint(shelterRoute, "Waypoint_ShelterFork", new Vector3(8f, 2.6f, -8f));
            CreateWaypoint(shelterRoute, "Waypoint_ShelterCorner", new Vector3(20f, 2.6f, -8f));
            CreateWaypoint(shelterRoute, "Waypoint_ShelterCourtyard", shelter);

            // Summit Route
            CreateWaypoint(summitRoute, "Waypoint_SummitAscent_1", new Vector3(0f, 10.5f, 52f));
            CreateWaypoint(summitRoute, "Waypoint_SummitAscent_2", new Vector3(0f, 13.5f, 57f));
            CreateWaypoint(summitRoute, "Waypoint_SummitLookout_SafeZone", summit);
        }

        private static GameObject CreateWaypoint(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return go;
        }

        public class GameplaySetupResult
        {
            public GameObject Player;
            public Camera MainCamera;
        }

        private static GameplaySetupResult IntegrateGameplay(Transform gpRoot, Vector3 spawnPos, Vector3 hospPos, Vector3 shelterPos, Vector3 summitPos)
        {
            var res = new GameplaySetupResult();

            // 1. Player Setup (KayKit Rogue + Animator + PlayerController)
            GameObject player = KayKitIntegration.BuildPlayer(gpRoot, spawnPos);
            player.name = "DisasterReady_Player";
            res.Player = player;

            // 2. Camera Setup (OrbitCameraController configured with Phase 2 polished parameters)
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.transform.SetParent(gpRoot, false);
                mainCam = camGO.AddComponent<Camera>();
                mainCam.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }
            else
            {
                mainCam.transform.SetParent(gpRoot, false);
            }

            res.MainCamera = mainCam;
            mainCam.fieldOfView = 55f;
            mainCam.nearClipPlane = 0.3f;
            mainCam.farClipPlane = 600f;

            var orbitCam = mainCam.GetComponent<OrbitCameraController>();
            if (orbitCam == null) orbitCam = mainCam.gameObject.AddComponent<OrbitCameraController>();
            orbitCam.Target = player.transform;
            orbitCam.StartYaw = 0f;
            orbitCam.StartPitch = 18f;
            orbitCam.Distance = 6.0f;
            orbitCam.TargetOffset = new Vector3(0f, 1.8f, 0f);
            orbitCam.AutoFollowYaw = false;
            orbitCam.EnableCollision = true;
            orbitCam.CollisionRadius = 0.25f;
            orbitCam.PlayerRef = player.GetComponent<PlayerController>();

            // Orient camera immediately behind player facing North (+Z)
            Vector3 focusPoint = player.transform.position + orbitCam.TargetOffset;
            Quaternion camRot = Quaternion.Euler(orbitCam.StartPitch, orbitCam.StartYaw, 0f);
            mainCam.transform.position = focusPoint - camRot * Vector3.forward * orbitCam.Distance;
            mainCam.transform.rotation = camRot;

            // 3. POIs (Hospital, Shelter, Summit)
            var poiRoot = new GameObject("POIs");
            poiRoot.transform.SetParent(gpRoot, false);

            var hospTrigger = CreatePoiMarker(poiRoot.transform, "POI_Hospital", hospPos, PoiType.Hospital, "HOSPITAL", new Color(0.2f, 0.85f, 0.3f));
            hospTrigger.MissionId = "find_hospital";

            var shelterTrigger = CreatePoiMarker(poiRoot.transform, "POI_Shelter", shelterPos, PoiType.EmergencyShelter, "EMERGENCY SHELTER", new Color(0.2f, 0.5f, 0.95f));
            shelterTrigger.MissionId = "find_shelter";

            var summitTrigger = CreatePoiMarker(poiRoot.transform, "POI_Summit", summitPos, PoiType.Summit, "HIGHEST POINT (SUMMIT)", new Color(1f, 0.85f, 0.1f), out Text summitLabel);
            summitTrigger.MissionId = "find_highest";
            var emergencyObjectiveTrigger = summitTrigger.gameObject.AddComponent<EmergencyObjectiveTrigger>();

            // 4. UI Canvas & Systems
            var uiResult = UIBuilder.Build();

            // 5. Mission Manager
            var missionManagerGO = new GameObject("MissionManager");
            missionManagerGO.transform.SetParent(gpRoot, false);
            var missionManager = missionManagerGO.AddComponent<MissionManager>();
            missionManager.Missions = new List<MissionDefinition>
            {
                new MissionDefinition("find_highest", "Find the Highest Point", "Climb to the summit safe zone above the valley.", 100),
                new MissionDefinition("find_shelter", "Locate the Emergency Shelter", "Find the fortified emergency community shelter.", 100),
                new MissionDefinition("find_hospital", "Locate the Hospital", "Locate the emergency medical facility in the valley.", 100),
            };

            uiResult.Hud.Missions = missionManager;
            uiResult.Hud.CompleteBanner = uiResult.Banner;
            uiResult.TerrainInfo.TrackedTarget = player.transform;

            // 6. Emergency System (Landslide scenario + Safe Direction Guide)
            var safeGuide = player.AddComponent<SafeDirectionGuide>();
            safeGuide.StatusText = uiResult.SafeDirectionText;
            safeGuide.StatusPanel = uiResult.SafeDirectionPlate;
            safeGuide.Objective = emergencyObjectiveTrigger.transform;

            var emergencyGO = new GameObject("EmergencySystem");
            emergencyGO.transform.SetParent(gpRoot, false);
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

            // 7. Game Bootstrapper
            var bootstrapGO = new GameObject("GameBootstrapper");
            bootstrapGO.transform.SetParent(gpRoot, false);
            var bootstrapper = bootstrapGO.AddComponent<GameBootstrapper>();
            bootstrapper.TitleScreen = uiResult.Title;
            bootstrapper.Hud = uiResult.Hud;
            bootstrapper.Player = player.GetComponent<PlayerController>();
            bootstrapper.OrbitCamera = orbitCam;

            return res;
        }

        private static MissionZoneTrigger CreatePoiMarker(Transform parent, string name, Vector3 pos, PoiType type, string labelText, Color markerColor)
        {
            return CreatePoiMarker(parent, name, pos, type, labelText, markerColor, out _);
        }

        private static MissionZoneTrigger CreatePoiMarker(Transform parent, string name, Vector3 pos, PoiType type, string labelText, Color markerColor, out Text labelComp)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 7f;
            col.center = new Vector3(0f, 1.5f, 0f);

            var trigger = go.AddComponent<MissionZoneTrigger>();
            var poi = go.AddComponent<PoiMarker>();
            poi.Type = type;

            // Refined slender beacon pole
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "MarkerPole";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(0.08f, 1.2f, 0.08f);
            visual.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = markerColor;
            visual.GetComponent<Renderer>().sharedMaterial = mat;

            // Glowing Beacon Sphere at top of pole
            var beaconSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beaconSphere.name = "BeaconSphere";
            beaconSphere.transform.SetParent(go.transform, false);
            beaconSphere.transform.localScale = Vector3.one * 0.4f;
            beaconSphere.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            UnityEngine.Object.DestroyImmediate(beaconSphere.GetComponent<Collider>());
            beaconSphere.GetComponent<Renderer>().sharedMaterial = mat;

            // 3D Billboard Label
            var canvasGO = new GameObject("WorldCanvas");
            canvasGO.transform.SetParent(go.transform, false);
            canvasGO.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<BillboardLabel>();

            var textGO = new GameObject("LabelText");
            textGO.transform.SetParent(canvasGO.transform, false);
            var text = textGO.AddComponent<Text>();
            text.text = labelText;
            text.font = UIBuilder.GetDefaultFont();
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = markerColor;
            text.rectTransform.sizeDelta = new Vector2(320f, 60f);
            textGO.transform.localScale = Vector3.one * 0.025f;

            labelComp = text;
            return trigger;
        }

        private static void EnsureMeshCollider(GameObject go)
        {
            var mf = go.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var mc = go.GetComponent<MeshCollider>();
                if (mc == null) mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        private static void EnsureBoxCollider(GameObject go, Vector3 size, Vector3 center)
        {
            var bc = go.GetComponent<BoxCollider>();
            if (bc == null) bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            bc.center = center;
        }

        private static void StripColliders(GameObject go)
        {
            var cols = go.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols) UnityEngine.Object.DestroyImmediate(c);
        }

        private static void SetStaticFlags(GameObject go)
        {
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            foreach (Transform t in go.transform)
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
            }
        }

        private static void SetSceneAsBuildIndexZero(string scenePath)
        {
            var currentScenes = EditorBuildSettings.scenes;
            var list = new List<EditorBuildSettingsScene>(currentScenes);
            list.RemoveAll(s => s.path == scenePath);
            list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[AizawlValleySceneBuilder] Registered {scenePath} as Scene 0 in Build Settings.");
        }

        private static void CaptureInspectionScreenshots(GameObject player, Camera cam, Vector3 hosp, Vector3 shelter, Vector3 summit)
        {
            if (cam == null) return;

            var shots = new (string filename, Vector3 camPos, Quaternion camRot)[] {
                // 1. Player Spawn Third-Person View looking into Aizawl Valley
                ("aizawl_player_spawn.png", new Vector3(0f, 2.2f, -38.5f), Quaternion.Euler(11f, 0f, 0f)),
                // 2. Overview of Valley Settlement & Terraced Roads
                ("aizawl_valley_overview.png", new Vector3(0f, 24.0f, -32.0f), Quaternion.Euler(32f, 0f, 0f)),
                // 3. Hospital Medical Compound View (Clean angle: Red cross facade, ambulance, sign, mountains)
                ("aizawl_hospital_complex.png", new Vector3(-16.0f, 4.8f, -2.0f), Quaternion.Euler(7f, -14f, 0f)),
                // 4. Landslide Hazard Zone & Warning Barrier Storytelling
                ("aizawl_landslide_hazard.png", new Vector3(-1.0f, 1.8f, -22.5f), Quaternion.Euler(3f, -80f, 0f)),
                // 5. Summit Safe Zone Lookout View (Observation deck, comms mast, warning beacon, and peak)
                ("aizawl_summit_lookout.png", new Vector3(0f, 14.2f, 56.0f), Quaternion.Euler(5f, 0f, 0f))
            };

            int width = 1280;
            int height = 720;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevRt = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;

            cam.targetTexture = rt;

            foreach (var s in shots)
            {
                foreach (var bb in UnityEngine.Object.FindObjectsByType<BillboardLabel>())
                {
                    bb.transform.rotation = Quaternion.LookRotation(bb.transform.position - s.camPos);
                }
                cam.transform.position = s.camPos;
                cam.transform.rotation = s.camRot;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                byte[] bytes = tex.EncodeToPNG();
                string outPath = Path.Combine(ScreenshotsDir, s.filename);
                File.WriteAllBytes(outPath, bytes);
                UnityEngine.Object.DestroyImmediate(tex);
                Debug.Log($"[AizawlValleySceneBuilder] Captured verification screenshot: {outPath} ({bytes.Length} bytes)");
            }

            if (player != null)
            {
                var orbit = cam.GetComponent<OrbitCameraController>();
                if (orbit != null)
                {
                    Vector3 focusPoint = player.transform.position + orbit.TargetOffset;
                    Quaternion rot = Quaternion.Euler(orbit.StartPitch, orbit.StartYaw, 0f);
                    cam.transform.position = focusPoint - rot * Vector3.forward * orbit.Distance;
                    cam.transform.rotation = rot;
                }
            }

            cam.targetTexture = prevRt;
            RenderTexture.active = prevActive;
            UnityEngine.Object.DestroyImmediate(rt);
        }

        private static void BuildRampGuardrails(Transform terrainParent)
        {
            // Simple bollard markers at each terrace-climb's top edge, reinforcing where the
            // ground drops away next to the road. Purely decorative, non-colliding.
            var bollardMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bollardMat.color = new Color(0.95f, 0.82f, 0.12f);

            void Bollard(string name, Vector3 pos)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = name;
                post.transform.SetParent(terrainParent, false);
                post.transform.position = pos;
                post.transform.localScale = new Vector3(0.12f, 0.55f, 0.12f);
                UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
                post.GetComponent<Renderer>().sharedMaterial = bollardMat;
            }

            // Level 0 -> Level 1 (road resumes ~Y=2.99, Z=-11.5)
            Bollard("Guardrail_L0L1_L", new Vector3(-2.3f, 3.54f, -11.3f));
            Bollard("Guardrail_L0L1_R", new Vector3(2.3f, 3.54f, -11.3f));

            // Level 1 -> Level 2 (road resumes ~Y=5.99, Z=17.5)
            Bollard("Guardrail_L1L2_L", new Vector3(-2.3f, 6.54f, 17.3f));
            Bollard("Guardrail_L1L2_R", new Vector3(2.3f, 6.54f, 17.3f));

            // Level 2 -> Level 3 (trailhead surface ~Y=8.96, Z=48)
            Bollard("Guardrail_L2L3_L", new Vector3(-2.3f, 9.51f, 46.7f));
            Bollard("Guardrail_L2L3_R", new Vector3(2.3f, 9.51f, 46.7f));
        }

        private static void BuildSummitSafetyFeatures(Transform mountainsParent, Vector3 summitPos)
        {
            // The summit lookout deck (Ground_SummitDeck_Base) is a ~12x12 flat tile centered at
            // (0, 12.8, 62), i.e. roughly X:[-6,6] Z:[56,68]. Existing props (bench, comms mast,
            // POI beacon) sit around Z=60.5-62, so the new edge rail/sign/flag are placed on the
            // far (north) edge and west corner, clear of them.
            float deckY = summitPos.y - 0.24f; // matches existing bench/mast height (13.26)

            var railMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            railMat.color = new Color(0.92f, 0.92f, 0.90f);

            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Summit_SafetyRail_NorthEdge";
            rail.transform.SetParent(mountainsParent, false);
            rail.transform.position = new Vector3(summitPos.x, deckY, summitPos.z + 5.3f);
            rail.transform.localScale = new Vector3(9.5f, 0.5f, 0.08f);
            UnityEngine.Object.DestroyImmediate(rail.GetComponent<Collider>());
            rail.GetComponent<Renderer>().sharedMaterial = railMat;

            void RailPost(string name, float xOffset)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = name;
                post.transform.SetParent(mountainsParent, false);
                post.transform.position = new Vector3(summitPos.x + xOffset, deckY, summitPos.z + 5.3f);
                post.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f);
                UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
                post.GetComponent<Renderer>().sharedMaterial = railMat;
            }
            RailPost("Summit_SafetyRail_PostL", -4.9f);
            RailPost("Summit_SafetyRail_PostR", 4.9f);

            // Safe Zone assembly signage, facing back toward players arriving from the stairs
            CreateSignpost(mountainsParent, new Vector3(summitPos.x, deckY, summitPos.z + 3.5f), "SAFE ZONE\nASSEMBLY POINT", 180f, SignType.Welcome);

            // Small marker flag on a pole, west side of the deck
            var poleGO = new GameObject("Summit_SafeZoneFlagpole");
            poleGO.transform.SetParent(mountainsParent, false);
            poleGO.transform.position = new Vector3(summitPos.x - 4.5f, deckY, summitPos.z + 3.5f);

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(poleGO.transform, false);
            pole.transform.localScale = new Vector3(0.06f, 1.4f, 0.06f);
            pole.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            UnityEngine.Object.DestroyImmediate(pole.GetComponent<Collider>());
            var poleMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            poleMat.color = new Color(0.30f, 0.30f, 0.30f);
            pole.GetComponent<Renderer>().sharedMaterial = poleMat;

            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(poleGO.transform, false);
            flag.transform.localScale = new Vector3(0.6f, 0.4f, 0.02f);
            flag.transform.localPosition = new Vector3(0.32f, 2.55f, 0f);
            UnityEngine.Object.DestroyImmediate(flag.GetComponent<Collider>());
            var flagMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            flagMat.color = new Color(0.10f, 0.70f, 0.35f);
            flag.GetComponent<Renderer>().sharedMaterial = flagMat;
        }

        private static void BuildHospitalDetailPass(Transform hospitalFolder, Vector3 hospEntrancePos)
        {
            // Fire hydrant near the apron for grounded realism
            string hydrantFbx = $"{KayKitCityDir}/firehydrant.fbx";
            var hydrantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hydrantFbx);
            if (hydrantPrefab != null)
            {
                var hyd = (GameObject)PrefabUtility.InstantiatePrefab(hydrantPrefab, hospitalFolder);
                hyd.name = "Hospital_FireHydrant";
                hyd.transform.position = new Vector3(-22.5f, 3.02f, 3.5f);
                hyd.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
                hyd.transform.localScale = Vector3.one * 1.3f;
                StripColliders(hyd);
            }

            // Painted helipad marking on the courtyard apron (reinforces "emergency response")
            var padMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            padMat.color = Color.white;
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "Hospital_HelipadMarking";
            pad.transform.SetParent(hospitalFolder, false);
            pad.transform.position = new Vector3(-18f, 3.02f, 6f);
            pad.transform.localScale = new Vector3(2.2f, 0.01f, 2.2f);
            UnityEngine.Object.DestroyImmediate(pad.GetComponent<Collider>());
            pad.GetComponent<Renderer>().sharedMaterial = padMat;

            var hCanvasGO = new GameObject("HelipadLabelCanvas");
            hCanvasGO.transform.SetParent(hospitalFolder, false);
            hCanvasGO.transform.position = new Vector3(-18f, 3.04f, 6f);
            hCanvasGO.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var hCanvas = hCanvasGO.AddComponent<Canvas>();
            hCanvas.renderMode = RenderMode.WorldSpace;
            var hTextGO = new GameObject("H");
            hTextGO.transform.SetParent(hCanvasGO.transform, false);
            var hText = hTextGO.AddComponent<Text>();
            hText.text = "H";
            hText.font = UIBuilder.GetDefaultFont();
            hText.fontSize = 80;
            hText.fontStyle = FontStyle.Bold;
            hText.alignment = TextAnchor.MiddleCenter;
            hText.color = new Color(0.85f, 0.1f, 0.1f);
            hText.rectTransform.sizeDelta = new Vector2(200f, 200f);
            hTextGO.transform.localScale = Vector3.one * 0.01f;
        }

        private static void BuildLandslideDetailPass(Transform barriersFolder, Transform rocksFolder, Transform signsFolder)
        {
            // Hazard tape strung between two posts, reinforcing the cone-marked closed lane
            var tapeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            tapeMat.color = new Color(0.95f, 0.75f, 0.1f);

            var tapePostL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tapePostL.name = "Landslide_TapePost_L";
            tapePostL.transform.SetParent(barriersFolder, false);
            tapePostL.transform.position = new Vector3(-6f, 0.85f, -20.2f);
            tapePostL.transform.localScale = new Vector3(0.07f, 0.55f, 0.07f);
            UnityEngine.Object.DestroyImmediate(tapePostL.GetComponent<Collider>());
            tapePostL.GetComponent<Renderer>().sharedMaterial = tapeMat;

            var tapePostR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tapePostR.name = "Landslide_TapePost_R";
            tapePostR.transform.SetParent(barriersFolder, false);
            tapePostR.transform.position = new Vector3(-6f, 0.85f, -23.8f);
            tapePostR.transform.localScale = new Vector3(0.07f, 0.55f, 0.07f);
            UnityEngine.Object.DestroyImmediate(tapePostR.GetComponent<Collider>());
            tapePostR.GetComponent<Renderer>().sharedMaterial = tapeMat;

            var tapeStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tapeStrip.name = "Landslide_HazardTape";
            tapeStrip.transform.SetParent(barriersFolder, false);
            tapeStrip.transform.position = new Vector3(-6f, 1.15f, -22f);
            tapeStrip.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            tapeStrip.transform.localScale = new Vector3(3.6f, 0.12f, 0.02f);
            UnityEngine.Object.DestroyImmediate(tapeStrip.GetComponent<Collider>());
            tapeStrip.GetComponent<Renderer>().sharedMaterial = tapeMat;

            // Extra rubble scattered directly on the collapsed road tile
            var smallRockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyRock01Prefab);
            if (smallRockPrefab != null)
            {
                Vector3[] rubbleSpots = {
                    new Vector3(-11f, 0.55f, -21.3f),
                    new Vector3(-12.5f, 0.6f, -22.4f),
                    new Vector3(-10f, 0.5f, -22.8f),
                };
                for (int i = 0; i < rubbleSpots.Length; i++)
                {
                    var rub = (GameObject)PrefabUtility.InstantiatePrefab(smallRockPrefab, rocksFolder);
                    rub.name = $"Landslide_RoadRubble_{i}";
                    rub.transform.position = rubbleSpots[i];
                    rub.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-15f, 15f));
                    rub.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.3f);
                    StripColliders(rub);
                }
            }

            // Damaged infrastructure: a partially collapsed wall section, tilted in the debris
            var wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Buildings_WallWindow_2x3_01P.prefab");
            if (wallPrefab != null)
            {
                var wall = (GameObject)PrefabUtility.InstantiatePrefab(wallPrefab, barriersFolder);
                wall.name = "Landslide_CollapsedWallSection";
                wall.transform.position = new Vector3(-16.5f, 0.9f, -23.5f);
                wall.transform.rotation = Quaternion.Euler(0f, 25f, 35f);
                wall.transform.localScale = Vector3.one * 1.4f;
                StripColliders(wall);
                SetStaticFlags(wall);
            }
        }

        private static void BuildRouteMarkers(Transform routeMarkersFolder)
        {
            string arrowPath = "Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Icon_Arrow_Small_01.prefab";
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(arrowPath);
            if (arrowPrefab == null) return;

            var arrowMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            arrowMat.color = new Color(0.15f, 0.85f, 0.35f);

            void PlaceArrow(string name, Vector3 pos, float yaw)
            {
                var a = (GameObject)PrefabUtility.InstantiatePrefab(arrowPrefab, routeMarkersFolder);
                a.name = name;
                a.transform.position = pos;
                a.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                a.transform.localScale = Vector3.one * 1.4f;
                StripColliders(a);
                foreach (var rend in a.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = arrowMat;
            }

            // Pointing north (+Z), up-valley, at the base of each climb toward the summit
            PlaceArrow("RouteMarker_ToL1", new Vector3(2.6f, 0.55f, -17f), 0f);
            PlaceArrow("RouteMarker_ToL2", new Vector3(2.6f, 3.05f, 13f), 0f);
            PlaceArrow("RouteMarker_ToL3", new Vector3(2.6f, 6.05f, 39f), 0f);
            PlaceArrow("RouteMarker_ToSummit", new Vector3(2.2f, 9.05f, 49f), 0f);
        }

        private static void BuildBackgroundSettlementSilhouette(Transform mountainsParent)
        {
            // Distant, non-collidable building silhouettes on the mountain flanks beyond the
            // playable terraces. Purely a backdrop trick: it sells "this valley is part of a
            // much bigger hillside city" without adding any walkable area or gameplay props.
            string[] silhouettePaths = {
                $"{KayKitCityDir}/building_B.fbx",
                $"{KayKitCityDir}/building_D.fbx",
                $"{KayKitCityDir}/building_E.fbx",
                $"{KayKitCityDir}/building_G.fbx",
            };

            (Vector3 pos, float yaw)[] spots = {
                (new Vector3(-38f, 0.6f, -37f), 25f),
                (new Vector3(-44f, 1.0f, -30f), 55f),
                (new Vector3(-42f, 3.3f,   4f), 15f),
                (new Vector3(-37f, 3.6f,  16f), -20f),
                (new Vector3(-36f, 6.3f,  29f), 30f),
                (new Vector3(-30f, 6.6f,  40f), -15f),
                (new Vector3( 38f, 0.6f, -37f), -25f),
                (new Vector3( 44f, 1.0f, -30f), -55f),
                (new Vector3( 42f, 3.3f,   4f), -15f),
                (new Vector3( 37f, 3.6f,  16f), 20f),
                (new Vector3( 36f, 6.3f,  29f), -30f),
                (new Vector3( 30f, 6.6f,  40f), 15f),
            };

            for (int i = 0; i < spots.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(silhouettePaths[i % silhouettePaths.Length]);
                if (prefab == null) continue;
                var bldg = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mountainsParent);
                bldg.name = $"BackgroundSettlement_{i:D2}";
                bldg.transform.position = spots[i].pos;
                bldg.transform.rotation = Quaternion.Euler(0f, spots[i].yaw, 0f);
                bldg.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.3f, 1.9f);
                StripColliders(bldg);
                SetStaticFlags(bldg);
            }
        }

        private static void BuildHillsideResidentialClusters(Transform buildingsFolder, Transform propsFolder, Transform vegFolder)
        {
            // Fills the large bare-grass gaps left between the existing spine road, town-center
            // houses, and the hospital/shelter branch roads with small, thoughtfully placed
            // hillside residential clusters. Coordinates are chosen to sit on already-existing
            // flat ground tiles and to stay clear of the landslide zone (X:[-19,-4] Z:[-26,-18]
            // at Level 0), the hospital/shelter branch roads (Z=-8 at Level 1), and the central
            // spine road (X=0 at every level).
            var syntyHousePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyHousePrefab);
            string[] kaykitHouses = {
                $"{KayKitCityDir}/building_A.fbx",
                $"{KayKitCityDir}/building_B.fbx",
                $"{KayKitCityDir}/building_D.fbx",
                $"{KayKitCityDir}/building_F.fbx",
                $"{KayKitCityDir}/building_H.fbx",
            };

            var wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            wallMat.color = new Color(0.40f, 0.38f, 0.33f);

            void RetainingAccent(string name, Transform parent, Vector3 pos, Vector3 scale)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = name;
                wall.transform.SetParent(parent, false);
                wall.transform.position = pos;
                wall.transform.localScale = scale;
                EnsureBoxCollider(wall, Vector3.one, Vector3.zero);
                SetStaticFlags(wall);
                wall.GetComponent<Renderer>().sharedMaterial = wallMat;
            }

            int houseIndex = 0;
            void House(Vector3 pos, float yaw, float scale, bool useSynty)
            {
                GameObject prefab = useSynty
                    ? syntyHousePrefab
                    : AssetDatabase.LoadAssetAtPath<GameObject>(kaykitHouses[houseIndex % kaykitHouses.Length]);
                houseIndex++;
                if (prefab == null) return;
                PlaceBuilding(prefab, buildingsFolder, $"HillsideHouse_{houseIndex:D2}", pos, Quaternion.Euler(0f, yaw, 0f), Vector3.one * scale);
            }

            // --- Level 0: Lower Valley fringe clusters (clear of the landslide bounding box) ---
            // East fringe (open ground, no hazard nearby)
            House(new Vector3(9f, 0.5f, -30f), -110f, 0.9f, true);
            House(new Vector3(13f, 0.5f, -22f), -70f, 2.2f, false);
            House(new Vector3(9f, 0.5f, -17.5f), -100f, 2.2f, false);
            RetainingAccent("Retain_L0_East", buildingsFolder, new Vector3(15.5f, 0.75f, -23f), new Vector3(0.6f, 1.0f, 14f));

            // West fringe: one cluster south of the landslide, one north of it near the ramp —
            // framing the hazard with intact housing rather than overlapping it.
            House(new Vector3(-9f, 0.5f, -31.5f), 110f, 0.9f, true);
            House(new Vector3(-13f, 0.5f, -16.5f), 75f, 2.2f, false);

            // --- Level 1: Mid-slope neighborhood between the town street and the branch roads ---
            House(new Vector3(-11f, 3.0f, 0f), 90f, 2.2f, false);
            House(new Vector3(-11f, 3.0f, 8f), 90f, 2.2f, false);
            House(new Vector3(11f, 3.0f, 0f), -90f, 2.2f, false);
            House(new Vector3(11f, 3.0f, 8f), -90f, 2.2f, false);

            // Parked cars for street life
            string sedanFbx = $"{KayKitCityDir}/car_sedan.fbx";
            var sedanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sedanFbx);
            if (sedanPrefab != null)
            {
                PlaceVehicle(sedanPrefab, propsFolder, "ParkedCar_L1_West", new Vector3(-8.5f, 3.02f, 4f), Quaternion.Euler(0f, 90f, 0f));
                PlaceVehicle(sedanPrefab, propsFolder, "ParkedCar_L0_East", new Vector3(11.5f, 0.51f, -22f), Quaternion.Euler(0f, -90f, 0f));
            }

            // --- Level 2: Upper Residential Ridge fill (between the spine and the existing X=12 houses) ---
            House(new Vector3(-5f, 6.0f, 27f), 100f, 0.9f, true);
            House(new Vector3(5f, 6.0f, 27f), -100f, 2.2f, false);
            House(new Vector3(-5f, 6.0f, 39f), 80f, 2.2f, false);
            House(new Vector3(5f, 6.0f, 39f), -80f, 0.9f, true);
            RetainingAccent("Retain_L2_North", buildingsFolder, new Vector3(0f, 6.3f, 43f), new Vector3(20f, 0.5f, 0.5f));

            // Softening shrubs around the new clusters
            string bushFbx = $"{KayKitForestDir}/Bush_2_A_Color1.fbx";
            var bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bushFbx);
            if (bushPrefab != null)
            {
                Vector3[] extraShrubs = {
                    new Vector3(10f, 0.1f, -25f), new Vector3(-10.5f, 2.6f, 4f),
                    new Vector3(10.5f, 2.6f, 4f), new Vector3(-3f, 5.6f, 30f), new Vector3(3f, 5.6f, 36f),
                };
                for (int i = 0; i < extraShrubs.Length; i++)
                {
                    var shrub = (GameObject)PrefabUtility.InstantiatePrefab(bushPrefab, vegFolder);
                    shrub.name = $"HillsideShrub_{i:D2}";
                    shrub.transform.position = extraShrubs[i];
                    shrub.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    shrub.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.2f, 1.6f);
                    StripColliders(shrub);
                    SetStaticFlags(shrub);
                }
            }
        }

        private static void BuildLandslideHeroUpgrade(Transform barriersFolder, Transform rocksFolder, Transform signsFolder)
        {
            Vector3 hazardCenter = new Vector3(-18f, 0.5f, -22f);

            // A single oversized hero boulder, clearly larger than the surrounding debris field,
            // resting across the collapsed road tile as the unmistakable centerpiece of the slide.
            var heroRockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyRock03Prefab);
            if (heroRockPrefab != null)
            {
                var hero = (GameObject)PrefabUtility.InstantiatePrefab(heroRockPrefab, rocksFolder);
                hero.name = "Landslide_HeroBoulder";
                hero.transform.position = hazardCenter + new Vector3(-1.5f, 1.1f, -0.5f);
                hero.transform.rotation = Quaternion.Euler(15f, 50f, 5f);
                hero.transform.localScale = Vector3.one * 5.2f;
                EnsureMeshCollider(hero);
                SetStaticFlags(hero);
            }

            // Striped roadblock barricade gate across the branch road entrance, well before the
            // cones, so the "you cannot drive through here" read is unmistakable from a distance.
            var barMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            barMat.color = new Color(0.95f, 0.75f, 0.05f);
            var barDarkMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            barDarkMat.color = new Color(0.08f, 0.08f, 0.08f);

            void BarPost(string name, Vector3 pos)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = name;
                post.transform.SetParent(barriersFolder, false);
                post.transform.position = pos;
                post.transform.localScale = new Vector3(0.14f, 0.75f, 0.14f);
                UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
                post.GetComponent<Renderer>().sharedMaterial = barDarkMat;
            }
            BarPost("Landslide_GatePost_L", new Vector3(-3.3f, 1.0f, -20.7f));
            BarPost("Landslide_GatePost_R", new Vector3(-3.3f, 1.0f, -23.3f));

            var gateArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gateArm.name = "Landslide_GateArm";
            gateArm.transform.SetParent(barriersFolder, false);
            gateArm.transform.position = new Vector3(-3.3f, 1.55f, -22f);
            gateArm.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            gateArm.transform.localScale = new Vector3(2.9f, 0.22f, 0.16f);
            UnityEngine.Object.DestroyImmediate(gateArm.GetComponent<Collider>());
            gateArm.GetComponent<Renderer>().sharedMaterial = barMat;

            var gateStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gateStripe.name = "Landslide_GateArm_Stripe";
            gateStripe.transform.SetParent(barriersFolder, false);
            gateStripe.transform.position = new Vector3(-3.3f, 1.55f, -22f);
            gateStripe.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            gateStripe.transform.localScale = new Vector3(0.6f, 0.24f, 0.17f);
            UnityEngine.Object.DestroyImmediate(gateStripe.GetComponent<Collider>());
            gateStripe.GetComponent<Renderer>().sharedMaterial = barDarkMat;

            // Detour signage at the fork itself, facing players approaching from the spawn side
            // (south), before they would think to turn west into the closed branch.
            CreateSignpost(signsFolder, new Vector3(-2f, 0.49f, -18.5f), "ROAD CLOSED WEST\nDETOUR: CONTINUE NORTH", 0f, SignType.Warning);
        }
    }
}
