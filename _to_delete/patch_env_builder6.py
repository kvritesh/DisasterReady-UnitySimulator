import sys, io

path = r"Assets/Scripts/Editor/EnvironmentBuilder.cs"
with io.open(path, "r", encoding="utf-8") as f:
    src = f.read()

replacements = []

# A fence run parallel to its own spine will always cross any OTHER
# corridor segment that isn't parallel to it at some point along its
# infinite line - offsetting further out just slides where that crossing
# falls, it never removes it if the crossing point still lands within both
# finite segments' extents. That's exactly what kept a couple of posts
# CRITICAL near each spine's own endpoints even after many retries: that's
# where the OTHER road (the one arriving at this settlement corner) is close
# enough to the spine to cross the offset fence line. The real fix is to
# trim the run back from both ends before offsetting, so it doesn't reach
# into the road-junction zone at all - which also reads better anyway (a
# settlement fence shouldn't wall off the road entrance it brackets).
replacements.append((
'''        private static GameObject BuildFenceRunClearOfCorridor(Transform parent, SceneBuilder.TerrainBuildResult terrain, Vector2 spineA, Vector2 spineB, List<(Vector3 a, Vector3 b)> corridorWorld, float requiredWorldRadius)
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
        }''',
'''        private static GameObject BuildFenceRunClearOfCorridor(Transform parent, SceneBuilder.TerrainBuildResult terrain, Vector2 spineA, Vector2 spineB, List<(Vector3 a, Vector3 b)> corridorWorld, float requiredWorldRadius)
        {
            // Trim both ends back from the spine's own endpoints first: those
            // endpoints are exactly where the settlement's OTHER connecting
            // road arrives, so a fence running flush to them will always
            // cross that other road somewhere along an offset copy of the
            // spine, no matter how far the offset - trimming leaves a
            // deliberate opening at each road junction instead (also just
            // reads better than a fence walling off the entrance it flanks).
            Vector2 trimmedA = Vector2.Lerp(spineA, spineB, 0.12f);
            Vector2 trimmedB = Vector2.Lerp(spineA, spineB, 0.88f);

            Vector2 perp = Perp(spineB - spineA);
            float offsetMag = 0.03f;
            GameObject fence = null;

            for (int pass = 0; pass < 12; pass++)
            {
                if (fence != null) Object.DestroyImmediate(fence);

                Vector2 a01 = trimmedA + perp * offsetMag;
                Vector2 b01 = trimmedB + perp * offsetMag;
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

print("Patched EnvironmentBuilder.cs successfully (pass 6 - trim fence ends off road junctions).")
