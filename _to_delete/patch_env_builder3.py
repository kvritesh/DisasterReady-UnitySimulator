import sys, io

path = r"Assets/Scripts/Editor/EnvironmentBuilder.cs"
with io.open(path, "r", encoding="utf-8") as f:
    src = f.read()

replacements = []

# 1) Insert corridor-clearance helper methods right after GroundBuildingToTerrain,
#    before NearestPointOnSegment. These are the actual "does this obstacle
#    physically sit in the player's walking route" primitives - same math the
#    read-only KayKitLayoutDiagnostic tool uses to verify the scene, so a
#    building/fence/tree that passes these checks at generation time will also
#    pass the diagnostic's independent re-check afterward.
replacements.append((
'''            buildingRoot.transform.position = new Vector3(pos.x, maxY, pos.z);
        }

        /// <summary>Closest point to <paramref name="p"/> on segment a-b, in the same normalized terrain space.</summary>
        private static Vector2 NearestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)''',
'''            buildingRoot.transform.position = new Vector3(pos.x, maxY, pos.z);
        }

        // Route-corridor clearance constants - kept numerically identical to
        // KayKitLayoutDiagnostic's RouteHalfWidth/TargetClearance so anything
        // that satisfies these checks at generation time also reads as
        // "clear" (not just "not CRITICAL") when independently re-verified.
        private const float RouteHalfWidth = 0.8f;
        private const float RouteTargetClearance = 2.0f;

        /// <summary>Point-to-segment distance in the XZ plane (world space, Y ignored).</summary>
        private static float DistancePointToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 p2 = new Vector2(p.x, p.z);
            Vector2 a2 = new Vector2(a.x, a.z);
            Vector2 b2 = new Vector2(b.x, b.z);
            Vector2 ab = b2 - a2;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f) return Vector2.Distance(p2, a2);
            float t = Mathf.Clamp01(Vector2.Dot(p2 - a2, ab) / len2);
            Vector2 proj = a2 + ab * t;
            return Vector2.Distance(p2, proj);
        }

        /// <summary>Closest distance (world space, XZ) from a world point to any segment of the route corridor.</summary>
        private static float MinDistanceToCorridorWorldXZ(List<(Vector3 a, Vector3 b)> corridorWorld, Vector3 worldPoint)
        {
            float min = float.PositiveInfinity;
            foreach (var (a, b) in corridorWorld)
            {
                float d = DistancePointToSegmentXZ(worldPoint, a, b);
                if (d < min) min = d;
            }
            return min;
        }

        /// <summary>
        /// Section-4 building placement rule, implemented directly: measures
        /// a building's ACTUAL collider footprint (falling back to renderer
        /// bounds if it has no collider) against the real route corridor and,
        /// if it overlaps or sits inside the required clearance, pushes the
        /// building directly away from the nearest corridor segment until
        /// clear - then re-grounds it, since the push only changes X/Z. A
        /// building already clear is left untouched. maxTotalPush optionally
        /// caps how far a building may move (used for Hospital/Shelter so
        /// they stay within their own 7m MissionZoneTrigger's reach of the
        /// path they were pushed off of).
        /// </summary>
        private static void EnsureClearOfCorridor(GameObject buildingRoot, SceneBuilder.TerrainBuildResult terrain, List<(Vector3 a, Vector3 b)> corridorWorld, string letter, float scale, float maxTotalPush = float.PositiveInfinity)
        {
            var colliders = buildingRoot.GetComponentsInChildren<Collider>();
            Bounds bounds;
            if (colliders.Length > 0)
            {
                bounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++) bounds.Encapsulate(colliders[i].bounds);
            }
            else
            {
                var renderers = buildingRoot.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            }

            float footprintRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
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
            }

            // The push (if any) only touched X/Z - re-ground so the building
            // still sits flush with the terrain at its final position.
            GroundBuildingToTerrain(buildingRoot, terrain, letter, scale);
        }

        /// <summary>
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
            return perp * offsetMag;
        }

        /// <summary>Closest point to <paramref name="p"/> on segment a-b, in the same normalized terrain space.</summary>
        private static Vector2 NearestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)'''
))

# 2) Build the actual world-space route corridor right where westSpineA/B and
#    eastSpineA/B are already declared, from the exact same point pairs the
#    BuildPath calls further down use to draw the real walkable road/spurs -
#    this is the real "can the player walk here" definition, reconstructed
#    the same way KayKitLayoutDiagnostic does it from the generated scene.
replacements.append((
'''            Vector2 westSpineA = new Vector2(0.47f, 0.17f), westSpineB = new Vector2(0.40f, 0.35f);
            Vector2 eastSpineA = new Vector2(0.54f, 0.15f), eastSpineB = new Vector2(0.59f, 0.31f);

            var buildingsRoot = new GameObject("Buildings");''',
'''            Vector2 westSpineA = new Vector2(0.47f, 0.17f), westSpineB = new Vector2(0.40f, 0.35f);
            Vector2 eastSpineA = new Vector2(0.54f, 0.15f), eastSpineB = new Vector2(0.59f, 0.31f);

            // World-space route corridor: the exact same point pairs the
            // BuildPath calls below draw the actual walkable road/spurs
            // between. Every obstacle placed after this point (buildings,
            // fences, vegetation) is checked against THIS, not against
            // source-coordinate assumptions.
            var corridorWorld = new List<(Vector3 a, Vector3 b)>
            {
                (WorldFromNormalized(terrain, playerStart01.x, playerStart01.y), WorldFromNormalized(terrain, hospital01.x, hospital01.y)),
                (WorldFromNormalized(terrain, playerStart01.x, playerStart01.y), WorldFromNormalized(terrain, shelter01.x, shelter01.y)),
                (WorldFromNormalized(terrain, hospital01.x, hospital01.y), WorldFromNormalized(terrain, shelter01.x, shelter01.y)),
                (WorldFromNormalized(terrain, westSpineA.x, westSpineA.y), WorldFromNormalized(terrain, westSpineB.x, westSpineB.y)),
                (WorldFromNormalized(terrain, eastSpineA.x, eastSpineA.y), WorldFromNormalized(terrain, eastSpineB.x, eastSpineB.y)),
            };

            var buildingsRoot = new GameObject("Buildings");'''
))

# 3) Generic settlement buildings: push clear of the corridor after grounding.
replacements.append((
'''                // Hillside grounding: re-sample terrain height at this
                // building's actual rotated footprint (not just the single
                // center point it was placed at) so it neither floats above
                // nor sinks into a sloped settlement spot.
                GroundBuildingToTerrain(b, terrain, buildingLetter, buildingScale);
                result.Buildings.Add(b);
                colorIdx++;
            }''',
'''                // Hillside grounding: re-sample terrain height at this
                // building's actual rotated footprint (not just the single
                // center point it was placed at) so it neither floats above
                // nor sinks into a sloped settlement spot.
                GroundBuildingToTerrain(b, terrain, buildingLetter, buildingScale);
                // Section-4 fix: a grounded building can still physically
                // occupy the player's walking corridor - push it clear (then
                // re-ground) against the REAL route corridor, not just "off
                // the exact centerline."
                EnsureClearOfCorridor(b, terrain, corridorWorld, buildingLetter, buildingScale);
                result.Buildings.Add(b);
                colorIdx++;
            }'''
))

# 4) Hospital: push clear, capped so its 7m MissionZoneTrigger still reaches
#    the path it was moved off of.
replacements.append((
'''            var hospital = KayKitIntegration.BuildBuilding(envRoot.transform, hospitalPos, letter: "H", scale: hospitalScale, name: "Hospital");
            GroundBuildingToTerrain(hospital, terrain, "H", hospitalScale);
            KayKitIntegration.AddRoofCross(hospital.transform, 3.05f * hospitalScale, hospitalCrossMat);''',
'''            var hospital = KayKitIntegration.BuildBuilding(envRoot.transform, hospitalPos, letter: "H", scale: hospitalScale, name: "Hospital");
            GroundBuildingToTerrain(hospital, terrain, "H", hospitalScale);
            // Capped at 6m (< the 7m MissionZoneTrigger radius added by
            // SetupPoi below, centered on this same building) so the
            // Hospital keeps reading as reachable from the path even after
            // being pushed clear of it.
            EnsureClearOfCorridor(hospital, terrain, corridorWorld, "H", hospitalScale, maxTotalPush: 6f);
            KayKitIntegration.AddRoofCross(hospital.transform, 3.05f * hospitalScale, hospitalCrossMat);'''
))

# 5) Shelter: same treatment.
replacements.append((
'''            var shelter = KayKitIntegration.BuildBuilding(envRoot.transform, shelterPos, letter: "G", scale: shelterScale, name: "EmergencyShelter");
            GroundBuildingToTerrain(shelter, terrain, "G", shelterScale);
            AddFlagpole(shelter.transform, new Vector3(0f, 2.98f * shelterScale, 0f), shelterFlagMat, poleName: "Shelter");''',
'''            var shelter = KayKitIntegration.BuildBuilding(envRoot.transform, shelterPos, letter: "G", scale: shelterScale, name: "EmergencyShelter");
            GroundBuildingToTerrain(shelter, terrain, "G", shelterScale);
            EnsureClearOfCorridor(shelter, terrain, corridorWorld, "G", shelterScale, maxTotalPush: 6f);
            AddFlagpole(shelter.transform, new Vector3(0f, 2.98f * shelterScale, 0f), shelterFlagMat, poleName: "Shelter");'''
))

# 6) Trees: reject any candidate spot too close to the actual corridor.
replacements.append((
'''                float slope = SlopeAt(terrain, nx, nz);
                if (slope > 36f) continue;

                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                // Roughly 1-in-3 broadleaf for variety; the rest stay the
                // original pine-cone silhouette that already reads well
                // against this NER hillside setting.
                GameObject tree = KayKitIntegration.BuildTree(treesRoot.transform, pos);''',
'''                float slope = SlopeAt(terrain, nx, nz);
                if (slope > 36f) continue;

                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                // Keep trees off the actual walking route - a tree close
                // enough to visually/physically block the corridor is worse
                // than a slightly less "full" grove.
                if (MinDistanceToCorridorWorldXZ(corridorWorld, pos) < RouteHalfWidth + 1.6f) continue;
                // Roughly 1-in-3 broadleaf for variety; the rest stay the
                // original pine-cone silhouette that already reads well
                // against this NER hillside setting.
                GameObject tree = KayKitIntegration.BuildTree(treesRoot.transform, pos);'''
))

# 7) Rocks: same idea, smaller clearance since rocks are lower obstacles.
replacements.append((
'''                float slope = SlopeAt(terrain, nx, nz);
                if (slope > 42f) continue;

                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                KayKitIntegration.BuildRockCluster(rocksRoot.transform, pos);
                occupied.Add(candidate);
                rocksPlaced++;''',
'''                float slope = SlopeAt(terrain, nx, nz);
                if (slope > 42f) continue;

                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                if (MinDistanceToCorridorWorldXZ(corridorWorld, pos) < RouteHalfWidth + 1.0f) continue;
                KayKitIntegration.BuildRockCluster(rocksRoot.transform, pos);
                occupied.Add(candidate);
                rocksPlaced++;'''
))

# 8) Shrubs (ground-cover pass): same idea, smallest clearance since these are
#    small, low, and were already placed with a tight avoidance radius.
replacements.append((
'''                float slope = SlopeAt(terrain, nx, nz);
                if (slope > 40f) continue;

                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                KayKitIntegration.BuildShrubCluster(shrubsRoot.transform, pos);
                shrubsPlaced++;''',
'''                float slope = SlopeAt(terrain, nx, nz);
                if (slope > 40f) continue;

                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                if (MinDistanceToCorridorWorldXZ(corridorWorld, pos) < RouteHalfWidth + 0.5f) continue;
                KayKitIntegration.BuildShrubCluster(shrubsRoot.transform, pos);
                shrubsPlaced++;'''
))

# 9) Fences: widen the offset dynamically (was a fixed 0.03) until the whole
#    run actually clears the real corridor, not just its own parallel spine.
replacements.append((
'''            Vector2 westPerp = Perp(westSpineB - westSpineA) * 0.03f;
            BuildFenceRun(envRoot.transform, WorldFromNormalized(terrain, westSpineA.x + westPerp.x, westSpineA.y + westPerp.y), WorldFromNormalized(terrain, westSpineB.x + westPerp.x, westSpineB.y + westPerp.y));
            Vector2 eastPerp = Perp(eastSpineB - eastSpineA) * 0.03f;
            BuildFenceRun(envRoot.transform, WorldFromNormalized(terrain, eastSpineA.x + eastPerp.x, eastSpineA.y + eastPerp.y), WorldFromNormalized(terrain, eastSpineB.x + eastPerp.x, eastSpineB.y + eastPerp.y));''',
'''            Vector2 westPerp = FenceOffsetClearOfCorridor(terrain, westSpineA, westSpineB, corridorWorld, RouteHalfWidth + RouteTargetClearance + 0.5f);
            BuildFenceRun(envRoot.transform, WorldFromNormalized(terrain, westSpineA.x + westPerp.x, westSpineA.y + westPerp.y), WorldFromNormalized(terrain, westSpineB.x + westPerp.x, westSpineB.y + westPerp.y));
            Vector2 eastPerp = FenceOffsetClearOfCorridor(terrain, eastSpineA, eastSpineB, corridorWorld, RouteHalfWidth + RouteTargetClearance + 0.5f);
            BuildFenceRun(envRoot.transform, WorldFromNormalized(terrain, eastSpineA.x + eastPerp.x, eastSpineA.y + eastPerp.y), WorldFromNormalized(terrain, eastSpineB.x + eastPerp.x, eastSpineB.y + eastPerp.y));'''
))

# 10) The two hand-placed spawn-flanking trees: also keep them off the corridor.
replacements.append((
'''            {
                float fnx1 = playerStart01.x - 0.10f, fnz1 = playerStart01.y + 0.155f;
                float fnx2 = playerStart01.x + 0.11f, fnz2 = playerStart01.y + 0.15f;
                if (SlopeAt(terrain, fnx1, fnz1) < 36f)
                    KayKitIntegration.BuildTree(treesRoot.transform, WorldFromNormalized(terrain, fnx1, fnz1));
                if (SlopeAt(terrain, fnx2, fnz2) < 36f)
                    KayKitIntegration.BuildTree(treesRoot.transform, WorldFromNormalized(terrain, fnx2, fnz2));
            }''',
'''            {
                float fnx1 = playerStart01.x - 0.10f, fnz1 = playerStart01.y + 0.155f;
                float fnx2 = playerStart01.x + 0.11f, fnz2 = playerStart01.y + 0.15f;
                Vector3 fpos1 = WorldFromNormalized(terrain, fnx1, fnz1);
                Vector3 fpos2 = WorldFromNormalized(terrain, fnx2, fnz2);
                if (SlopeAt(terrain, fnx1, fnz1) < 36f && MinDistanceToCorridorWorldXZ(corridorWorld, fpos1) >= RouteHalfWidth + 1.6f)
                    KayKitIntegration.BuildTree(treesRoot.transform, fpos1);
                if (SlopeAt(terrain, fnx2, fnz2) < 36f && MinDistanceToCorridorWorldXZ(corridorWorld, fpos2) >= RouteHalfWidth + 1.6f)
                    KayKitIntegration.BuildTree(treesRoot.transform, fpos2);
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

print("Patched EnvironmentBuilder.cs successfully (pass 3 - corridor clearance).")
