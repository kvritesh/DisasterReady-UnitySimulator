using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DisasterReady.EditorTools
{
    /// <summary>
    /// Inspects the ACTUAL generated scene (real world-space Transform
    /// positions and real Collider.bounds - not source coordinates) and
    /// checks every building/pole/fence/major-vegetation obstacle against the
    /// actual route corridor reconstructed from the real "Path" segment
    /// GameObjects already in the scene. Writes KayKitLayoutDiagnosticReport.txt
    /// to the project root. Read-only - never modifies the scene.
    /// </summary>
    internal static class KayKitLayoutDiagnostic
    {
        private const float RouteHalfWidth = 0.8f; // approx half of the visual path width
        private const float CriticalClearance = 1.0f; // obstacle edge closer than this to the route edge = definitely blocking
        private const float TargetClearance = 2.0f; // obstacle edge should be at least this far from the route edge (~4-5m total corridor)

        [MenuItem("DisasterReady/KayKit/Dump Layout Diagnostic")]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== KayKit Layout Diagnostic ===");
            sb.AppendLine($"Generated: {System.DateTime.Now}");

            var scene = EditorSceneManager.GetActiveScene();
            sb.AppendLine($"Active scene: {scene.path}");

            var envRoot = GameObject.Find("Environment");
            if (envRoot == null)
            {
                sb.AppendLine("ERROR: 'Environment' root not found in the open scene. Open Assets/Scenes/DisasterReadyDemo.unity first.");
                Write(sb);
                return;
            }

            // ---- Reconstruct the actual route corridor from real Path segment GameObjects ----
            var routeSegments = new List<(Vector3 a, Vector3 b, string label)>();
            int pathIndex = 0;
            foreach (Transform child in envRoot.transform)
            {
                if (child.name != "Path") continue;
                pathIndex++;
                string pathLabel = $"Path#{pathIndex}";
                var segs = child.GetComponentsInChildren<Transform>().Where(t => t != child).OrderBy(t => t.GetSiblingIndex()).ToList();
                for (int i = 0; i < segs.Count - 1; i++)
                {
                    routeSegments.Add((segs[i].position, segs[i + 1].position, pathLabel));
                }
            }
            sb.AppendLine($"Reconstructed {routeSegments.Count} route segments from {envRoot.transform.Cast<Transform>().Count(t => t.name == "Path")} Path objects.");

            var player = GameObject.FindWithTag("Player");
            Vector3 spawnPos = player != null ? player.transform.position : Vector3.zero;
            sb.AppendLine($"Player spawn world pos: {spawnPos}");

            var summitMarker = GameObject.Find("HighestPointMarker");
            if (summitMarker != null) sb.AppendLine($"HighestPointMarker world pos: {summitMarker.transform.position}");

            sb.AppendLine();
            sb.AppendLine("=== Obstacle clearance check (RouteHalfWidth=" + RouteHalfWidth + "m, target clearance>=" + TargetClearance + "m) ===");

            var obstacles = new List<(string name, string kind, Bounds bounds)>();

            void CollectColliders(Transform root, string kind)
            {
                if (root == null) return;
                foreach (var col in root.GetComponentsInChildren<Collider>())
                {
                    // Triggers (e.g. the 7m MissionZoneTrigger SphereCollider
                    // on Hospital/Shelter) are meant to reach the route - they
                    // don't physically block movement, so they're not a
                    // "blocking obstacle" in the sense this check cares about.
                    if (col.isTrigger) continue;
                    // The fence Rail's axis-aligned Bounds wildly overstates
                    // its true (thin) footprint once rotated off-axis; the
                    // individual Post_N colliders along the same run already
                    // cover this check accurately.
                    if (col.transform.name == "Rail") continue;
                    obstacles.Add((col.transform.name + " (under " + root.name + ")", kind, col.bounds));
                }
            }

            var buildingsRoot = envRoot.transform.Find("Buildings");
            if (buildingsRoot != null)
            {
                foreach (Transform b in buildingsRoot) CollectColliders(b, "Building");
            }
            var hospital = envRoot.transform.Find("Hospital");
            if (hospital != null) CollectColliders(hospital, "Hospital");
            var shelter = envRoot.transform.Find("EmergencyShelter");
            if (shelter != null) CollectColliders(shelter, "Shelter");

            foreach (Transform child in envRoot.transform)
            {
                if (child.name == "Fence") CollectColliders(child, "Fence");
                if (child.name == "UtilityLine") CollectColliders(child, "Pole");
            }

            // Vegetation: no colliders (by design), so approximate via renderer bounds instead.
            void CollectRendererObstacles(string rootName, string kind)
            {
                var root = envRoot.transform.Find(rootName);
                if (root == null) return;
                foreach (Transform cluster in root)
                {
                    var renderers = cluster.GetComponentsInChildren<Renderer>();
                    if (renderers.Length == 0) continue;
                    Bounds combined = renderers[0].bounds;
                    foreach (var r in renderers) combined.Encapsulate(r.bounds);
                    obstacles.Add((cluster.name, kind, combined));
                }
            }
            CollectRendererObstacles("Trees", "Tree");
            CollectRendererObstacles("Rocks", "Rock");
            CollectRendererObstacles("Shrubs", "Shrub");

            int criticalCount = 0, warnCount = 0;
            foreach (var (name, kind, bounds) in obstacles)
            {
                Vector2 c = new Vector2(bounds.center.x, bounds.center.z);
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);

                float minDist = float.PositiveInfinity;
                string nearestSeg = "";
                foreach (var (a, b, label) in routeSegments)
                {
                    float d = DistancePointToSegmentXZ(c, new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                    if (d < minDist) { minDist = d; nearestSeg = label; }
                }
                if (routeSegments.Count == 0) continue;

                float clearance = minDist - radius - RouteHalfWidth;
                string flag = "OK";
                if (clearance < CriticalClearance) { flag = "CRITICAL-BLOCKING"; criticalCount++; }
                else if (clearance < TargetClearance) { flag = "WARN-TIGHT"; warnCount++; }

                if (flag != "OK")
                {
                    sb.AppendLine($"[{flag}] {kind} '{name}' pos=({bounds.center.x:F1},{bounds.center.z:F1}) radius={radius:F2} distToRoute={minDist:F2} clearance={clearance:F2} nearestSeg={nearestSeg}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"Total obstacles checked: {obstacles.Count}. CRITICAL-BLOCKING: {criticalCount}. WARN-TIGHT: {warnCount}.");

            Write(sb);
        }

        private static float DistancePointToSegmentXZ(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            Vector2 proj = a + ab * t;
            return Vector2.Distance(p, proj);
        }

        private static void Write(StringBuilder sb)
        {
            var path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "KayKitLayoutDiagnosticReport.txt");
            System.IO.File.WriteAllText(path, sb.ToString());
            Debug.Log($"[KayKitLayoutDiagnostic] Wrote report to {path}");
        }
    }
}
