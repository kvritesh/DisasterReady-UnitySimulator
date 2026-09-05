import sys, io

path = r"Assets/Scripts/Editor/EnvironmentBuilder.cs"
with io.open(path, "r", encoding="utf-8") as f:
    src = f.read()

replacements = []

# 1) EnsureClearOfCorridor: the single-nearest-segment push oscillates near a
#    junction where two corridor segments meet (exactly the Hospital/Shelter
#    case - their own path arrives AT their placement point, so two segments
#    converge right where the building starts). Replace with a summed
#    escape vector across every segment currently violating clearance, which
#    naturally pushes along the wedge bisector at a junction instead of
#    zig-zagging between the two segments. Also more passes, and the
#    Hospital/Shelter cap is raised slightly (still comfortably under their
#    7m MissionZoneTrigger radius).
replacements.append((
'''            float footprintRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float requiredClearance = footprintRadius + RouteHalfWidth + RouteTargetClearance;
            float totalPushed = 0f;

            for (int pass = 0; pass < 6; pass++)
            {
                Vector3 center = buildingRoot.transform.position;
                float minDist = float.PositiveInfinity;
                Vector3 nearestA = Vector3.zero, nearestB = Vector3.zero;
                foreach (var (a, b) in corridorWorld)
                {
                    float d = DistancePointToSegmentXZ(center, a, b);
                    if (d < minDist) { minDist = d; nearestA = a; nearestB = b; }
                }
                if (minDist >= requiredClearance) break;
                if (totalPushed >= maxTotalPush) break;

                Vector2 c2 = new Vector2(center.x, center.z);
                Vector2 a2 = new Vector2(nearestA.x, nearestA.z);
                Vector2 b2 = new Vector2(nearestB.x, nearestB.z);
                Vector2 ab = b2 - a2;
                float len2 = ab.sqrMagnitude;
                float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(c2 - a2, ab) / len2);
                Vector2 proj = a2 + ab * t;
                Vector2 away = c2 - proj;
                if (away.sqrMagnitude < 0.0001f)
                {
                    away = Perp(ab);
                    if (away.sqrMagnitude < 0.0001f) away = Vector2.right;
                }
                away.Normalize();

                float pushDist = Mathf.Min((requiredClearance - minDist) + 0.5f, maxTotalPush - totalPushed);
                if (pushDist <= 0.0001f) break;
                buildingRoot.transform.position += new Vector3(away.x, 0f, away.y) * pushDist;
                totalPushed += pushDist;
            }''',
'''            float footprintRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float requiredClearance = footprintRadius + RouteHalfWidth + RouteTargetClearance;
            float totalPushed = 0f;

            for (int pass = 0; pass < 10; pass++)
            {
                Vector3 center = buildingRoot.transform.position;
                Vector2 c2 = new Vector2(center.x, center.z);
                Vector2 accum = Vector2.zero;
                float worstDeficit = 0f;
                bool anyViolation = false;
                // Sum an escape vector from EVERY corridor segment currently
                // too close, not just the single nearest one - a building
                // whose own path arrives right at its placement point (true
                // of both Hospital and Shelter) sits at a junction of two
                // segments, and pushing away from only one at a time just
                // oscillates between them instead of escaping the wedge.
                foreach (var (a, b) in corridorWorld)
                {
                    Vector2 a2 = new Vector2(a.x, a.z);
                    Vector2 b2 = new Vector2(b.x, b.z);
                    Vector2 ab = b2 - a2;
                    float len2 = ab.sqrMagnitude;
                    float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(c2 - a2, ab) / len2);
                    Vector2 proj = a2 + ab * t;
                    float dist = Vector2.Distance(c2, proj);
                    if (dist >= requiredClearance) continue;

                    anyViolation = true;
                    float deficit = requiredClearance - dist;
                    if (deficit > worstDeficit) worstDeficit = deficit;
                    Vector2 away = c2 - proj;
                    if (away.sqrMagnitude < 0.0001f)
                    {
                        away = Perp(ab);
                        if (away.sqrMagnitude < 0.0001f) away = Vector2.right;
                    }
                    away.Normalize();
                    accum += away * deficit;
                }
                if (!anyViolation) break;
                if (totalPushed >= maxTotalPush) break;
                if (accum.sqrMagnitude < 0.0001f) break;
                accum.Normalize();

                float pushDist = Mathf.Min(worstDeficit * 0.6f + 0.3f, maxTotalPush - totalPushed);
                if (pushDist <= 0.0001f) break;
                buildingRoot.transform.position += new Vector3(accum.x, 0f, accum.y) * pushDist;
                totalPushed += pushDist;
            }'''
))

# 2) Hospital/Shelter push cap: 6m wasn't always enough to escape the
#    junction wedge where their own path arrives - 6.5m keeps a full 0.5m of
#    slack under their 7m MissionZoneTrigger radius while giving the smarter
#    multi-segment push above room to actually converge.
replacements.append((
'''            EnsureClearOfCorridor(hospital, terrain, corridorWorld, "H", hospitalScale, maxTotalPush: 6f);''',
'''            EnsureClearOfCorridor(hospital, terrain, corridorWorld, "H", hospitalScale, maxTotalPush: 6.5f);'''
))
replacements.append((
'''            EnsureClearOfCorridor(shelter, terrain, corridorWorld, "G", shelterScale, maxTotalPush: 6f);''',
'''            EnsureClearOfCorridor(shelter, terrain, corridorWorld, "G", shelterScale, maxTotalPush: 6.5f);'''
))

# 3) FenceOffsetClearOfCorridor: 3 samples (both endpoints + midpoint) missed
#    intermediate points where an offset fence line - which isn't always
#    parallel to every corridor segment - dips back within range of a
#    DIFFERENT road than the one it's deliberately paralleling. Sample
#    densely along the whole run instead (roughly matching how many actual
#    posts BuildFenceRun will place along it), and search further/longer.
replacements.append((
'''            Vector2 perp = Perp(spineB - spineA);
            float offsetMag = 0.03f;
            for (int pass = 0; pass < 10; pass++)
            {
                Vector2 a01 = spineA + perp * offsetMag;
                Vector2 b01 = spineB + perp * offsetMag;
                Vector2 mid01 = Vector2.Lerp(a01, b01, 0.5f);
                Vector3 aWorld = WorldFromNormalized(terrain, a01.x, a01.y);
                Vector3 bWorld = WorldFromNormalized(terrain, b01.x, b01.y);
                Vector3 midWorld = WorldFromNormalized(terrain, mid01.x, mid01.y);
                float minClear = Mathf.Min(MinDistanceToCorridorWorldXZ(corridorWorld, aWorld),
                    Mathf.Min(MinDistanceToCorridorWorldXZ(corridorWorld, bWorld), MinDistanceToCorridorWorldXZ(corridorWorld, midWorld)));
                if (minClear >= requiredWorldRadius) break;
                offsetMag += 0.008f;
            }
            return perp * offsetMag;''',
'''            Vector2 perp = Perp(spineB - spineA);
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
            return perp * offsetMag;'''
))

# 4) Shrubs (ground-cover pass): the corridor check only tested the cluster's
#    CENTER point, but KayKitLayoutDiagnostic (correctly) measures against
#    the cluster's actual rendered footprint, which extends past that center
#    point - a center 1.3m from the corridor with a ~1.8m cluster radius
#    still physically reaches into it. Widen the required clearance to cover
#    a generous cluster radius too.
replacements.append((
'''                if (MinDistanceToCorridorWorldXZ(corridorWorld, pos) < RouteHalfWidth + 0.5f) continue;''',
'''                if (MinDistanceToCorridorWorldXZ(corridorWorld, pos) < RouteHalfWidth + 2.6f) continue;'''
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

print("Patched EnvironmentBuilder.cs successfully (pass 4 - convergence fixes).")
