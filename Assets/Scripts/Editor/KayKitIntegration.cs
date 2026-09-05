using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using DisasterReady.Player;

namespace DisasterReady.EditorTools
{
    /// <summary>
    /// Visual-replacement layer: instantiates real, verified KayKit assets
    /// (player, buildings, vegetation) in place of EnvironmentBuilder's old
    /// primitive-geometry builders. Kept in its own file rather than folded
    /// into EnvironmentBuilder so the KayKit-specific asset paths, scale/offset
    /// math and one-off importer setup (animation clip looping) all live
    /// together. EnvironmentBuilder.Populate calls into this file at the same
    /// call sites the old BuildPlayer/BuildSimpleBuilding/BuildTree/
    /// BuildRockCluster/BuildShrubCluster calls used to occupy; nothing about
    /// Populate's overall composition (spot lists, spacing, occupancy
    /// avoidance, roads, fences, POI trigger wiring) changed.
    ///
    /// Every gameplay-critical component (CharacterController, Rigidbody,
    /// PlayerController, FallRecoveryGuard, BoxCollider on buildings,
    /// MissionZoneTrigger/PoiMarker wiring) is still created exactly the same
    /// way it always was - only the *visual* GameObjects underneath them are
    /// now imported meshes instead of primitives.
    /// </summary>
    internal static class KayKitIntegration
    {
        // -----------------------------------------------------------------
        // Asset paths - every one of these was confirmed to exist on disk via
        // KayKitInspector.DumpReport() / a device-side directory listing
        // before being referenced here. Do not add a path below without the
        // same verification.
        // -----------------------------------------------------------------
        private const string PlayerFbx = "Assets/KayKit/Adventurers/Characters/fbx/Rogue_Hooded.fbx";

        private const string AnimGeneralFbx = "Assets/KayKit/CharacterAnimations/Animations/fbx/Rig_Medium/Rig_Medium_General.fbx";
        private const string AnimMovementBasicFbx = "Assets/KayKit/CharacterAnimations/Animations/fbx/Rig_Medium/Rig_Medium_MovementBasic.fbx";

        private const string IdleClipName = "Idle_A";
        private const string WalkClipName = "Walking_B";
        private const string RunClipName = "Running_A";

        private const string BuildingDir = "Assets/KayKit/CityBuilder/Assets/fbx (unity)";
        private const string ForestDir = "Assets/KayKit/Forest/Assets/fbx(unity)";

        // Real KayKit CityBuilder road pieces (confirmed present on disk via a
        // device-side `ls` of BuildingDir before being referenced here, same
        // discipline as every other path in this file) - used by
        // EnvironmentBuilder's route-building pass instead of a stretched
        // flat-colour primitive strip. Modular, on the same city grid as the
        // buildings.
        private const string RoadStraightFbx = BuildingDir + "/road_straight.fbx";
        private const string RoadCornerCurvedFbx = BuildingDir + "/road_corner_curved.fbx";

        // Known "with base" building bounds (unscaled), captured from
        // KayKitInspectionReport.txt - used to size each building's
        // BoxCollider without having to re-measure renderer bounds at
        // scene-generation time. Vertical placement no longer depends on
        // these being exactly right (see SnapToGround), but collider sizing
        // and the hillside footprint-grounding pass still use them.
        private static readonly Dictionary<string, (Vector3 size, Vector3 center)> BuildingBounds = new Dictionary<string, (Vector3, Vector3)>
        {
            ["A"] = (new Vector3(2.00f, 1.65f, 2.00f), new Vector3(0f, 0.82f, 0f)),
            ["B"] = (new Vector3(2.00f, 1.65f, 2.00f), new Vector3(0f, 0.82f, 0f)),
            ["C"] = (new Vector3(2.00f, 2.98f, 2.00f), new Vector3(0f, 1.49f, 0f)),
            ["D"] = (new Vector3(2.00f, 2.97f, 2.02f), new Vector3(0f, 1.49f, 0.01f)),
            ["E"] = (new Vector3(2.01f, 2.35f, 2.00f), new Vector3(0f, 1.18f, 0f)),
            ["F"] = (new Vector3(2.01f, 2.35f, 2.00f), new Vector3(0f, 1.18f, 0f)),
            ["G"] = (new Vector3(2.01f, 2.98f, 2.00f), new Vector3(0f, 1.49f, 0f)),
            ["H"] = (new Vector3(2.01f, 3.05f, 2.00f), new Vector3(0f, 1.53f, 0f)),
        };

        // Verified tree/rock/bush filename pools (device-side `ls`, not
        // guessed) - one entry per confirmed file.
        private static readonly string[] TreeVariants =
        {
            "Tree_1_A_Color1", "Tree_1_B_Color1", "Tree_1_C_Color1",
            "Tree_2_A_Color1", "Tree_2_B_Color1", "Tree_2_C_Color1",
            "Tree_3_A_Color1", "Tree_3_B_Color1", "Tree_3_C_Color1",
            "Tree_4_A_Color1", "Tree_4_B_Color1", "Tree_4_C_Color1",
        };
        private static readonly string[] RockVariants =
        {
            "Rock_1_A_Color1", "Rock_1_B_Color1", "Rock_1_C_Color1",
            "Rock_2_A_Color1", "Rock_2_B_Color1",
            "Rock_3_A_Color1", "Rock_3_B_Color1", "Rock_3_C_Color1",
        };
        private static readonly string[] BushVariants =
        {
            "Bush_1_A_Color1", "Bush_1_B_Color1", "Bush_1_C_Color1",
            "Bush_2_A_Color1", "Bush_2_B_Color1",
            "Bush_4_A_Color1", "Bush_4_B_Color1",
        };

        // -----------------------------------------------------------------
        // Shared helpers
        // -----------------------------------------------------------------
        private static readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

        private static GameObject LoadPrefab(string path)
        {
            if (_prefabCache.TryGetValue(path, out var cached) && cached != null) return cached;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null)
            {
                Debug.LogWarning($"[KayKitIntegration] Missing asset at '{path}' - check KayKitInspectionReport.txt for the verified path list.");
            }
            _prefabCache[path] = go;
            return go;
        }

        /// <summary>
        /// Instantiates a KayKit source mesh as a plain (non-prefab-linked)
        /// GameObject and strips any Collider the importer might have added,
        /// so imported visual geometry can never interfere with gameplay
        /// collision - the same discipline SafeDestroyCollider already
        /// enforces for every procedural primitive in this file.
        /// </summary>
        private static GameObject InstantiateStripped(string path, Transform parent)
        {
            var prefab = LoadPrefab(path);
            if (prefab == null) return null;
            var instance = (GameObject)Object.Instantiate(prefab, parent);
            instance.name = prefab.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            foreach (var col in instance.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(col);
            }
            return instance;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// Shifts <paramref name="instance"/> straight up/down (world Y only)
        /// so the LOWEST point of its actual, currently-measured renderer
        /// bounds sits exactly at <paramref name="groundWorldY"/>. Uses real
        /// bounds instead of a hardcoded per-asset offset, so it self-corrects
        /// for KayKit source meshes whose pivot isn't at the mesh base -
        /// e.g. Rock_3's pivot sits ~0.47m above its own base (confirmed via
        /// KayKitInspectionReport.txt: size.y=0.86, center.y=-0.04, so
        /// min.y=-0.47) which would otherwise bury most of the rock in the
        /// terrain. Must be called after the instance's final local scale is
        /// set, since scale changes the measured bounds.
        /// </summary>
        private static void SnapToGround(GameObject instance, float groundWorldY)
        {
            if (instance == null) return;
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            float minY = float.PositiveInfinity;
            foreach (var r in renderers)
            {
                if (r.bounds.min.y < minY) minY = r.bounds.min.y;
            }
            if (float.IsInfinity(minY)) return;
            float delta = groundWorldY - minY;
            if (Mathf.Abs(delta) > 0.0005f)
            {
                instance.transform.position += new Vector3(0f, delta, 0f);
            }
        }

        // -----------------------------------------------------------------
        // Roads (KayKit CityBuilder pieces) - replaces the old flat-colour
        // primitive path strip. Tiles are measured once (actual renderer
        // bounds, not a guessed length) and repeated along each route
        // segment; corners get a dedicated curved piece instead of two
        // straight runs abutting at an angle.
        // -----------------------------------------------------------------
        private static float _roadTileLength = -1f;
        // True if the imported mesh's long (travel) axis turns out to be
        // local X rather than Z once actually measured - rather than assume
        // the same +Z-forward convention the building/tree assets happen to
        // use, this is detected from the real bounds and corrected with a
        // one-time extra yaw on the instantiated mesh, so BuildRoadSegment/
        // BuildRoadCorner's own direction math (which always treats "the
        // tile's length" as the travel direction) stays correct either way.
        private static bool _roadLengthAlongX;

        /// <summary>
        /// Measures the real world-space length/width of one road_straight
        /// tile by instantiating a throwaway copy and reading its combined
        /// renderer bounds (not a guessed size) - and detects which local
        /// axis is actually the long/travel axis instead of assuming +Z.
        /// Cached so this only happens once per editor run.
        /// </summary>
        private static void EnsureRoadTileMeasured()
        {
            if (_roadTileLength > 0f) return;
            var probe = InstantiateStripped(RoadStraightFbx, null);
            if (probe == null) { _roadTileLength = 2f; return; }
            var renderers = probe.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { _roadTileLength = 2f; }
            else
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                _roadLengthAlongX = b.size.x > b.size.z;
                _roadTileLength = Mathf.Max(0.5f, _roadLengthAlongX ? b.size.x : b.size.z);
            }
            Object.DestroyImmediate(probe);
        }

        /// <summary>Instantiates one road piece under tileRoot with the axis correction from EnsureRoadTileMeasured already applied.</summary>
        private static void InstantiateRoadPiece(string fbxPath, Transform tileRoot)
        {
            var instance = InstantiateStripped(fbxPath, tileRoot);
            if (instance != null && _roadLengthAlongX)
            {
                instance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }

        /// <summary>
        /// Lays real KayKit road tiles along a straight world-space segment,
        /// each individually terrain-sampled and tilted to the local slope
        /// (Quaternion.LookRotation with the terrain normal as "up") instead
        /// of one rigid flat strip - this is what makes the road actually
        /// follow the hillside instead of clipping/floating on slopes. Ends
        /// are trimmed slightly (trimEachEnd, world units) so a
        /// BuildRoadCorner placed at a shared endpoint has room to sit
        /// without two tile sets overlapping heavily.
        /// </summary>
        public static void BuildRoadSegment(Transform parent, Vector3 fromWorld, Vector3 toWorld, System.Func<float, float, float> sampleHeight, System.Func<float, float, Vector3> sampleNormal, float trimEachEnd = 0f)
        {
            EnsureRoadTileMeasured();
            Vector3 full = toWorld - fromWorld;
            Vector3 flatDir = new Vector3(full.x, 0f, full.z);
            float totalLen = flatDir.magnitude;
            if (totalLen < 0.01f) return;
            flatDir /= totalLen;

            float usableLen = Mathf.Max(0f, totalLen - trimEachEnd * 2f);
            if (usableLen < 0.01f) return;
            Vector3 start = fromWorld + flatDir * trimEachEnd;

            int tileCount = Mathf.Max(1, Mathf.RoundToInt(usableLen / _roadTileLength));
            float actualTileLen = usableLen / tileCount;

            var root = new GameObject("RoadSegment");
            root.transform.SetParent(parent);

            for (int i = 0; i < tileCount; i++)
            {
                float centerDist = (i + 0.5f) * actualTileLen;
                Vector3 centerXZ = start + flatDir * centerDist;
                float groundY = sampleHeight(centerXZ.x, centerXZ.z);
                Vector3 normal = sampleNormal(centerXZ.x, centerXZ.z);
                Vector3 tilePos = new Vector3(centerXZ.x, groundY, centerXZ.z);

                var tileRoot = new GameObject($"RoadTile_{i}");
                tileRoot.transform.SetParent(root.transform);
                tileRoot.transform.position = tilePos;
                tileRoot.transform.rotation = Quaternion.LookRotation(flatDir, normal);
                // Uniform scale so the tile's own measured length matches the
                // even spacing computed above (avoids a visible gap/overlap
                // when the segment length isn't an exact multiple of the
                // tile's native size).
                float scaleFix = actualTileLen / _roadTileLength;
                tileRoot.transform.localScale = new Vector3(scaleFix, 1f, scaleFix);

                InstantiateRoadPiece(RoadStraightFbx, tileRoot.transform);
            }
        }

        /// <summary>
        /// A single curved corner tile at a route junction, oriented to
        /// bisect the angle between the two adjoining segment directions so
        /// it reads as one continuous bend rather than two straight runs
        /// meeting edge-on.
        /// </summary>
        public static void BuildRoadCorner(Transform parent, Vector3 worldPos, Vector3 dirA, Vector3 dirB, System.Func<float, float, float> sampleHeight, System.Func<float, float, Vector3> sampleNormal)
        {
            EnsureRoadTileMeasured();
            Vector3 flatA = new Vector3(dirA.x, 0f, dirA.z).normalized;
            Vector3 flatB = new Vector3(dirB.x, 0f, dirB.z).normalized;
            Vector3 bisector = (flatA + flatB);
            if (bisector.sqrMagnitude < 0.0001f) bisector = flatA;
            bisector.Normalize();

            float groundY = sampleHeight(worldPos.x, worldPos.z);
            Vector3 normal = sampleNormal(worldPos.x, worldPos.z);

            var tileRoot = new GameObject("RoadCorner");
            tileRoot.transform.SetParent(parent);
            tileRoot.transform.position = new Vector3(worldPos.x, groundY, worldPos.z);
            tileRoot.transform.rotation = Quaternion.LookRotation(bisector, normal);
            InstantiateRoadPiece(RoadCornerCurvedFbx, tileRoot.transform);
        }

        // -----------------------------------------------------------------
        // Player
        // -----------------------------------------------------------------
        public static GameObject BuildPlayer(Transform parent, Vector3 pos)
        {
            var root = new GameObject("Player");
            root.transform.SetParent(parent);
            root.transform.position = pos;
            root.tag = "Player";

            // Identical CharacterController/Rigidbody setup to the procedural
            // player this replaces - gameplay collision body is untouched.
            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.95f, 0f);
            controller.height = 1.9f;
            controller.radius = 0.45f;

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var playerScript = root.AddComponent<PlayerController>();
            playerScript.MoveSpeed = 6.5f;

            root.AddComponent<FallRecoveryGuard>();

            // Kept attached purely for its independent, VisualRoot/limb-null-
            // safe footstep audio + dust puff systems (both parented directly
            // to this root transform, not to the KayKit visual below) - see
            // ProceduralCharacterAnimator's null checks throughout Update().
            // Limb/VisualRoot fields intentionally left unassigned.
            var footstepFx = root.AddComponent<ProceduralCharacterAnimator>();
            footstepFx.Player = playerScript;

            // ---- KayKit visual ----
            var visualHolder = new GameObject("CharacterVisual");
            visualHolder.transform.SetParent(root.transform, false);

            var characterInstance = InstantiateStripped(PlayerFbx, visualHolder.transform);
            if (characterInstance != null)
            {
                // Rogue_Hooded's combined renderer bounds (import space,
                // unscaled) are size=(1.94,2.35,1.10) center=(0,1.00,0.01) -
                // see KayKitInspectionReport.txt. Scale so total height sits
                // just inside the 1.9m CharacterController capsule, then
                // shift up so the feet (bounds.min.y) land on y=0 (ground)
                // rather than ~0.175 below it.
                const float rawHeight = 2.35f;
                const float rawMinY = -0.175f;
                const float targetHeight = 1.85f;
                float scale = targetHeight / rawHeight;
                visualHolder.transform.localScale = Vector3.one * scale;
                visualHolder.transform.localPosition = new Vector3(0f, -rawMinY * scale, 0f);

                SetupAnimator(characterInstance);
            }

            var cameraPivot = new GameObject("CameraLookTarget");
            cameraPivot.transform.SetParent(root.transform);
            cameraPivot.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            return root;
        }

        private static AnimatorController _cachedController;

        private static void SetupAnimator(GameObject characterInstance)
        {
            var rigMedium = FindDeep(characterInstance.transform, "Rig_Medium");
            if (rigMedium == null)
            {
                Debug.LogWarning("[KayKitIntegration] Could not find 'Rig_Medium' inside the instantiated player - animation not wired.");
                return;
            }

            // IMPORTANT: the Animator goes on the character's TOP-LEVEL
            // instantiated GameObject (characterInstance), NOT on the
            // "Rig_Medium" node. Confirmed via KayKitAnimDiagnostic (dumps
            // AnimationUtility.GetCurveBindings against the real instantiated
            // hierarchy): every curve in every Rig_Medium_*.fbx clip is bound
            // to a path beginning with "Rig_Medium/..." (e.g.
            // "Rig_Medium/root/hips/spine/..."), meaning those paths are
            // relative to Rig_Medium's PARENT, not to Rig_Medium itself.
            // Putting the Animator on Rig_Medium made Unity look for a
            // non-existent "Rig_Medium/Rig_Medium/..." child every frame, so
            // zero curves ever bound and the character stayed in its import
            // bind pose (T-pose) regardless of which state/clip was playing.
            var animator = characterInstance.GetComponent<Animator>();
            if (animator == null) animator = characterInstance.AddComponent<Animator>();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(PlayerFbx).OfType<Avatar>().FirstOrDefault();
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = EnsureAnimatorController();

            var rigRoot = rigMedium.Find("root");

            var playerGO = characterInstance.GetComponentInParent<PlayerController>();
            if (playerGO != null)
            {
                var locomotion = playerGO.gameObject.AddComponent<KayKitLocomotionAnimator>();
                locomotion.Player = playerGO;
                locomotion.Animator = animator;
                // Both "Rig_Medium" and "root" carry their own baked
                // translation keys in the source clips (confirmed by the
                // same curve-binding dump - both appear as distinct animated
                // paths). World-space movement must come only from the
                // CharacterController via PlayerController, so both
                // transforms are re-anchored to their rest local position
                // every frame; this only cancels translation drift, every
                // rotation curve (the actual limb swing that makes the walk/
                // run poses read) plays completely normally.
                locomotion.PositionAnchors = new[] { rigMedium, rigRoot };
                // Overnight stabilization pass: wire the CharacterVisual
                // holder so the locomotion animator can do a one-time,
                // post-animation measurement-based grounding correction
                // (see KayKitLocomotionAnimator.ApplyOneTimeGroundingCorrection)
                // instead of trusting the static import-time offset below to
                // already be exactly right once the Idle clip's own baked
                // translation is actually playing.
                locomotion.VisualRoot = characterInstance.transform.parent;
            }
        }

        private static AnimatorController EnsureAnimatorController()
        {
            const string path = "Assets/Art/Animation/KayKitPlayerAnimator.controller";
            System.IO.Directory.CreateDirectory("Assets/Art/Animation");

            EnsureClipsLoop(AnimGeneralFbx, new[] { IdleClipName });
            EnsureClipsLoop(AnimMovementBasicFbx, new[] { WalkClipName, RunClipName });

            var idle = LoadClip(AnimGeneralFbx, IdleClipName);
            var walk = LoadClip(AnimMovementBasicFbx, WalkClipName);
            var run = LoadClip(AnimMovementBasicFbx, RunClipName);

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed01", AnimatorControllerParameterType.Float);

            var tree = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed01" };
            AssetDatabase.AddObjectToAsset(tree, controller);
            if (idle != null) tree.AddChild(idle, 0f);
            if (walk != null) tree.AddChild(walk, 0.62f);
            if (run != null) tree.AddChild(run, 1f);

            var rootSM = controller.layers[0].stateMachine;
            var state = rootSM.AddState("Locomotion");
            state.motion = tree;
            rootSM.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            _cachedController = controller;
            return controller;
        }

        private static AnimationClip LoadClip(string fbxPath, string clipName)
        {
            return AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().FirstOrDefault(c => c.name == clipName);
        }

        /// <summary>
        /// Marks specific sub-clips inside an imported FBX as looping via the
        /// ModelImporter's per-clip settings (the actual source of
        /// AnimationClip.isLooping at runtime), reimporting only if something
        /// actually needs to change - idempotent across repeated
        /// "Build Demo Scene" runs.
        /// </summary>
        private static void EnsureClipsLoop(string fbxPath, string[] clipNames)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;

            bool changed = false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (System.Array.IndexOf(clipNames, clips[i].name) >= 0 && !clips[i].loopTime)
                {
                    clips[i].loopTime = true;
                    changed = true;
                }
            }
            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        // -----------------------------------------------------------------
        // Buildings
        // -----------------------------------------------------------------
        public static GameObject BuildBuilding(Transform parent, Vector3 groundPos, string letter, float scale, string name)
        {
            string path = $"{BuildingDir}/building_{letter}.fbx";
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = groundPos;

            var instance = InstantiateStripped(path, root.transform);
            if (instance == null)
            {
                Object.DestroyImmediate(root);
                return null;
            }
            instance.transform.localScale = Vector3.one * scale;
            SnapToGround(instance, groundPos.y);

            // Footprint-accurate, non-guessed collider: measured directly
            // from this instance's actual combined renderer bounds (post-
            // scale) rather than trusting the static per-letter table below.
            // root's rotation is still identity at this point (EnvironmentBuilder
            // rotates the whole root AFTER BuildBuilding returns), so a simple
            // world-space-minus-root-position conversion gives the correct
            // root-local box - which then rotates correctly with the root
            // exactly like the old table-driven box did.
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combined = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) combined.Encapsulate(renderers[i].bounds);
                var col = root.AddComponent<BoxCollider>();
                col.center = combined.center - root.transform.position;
                col.size = combined.size;
            }
            else if (BuildingBounds.TryGetValue(letter, out var bounds))
            {
                // Fallback only if this building's mesh somehow has no
                // renderers to measure (e.g. asset failed to load) - keeps
                // old behaviour rather than leaving the building with no
                // collider at all.
                var col = root.AddComponent<BoxCollider>();
                col.size = bounds.size * scale;
                col.center = bounds.center * scale;
            }

            return root;
        }

        /// <summary>World-space (X,Z) footprint size for a building letter, unscaled - used by EnvironmentBuilder to sample terrain height at the building's actual rotated footprint corners (hillside grounding) rather than a single center point.</summary>
        public static Vector2 GetBuildingFootprint(string letter)
        {
            return BuildingBounds.TryGetValue(letter, out var b) ? new Vector2(b.size.x, b.size.z) : new Vector2(2f, 2f);
        }

        /// <summary>Small red-cross panel on a short mast above the roofline - readable from gameplay distance, doesn't depend on knowing which side the mesh's door/window face is on.</summary>
        public static void AddRoofCross(Transform buildingRoot, float worldHeight, Material crossMat)
        {
            var pivot = new GameObject("CrossSymbol");
            pivot.transform.SetParent(buildingRoot, false);
            pivot.transform.localPosition = new Vector3(0f, worldHeight + 1.1f, 0f);

            var vertical = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vertical.name = "Vertical";
            vertical.transform.SetParent(pivot.transform, false);
            vertical.transform.localScale = new Vector3(0.45f, 1.7f, 0.15f);
            vertical.GetComponent<Renderer>().sharedMaterial = crossMat;
            Object.DestroyImmediate(vertical.GetComponent<Collider>());

            var horizontal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            horizontal.name = "Horizontal";
            horizontal.transform.SetParent(pivot.transform, false);
            horizontal.transform.localScale = new Vector3(1.7f, 0.45f, 0.15f);
            horizontal.GetComponent<Renderer>().sharedMaterial = crossMat;
            Object.DestroyImmediate(horizontal.GetComponent<Collider>());

            var mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Mast";
            mast.transform.SetParent(buildingRoot, false);
            mast.transform.localPosition = new Vector3(0f, worldHeight + 0.55f, 0f);
            mast.transform.localScale = new Vector3(0.08f, 0.55f, 0.08f);
            mast.GetComponent<Renderer>().sharedMaterial = crossMat;
            Object.DestroyImmediate(mast.GetComponent<Collider>());
        }

        // -----------------------------------------------------------------
        // Vegetation
        // -----------------------------------------------------------------
        public static GameObject BuildTree(Transform parent, Vector3 pos, System.Func<float, float, float> sampleHeight = null)
        {
            string variant = TreeVariants[Random.Range(0, TreeVariants.Length)];
            string path = $"{ForestDir}/{variant}.fbx";
            var root = new GameObject("Tree");
            root.transform.SetParent(parent);
            root.transform.position = pos;
            root.transform.Rotate(Vector3.up, Random.Range(0f, 360f));
            root.transform.localScale = Vector3.one * Random.Range(0.85f, 1.25f);

            var instance = InstantiateStripped(path, root.transform);
            float groundY = sampleHeight != null ? sampleHeight(pos.x, pos.z) : pos.y;
            SnapToGround(instance, groundY);

            // Trunk-sized collision (not the full canopy, so the player
            // collides with the trunk the way they would in reality rather
            // than an invisible wall at branch height) - measured from this
            // tree's actual combined renderer bounds, not a guessed radius.
            var renderers = instance != null ? instance.GetComponentsInChildren<Renderer>() : System.Array.Empty<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combined = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) combined.Encapsulate(renderers[i].bounds);
                float uniformScale = Mathf.Max(0.0001f, root.transform.lossyScale.y);
                float worldRadius = Mathf.Max(0.22f, Mathf.Min(combined.extents.x, combined.extents.z) * 0.4f);
                float worldHeight = Mathf.Max(1.4f, combined.size.y * 0.6f);
                var col = root.AddComponent<CapsuleCollider>();
                col.direction = 1; // Y axis
                col.radius = worldRadius / uniformScale;
                col.height = worldHeight / uniformScale;
                col.center = new Vector3(0f, col.height * 0.5f, 0f);
            }
            return root;
        }

        public static void BuildRockCluster(Transform parent, Vector3 pos, System.Func<float, float, float> sampleHeight = null)
        {
            var root = new GameObject("RockCluster");
            root.transform.SetParent(parent);
            root.transform.position = pos;
            root.transform.Rotate(Vector3.up, Random.Range(0f, 360f));

            int count = Random.Range(2, 4);
            for (int i = 0; i < count; i++)
            {
                string variant = RockVariants[Random.Range(0, RockVariants.Length)];
                string path = $"{ForestDir}/{variant}.fbx";
                var pivot = new GameObject($"Rock_{i}");
                pivot.transform.SetParent(root.transform);
                Vector2 jitter = Random.insideUnitCircle * 0.6f;
                pivot.transform.localPosition = new Vector3(jitter.x, 0f, jitter.y);
                pivot.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                pivot.transform.localScale = Vector3.one * Random.Range(0.6f, 1.3f);
                var instance = InstantiateStripped(path, pivot.transform);
                // Sample terrain height at THIS rock's actual jittered world
                // position rather than reusing the whole cluster's single
                // center-point height - on a slope, a rock offset by up to
                // 0.6m from the cluster center can otherwise float or sink
                // relative to the real ground right under it.
                float groundY = sampleHeight != null ? sampleHeight(pivot.transform.position.x, pivot.transform.position.z) : pivot.transform.position.y;
                SnapToGround(instance, groundY);

                // Sensible (not tiny-decorative) collision - a rock cluster
                // is large enough to be a real obstacle. SphereCollider
                // keeps this correct regardless of the pivot's random yaw
                // (a rotation-sensitive box would need extra work to stay
                // footprint-accurate under an arbitrary yaw).
                if (instance != null)
                {
                    var renderers = instance.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds combined = renderers[0].bounds;
                        for (int r = 1; r < renderers.Length; r++) combined.Encapsulate(renderers[r].bounds);
                        float uniformScale = Mathf.Max(0.0001f, pivot.transform.lossyScale.x);
                        float worldRadius = Mathf.Max(combined.extents.x, combined.extents.z);
                        var col = pivot.gameObject.AddComponent<SphereCollider>();
                        col.center = pivot.transform.InverseTransformPoint(combined.center);
                        col.radius = worldRadius / uniformScale;
                    }
                }
            }
        }

        public static void BuildShrubCluster(Transform parent, Vector3 pos)
        {
            var root = new GameObject("Shrub");
            root.transform.SetParent(parent);
            root.transform.position = pos;
            root.transform.Rotate(Vector3.up, Random.Range(0f, 360f));

            int count = Random.Range(1, 3);
            for (int i = 0; i < count; i++)
            {
                string variant = BushVariants[Random.Range(0, BushVariants.Length)];
                string path = $"{ForestDir}/{variant}.fbx";
                var pivot = new GameObject($"Bush_{i}");
                pivot.transform.SetParent(root.transform);
                Vector2 jitter = Random.insideUnitCircle * 0.35f;
                pivot.transform.localPosition = new Vector3(jitter.x, 0f, jitter.y);
                pivot.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                pivot.transform.localScale = Vector3.one * Random.Range(2.2f, 3.6f);
                var instance = InstantiateStripped(path, pivot.transform);
                SnapToGround(instance, pivot.transform.position.y);
            }
        }
    }
}
