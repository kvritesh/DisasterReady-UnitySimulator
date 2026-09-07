using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DisasterReady.Missions;
using DisasterReady.Player;
using DisasterReady.Emergency;
using DisasterReady.CameraSystem;
using DisasterReady.Core;
using DisasterReady.POI;

namespace DisasterReady.EditorTools
{
    /// <summary>
    /// Read-only, project-wide sanity check. Complements DisasterReadySceneValidator (which only
    /// inspects the currently open scene's object wiring) by also covering things outside any one
    /// scene: does the main scene file exist and is it registered in Build Settings, are the
    /// packages/project settings this project actually depends on present and set correctly, and
    /// is there any obviously WebGL-incompatible API usage in the scripts. When the main scene
    /// happens to be the one currently open, it also runs a broader scene-content pass (missing
    /// scripts, missing prefab/material references, duplicate mission/POI ids) that goes beyond
    /// what the scene validator checks.
    ///
    /// NEVER modifies the project or the scene -- every check only reads state (AssetDatabase
    /// lookups, PlayerSettings/GraphicsSettings reads, plain file reads, Find/GetComponent calls).
    /// It never opens, closes, or saves a scene, and never touches disk.
    ///
    /// Run via DisasterReady/Project Health Check any time; run it with the main scene
    /// (DisasterReady_AizawlValley.unity) open for the full report, or without it open for just
    /// the project-level checks.
    /// </summary>
    public static class DisasterReadyProjectHealthCheck
    {
        private const string MainScenePath = "Assets/Scenes/DisasterReady_AizawlValley.unity";

        private enum Status { Pass, Warn, Fail }

        private static int _passCount;
        private static int _warnCount;
        private static int _failCount;

        [MenuItem("DisasterReady/Project Health Check")]
        public static void Run()
        {
            _passCount = 0;
            _warnCount = 0;
            _failCount = 0;

            Debug.Log("[HealthCheck] ==============================================");
            Debug.Log("[HealthCheck] DisasterReady Project Health Check");
            Debug.Log("[HealthCheck] ==============================================");

            CheckMainSceneExists();
            CheckMainSceneInBuildSettings();
            CheckRequiredPackages();
            CheckProjectSettings();
            CheckWebGLUnsafeApiUsage();
            CheckSceneContent();

            Debug.Log("[HealthCheck] ==============================================");
            Debug.Log($"[HealthCheck] DONE. {_passCount} PASS, {_warnCount} WARN, {_failCount} FAIL.");
            Debug.Log("[HealthCheck] ==============================================");

            string overall = _failCount > 0 ? "FAIL" : (_warnCount > 0 ? "WARN" : "PASS");
            string summary = $"Overall: {overall}\n\n{_passCount} PASS, {_warnCount} WARN, {_failCount} FAIL.\n\nSee Console for the full report (each line prefixed [HealthCheck]).";
            EditorUtility.DisplayDialog("DisasterReady Project Health Check", summary, "OK");
        }

        private static void Report(Status status, string message)
        {
            string tag = status == Status.Pass ? "PASS" : status == Status.Warn ? "WARN" : "FAIL";
            string line = $"[HealthCheck] [{tag}] {message}";
            if (status == Status.Fail) { _failCount++; Debug.LogError(line); }
            else if (status == Status.Warn) { _warnCount++; Debug.LogWarning(line); }
            else { _passCount++; Debug.Log(line); }
        }

        // ---------------------------------------------------------------
        // A. Project-level checks (always run, don't need any scene open)
        // ---------------------------------------------------------------

        private static void CheckMainSceneExists()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            if (sceneAsset != null)
                Report(Status.Pass, $"Main scene exists at {MainScenePath}.");
            else
                Report(Status.Fail, $"Main scene NOT FOUND at {MainScenePath}.");
        }

        private static void CheckMainSceneInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            int index = Array.FindIndex(scenes, s => s.path == MainScenePath);
            if (index < 0)
            {
                Report(Status.Fail, "Main scene is NOT present in Build Settings.");
                return;
            }
            if (!scenes[index].enabled)
                Report(Status.Warn, $"Main scene is in Build Settings at index {index} but DISABLED (unchecked).");
            else
                Report(Status.Pass, $"Main scene is in Build Settings and enabled (index {index}).");
        }

        private static void CheckRequiredPackages()
        {
            string manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Report(Status.Warn, "Could not find Packages/manifest.json -- skipped required-package check.");
                return;
            }
            string manifest = File.ReadAllText(manifestPath);
            CheckPackagePresent(manifest, "com.unity.render-pipelines.universal", "Universal Render Pipeline");
            CheckPackagePresent(manifest, "com.unity.inputsystem", "Input System");
        }

        private static void CheckPackagePresent(string manifest, string packageId, string displayName)
        {
            if (manifest.Contains("\"" + packageId + "\""))
                Report(Status.Pass, $"Required package present: {displayName} ({packageId}).");
            else
                Report(Status.Fail, $"Required package MISSING: {displayName} ({packageId}).");
        }

        private static void CheckProjectSettings()
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
                Report(Status.Pass, "Color Space is Linear (expected for URP).");
            else
                Report(Status.Warn, $"Color Space is {PlayerSettings.colorSpace}, not Linear -- URP lighting/materials may look wrong.");

            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
                Report(Status.Pass, "A Render Pipeline Asset is assigned in Graphics Settings.");
            else
                Report(Status.Fail, "No Render Pipeline Asset assigned in Graphics Settings -- URP will not actually be used.");

            string projectSettingsPath = Path.Combine(Application.dataPath, "..", "ProjectSettings", "ProjectSettings.asset");
            if (File.Exists(projectSettingsPath))
            {
                string text = File.ReadAllText(projectSettingsPath);
                var m = Regex.Match(text, @"activeInputHandler:\s*(\d+)");
                if (m.Success)
                {
                    int handler = int.Parse(m.Groups[1].Value);
                    // 0 = old Input Manager only, 1 = new Input System Package only, 2 = Both
                    if (handler == 1 || handler == 2)
                        Report(Status.Pass, $"Active Input Handling includes the new Input System (setting value {handler}).");
                    else
                        Report(Status.Fail, "Active Input Handling is set to the OLD Input Manager only -- this project's PlayerInput/Input System scripts will not receive input.");
                }
                else
                {
                    Report(Status.Warn, "Could not read the Active Input Handling setting from ProjectSettings.asset.");
                }
            }
            else
            {
                Report(Status.Warn, "Could not find ProjectSettings/ProjectSettings.asset -- skipped Active Input Handling check.");
            }
        }

        private static void CheckWebGLUnsafeApiUsage()
        {
            string scriptsDir = Path.Combine(Application.dataPath, "Scripts");
            if (!Directory.Exists(scriptsDir))
            {
                Report(Status.Warn, "Assets/Scripts folder not found -- skipped WebGL API scan.");
                return;
            }

            // Small, deliberately non-exhaustive list of APIs that are known-problematic on
            // WebGL (single-threaded, sandboxed, no real OS process/socket access). "Where
            // practical" per the task -- this is a smoke test, not a full IL analyzer.
            var patterns = new[]
            {
                "System.Threading.Thread",
                "new Thread(",
                ".Sleep(",
                "System.Net.Sockets",
                "System.Diagnostics.Process",
                "Process.Start(",
            };

            var hits = new List<string>();
            foreach (var file in Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories))
            {
                string relative = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');

                // Anything under an "Editor" folder is Editor-only by Unity convention and is
                // stripped out of every player build, WebGL included -- it never actually ships,
                // so scanning it here would only produce false positives (for example this very
                // tool's own source, which necessarily contains these pattern strings as literals
                // in the list above).
                if (relative.Contains("/Editor/")) continue;

                string content = File.ReadAllText(file);
                foreach (var pattern in patterns)
                {
                    if (content.Contains(pattern))
                    {
                        hits.Add($"{relative} references '{pattern}'");
                    }
                }
            }

            if (hits.Count == 0)
            {
                Report(Status.Pass, "No obvious WebGL-incompatible API usage found in Assets/Scripts (threading/socket/process patterns).");
            }
            else
            {
                foreach (var hit in hits)
                    Report(Status.Warn, $"Possible WebGL-incompatible API usage: {hit}");
            }
        }

        // ---------------------------------------------------------------
        // B. Scene-content checks -- only meaningful, and only run, when
        // the main scene is the one currently open in the Editor. This
        // tool never opens/closes/saves a scene itself.
        // ---------------------------------------------------------------

        private static void CheckSceneContent()
        {
            Scene active = SceneManager.GetActiveScene();
            bool mainSceneOpen = active.IsValid() && active.path == MainScenePath;

            if (!mainSceneOpen)
            {
                string activeDesc = active.IsValid() && !string.IsNullOrEmpty(active.path) ? active.path : "<none>";
                Report(Status.Warn, $"Main scene is not currently open (active scene: {activeDesc}) -- open {MainScenePath} and re-run Project Health Check for Player/Camera/Mission/POI content checks.");
                return;
            }

            CheckPlayerAndCamera();
            CheckManagers();
            var missionIds = CheckMissionsAndTriggers();
            CheckDuplicateTriggerIds();
            CheckPois(missionIds);
            CheckMissingScriptsPrefabsMaterials();
        }

        private static void CheckPlayerAndCamera()
        {
            var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            if (players.Length == 1)
                Report(Status.Pass, "Player exists (exactly 1 PlayerController).");
            else
                Report(Status.Fail, $"Expected exactly 1 PlayerController, found {players.Length}.");

            if (Camera.main != null)
                Report(Status.Pass, $"Main camera exists ('{Camera.main.name}', tagged MainCamera).");
            else
                Report(Status.Fail, "No camera tagged MainCamera found in the scene.");
        }

        private static void CheckManagers()
        {
            var missionManagers = UnityEngine.Object.FindObjectsByType<MissionManager>(FindObjectsSortMode.None);
            if (missionManagers.Length == 1)
                Report(Status.Pass, "MissionManager exists (exactly 1).");
            else
                Report(Status.Fail, $"Expected exactly 1 MissionManager, found {missionManagers.Length}.");

            var emergencyControllers = UnityEngine.Object.FindObjectsByType<EmergencyScenarioController>(FindObjectsSortMode.None);
            if (emergencyControllers.Length == 1)
            {
                Report(Status.Pass, "EmergencyScenarioController exists (exactly 1).");
                var ec = emergencyControllers[0];
                CheckRef(ec.Missions != null, "EmergencyScenarioController.Missions");
                CheckRef(ec.Hud != null, "EmergencyScenarioController.Hud");
                CheckRef(ec.Player != null, "EmergencyScenarioController.Player");
                CheckRef(ec.Guide != null, "EmergencyScenarioController.Guide");
                CheckRef(ec.ResultPanel != null, "EmergencyScenarioController.ResultPanel");
            }
            else
            {
                Report(Status.Fail, $"Expected exactly 1 EmergencyScenarioController, found {emergencyControllers.Length}.");
            }

            var cams = UnityEngine.Object.FindObjectsByType<OrbitCameraController>(FindObjectsSortMode.None);
            if (cams.Length == 1)
                CheckRef(cams[0].Target != null, "OrbitCameraController.Target");

            var bootstrappers = UnityEngine.Object.FindObjectsByType<GameBootstrapper>(FindObjectsSortMode.None);
            if (bootstrappers.Length == 0)
            {
                Report(Status.Warn, "No GameBootstrapper found -- the title-screen gate will be skipped at scene start.");
            }
            else
            {
                var gb = bootstrappers[0];
                CheckRef(gb.TitleScreen != null, "GameBootstrapper.TitleScreen");
                CheckRef(gb.Hud != null, "GameBootstrapper.Hud");
                CheckRef(gb.Player != null, "GameBootstrapper.Player");
                CheckRef(gb.OrbitCamera != null, "GameBootstrapper.OrbitCamera");
            }
        }

        private static void CheckRef(bool isAssigned, string label)
        {
            if (isAssigned)
                Report(Status.Pass, $"{label} is assigned.");
            else
                Report(Status.Fail, $"Missing serialized reference: {label} is not assigned.");
        }

        /// <summary>Checks mission ids are non-empty and each has exactly one matching trigger. Returns the set of valid mission ids (for the POI check below).</summary>
        private static HashSet<string> CheckMissionsAndTriggers()
        {
            var missionIds = new HashSet<string>();
            var missionManagers = UnityEngine.Object.FindObjectsByType<MissionManager>(FindObjectsSortMode.None);
            if (missionManagers.Length != 1 || missionManagers[0].Missions == null || missionManagers[0].Missions.Count == 0)
            {
                Report(Status.Warn, "No mission list to check (MissionManager missing or empty) -- skipped mission id / trigger checks.");
                return missionIds;
            }

            var allTriggers = UnityEngine.Object.FindObjectsByType<MissionZoneTrigger>(FindObjectsSortMode.None);
            foreach (var mission in missionManagers[0].Missions)
            {
                if (string.IsNullOrEmpty(mission.Id))
                {
                    Report(Status.Fail, $"Mission '{mission.Title}' has a blank Id.");
                    continue;
                }
                Report(Status.Pass, $"Mission id '{mission.Id}' is non-empty.");
                missionIds.Add(mission.Id);

                int matchCount = allTriggers.Count(t => t.MissionId == mission.Id);
                if (matchCount == 1)
                    Report(Status.Pass, $"Mission trigger reference OK for '{mission.Id}' (exactly 1 MissionZoneTrigger).");
                else if (matchCount == 0)
                    Report(Status.Fail, $"Mission trigger MISSING for '{mission.Id}' -- it can never be completed.");
                else
                    Report(Status.Warn, $"Mission '{mission.Id}' has {matchCount} MissionZoneTriggers pointing at it (expected exactly 1).");
            }
            return missionIds;
        }

        private static void CheckDuplicateTriggerIds()
        {
            var allTriggers = UnityEngine.Object.FindObjectsByType<MissionZoneTrigger>(FindObjectsSortMode.None);
            var groups = allTriggers
                .Where(t => !string.IsNullOrEmpty(t.MissionId))
                .GroupBy(t => t.MissionId)
                .Where(g => g.Count() > 1);

            bool anyDuplicates = false;
            foreach (var group in groups)
            {
                anyDuplicates = true;
                string names = string.Join(", ", group.Select(t => t.gameObject.name));
                Report(Status.Fail, $"Duplicate mission id '{group.Key}' used by {group.Count()} triggers: {names}.");
            }
            if (!anyDuplicates)
                Report(Status.Pass, "No duplicate MissionZoneTrigger ids found.");
        }

        private static void CheckPois(HashSet<string> knownMissionIds)
        {
            var pois = UnityEngine.Object.FindObjectsByType<PoiMarker>(FindObjectsSortMode.None);
            if (pois.Length == 0)
            {
                Report(Status.Warn, "No PoiMarker objects found in the scene.");
                return;
            }

            foreach (var poi in pois)
            {
                var col = poi.GetComponent<Collider>();
                if (col == null)
                    Report(Status.Fail, $"POI '{poi.name}' has no Collider -- it can never be entered.");
                else if (!col.isTrigger)
                    Report(Status.Warn, $"POI '{poi.name}' Collider is not marked isTrigger.");

                var trigger = poi.GetComponent<MissionZoneTrigger>();
                if (trigger == null)
                {
                    Report(Status.Warn, $"POI '{poi.name}' has no MissionZoneTrigger -- purely decorative, not mission-bearing.");
                    continue;
                }
                if (string.IsNullOrEmpty(trigger.MissionId))
                    Report(Status.Fail, $"POI '{poi.name}' has an empty MissionId.");
                else
                    Report(Status.Pass, $"POI '{poi.name}' id '{trigger.MissionId}' is non-empty.");
            }
        }

        private static void CheckMissingScriptsPrefabsMaterials()
        {
            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);

            int missingScripts = 0;
            int missingPrefabs = 0;
            int missingMaterials = 0;

            foreach (var t in allTransforms)
            {
                var go = t.gameObject;

                // Missing Script: a Component slot whose backing script asset was deleted shows up as null.
                var components = go.GetComponents<Component>();
                foreach (var c in components)
                {
                    if (c == null)
                    {
                        missingScripts++;
                        Report(Status.Fail, $"GameObject '{go.name}' has a missing script.");
                        break; // one report per object is enough
                    }
                }

                // Missing prefab source.
                var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(go);
                if (prefabStatus == PrefabInstanceStatus.MissingAsset)
                {
                    missingPrefabs++;
                    Report(Status.Fail, $"GameObject '{go.name}' is a prefab instance with a MISSING source prefab.");
                }

                // Missing materials on any renderer.
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null)
                        {
                            missingMaterials++;
                            Report(Status.Warn, $"GameObject '{go.name}' Renderer has a missing/empty material slot (index {i}).");
                        }
                    }
                }
            }

            if (missingScripts == 0)
                Report(Status.Pass, "No missing scripts found on any scene object.");
            if (missingPrefabs == 0)
                Report(Status.Pass, "No missing prefab sources found.");
            if (missingMaterials == 0)
                Report(Status.Pass, "No missing material slots found on any Renderer.");
        }
    }
}
