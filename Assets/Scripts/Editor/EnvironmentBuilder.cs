using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DisasterReady.Utility;
using DisasterReady.Terrain;
using DisasterReady.Missions;
using DisasterReady.POI;
using DisasterReady.Player;
using DisasterReady.CameraSystem;

namespace DisasterReady.EditorTools
{
    /// <summary>
    /// Populates a generated terrain with trees, buildings, POIs, the player and the
    /// camera rig. Kept separate from SceneBuilder so terrain/lighting concerns don't
    /// mix with "what lives on top of the terrain" concerns.
    /// </summary>
    internal static class EnvironmentBuilder
    {
        internal class EnvResult
        {
            public GameObject Player;
            public Camera MainCamera;
            public GameObject HospitalBuilding;
            public GameObject ShelterBuilding;
            public MissionZoneTrigger HospitalTrigger;
            public MissionZoneTrigger ShelterTrigger;
            public MissionZoneTrigger SummitTrigger;
            public Text SummitLabelText;
            public List<GameObject> Buildings = new List<GameObject>();
            public List<GameObject> Trees = new List<GameObject>();
        }

        private static Vector3 WorldFromNormalized(SceneBuilder.TerrainBuildResult t, float nx, float nz)
        {
            float worldY = t.Data.GetInterpolatedHeight(nx, nz);
            float worldX = t.Origin.x + nx * t.Size.x;
            float worldZ = t.Origin.z + nz * t.Size.z;
            return new Vector3(worldX, worldY, worldZ);
        }

        private static float SlopeAt(SceneBuilder.TerrainBuildResult t, float nx, float nz)
        {
            Vector3 n = t.Data.GetInterpolatedNormal(nx, nz);
            return Vector3.Angle(n, Vector3.up);
        }

        public static EnvResult Populate(SceneBuilder.TerrainBuildResult terrain)
        {
            var result = new EnvResult();
            var envRoot = new GameObject("Environment");

            // ---- Materials ----
            var trunkMat = SharedAssetUtility.CreateColorMaterial("TreeTrunk", new Color(0.4f, 0.27f, 0.16f));
            var foliageMats = new[]
            {
                SharedAssetUtility.CreateColorMaterial("TreeFoliageA", new Color(0.28f, 0.5f, 0.22f)),
                SharedAssetUtility.CreateColorMaterial("TreeFoliageB", new Color(0.22f, 0.42f, 0.2f)),
                SharedAssetUtility.CreateColorMaterial("TreeFoliageC", new Color(0.34f, 0.55f, 0.24f)),
            };

            var buildingPalette = new[]
            {
                SharedAssetUtility.CreateColorMaterial("BuildingCream", new Color(0.9f, 0.83f, 0.68f)),
                SharedAssetUtility.CreateColorMaterial("BuildingTerracotta", new Color(0.75f, 0.45f, 0.34f)),
                SharedAssetUtility.CreateColorMaterial("BuildingSlate", new Color(0.55f, 0.58f, 0.62f)),
                SharedAssetUtility.CreateColorMaterial("BuildingMoss", new Color(0.55f, 0.6f, 0.42f)),
            };
            var roofPalette = new[]
            {
                SharedAssetUtility.CreateColorMaterial("RoofBrown", new Color(0.35f, 0.22f, 0.16f)),
                SharedAssetUtility.CreateColorMaterial("RoofRust", new Color(0.5f, 0.24f, 0.14f)),
            };

            var hospitalWallMat = SharedAssetUtility.CreateColorMaterial("HospitalWall", new Color(0.95f, 0.95f, 0.95f));
            var hospitalRoofMat = SharedAssetUtility.CreateColorMaterial("HospitalRoof", new Color(0.78f, 0.15f, 0.15f));
            var hospitalCrossMat = SharedAssetUtility.CreateEmissiveMaterial("HospitalCross", new Color(0.85f, 0.1f, 0.1f), new Color(0.9f, 0.15f, 0.15f) * 1.4f);

            var shelterWallMat = SharedAssetUtility.CreateColorMaterial("ShelterWall", new Color(0.68f, 0.58f, 0.4f));
            var shelterRoofMat = SharedAssetUtility.CreateColorMaterial("ShelterRoof", new Color(0.25f, 0.42f, 0.55f));
            var shelterFlagMat = SharedAssetUtility.CreateEmissiveMaterial("ShelterFlag", new Color(0.95f, 0.6f, 0.1f), new Color(0.95f, 0.6f, 0.1f) * 1.2f);

            var summitFlagMat = SharedAssetUtility.CreateEmissiveMaterial("SummitFlag", new Color(0.95f, 0.8f, 0.15f), new Color(0.95f, 0.8f, 0.15f) * 1.4f);

            // ---- Key locations (normalized terrain space) ----
            Vector2 playerStart01 = new Vector2(0.50f, 0.10f);
            Vector2 hospital01 = new Vector2(0.44f, 0.40f);
            Vector2 shelter01 = new Vector2(0.60f, 0.28f);

            var buildingSpots01 = new List<Vector2>
            {
                new Vector2(0.40f, 0.22f),
                new Vector2(0.52f, 0.20f),
                new Vector2(0.36f, 0.34f),
                new Vector2(0.58f, 0.42f),
                new Vector2(0.46f, 0.48f),
                new Vector2(0.64f, 0.24f),
            };

            // ---- Buildings ----
            var buildingsRoot = new GameObject("Buildings");
            buildingsRoot.transform.SetParent(envRoot.transform);
            int colorIdx = 0;
            foreach (var spot in buildingSpots01)
            {
                Vector3 pos = WorldFromNormalized(terrain, spot.x, spot.y);
                var b = BuildSimpleBuilding(buildingsRoot.transform, pos,
                    width: Random.Range(5.5f, 8f), depth: Random.Range(5f, 7f), height: Random.Range(3.2f, 4.5f),
                    wallMat: buildingPalette[colorIdx % buildingPalette.Length],
                    roofMat: roofPalette[colorIdx % roofPalette.Length],
                    name: $"Building_{colorIdx + 1}",
                    altRoof: colorIdx % 2 == 1);
                b.transform.Rotate(Vector3.up, Random.Range(0f, 360f));
                result.Buildings.Add(b);
                colorIdx++;
            }

            // ---- Hospital POI ----
            Vector3 hospitalPos = WorldFromNormalized(terrain, hospital01.x, hospital01.y);
            var hospital = BuildSimpleBuilding(envRoot.transform, hospitalPos, width: 11f, depth: 9f, height: 5f,
                wallMat: hospitalWallMat, roofMat: hospitalRoofMat, name: "Hospital");
            AddCrossSymbol(hospital.transform, hospitalCrossMat);
            SetupPoi(hospital, PoiType.Hospital, "HOSPITAL", new Color(0.9f, 0.15f, 0.15f), out var hospitalTrigger);
            result.HospitalBuilding = hospital;
            result.HospitalTrigger = hospitalTrigger;

            // ---- Shelter POI ----
            Vector3 shelterPos = WorldFromNormalized(terrain, shelter01.x, shelter01.y);
            var shelter = BuildSimpleBuilding(envRoot.transform, shelterPos, width: 10f, depth: 8f, height: 4.6f,
                wallMat: shelterWallMat, roofMat: shelterRoofMat, name: "EmergencyShelter");
            AddFlagpole(shelter.transform, new Vector3(0f, 4.6f, -3.5f), shelterFlagMat, poleName: "Shelter");
            SetupPoi(shelter, PoiType.EmergencyShelter, "EMERGENCY SHELTER", new Color(0.2f, 0.5f, 0.9f), out var shelterTrigger);
            result.ShelterBuilding = shelter;
            result.ShelterTrigger = shelterTrigger;

            // ---- Highest point marker ----
            Vector3 summitLocal = TerrainGenerator.FindHighestPointLocal(terrain.Data);
            Vector3 summitWorld = terrain.Origin + summitLocal;
            var summitGO = new GameObject("HighestPointMarker");
            summitGO.transform.SetParent(envRoot.transform);
            summitGO.transform.position = summitWorld;
            AddFlagpole(summitGO.transform, new Vector3(0f, 0f, 0f), summitFlagMat, poleHeight: 5f, poleName: "Summit");
            result.SummitLabelText = CreateWorldLabel(summitGO.transform, "HIGHEST POINT", new Vector3(0f, 6.2f, 0f));
            var summitCollider = summitGO.AddComponent<SphereCollider>();
            summitCollider.isTrigger = true;
            summitCollider.radius = 9f;
            summitCollider.center = new Vector3(0f, 2f, 0f);
            var summitTrigger = summitGO.AddComponent<MissionZoneTrigger>();
            result.SummitTrigger = summitTrigger;

            // ---- Trees ----
            var treesRoot = new GameObject("Trees");
            treesRoot.transform.SetParent(envRoot.transform);
            var occupied = new List<Vector2>(buildingSpots01) { hospital01, shelter01, playerStart01 };
            var coneMesh = ProceduralMeshFactory.CreateCone(1.1f, 2.6f, 7);
            var coneMeshSmall = ProceduralMeshFactory.CreateCone(0.85f, 1.8f, 7);
            SharedAssetUtility.CreateOrReplaceAsset(coneMesh, "Assets/Art/TreeFoliageCone.asset");
            SharedAssetUtility.CreateOrReplaceAsset(coneMeshSmall, "Assets/Art/TreeFoliageConeSmall.asset");

            int targetTrees = 26;
            int placed = 0;
            int attempts = 0;
            while (placed < targetTrees && attempts < targetTrees * 20)
            {
                attempts++;
                float nx = Random.Range(0.05f, 0.95f);
                float nz = Random.Range(0.05f, 0.95f);
                Vector2 candidate = new Vector2(nx, nz);

                bool tooClose = false;
                foreach (var occ in occupied)
                {
                    if (Vector2.Distance(candidate, occ) < 0.06f) { tooClose = true; break; }
                }
                if (tooClose) continue;
                if (Vector2.Distance(candidate, new Vector2(hospital01.x, hospital01.y)) < 0.07f) continue;
                if (Vector2.Distance(candidate, new Vector2(shelter01.x, shelter01.y)) < 0.07f) continue;

                float slope = SlopeAt(terrain, nx, nz);
                if (slope > 36f) continue;

                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                // Roughly 1-in-3 broadleaf for variety; the rest stay the
                // original pine-cone silhouette that already reads well
                // against this NER hillside setting.
                GameObject tree = placed % 3 == 0
                    ? BuildBroadleafTree(treesRoot.transform, pos, trunkMat, foliageMats[placed % foliageMats.Length])
                    : BuildTree(treesRoot.transform, pos, trunkMat, foliageMats[placed % foliageMats.Length], coneMesh, coneMeshSmall);
                result.Trees.Add(tree);
                occupied.Add(candidate);
                placed++;
            }

            // ---- Rocks: small scattered clusters for ground-level visual
            // texture, using the same occupancy list so they never overlap
            // buildings/trees/POIs. Kept sparse and off mission paths. ----
            var rocksRoot = new GameObject("Rocks");
            rocksRoot.transform.SetParent(envRoot.transform);
            var rockMat = SharedAssetUtility.CreateColorMaterial("RockProp", new Color(0.52f, 0.5f, 0.47f));
            int targetRocks = 14;
            int rocksPlaced = 0;
            int rockAttempts = 0;
            while (rocksPlaced < targetRocks && rockAttempts < targetRocks * 20)
            {
                rockAttempts++;
                float nx = Random.Range(0.05f, 0.95f);
                float nz = Random.Range(0.05f, 0.95f);
                Vector2 candidate = new Vector2(nx, nz);

                bool tooClose = false;
                foreach (var occ in occupied)
                {
                    if (Vector2.Distance(candidate, occ) < 0.045f) { tooClose = true; break; }
                }
                if (tooClose) continue;

                float slope = SlopeAt(terrain, nx, nz);
                if (slope > 42f) continue;

                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                BuildRockCluster(rocksRoot.transform, pos, rockMat);
                occupied.Add(candidate);
                rocksPlaced++;
            }

            // ---- A short field-boundary fence line near the building
            // cluster - purely decorative set dressing, placed well clear of
            // the mission POIs/paths. ----
            BuildFenceRun(envRoot.transform, WorldFromNormalized(terrain, 0.34f, 0.18f), WorldFromNormalized(terrain, 0.5f, 0.16f));
            BuildFenceRun(envRoot.transform, WorldFromNormalized(terrain, 0.34f, 0.30f), WorldFromNormalized(terrain, 0.34f, 0.18f));

            // ---- Player ----
            Vector3 playerPos = WorldFromNormalized(terrain, playerStart01.x, playerStart01.y);
            result.Player = BuildPlayer(envRoot.transform, playerPos);

            // ---- Welcome signpost at spawn ----
            Vector3 signPos = playerPos + new Vector3(3.2f, 0f, 1.6f);
            BuildSignpost(envRoot.transform, signPos, "AIZAWL\nDEMO TERRAIN");

            // ---- Camera ----
            result.MainCamera = BuildCamera(envRoot.transform, result.Player.transform);

            // Player movement direction is relative to camera facing.
            var playerScript = result.Player.GetComponent<PlayerController>();
            if (playerScript != null) playerScript.CameraPivot = result.MainCamera.transform;

            return result;
        }

        // ---------------------------------------------------------------
        // Buildings
        // ---------------------------------------------------------------
        private static GameObject BuildSimpleBuilding(Transform parent, Vector3 groundPos, float width, float depth, float height, Material wallMat, Material roofMat, string name, bool altRoof = false)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = groundPos;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            body.transform.localScale = new Vector3(width, height, depth);
            body.GetComponent<Renderer>().sharedMaterial = wallMat;
            SafeDestroyCollider(body);
            var boxCol = body.AddComponent<BoxCollider>();
            boxCol.size = Vector3.one;

            if (altRoof)
            {
                // Simple shed/lean-to roof - a single tilted slab - for visual
                // variety against the gable roofs on the other buildings.
                var shed = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shed.name = "Roof";
                shed.transform.SetParent(root.transform);
                float shedRise = height * 0.4f;
                shed.transform.localPosition = new Vector3(0f, height + shedRise * 0.5f, 0f);
                shed.transform.localScale = new Vector3(width * 1.14f, 0.16f, depth * 1.2f);
                shed.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(shedRise, depth) * Mathf.Rad2Deg, 0f, 0f);
                shed.GetComponent<Renderer>().sharedMaterial = roofMat;
                SafeDestroyCollider(shed);
            }
            else
            {
                var roof = new GameObject("Roof");
                roof.transform.SetParent(root.transform);
                roof.transform.localPosition = new Vector3(0f, height, 0f);
                var roofMesh = ProceduralMeshFactory.CreateGableRoof(width * 1.12f, depth * 1.12f, height * 0.55f);
                var mf = roof.AddComponent<MeshFilter>();
                mf.sharedMesh = roofMesh;
                var mr = roof.AddComponent<MeshRenderer>();
                mr.sharedMaterial = roofMat;
            }

            AddWindowsAndDoor(root.transform, width, depth, height);

            return root;
        }

        /// <summary>
        /// Cheap facade detail: a handful of flat emissive-ish window panes on
        /// the front face plus a door, so buildings read as buildings up close
        /// instead of flat-colored boxes. Purely cosmetic, no colliders, no
        /// gameplay effect.
        /// </summary>
        private static void AddWindowsAndDoor(Transform buildingRoot, float width, float depth, float height)
        {
            var windowMat = SharedAssetUtility.CreateEmissiveMaterial($"WindowGlow_{buildingRoot.gameObject.name}", new Color(0.85f, 0.78f, 0.5f), new Color(0.95f, 0.85f, 0.55f) * 1.1f);
            var doorMat = SharedAssetUtility.CreateColorMaterial($"Door_{buildingRoot.gameObject.name}", new Color(0.32f, 0.2f, 0.13f));

            float frontZ = depth * 0.5f + 0.03f;
            float doorWidth = Mathf.Min(0.9f, width * 0.22f);
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door";
            door.transform.SetParent(buildingRoot, false);
            door.transform.localPosition = new Vector3(0f, height * 0.28f, frontZ);
            door.transform.localScale = new Vector3(doorWidth, height * 0.5f, 0.06f);
            door.GetComponent<Renderer>().sharedMaterial = doorMat;
            Object.DestroyImmediate(door.GetComponent<Collider>());

            int windowCount = width > 7f ? 2 : 1;
            float windowSize = Mathf.Min(0.7f, width * 0.16f);
            for (int side = 0; side < windowCount; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                float xOff = sign * (doorWidth * 0.5f + windowSize * 0.9f + 0.35f);
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = $"Window_{side}";
                win.transform.SetParent(buildingRoot, false);
                win.transform.localPosition = new Vector3(xOff, height * 0.6f, frontZ);
                win.transform.localScale = new Vector3(windowSize, windowSize, 0.05f);
                win.GetComponent<Renderer>().sharedMaterial = windowMat;
                Object.DestroyImmediate(win.GetComponent<Collider>());
            }
        }

        private static void AddCrossSymbol(Transform buildingRoot, Material crossMat)
        {
            var pivot = new GameObject("CrossSymbol");
            pivot.transform.SetParent(buildingRoot);
            pivot.transform.localPosition = new Vector3(0f, 3.2f, -4.55f);

            var vertical = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vertical.name = "Vertical";
            vertical.transform.SetParent(pivot.transform);
            vertical.transform.localPosition = Vector3.zero;
            vertical.transform.localScale = new Vector3(0.5f, 2f, 0.15f);
            vertical.GetComponent<Renderer>().sharedMaterial = crossMat;
            SafeDestroyCollider(vertical);

            var horizontal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            horizontal.name = "Horizontal";
            horizontal.transform.SetParent(pivot.transform);
            horizontal.transform.localPosition = Vector3.zero;
            horizontal.transform.localScale = new Vector3(2f, 0.5f, 0.15f);
            horizontal.GetComponent<Renderer>().sharedMaterial = crossMat;
            SafeDestroyCollider(horizontal);
        }

        private static void AddFlagpole(Transform parent, Vector3 localBasePos, Material flagMat, float poleHeight = 3.5f, string poleName = "Pole")
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Flagpole";
            pole.transform.SetParent(parent);
            pole.transform.localPosition = localBasePos + new Vector3(0f, poleHeight * 0.5f, 0f);
            pole.transform.localScale = new Vector3(0.12f, poleHeight * 0.5f, 0.12f);
            pole.GetComponent<Renderer>().sharedMaterial = SharedAssetUtility.CreateColorMaterial($"PoleMat_{poleName}", new Color(0.35f, 0.33f, 0.3f));
            SafeDestroyCollider(pole);

            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(parent);
            flag.transform.localPosition = localBasePos + new Vector3(0.6f, poleHeight - 0.6f, 0f);
            flag.transform.localScale = new Vector3(1.2f, 0.7f, 0.05f);
            flag.GetComponent<Renderer>().sharedMaterial = flagMat;
            SafeDestroyCollider(flag);
        }

        private static void SetupPoi(GameObject building, PoiType type, string label, Color markerColor, out MissionZoneTrigger trigger)
        {
            var marker = building.AddComponent<PoiMarker>();
            marker.Type = type;
            marker.Label = label;

            var iconPivot = new GameObject("Icon");
            iconPivot.transform.SetParent(building.transform);
            iconPivot.transform.localPosition = new Vector3(0f, 7.5f, 0f);
            var iconMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            iconMesh.name = "IconMesh";
            iconMesh.transform.SetParent(iconPivot.transform);
            iconMesh.transform.localScale = Vector3.one * 0.8f;
            iconMesh.GetComponent<Renderer>().sharedMaterial = SharedAssetUtility.CreateEmissiveMaterial($"POIIcon_{label.Replace(" ", "")}", markerColor, markerColor * 1.5f);
            SafeDestroyCollider(iconMesh);
            marker.IconPivot = iconPivot.transform;

            CreateWorldLabel(building.transform, label, new Vector3(0f, 8.5f, 0f));

            var col = building.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 7f;
            col.center = new Vector3(0f, 2f, 0f);
            trigger = building.AddComponent<MissionZoneTrigger>();
        }

        private static Text CreateWorldLabel(Transform parent, string text, Vector3 localPos, float scale = 0.03f)
        {
            var canvasGO = new GameObject($"Label_{text}");
            canvasGO.transform.SetParent(parent);
            canvasGO.transform.localPosition = localPos;
            canvasGO.transform.localRotation = Quaternion.identity;
            canvasGO.transform.localScale = Vector3.one * scale;

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 90f);
            canvasGO.AddComponent<CanvasGroup>().blocksRaycasts = false;

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.06f, 0.08f, 0.65f);
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(canvasGO.transform, false);
            var uiText = textGO.AddComponent<Text>();
            uiText.text = text;
            uiText.font = UIBuilder.GetDefaultFont();
            uiText.fontSize = 30;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = Color.white;
            uiText.fontStyle = FontStyle.Bold;
            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;

            canvasGO.AddComponent<BillboardLabel>();

            return uiText;
        }

        // ---------------------------------------------------------------
        // Trees
        // ---------------------------------------------------------------
        private static GameObject BuildTree(Transform parent, Vector3 pos, Material trunkMat, Material foliageMat, Mesh coneBig, Mesh coneSmall)
        {
            float scale = Random.Range(0.8f, 1.3f);
            var root = new GameObject("Tree");
            root.transform.SetParent(parent);
            root.transform.position = pos;
            root.transform.localScale = Vector3.one * scale;
            root.transform.Rotate(Vector3.up, Random.Range(0f, 360f));

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root.transform);
            trunk.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            trunk.transform.localScale = new Vector3(0.28f, 0.9f, 0.28f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
            SafeDestroyCollider(trunk);

            var foliageLower = new GameObject("FoliageLower");
            foliageLower.transform.SetParent(root.transform);
            foliageLower.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var mfLower = foliageLower.AddComponent<MeshFilter>();
            mfLower.sharedMesh = coneBig;
            foliageLower.AddComponent<MeshRenderer>().sharedMaterial = foliageMat;

            var foliageUpper = new GameObject("FoliageUpper");
            foliageUpper.transform.SetParent(root.transform);
            foliageUpper.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            var mfUpper = foliageUpper.AddComponent<MeshFilter>();
            mfUpper.sharedMesh = coneSmall;
            foliageUpper.AddComponent<MeshRenderer>().sharedMaterial = foliageMat;

            return root;
        }

        // ---------------------------------------------------------------
        // Player - stylized low-poly hiker: head, torso, two arms, two legs,
        // backpack. Built entirely from primitives (no imported model/rig) so
        // it stays tiny and WebGL-safe. Limbs are separate pivots so
        // ProceduralCharacterAnimator can swing them for a walk cycle instead
        // of the old static capsule.
        // ---------------------------------------------------------------
        private static GameObject BuildPlayer(Transform parent, Vector3 pos)
        {
            var root = new GameObject("Player");
            root.transform.SetParent(parent);
            root.transform.position = pos;
            root.tag = "Player";

            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.95f, 0f);
            controller.height = 1.9f;
            controller.radius = 0.45f;

            // Kinematic Rigidbody guarantees OnTriggerEnter fires reliably for the
            // mission zone triggers regardless of CharacterController trigger nuances.
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var jacketMat = SharedAssetUtility.CreateColorMaterial("PlayerJacket", new Color(0.2f, 0.45f, 0.85f));
            var pantsMat = SharedAssetUtility.CreateColorMaterial("PlayerPants", new Color(0.3f, 0.27f, 0.24f));
            var accentMat = SharedAssetUtility.CreateColorMaterial("PlayerAccent", new Color(0.95f, 0.6f, 0.15f));
            var skinMat = SharedAssetUtility.CreateColorMaterial("PlayerSkin", new Color(0.87f, 0.68f, 0.55f));
            var shoeMat = SharedAssetUtility.CreateColorMaterial("PlayerShoe", new Color(0.22f, 0.18f, 0.16f));
            var faceMat = SharedAssetUtility.CreateColorMaterial("PlayerFace", new Color(0.15f, 0.14f, 0.16f));

            // VisualRoot carries every rendered part; ProceduralCharacterAnimator
            // bobs/sways *this* transform while limb pivots swing independently,
            // so none of it ever touches CharacterController/collision.
            var visualRoot = new GameObject("CharacterVisual");
            visualRoot.transform.SetParent(root.transform, false);

            GameObject MakePart(string name, Transform parentT, PrimitiveType prim, Vector3 localPos, Vector3 localScale, Material mat)
            {
                var go = GameObject.CreatePrimitive(prim);
                go.name = name;
                go.transform.SetParent(parentT, false);
                go.transform.localPosition = localPos;
                go.transform.localScale = localScale;
                go.GetComponent<Renderer>().sharedMaterial = mat;
                SafeDestroyCollider(go);
                return go;
            }

            // ---- Legs (hip pivots so the animator can swing them) ----
            var leftLegPivot = new GameObject("LeftLegPivot");
            leftLegPivot.transform.SetParent(visualRoot.transform, false);
            leftLegPivot.transform.localPosition = new Vector3(-0.15f, 0.9f, 0f);
            MakePart("LeftLeg", leftLegPivot.transform, PrimitiveType.Cube, new Vector3(0f, -0.42f, 0f), new Vector3(0.22f, 0.84f, 0.24f), pantsMat);
            MakePart("LeftShoe", leftLegPivot.transform, PrimitiveType.Cube, new Vector3(0f, -0.86f, 0.05f), new Vector3(0.25f, 0.16f, 0.34f), shoeMat);

            var rightLegPivot = new GameObject("RightLegPivot");
            rightLegPivot.transform.SetParent(visualRoot.transform, false);
            rightLegPivot.transform.localPosition = new Vector3(0.15f, 0.9f, 0f);
            MakePart("RightLeg", rightLegPivot.transform, PrimitiveType.Cube, new Vector3(0f, -0.42f, 0f), new Vector3(0.22f, 0.84f, 0.24f), pantsMat);
            MakePart("RightShoe", rightLegPivot.transform, PrimitiveType.Cube, new Vector3(0f, -0.86f, 0.05f), new Vector3(0.25f, 0.16f, 0.34f), shoeMat);

            // ---- Torso ----
            MakePart("Torso", visualRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.18f, 0f), new Vector3(0.52f, 0.62f, 0.32f), jacketMat);

            // ---- Arms (shoulder pivots) ----
            var leftArmPivot = new GameObject("LeftArmPivot");
            leftArmPivot.transform.SetParent(visualRoot.transform, false);
            leftArmPivot.transform.localPosition = new Vector3(-0.34f, 1.42f, 0f);
            MakePart("LeftArm", leftArmPivot.transform, PrimitiveType.Cube, new Vector3(0f, -0.33f, 0f), new Vector3(0.17f, 0.66f, 0.17f), jacketMat);
            MakePart("LeftHand", leftArmPivot.transform, PrimitiveType.Sphere, new Vector3(0f, -0.68f, 0f), Vector3.one * 0.16f, skinMat);

            var rightArmPivot = new GameObject("RightArmPivot");
            rightArmPivot.transform.SetParent(visualRoot.transform, false);
            rightArmPivot.transform.localPosition = new Vector3(0.34f, 1.42f, 0f);
            MakePart("RightArm", rightArmPivot.transform, PrimitiveType.Cube, new Vector3(0f, -0.33f, 0f), new Vector3(0.17f, 0.66f, 0.17f), jacketMat);
            MakePart("RightHand", rightArmPivot.transform, PrimitiveType.Sphere, new Vector3(0f, -0.68f, 0f), Vector3.one * 0.16f, skinMat);

            // ---- Head + friendly face hint (no expression rig - just a small
            // dark visor-ish band so the silhouette reads as "facing forward"). ----
            MakePart("Head", visualRoot.transform, PrimitiveType.Sphere, new Vector3(0f, 1.68f, 0f), Vector3.one * 0.42f, skinMat);
            MakePart("FaceBand", visualRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.66f, 0.185f), new Vector3(0.26f, 0.08f, 0.05f), faceMat);

            // ---- Backpack ----
            MakePart("Backpack", visualRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.2f, -0.24f), new Vector3(0.42f, 0.5f, 0.22f), accentMat);

            var cameraPivot = new GameObject("CameraLookTarget");
            cameraPivot.transform.SetParent(root.transform);
            cameraPivot.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            var playerScript = root.AddComponent<PlayerController>();
            playerScript.MoveSpeed = 6.5f;

            var animator = root.AddComponent<DisasterReady.Player.ProceduralCharacterAnimator>();
            animator.Player = playerScript;
            animator.VisualRoot = visualRoot.transform;
            animator.LeftArm = leftArmPivot.transform;
            animator.RightArm = rightArmPivot.transform;
            animator.LeftLeg = leftLegPivot.transform;
            animator.RightLeg = rightLegPivot.transform;

            return root;
        }

        // ---------------------------------------------------------------
        // Camera
        // ---------------------------------------------------------------
        private static Camera BuildCamera(Transform parent, Transform playerTransform)
        {
            var camGO = new GameObject("MainCamera");
            camGO.transform.SetParent(parent);
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 550f;
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.6f, 0.75f, 0.92f);
            camGO.AddComponent<AudioListener>();

            var camData = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing;

            var orbit = camGO.AddComponent<OrbitCameraController>();
            orbit.Target = playerTransform;
            orbit.StartYaw = 0f;
            orbit.StartPitch = 34f;
            orbit.Distance = 10.5f;
            orbit.TargetOffset = new Vector3(0f, 1.5f, 0f);

            return cam;
        }

        // ---------------------------------------------------------------
        // Broadleaf tree variant (round canopy instead of conifer cones) -
        // reuses the same trunk cylinder approach as BuildTree for a
        // consistent "cheap primitives" build cost.
        // ---------------------------------------------------------------
        private static GameObject BuildBroadleafTree(Transform parent, Vector3 pos, Material trunkMat, Material foliageMat)
        {
            float scale = Random.Range(0.85f, 1.25f);
            var root = new GameObject("BroadleafTree");
            root.transform.SetParent(parent);
            root.transform.position = pos;
            root.transform.localScale = Vector3.one * scale;
            root.transform.Rotate(Vector3.up, Random.Range(0f, 360f));

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root.transform);
            trunk.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            trunk.transform.localScale = new Vector3(0.22f, 0.7f, 0.22f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
            SafeDestroyCollider(trunk);

            // Three overlapping spheres instead of one, so the canopy reads
            // as a rounded clump rather than a single perfect ball.
            Vector3[] offsets =
            {
                new Vector3(0f, 1.85f, 0f),
                new Vector3(0.42f, 1.65f, 0.15f),
                new Vector3(-0.38f, 1.6f, -0.2f),
            };
            float[] sizes = { 1.15f, 0.8f, 0.75f };
            for (int i = 0; i < offsets.Length; i++)
            {
                var lobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lobe.name = $"CanopyLobe_{i}";
                lobe.transform.SetParent(root.transform);
                lobe.transform.localPosition = offsets[i];
                lobe.transform.localScale = Vector3.one * sizes[i];
                lobe.GetComponent<Renderer>().sharedMaterial = foliageMat;
                SafeDestroyCollider(lobe);
            }

            return root;
        }

        // ---------------------------------------------------------------
        // Rock cluster - 2-4 small rotated/scaled cubes grouped together, the
        // same "cheap primitive, stylized by transform variety" trick used
        // for everything else in this file.
        // ---------------------------------------------------------------
        private static void BuildRockCluster(Transform parent, Vector3 pos, Material rockMat)
        {
            var root = new GameObject("RockCluster");
            root.transform.SetParent(parent);
            root.transform.position = pos;
            root.transform.Rotate(Vector3.up, Random.Range(0f, 360f));

            int count = Random.Range(2, 4);
            for (int i = 0; i < count; i++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = $"Rock_{i}";
                rock.transform.SetParent(root.transform);
                Vector2 jitter = Random.insideUnitCircle * 0.5f;
                float h = Random.Range(0.22f, 0.5f);
                rock.transform.localPosition = new Vector3(jitter.x, h * 0.4f, jitter.y);
                rock.transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f));
                rock.transform.localScale = new Vector3(h * Random.Range(0.8f, 1.3f), h, h * Random.Range(0.8f, 1.3f));
                rock.GetComponent<Renderer>().sharedMaterial = rockMat;
                SafeDestroyCollider(rock);
            }
        }

        // ---------------------------------------------------------------
        // Simple decorative fence: a straight run of posts + a rail between
        // two world points. Purely cosmetic set dressing (no colliders), so
        // it can never accidentally block the player or a mission trigger.
        // ---------------------------------------------------------------
        private static void BuildFenceRun(Transform parent, Vector3 from, Vector3 to)
        {
            var woodMat = SharedAssetUtility.CreateColorMaterial("FenceWood", new Color(0.42f, 0.32f, 0.22f));
            var root = new GameObject("Fence");
            root.transform.SetParent(parent);
            root.transform.position = from;

            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f) return;
            Vector3 dir = delta / length;
            Quaternion rot = Quaternion.LookRotation(dir);

            int postCount = Mathf.Max(2, Mathf.RoundToInt(length / 2.5f) + 1);
            for (int i = 0; i < postCount; i++)
            {
                float t = (float)i / (postCount - 1);
                Vector3 p = Vector3.Lerp(from, to, t);
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"Post_{i}";
                post.transform.SetParent(root.transform);
                post.transform.position = p + new Vector3(0f, 0.45f, 0f);
                post.transform.localScale = new Vector3(0.1f, 0.9f, 0.1f);
                post.GetComponent<Renderer>().sharedMaterial = woodMat;
                SafeDestroyCollider(post);
            }

            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Rail";
            rail.transform.SetParent(root.transform);
            rail.transform.position = from + dir * (length * 0.5f) + new Vector3(0f, 0.65f, 0f);
            rail.transform.rotation = rot;
            rail.transform.localScale = new Vector3(0.06f, 0.08f, length);
            rail.GetComponent<Renderer>().sharedMaterial = woodMat;
            SafeDestroyCollider(rail);
        }

        // ---------------------------------------------------------------
        // Small wooden welcome signpost near player spawn. Reuses
        // CreateWorldLabel's billboard text panel so it's legible from any
        // angle, mounted on a plank instead of floating.
        // ---------------------------------------------------------------
        private static void BuildSignpost(Transform parent, Vector3 pos, string text)
        {
            var woodMat = SharedAssetUtility.CreateColorMaterial("SignpostWood", new Color(0.42f, 0.32f, 0.22f));
            var plankMat = SharedAssetUtility.CreateColorMaterial("SignpostPlank", new Color(0.62f, 0.48f, 0.32f));

            var root = new GameObject("WelcomeSignpost");
            root.transform.SetParent(parent);
            root.transform.position = pos;

            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post";
            post.transform.SetParent(root.transform);
            post.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            post.transform.localScale = new Vector3(0.09f, 1.1f, 0.09f);
            post.GetComponent<Renderer>().sharedMaterial = woodMat;
            SafeDestroyCollider(post);

            var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = "Plank";
            plank.transform.SetParent(root.transform);
            plank.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            plank.transform.localScale = new Vector3(1.5f, 0.5f, 0.08f);
            plank.GetComponent<Renderer>().sharedMaterial = plankMat;
            SafeDestroyCollider(plank);

            // Much smaller scale than the default world label (which is sized
            // to read from several meters away at a POI) - this sign sits
            // right next to the spawn point, so a small close-range label
            // reads correctly instead of looming over the whole view.
            CreateWorldLabel(root.transform, text, new Vector3(0f, 1.85f, 0.06f), scale: 0.009f);
        }

        private static void SafeDestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
    }
}
