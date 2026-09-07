# DisasterReady — Environment Inventory

Per-asset/prefab inventory for `Assets/Scenes/DisasterReady_AizawlValley.unity`, sourced directly from the generator script `Assets/Scripts/Editor/AizawlValleySceneBuilder.cs`. This scene has no hand-placed content — every object below is instantiated by that script's code, so "current usage location" is authoritative (read from the actual `Place*`/`Build*` calls), not inferred.

All positions are world-space meters. "Duplicable" means: can you safely call the same placement pattern again with a new position/name to add another copy (yes for essentially everything here — it's all procedural instantiation, no unique singleton assumptions except where noted).

## Ground / Terrain Prefabs

| Prefab | Path | Used for | Instances | Collision |
|---|---|---|---|---|
| `SM_Generic_Ground_Flat_01` | `Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Ground_Flat_01.prefab` | All flat ground tiles (L0/L1/L2/L3 decks + 3 underlay tiles) | ~20 (6 L0 + 6 L1 + 6 L2 + 2 L3 + 3 underlay) | MeshCollider, except the 3 underlay tiles (`addCollider:false` — visual-only, fixed this session to prevent an invisible obstruction) |
| `SM_Generic_Ground_01/02/03/04` | same dir | Defined as constants (`Ground01-04Prefab`) but not referenced by any `Place*` call found in `BuildScene()` — likely unused/reserved for future variety | 0 confirmed | — |
| `SM_PolygonPrototype_Buildings_Ramp_25_1x1_01P` | `Assets/Synty/PolygonStarter/Prefabs/...` | Visual ramp mesh for all 3 level transitions (`Ramp_L0_to_L1`, `Ramp_L1_to_L2`, `Ramp_L2_to_L3`) | 3 | **None** (deliberately stripped — non-convex MeshCollider caused player-stuck bugs; see Handoff Section J). Walkability comes entirely from the separate invisible `SlopedRouteRamp` BoxColliders below. |
| (procedural, no prefab) | `SlopedRouteRamp()` helper | Invisible walkable-slope BoxColliders, one per ramp, named `RouteBridge_L0_L1`/`RouteBridge_L1_L2`/`RouteBridge_L2_L3` | 3 | BoxCollider, size `(width, 1.6, run+4)` — this exact shape was tuned live this session; don't shrink without retesting |
| (procedural cubes) | `GameObject.CreatePrimitive(PrimitiveType.Cube)` | Terrace retaining berms, named `TerraceBerm_L0_L1_West/East`, `TerraceBerm_L1_L2_West/East`, `TerraceBerm_L2_L3_West/East` | 6 | BoxCollider (via `EnsureBoxCollider`) |
| `SM_Generic_Small_Rocks_02` | `Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Small_Rocks_02.prefab` | Retaining-wall rock rows along each terrace step (`RetainingWall_L0_L1_*`, `RetainingWall_L1_L2_*`, `RetainingWall_L2_L3_*`), spaced every 3.5m along X, gap in the middle for the ramp | ~17 per row x 3 rows ≈ 50 | MeshCollider |

## Mountains / Background

Built by `BuildMountains` (uses `MountainPeakPrefab` = `SM_Generic_Mountains_Grass_02`, `MountainRidgePrefab` = `SM_Generic_Mountains_Soft_01`) — perimeter amphitheater + the summit peak (whose exact position is returned and reused as `POI_Summit`'s location and the emergency objective). Also: `BuildSummitSafetyFeatures` (edge marking/signage near the summit safe zone), `BuildBackgroundSettlementSilhouette` (distant non-interactive building silhouettes for macro composition — purely visual, not walkable/reachable).

## Roads

| Prefab (KayKit, under `Assets/KayKit/CityBuilder/Assets/fbx (unity)/`) | Used for | Placement pattern |
|---|---|---|
| `road_straight.fbx` | L0 spine (Z -34..-16), L1 approach (Z -11.5..-9.5), hospital/shelter branch spurs, town street (Z -6..13), L2 ridge spine (Z 17.5..37.5), landslide-zone damaged road segments | `PlaceRoadStraight`, tiled every 2.0m along an axis |
| `road_corner_curved.fbx` | Hospital curve (turning into apron at X=-20,Z=-8), shelter curve (X=20,Z=-8) | `PlaceRoadTile` |
| `road_junction.fbx` | Central 4-way junction at `(0, 2.99, -8)` | `PlaceRoadTile` |

Every road tile: auto flush `BoxCollider(2.0, 0.15, 2.0)` centered `(0, 0.05, 0)`, EXCEPT the last few Level-2-spine tiles at `z >= 35.5` which have colliders stripped (fix for a collider-seam overlap with the L2->L3 ramp — see Handoff Section J item 2d).

## Buildings

| Prefab | Path | Used for | Position(s) | Scale | Rotation |
|---|---|---|---|---|---|
| `SM_PolygonApocalypse_Bld_House_01` | Synty | Fallback house if KayKit variants missing | — | 2.0-2.2x | varies |
| `building_G.fbx` | KayKit CityBuilder | `House_L0_West` | `(-15, 0.5, -34)` | 2.0x | Y=85° |
| `building_E.fbx` | KayKit CityBuilder | `House_L0_East` | `(15, 0.5, -34)` | 2.0x | Y=-85° |
| `building_E.fbx` | KayKit CityBuilder | `House_L2_West` | `(-17, 6.0, 33)` **(widened from -12 this session)** | 2.0x | Y=85° |
| `building_G.fbx` | KayKit CityBuilder | `House_L2_East` | `(17, 6.0, 33)` **(widened from 12 this session)** | 2.0x | Y=-85° |
| `building_A/B/D/F.fbx` (cycled) | KayKit CityBuilder | `TownHouse_0..5`, lining the Level 1 town street | 6 spots, X=±6, Z ∈ {-3, 4, 11} | 2.2x | Y=±80° (west/east) |
| `building_C.fbx` | KayKit CityBuilder | `Hospital_MainFacility` | `(-20, 3.0, 9)` | 2.6x | Y=180° |
| `building_F.fbx` | KayKit CityBuilder | `Hospital_EmergencyWard_Annex` | offset `(-5.5, 0, 0)` from hospital center | 2.2x | Y=180° |
| `building_G.fbx` | KayKit CityBuilder | `Shelter_CommunityCenter_Main` | `(20, 3.0, 9)` | 2.6x | Y=180° |
| `building_H.fbx` | KayKit CityBuilder | `Shelter_SupplyStorage_Hall` | offset `(5.5, 0, 0)` from shelter center | 2.2x | Y=180° |

All buildings get a generic `EnsureBoxCollider(4,4,4)` centered `(0,2,0)` via `PlaceBuilding` — **not scaled to the building's actual footprint**. This was directly implicated in this session's Level-2-collision bug; verify clearance manually for any new/moved building near a walkway.

## Vegetation

`BuildVegetationAndRocks` places trees using `SyntyTree01Prefab`/`SyntyTree02Prefab`/`SyntyTree03Prefab`/`SyntyTree04Prefab` (healthy variety) and `SyntyTreeDeadPrefab` (also reused for the landslide zone's "collapsed tree" storytelling prop and the hillside residential clusters). Exact per-tree coordinates weren't individually cataloged this session (function not fully read line-by-line — see `AizawlValleySceneBuilder.cs` lines ~1238-1313 for the authoritative list); they scatter across the vegetation folder consistent with the valley's terraced levels.

## Rocks

`SyntyRock01Prefab` through `SyntyRock05Prefab` used two ways: (1) structural retaining-wall rows along each terrace step (see Ground/Terrain table above), and (2) decorative rockfall debris in the landslide hazard zone (`Landslide_RockfallDebris_0..6`, random rotation/scale 2.0-3.8x, centered near `(-18, 0.5, -22)`).

## Props

- **Streetlights** (KayKit `streetlight.fbx`, via `PlaceStreetlight`): flanking roads at Level 0/1/2 spine, plus 2 each at hospital and shelter courtyards. Walk-through (colliders stripped).
- **Crates** (`SyntyCratePrefab`): 5 stacked "Shelter_ReliefCrate_0..4" near the shelter, offsets from `(-4.0, 0, 1)` to `(-4.8, 0.8, 2.2)`. Walk-through.
- **Benches** (KayKit `bench.fbx`): 2 near the shelter (`Shelter_Bench_0/1`). Walk-through.
- **Traffic cones** (`SyntyConePrefab`, recolored bright safety orange): 5 blocking the collapsed road in the landslide zone. Walk-through.
- **Vehicles**: `Hospital_Ambulance_Vehicle` (KayKit `car_stationwagon.fbx`, repainted white, near `(-15, 3.02, 5.5)`), `Shelter_Police_Vehicle` (KayKit `car_police.fbx`, near `(14, 3.02, 6)`). Both have a manual `EnsureBoxCollider(2,1.5,4)`. `SyntyCarPrefab` is also defined as a constant but its specific placement wasn't traced this session — check `BuildHillsideResidentialClusters`/`BuildResidentialSettlement` if you need street-life vehicle positions (git history mentions "add street-life vehicles" as a separate pass).
- **Signposts** (fully procedural, `CreateSignpost` — code-built plate + text, no imported model): hospital entrance ("AIZAWL DISTRICT HOSPITAL / EMERGENCY SERVICES"), shelter entrance ("COMMUNITY EVACUATION SHELTER / DISTRICT RELIEF HQ"), landslide warning ("CAUTION: LANDSLIDE HAZARD / ROAD CLOSED - EVACUATE TO HIGH GROUND"), town welcome sign ("AIZAWL HILL DISTRICT / ELEVATION 1132m - EVAC ROUTE NORTH").
- **Medical cross insignia** (procedural, two colored cubes forming a red cross): mounted above the hospital entrance.
- **Route markers**: `BuildRouteMarkers` places in-world evacuation route markers (environmental signage, not screen-space UI) — not individually cataloged this session.

## Mission-critical objects (do not remove without replacing the mission logic)

| Object | Location | Component(s) | Mission ID |
|---|---|---|---|
| `POI_Hospital` | `(-20, ~3.0, ~6.8)` | `SphereCollider(trigger, r=7)`, `MissionZoneTrigger`, `PoiMarker(Hospital)` | `find_hospital` |
| `POI_Shelter` | `(20, ~3.0, ~6.8)` | `SphereCollider(trigger, r=7)`, `MissionZoneTrigger`, `PoiMarker(EmergencyShelter)` | `find_shelter` |
| `POI_Summit` | summit peak position (returned by `BuildMountains`) | `SphereCollider(trigger, r=7)`, `MissionZoneTrigger`, `PoiMarker(Summit)`, **also** `EmergencyObjectiveTrigger` (double duty: normal mission + post-emergency win condition) | `find_highest` |

## Gameplay/system singletons (not visual props, but critical scene objects)

| Object | Component | Notes |
|---|---|---|
| `DisasterReady_Player` | `CharacterController`, `PlayerController`, `Rigidbody`(kinematic), `FallRecoveryGuard`, `ProceduralCharacterAnimator`, `SafeDirectionGuide`, `KayKitLocomotionAnimator` (added dynamically if the KayKit rig's `Rig_Medium` node is found) | Tag `Player`. Only one should ever exist. |
| `Main Camera` | `Camera`, `AudioListener`, `OrbitCameraController` | Only one should ever exist; `orbitCam.Target` must point at the player. |
| `MissionManager` | `MissionManager` | Singleton (`Instance`) — duplicating this object will cause `Awake()` to silently overwrite `Instance`, which will break whichever copy loses the race. Don't duplicate. |
| `EmergencySystem` | `EmergencyScenarioController` | Singleton (`Instance`), same caveat as above. Starts `SetActive(false)` then immediately `true` again during scene construction — that pattern (from `IntegrateGameplay`) forces `Awake()` to run at a specific point in the build sequence; don't reorder it without understanding why. |
| `GameBootstrapper` | `GameBootstrapper` | Not a singleton but there should only be one — it's the sole thing that unlocks player/camera controls. |

## Non-visual organizational objects (safe to ignore, don't delete casually anyway)

`Navigation/MainRoute`, `HospitalRoute`, `ShelterRoute`, `SummitRoute` — empty GameObject waypoints (`CreateWaypoint`), purely for level-design reference. Nothing at runtime reads them. Harmless either way.

## Scene-wide static flags

Nearly every placed object gets `GameObjectUtility.SetStaticEditorFlags(go, BatchingStatic | OccludeeStatic)` via `SetStaticFlags()` — this is a batching/occlusion-culling optimization, not a gameplay flag. If you add a new object that should NOT be static (e.g. anything that will move or be enabled/disabled at runtime in an unusual way), don't call `SetStaticFlags` on it.
