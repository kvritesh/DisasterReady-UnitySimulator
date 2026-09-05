import sys, io

path = r"Assets/Scripts/Editor/EnvironmentBuilder.cs"
with io.open(path, "r", encoding="utf-8") as f:
    src = f.read()

replacements = []

# 1) BuildFenceRun needs to hand its finished GameObject back so a caller can
#    measure the ACTUAL posts it produced (not a hand-derived approximation
#    of where they'll land) and rebuild at a larger offset if any post is
#    still too close to the route.
replacements.append((
'''        private static void BuildFenceRun(Transform parent, Vector3 from, Vector3 to)
        {
            var woodMat = SharedAssetUtility.CreateColorMaterial("FenceWood", new Color(0.42f, 0.32f, 0.22f));
            var root = new GameObject("Fence");
            root.transform.SetParent(parent);
            root.transform.position = from;

            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f) return;''',
'''        private static GameObject BuildFenceRun(Transform parent, Vector3 from, Vector3 to)
        {
            var woodMat = SharedAssetUtility.CreateColorMaterial("FenceWood", new Color(0.42f, 0.32f, 0.22f));
            var root = new GameObject("Fence");
            root.transform.SetParent(parent);
            root.transform.position = from;

            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f) return root;'''
))
replacements.append((
'''            // Rail's collider is deliberately thin (matches its visual size)
            // but spans the full run length, which is enough for the
            // CharacterController's capsule sweep to catch it - kept for the
            // same reason as the posts above.
        }''',
'''            // Rail's collider is deliberately thin (matches its visual size)
            // but spans the full run length, which is enough for the
            // CharacterController's capsule sweep to catch it - kept for the
            // same reason as the posts above.
            return root;
        }'''
))

# 2) Replace the hand-derived offset search (FenceOffsetClearOfCorridor) with
#    a build-measure-retry loop that checks the run's ACTUAL generated Post_N
#    world positions against the corridor, and rebuilds at a larger offset if
#    any post is still too close. This is strictly more reliable than
#    predicting post positions with a parallel formula, since it can never
#    drift out of sync with what BuildFenceRun (or the terrain's height
#    field under each post) actually produces.
replacements.append((
'''        /// <summary>
        /// Fence-specific clearance helper: a fence run is offset parallel to
        /// its own settlement spine (which is itself part of the corridor),
        /// so the interesting failure case isn't the spine it runs beside but
        /// a DIFFERENT corridor segment (e.g. the main spawn/hospital/shelter
        /// road) passing close to it. Walks the offset outward, in the same
        /// direction the caller already offsets in, until every sample point
        /// along the run clears every corridor segment by the required
        /// radius.
        /// </summary>
        private static Vector2 FenceOffsetClearOfCorridor(SceneBuilder.TerrainBuildResult terrain, Vector2 spineA, Vector2 spineB, List<(Vector3 a, Vector3 b)> corridorWorld, float requiredWorldRadius)
        {
            Vector2 perp = Perp(spineB - spineA);
            float offsetMag = 0.03f;
            const int sampleSteps = 20;
            for (int pass = 0; pass < 16; pass++)
            {
                float minClear = float.PositiveInfinity;
                for (int i = 0; i <= sampleSteps; i++)
                {
                    float t = (float)i / sampleSteps;
                    Vector2 p01 = Vector2.Lerp(spineA, spineB, t) + perp * offsetMag;
                    Vector3 pWorld = WorldFromNormalized(terrain, p01.x, p01.y);
                    float d = MinDistanceToCorridorWorldXZ(corridorWorld, pWorld);
                    if (d < minClear) minClear = d;
                }
                if (minClear >= requiredWorldRadius) break;
                offsetMag += 0.006f;
            }
            return perp * offsetMag;
        }''',
'''        /// <summary>
        /// Fence-specific clearance helper: a fence run is offset parallel to
        /// its own settlement spine (which is itself part of the corridor),
        /// so the interesting failure case isn't the spine it runs beside but
        /// a DIFFERENT corridor segment (e.g. the main spawn/hospital/shelter
        /// road) passing close to it. Builds the fence for real at a
        /// candidate offset, measures every ACTUAL Post_N world position it
        /// produced against the corridor (not a hand-derived approximation
        /// of where they'd land), and - if any post is still too close -
        /// destroys that attempt and rebuilds further out. This can never
        /// drift out of sync with what BuildFenceRun/terrain height actually
        /// produce, unlike predicting post positions with a parallel formula.
        /// </summary>
        private static GameObject BuildFenceRunClearOfCorridor(Transform parent, SceneBuilder.TerrainBuildResult terrain, Vector2 spineA, Vector2 spineB, List<(Vector3 a, Vector3 b)> corridorWorld, float requiredWorldRadius)
        {
            Vector2 perp = Perp(spineB - spineA);
            float offsetMag = 0.03f;
            GameObject fence = null;

            for (int pass = 0; pass < 12; pass++)
            {
                if (fence != null) Object.DestroyImmediate(fence);

                Vector2 a01 = spineA + perp * offsetMag;
                Vector2 b01 = spineB + perp * offsetMag;
                fence = BuildFenceRun(parent, WorldFromNormalized(terrain, a01.x, a01.y), WorldFromNormalized(terrain, b01.x, b01.y));
                if (fence == null) return null;

                float worstClearance = float.PositiveInfinity;
                foreach (Transform child in fence.transform)
                {
                    if (!child.name.StartsWith("Post_")) continue;
                    float d = MinDistanceToCorridorWorldXZ(corridorWorld, child.position);
                    if (d < worstClearance) worstClearance = d;
                }
                if (worstClearance >= requiredWorldRadius) break;
                offsetMag += 0.006f;
            }
            return fence;
        }'''
))

# 3) Call sites: build each fence directly clear of the corridor instead of
#    computing an offset separately and building afterward.
replacements.append((
'''            Vector2 westPerp = FenceOffsetClearOfCorridor(terrain, westSpineA, westSpineB, corridorWorld, RouteHalfWidth + RouteTargetClearance + 0.5f);
            BuildFenceRun(envRoot.transform, WorldFromNormalized(terrain, westSpineA.x + westPerp.x, westSpineA.y + westPerp.y), WorldFromNormalized(terrain, westSpineB.x + westPerp.x, westSpineB.y + westPerp.y));
            Vector2 eastPerp = FenceOffsetClearOfCorridor(terrain, eastSpineA, eastSpineB, corridorWorld, RouteHalfWidth + RouteTargetClearance + 0.5f);
            BuildFenceRun(envRoot.transform, WorldFromNormalized(terrain, eastSpineA.x + eastPerp.x, eastSpineA.y + eastPerp.y), WorldFromNormalized(terrain, eastSpineB.x + eastPerp.x, eastSpineB.y + eastPerp.y));''',
'''            BuildFenceRunClearOfCorridor(envRoot.transform, terrain, westSpineA, westSpineB, corridorWorld, RouteHalfWidth + RouteTargetClearance + 0.5f);
            BuildFenceRunClearOfCorridor(envRoot.transform, terrain, eastSpineA, eastSpineB, corridorWorld, RouteHalfWidth + RouteTargetClearance + 0.5f);'''
))

errors = []
for i, (old, new) in enumerate(replacements):
    count = src.count(old)
    if count != 1:
        errors.append(f"replacement #{i} matched {count} times (expected 1)")
        continue
    src = src.replace(old, new, 1)

if errors:
    sys.stderr.write("PATCH FAILED:\n" + "\n".join(errors) + "\n")
    sys.exit(1)

with io.open(path, "w", encoding="utf-8") as f:
    f.write(src)

print("Patched EnvironmentBuilder.cs successfully (pass 5 - fence build-measure-retry).")
