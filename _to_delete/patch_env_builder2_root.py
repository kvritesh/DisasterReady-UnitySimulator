import sys, io

path = r"EnvironmentBuilder.cs"
with io.open(path, "r", encoding="utf-8") as f:
    src = f.read()

replacements = []

# 1) Add SampleHeightAtWorld + GroundBuildingToTerrain helpers right after SlopeAt.
replacements.append((
'''        private static float SlopeAt(SceneBuilder.TerrainBuildResult t, float nx, float nz)
        {
            Vector3 n = t.Data.GetInterpolatedNormal(nx, nz);
            return Vector3.Angle(n, Vector3.up);
        }
''',
'''        private static float SlopeAt(SceneBuilder.TerrainBuildResult t, float nx, float nz)
        {
            Vector3 n = t.Data.GetInterpolatedNormal(nx, nz);
            return Vector3.Angle(n, Vector3.up);
        }

        /// <summary>Interpolated terrain height at an arbitrary WORLD-space X/Z (inverse of WorldFromNormalized's X/Z mapping), clamped to the terrain extents.</summary>
        private static float SampleHeightAtWorld(SceneBuilder.TerrainBuildResult t, float worldX, float worldZ)
        {
            float nx = Mathf.Clamp01((worldX - t.Origin.x) / t.Size.x);
            float nz = Mathf.Clamp01((worldZ - t.Origin.z) / t.Size.z);
            return t.Data.GetInterpolatedHeight(nx, nz);
        }

        /// <summary>
        /// Re-grounds a building on sloped/hillside terrain using its ACTUAL
        /// footprint and current rotation, not just the single center-point
        /// height it was originally placed at. Samples terrain height at the
        /// four rotated footprint corners plus center and uses the highest
        /// sample, so the building's base never clips into the uphill side
        /// of the slope (the tradeoff - a small, far less objectionable gap
        /// on the downhill side - reads much better than intersecting the
        /// ground). Must be called after the building's final rotation is
        /// set, since the footprint corners are computed in its own rotated
        /// local space.
        /// </summary>
        private static void GroundBuildingToTerrain(GameObject buildingRoot, SceneBuilder.TerrainBuildResult terrain, string letter, float scale)
        {
            Vector2 footprint = KayKitIntegration.GetBuildingFootprint(letter);
            float halfW = footprint.x * 0.5f * scale;
            float halfD = footprint.y * 0.5f * scale;
            Vector3 pos = buildingRoot.transform.position;
            Vector3 right = buildingRoot.transform.right;
            Vector3 fwd = buildingRoot.transform.forward;

            float maxY = float.NegativeInfinity;
            Vector2[] corners = { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f), Vector2.zero };
            foreach (var c in corners)
            {
                Vector3 sampleWorld = pos + right * (c.x * halfW) + fwd * (c.y * halfD);
                float h = SampleHeightAtWorld(terrain, sampleWorld.x, sampleWorld.z);
                if (h > maxY) maxY = h;
            }
            buildingRoot.transform.position = new Vector3(pos.x, maxY, pos.z);
        }
'''
))

# 2) Generic settlement buildings: capture the per-instance scale, ground after rotation.
replacements.append((
'''                Vector3 pos = WorldFromNormalized(terrain, spot.x, spot.y);
                var b = KayKitIntegration.BuildBuilding(buildingsRoot.transform, pos,
                    letter: genericBuildingLetters[colorIdx % genericBuildingLetters.Length],
                    scale: Random.Range(2.2f, 2.8f),
                    name: $"Building_{colorIdx + 1}");
                if (b == null) { colorIdx++; continue; }

                // Face the nearest point on this building's own cluster spine
                // (front door toward the street), with a modest random jitter
                // so the row doesn't look mechanically perfect.
                bool west = colorIdx < 6;
                Vector2 nearest = NearestPointOnSegment(spot, west ? westSpineA : eastSpineA, west ? westSpineB : eastSpineB);
                Vector3 towardRoad = WorldFromNormalized(terrain, nearest.x, nearest.y) - pos;
                towardRoad.y = 0f;
                if (towardRoad.sqrMagnitude > 0.0001f)
                {
                    b.transform.rotation = Quaternion.LookRotation(towardRoad.normalized);
                    b.transform.Rotate(Vector3.up, Random.Range(-30f, 30f));
                }
                else
                {
                    b.transform.Rotate(Vector3.up, Random.Range(0f, 360f));
                }
                result.Buildings.Add(b);
                colorIdx++;
            }''',
'''                Vector3 pos = WorldFromNormalized(terrain, spot.x, spot.y);
                string buildingLetter = genericBuildingLetters[colorIdx % genericBuildingLetters.Length];
                float buildingScale = Random.Range(2.2f, 2.8f);
                var b = KayKitIntegration.BuildBuilding(buildingsRoot.transform, pos,
                    letter: buildingLetter,
                    scale: buildingScale,
                    name: $"Building_{colorIdx + 1}");
                if (b == null) { colorIdx++; continue; }

                // Face the nearest point on this building's own cluster spine
                // (front door toward the street), with a modest random jitter
                // so the row doesn't look mechanically perfect.
                bool west = colorIdx < 6;
                Vector2 nearest = NearestPointOnSegment(spot, west ? westSpineA : eastSpineA, west ? westSpineB : eastSpineB);
                Vector3 towardRoad = WorldFromNormalized(terrain, nearest.x, nearest.y) - pos;
                towardRoad.y = 0f;
                if (towardRoad.sqrMagnitude > 0.0001f)
                {
                    b.transform.rotation = Quaternion.LookRotation(towardRoad.normalized);
                    b.transform.Rotate(Vector3.up, Random.Range(-30f, 30f));
                }
                else
                {
                    b.transform.Rotate(Vector3.up, Random.Range(0f, 360f));
                }
                // Hillside grounding: re-sample terrain height at this
                // building's actual rotated footprint (not just the single
                // center point it was placed at) so it neither floats above
                // nor sinks into a sloped settlement spot.
                GroundBuildingToTerrain(b, terrain, buildingLetter, buildingScale);
                result.Buildings.Add(b);
                colorIdx++;
            }'''
))

# 3) Hospital grounding.
replacements.append((
'''            var hospital = KayKitIntegration.BuildBuilding(envRoot.transform, hospitalPos, letter: "H", scale: hospitalScale, name: "Hospital");
            KayKitIntegration.AddRoofCross(hospital.transform, 3.05f * hospitalScale, hospitalCrossMat);''',
'''            var hospital = KayKitIntegration.BuildBuilding(envRoot.transform, hospitalPos, letter: "H", scale: hospitalScale, name: "Hospital");
            GroundBuildingToTerrain(hospital, terrain, "H", hospitalScale);
            KayKitIntegration.AddRoofCross(hospital.transform, 3.05f * hospitalScale, hospitalCrossMat);'''
))

# 4) Shelter grounding.
replacements.append((
'''            var shelter = KayKitIntegration.BuildBuilding(envRoot.transform, shelterPos, letter: "G", scale: shelterScale, name: "EmergencyShelter");
            AddFlagpole(shelter.transform, new Vector3(0f, 2.98f * shelterScale, 0f), shelterFlagMat, poleName: "Shelter");''',
'''            var shelter = KayKitIntegration.BuildBuilding(envRoot.transform, shelterPos, letter: "G", scale: shelterScale, name: "EmergencyShelter");
            GroundBuildingToTerrain(shelter, terrain, "G", shelterScale);
            AddFlagpole(shelter.transform, new Vector3(0f, 2.98f * shelterScale, 0f), shelterFlagMat, poleName: "Shelter");'''
))

# 5) Utility poles: currently strung directly along the exact same
# playerStart->hospital / playerStart->shelter lines BuildPath uses for the
# actual walkable road, so a pole ends up dead-center on the path the player
# walks. Offset the whole line sideways off the road centerline.
replacements.append((
'''            var poleMat = SharedAssetUtility.CreateColorMaterial("UtilityPole", new Color(0.32f, 0.28f, 0.24f));
            var wireMat = SharedAssetUtility.CreateColorMaterial("UtilityWire", new Color(0.15f, 0.14f, 0.13f), smoothness: 0.3f);
            BuildUtilityLine(envRoot.transform, terrain, playerStart01, hospital01, poleMat, wireMat);
            BuildUtilityLine(envRoot.transform, terrain, playerStart01, shelter01, poleMat, wireMat);''',
'''            var poleMat = SharedAssetUtility.CreateColorMaterial("UtilityPole", new Color(0.32f, 0.28f, 0.24f));
            var wireMat = SharedAssetUtility.CreateColorMaterial("UtilityWire", new Color(0.15f, 0.14f, 0.13f), smoothness: 0.3f);
            // Offset well clear of the road centerline (BuildPath uses these
            // same from/to points for the actual walkable path) so poles run
            // alongside the road instead of standing in the middle of it.
            Vector2 hospitalLineOffset = Perp(hospital01 - playerStart01) * 0.035f;
            BuildUtilityLine(envRoot.transform, terrain, playerStart01 + hospitalLineOffset, hospital01 + hospitalLineOffset, poleMat, wireMat);
            Vector2 shelterLineOffset = Perp(shelter01 - playerStart01) * 0.035f;
            BuildUtilityLine(envRoot.transform, terrain, playerStart01 + shelterLineOffset, shelter01 + shelterLineOffset, poleMat, wireMat);'''
))

# 6) Utility poles were purely decorative (collider stripped), which is why
# the one that ended up on the road could be walked straight through even
# before considering placement. Now that they're off the road but still near
# it, keep the pole's own default CapsuleCollider (from CreatePrimitive) so
# the player can't walk through one if they wander close - crossbar/wire stay
# non-colliding (well above head height, purely decorative).
replacements.append((
'''                pole.GetComponent<Renderer>().sharedMaterial = poleMat;
                SafeDestroyCollider(pole);

                var crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);''',
'''                pole.GetComponent<Renderer>().sharedMaterial = poleMat;
                // Collider intentionally kept (default CapsuleCollider from
                // CreatePrimitive) so the player can't walk straight through
                // a pole - see the comment above the BuildUtilityLine call
                // sites about why this changed from purely decorative.

                var crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);'''
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

print("Patched EnvironmentBuilder.cs successfully (pass 2).")
