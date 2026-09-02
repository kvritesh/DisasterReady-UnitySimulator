# Changelog — Unity Preparedness Simulator

This is the Unity-side changelog. For the full DisasterReady product timeline
(web app + simulator together), see `CHANGELOG.md` in the `disasterready`
(web) repository.

Git version control was only established for this project on 2026-09-02, from
the current working state. Entries below for anything before that date
describe functionality that already existed in that working state; they are
not backed by individual historical commits.

## [Unreleased] — v0.8 — Visual / Game-Feel Polish (in progress)
- Procedurally built low-poly humanoid player character (torso, head, arms,
  legs, backpack, shoes) replacing the placeholder capsule, with a
  lightweight sine-wave walk/idle animator (no imported rig/clips).
- Building variety (alternate roofs, rotation, procedural windows/doors),
  broadleaf trees, rock clusters, decorative fences, a welcome signpost.
- Camera tuning for a more readable third-person framing.
- Procedural (runtime-synthesized) audio: footsteps, mission-complete chime,
  result-screen fanfare.
- Fixed a rendering regression: repeated `SharedAssetUtility.CreateColorMaterial`
  calls with the same material name (both fence runs used "FenceWood") were
  deleting and recreating the `.mat` asset on the second call, invalidating
  the first fence's already-assigned material reference and rendering it
  magenta. Fixed by updating the existing asset in place instead of
  delete+recreate.
- WebGL rebuild + full regression pass for this milestone: pending.

## v0.7 — Web ↔ Unity Integration
- `Assets/Plugins/WebGL/WebGLBridge.jslib` bridge (`DR_GetLaunchParams` /
  `DR_SendResultToBrowser`) posting the completion result to the hosting page
  via `window.postMessage`.
- Previously observed URP Unlit shader stripping issue in WebGL builds fixed
  via the project's Always Included Shaders list.

## v0.6 — Preparedness Simulator (standalone)
- Procedurally generated terrain/environment/player, driven entirely by
  editor-time C# generators (`SceneBuilder.cs`, `EnvironmentBuilder.cs`,
  `TerrainGenerator.cs`) — no imported 3D assets.
- Three preparedness missions, a scripted simulated emergency sequence with a
  safe-direction heuristic guide, and a preparedness result panel.
