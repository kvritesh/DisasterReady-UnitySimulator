# DisasterReady — Preparedness Simulator (Unity)

This is the Unity component of **DisasterReady**, a Smart India Hackathon (SIH26001)
prototype for AI-assisted early warning and landslide-risk preparedness in the
North-East Region (NER) of India.

**The React web app is the actual DisasterReady product.** This Unity project builds
the interactive **Preparedness Simulator** — a small third-person WebGL mini-game that
the web app launches from its Missions screen. They are two components of one product,
kept in separate repositories/folders because they are separate toolchains (Unity vs.
Vite/TypeScript), not because they are separate products.

Main web repository: `DisasterReady` (React app) — see its README for the full
product overview, architecture, and honesty/limitations section. This document covers
only the Unity side.

## What this is

A stylized, low-poly "cozy Himalayan/NER preparedness game" set in a procedurally
generated stand-in for Aizawl, Mizoram:

- Procedurally generated terrain, buildings, trees, rocks, fences and signage —
  **not** a real GIS/DEM reconstruction of Aizawl. It is a stylized demonstration
  environment.
- A third-person player character (procedurally built from primitives, not an
  imported/rigged asset) that walks around the terrain with WASD/arrow keys.
- Three preparedness missions (find the highest ground, find the emergency shelter,
  find the hospital) that teach terrain awareness before the emergency phase.
- A scripted simulated emergency: an environmental/warning shift, a safe-direction
  heuristic guide, and an objective the player must reach on foot.
- A preparedness result screen (score, missions completed, XP) at the end of the run.

**What it is not:** there is no real disaster data, no live sensor feed, and no
validated evacuation routing. The emergency is scripted gameplay; the safe-direction
guide is a terrain/objective heuristic, not a guaranteed real-world evacuation route.
The scene is always explicitly labeled as a simulated demonstration, never as real.

## Architecture

The entire demo scene is **generated procedurally from C# at editor time** — it is not
hand-authored in the `.unity` scene file. To change the world, edit the generator
scripts and re-run the build menu command; do not try to hand-edit the scene directly,
those changes will be lost on the next generate.

- `Assets/Scripts/Editor/SceneBuilder.cs` — orchestrator. Menu item
  `DisasterReady/Build Demo Scene` regenerates `Assets/Scenes/DisasterReadyDemo.unity`
  from scratch: terrain, environment, UI, missions, emergency systems, camera, player.
- `Assets/Scripts/Editor/TerrainGenerator.cs` — procedural terrain heightmap/material.
- `Assets/Scripts/Editor/EnvironmentBuilder.cs` — buildings, trees, rocks, fences,
  signage, and the player character, all built from Unity primitives + procedural
  materials (no imported meshes, no asset-store dependencies).
- `Assets/Scripts/Editor/UIBuilder.cs` — all screen-space UI (mission banners,
  emergency status plate, safe-direction plate, result panel).
- `Assets/Scripts/Editor/SharedAssetUtility.cs` — shared material/texture helpers
  (URP Lit shader based).
- `Assets/Scripts/Player/PlayerController.cs` + `ProceduralCharacterAnimator.cs` —
  CharacterController-based movement and a lightweight sine-wave walk/idle animator
  driven off actual movement speed (no animation rig/clips).
- `Assets/Scripts/Emergency/EmergencyScenarioController.cs`,
  `SafeDirectionGuide.cs` — the emergency sequence and directional guidance heuristic.
- `Assets/Scripts/UI/MissionCompleteBanner.cs`,
  `Assets/Scripts/Emergency/PreparednessResultPanel.cs` — mission/result feedback,
  including small procedurally synthesized audio cues (no external audio assets, so
  there is nothing that can go missing at runtime).
- `Assets/Plugins/WebGL/WebGLBridge.jslib` — the WebGL→browser bridge. Exposes
  `DR_GetLaunchParams` / `DR_SendResultToBrowser` via `[DllImport("__Internal")]`,
  used to read launch parameters from and post the completion result back to the
  hosting web page via `window.opener.postMessage(...)`.

## Web ↔ Unity integration

The web app opens the built WebGL player in a popup window (`window.open`, not an
iframe) from its Missions screen, passes launch parameters, and listens for a
`postMessage` completion result once the player finishes a run (3 missions → emergency
→ objective → result panel). See the web repo's `src/hooks/useUnitySimulator.ts` and
`src/screens/Missions.tsx` for the browser side of this contract.

## Building

1. Open this folder in Unity 6 (developed against 6000.5.10f1) with the Universal
   Render Pipeline.
2. Regenerate the scene if you've changed a generator script:
   `DisasterReady → Build Demo Scene` (menu bar).
3. **Windows/Editor testing:** just press Play. This is the fastest loop for
   verifying gameplay/animation/mission changes.
4. **WebGL build (what the web app actually serves):**
   - `File → Build Profiles`, select the `WebSimulator - Desktop - Development`
     Web build profile, `Build`.
   - Copy/compress the output into `disasterready/public/unity-sim/` as
     `unity-sim.data`, `unity-sim.framework.js`, `unity-sim.wasm` (brotli-compressed
     `.br` variants alongside them, matching Vite's static file serving) so the web
     app's Missions → Launch Preparedness Simulator flow picks it up.
   - Switch the active build target back to **Windows** afterward — Windows is the
     project's default/working target for day-to-day Editor testing.

## Known constraints

- No imported 3D assets, animation rigs, or asset-store packages are used anywhere —
  everything is built from Unity primitives and procedural materials/audio, by design,
  to keep the WebGL build small and dependency-free.
- Legacy Unity UI (`UnityEngine.UI`) is used for all HUD/world-space text.
