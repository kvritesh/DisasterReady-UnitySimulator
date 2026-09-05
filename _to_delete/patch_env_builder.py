import sys, io

path = r"EnvironmentBuilder.cs"
with io.open(path, "r", encoding="utf-8") as f:
    src = f.read()

replacements = []

replacements.append((
'''            var buildingsRoot = new GameObject("Buildings");
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
''',
'''            var buildingsRoot = new GameObject("Buildings");
            buildingsRoot.transform.SetParent(envRoot.transform);
            string[] genericBuildingLetters = { "A", "B", "E", "F" };
            int colorIdx = 0;
            foreach (var spot in buildingSpots01)
            {
                Vector3 pos = WorldFromNormalized(terrain, spot.x, spot.y);
                var b = KayKitIntegration.BuildBuilding(buildingsRoot.transform, pos,
                    letter: genericBuildingLetters[colorIdx % genericBuildingLetters.Length],
                    scale: Random.Range(2.2f, 2.8f),
                    name: $"Building_{colorIdx + 1}");
                if (b == null) { colorIdx++; continue; }
'''
))

replacements.append((
'''            // ---- Hospital POI ----
            Vector3 hospitalPos = WorldFromNormalized(terrain, hospital01.x, hospital01.y);
            var hospital = BuildSimpleBuilding(envRoot.transform, hospitalPos, width: 11f, depth: 9f, height: 5f,
                wallMat: hospitalWallMat, roofMat: hospitalRoofMat, name: "Hospital");
            AddCrossSymbol(hospital.transform, hospitalCrossMat);
            SetupPoi(hospital, PoiType.Hospital, "HOSPITAL", new Color(0.9f, 0.15f, 0.15f), out var hospitalTrigger);
''',
'''            // ---- Hospital POI ----
            Vector3 hospitalPos = WorldFromNormalized(terrain, hospital01.x, hospital01.y);
            const float hospitalScale = 2.8f;
            var hospital = KayKitIntegration.BuildBuilding(envRoot.transform, hospitalPos, letter: "H", scale: hospitalScale, name: "Hospital");
            KayKitIntegration.AddRoofCross(hospital.transform, 3.05f * hospitalScale, hospitalCrossMat);
            SetupPoi(hospital, PoiType.Hospital, "HOSPITAL", new Color(0.9f, 0.15f, 0.15f), out var hospitalTrigger);
'''
))

replacements.append((
'''            // ---- Shelter POI ----
            Vector3 shelterPos = WorldFromNormalized(terrain, shelter01.x, shelter01.y);
            var shelter = BuildSimpleBuilding(envRoot.transform, shelterPos, width: 10f, depth: 8f, height: 4.6f,
                wallMat: shelterWallMat, roofMat: shelterRoofMat, name: "EmergencyShelter");
            AddFlagpole(shelter.transform, new Vector3(0f, 4.6f, -3.5f), shelterFlagMat, poleName: "Shelter");
            SetupPoi(shelter, PoiType.EmergencyShelter, "EMERGENCY SHELTER", new Color(0.2f, 0.5f, 0.9f), out var shelterTrigger);
''',
'''            // ---- Shelter POI ----
            Vector3 shelterPos = WorldFromNormalized(terrain, shelter01.x, shelter01.y);
            const float shelterScale = 2.6f;
            var shelter = KayKitIntegration.BuildBuilding(envRoot.transform, shelterPos, letter: "G", scale: shelterScale, name: "EmergencyShelter");
            AddFlagpole(shelter.transform, new Vector3(0f, 2.98f * shelterScale, 0f), shelterFlagMat, poleName: "Shelter");
            SetupPoi(shelter, PoiType.EmergencyShelter, "EMERGENCY SHELTER", new Color(0.2f, 0.5f, 0.9f), out var shelterTrigger);
'''
))

replacements.append((
'''                GameObject tree = placed % 3 == 0
                    ? BuildBroadleafTree(treesRoot.transform, pos, trunkMat, foliageMats[placed % foliageMats.Length])
                    : BuildTree(treesRoot.transform, pos, trunkMat, foliageMats[placed % foliageMats.Length], coneMesh, coneMeshSmall);
''',
'''                GameObject tree = KayKitIntegration.BuildTree(treesRoot.transform, pos);
'''
))

replacements.append((
'''                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                BuildRockCluster(rocksRoot.transform, pos, rockMat);
                occupied.Add(candidate);
                rocksPlaced++;
            }
''',
'''                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                KayKitIntegration.BuildRockCluster(rocksRoot.transform, pos);
                occupied.Add(candidate);
                rocksPlaced++;
            }
'''
))

replacements.append((
'''                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                BuildShrubCluster(shrubsRoot.transform, pos, shrubMats[shrubsPlaced % shrubMats.Length]);
                shrubsPlaced++;
            }
''',
'''                Vector3 pos = WorldFromNormalized(terrain, nx, nz);
                KayKitIntegration.BuildShrubCluster(shrubsRoot.transform, pos);
                shrubsPlaced++;
            }
'''
))

replacements.append((
'''                    Vector3 pos = WorldFromNormalized(terrain, candidate.x, candidate.y);
                    BuildShrubCluster(parent, pos, shrubMats[placed % shrubMats.Length]);
                    placed++;
''',
'''                    Vector3 pos = WorldFromNormalized(terrain, candidate.x, candidate.y);
                    KayKitIntegration.BuildShrubCluster(parent, pos);
                    placed++;
'''
))

replacements.append((
'''                if (SlopeAt(terrain, fnx1, fnz1) < 36f)
                    BuildTree(treesRoot.transform, WorldFromNormalized(terrain, fnx1, fnz1), trunkMat, foliageMats[0], coneMesh, coneMeshSmall);
                if (SlopeAt(terrain, fnx2, fnz2) < 36f)
                    BuildBroadleafTree(treesRoot.transform, WorldFromNormalized(terrain, fnx2, fnz2), trunkMat, foliageMats[1]);
''',
'''                if (SlopeAt(terrain, fnx1, fnz1) < 36f)
                    KayKitIntegration.BuildTree(treesRoot.transform, WorldFromNormalized(terrain, fnx1, fnz1));
                if (SlopeAt(terrain, fnx2, fnz2) < 36f)
                    KayKitIntegration.BuildTree(treesRoot.transform, WorldFromNormalized(terrain, fnx2, fnz2));
'''
))

replacements.append((
'''            result.Player = BuildPlayer(envRoot.transform, playerPos);''',
'''            result.Player = KayKitIntegration.BuildPlayer(envRoot.transform, playerPos);'''
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

print("Patched EnvironmentBuilder.cs successfully.")
