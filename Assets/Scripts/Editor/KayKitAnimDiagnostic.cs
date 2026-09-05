using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DisasterReady.EditorTools
{
    /// <summary>
    /// One-shot diagnostic: dumps the actual instantiated Rogue_Hooded hierarchy
    /// paths alongside the actual AnimationClip curve-binding paths recorded on
    /// the KayKit locomotion clips, so the Animator retargeting mismatch causing
    /// the reported T-pose can be root-caused from real data instead of assumption.
    /// Writes KayKitAnimDiagnosticReport.txt to the project root. Cleans up any
    /// temporary GameObject it creates; does not touch the open scene otherwise.
    /// </summary>
    internal static class KayKitAnimDiagnostic
    {
        private const string PlayerFbx = "Assets/KayKit/Adventurers/Characters/fbx/Rogue_Hooded.fbx";
        private const string AnimMovementBasicFbx = "Assets/KayKit/CharacterAnimations/Animations/fbx/Rig_Medium/Rig_Medium_MovementBasic.fbx";
        private const string AnimGeneralFbx = "Assets/KayKit/CharacterAnimations/Animations/fbx/Rig_Medium/Rig_Medium_General.fbx";

        [MenuItem("DisasterReady/KayKit/Dump Anim Diagnostic")]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== KayKit Animation Diagnostic ===");
            sb.AppendLine($"Generated: {System.DateTime.Now}");
            sb.AppendLine();

            // ---- Importer settings ----
            DumpImporter(sb, PlayerFbx);
            DumpImporter(sb, AnimMovementBasicFbx);
            DumpImporter(sb, AnimGeneralFbx);
            sb.AppendLine();

            // ---- Instantiate a fresh, untouched copy of the player FBX ----
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerFbx);
            if (prefab == null)
            {
                sb.AppendLine($"ERROR: could not load prefab at {PlayerFbx}");
                Write(sb);
                return;
            }
            var instance = (GameObject)Object.Instantiate(prefab);
            instance.name = "DIAG_" + prefab.name;

            sb.AppendLine("=== Instantiated hierarchy (path relative to instance root) ===");
            DumpHierarchy(instance.transform, "", sb);
            sb.AppendLine();

            // Also dump relative to the "Rig_Medium" child specifically, since
            // that's the node KayKitIntegration currently places the Animator on.
            Transform rigMedium = FindDeep(instance.transform, "Rig_Medium");
            sb.AppendLine(rigMedium != null
                ? $"'Rig_Medium' found at path: {GetPath(instance.transform, rigMedium)}"
                : "'Rig_Medium' NOT FOUND anywhere in instantiated hierarchy!");
            sb.AppendLine();

            // ---- Curve bindings for every clip in both anim FBX files ----
            DumpClipBindings(sb, AnimMovementBasicFbx);
            DumpClipBindings(sb, AnimGeneralFbx);

            // ---- Cross-check: does each curve path resolve from instance root, and from Rig_Medium? ----
            sb.AppendLine("=== Path resolution cross-check (Walking_B clip) ===");
            var walkClip = AssetDatabase.LoadAllAssetsAtPath(AnimMovementBasicFbx).OfType<AnimationClip>()
                .FirstOrDefault(c => c.name == "Walking_B");
            if (walkClip != null)
            {
                var bindings = AnimationUtility.GetCurveBindings(walkClip);
                var uniquePaths = bindings.Select(b => b.path).Distinct().OrderBy(p => p).ToArray();
                foreach (var p in uniquePaths)
                {
                    string pathToTest = string.IsNullOrEmpty(p) ? "(root/empty path - applies to Animator's own transform)" : p;
                    bool resolvesFromInstanceRoot = string.IsNullOrEmpty(p) || instance.transform.Find(p) != null;
                    bool resolvesFromRigMedium = rigMedium != null && (string.IsNullOrEmpty(p) || rigMedium.Find(p) != null);
                    sb.AppendLine($"  curvePath='{pathToTest}'  resolvesFromInstanceRoot={resolvesFromInstanceRoot}  resolvesFromRigMedium={resolvesFromRigMedium}");
                }
            }
            else
            {
                sb.AppendLine("  Walking_B clip not found!");
            }

            Object.DestroyImmediate(instance);
            Write(sb);
        }

        private static void DumpImporter(StringBuilder sb, string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            sb.AppendLine($"--- ModelImporter: {fbxPath} ---");
            if (importer == null) { sb.AppendLine("  NOT FOUND"); return; }
            sb.AppendLine($"  animationType={importer.animationType}");
            sb.AppendLine($"  avatarSetup={importer.avatarSetup}");
            sb.AppendLine($"  sourceAvatar={importer.sourceAvatar}");
            sb.AppendLine($"  importAnimation={importer.importAnimation}");
        }

        private static void DumpHierarchy(Transform t, string prefix, StringBuilder sb)
        {
            string path = string.IsNullOrEmpty(prefix) ? t.name : prefix + "/" + t.name;
            sb.AppendLine($"  {path}");
            foreach (Transform child in t)
            {
                DumpHierarchy(child, path == t.name && prefix == "" ? "" : path, sb);
            }
        }

        // Simpler recursive dump with correct relative path accumulation.
        private static string GetPath(Transform root, Transform target)
        {
            if (target == root) return "";
            var stack = new System.Collections.Generic.List<string>();
            var cur = target;
            while (cur != null && cur != root)
            {
                stack.Add(cur.name);
                cur = cur.parent;
            }
            stack.Reverse();
            return string.Join("/", stack);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void DumpClipBindings(StringBuilder sb, string fbxPath)
        {
            sb.AppendLine($"=== Clip curve bindings: {fbxPath} ===");
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().ToArray();
            foreach (var clip in clips)
            {
                if (clip.name == "__preview__" || clip.name.Contains("Preview")) continue;
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var objBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                var uniquePaths = bindings.Select(b => b.path).Concat(objBindings.Select(b => b.path)).Distinct().OrderBy(p => p).ToArray();
                sb.AppendLine($"  Clip '{clip.name}' length={clip.length:F2} curveCount={bindings.Length} uniquePathCount={uniquePaths.Length}");
                foreach (var p in uniquePaths.Take(6))
                {
                    sb.AppendLine($"    path='{p}'");
                }
                if (uniquePaths.Length > 6) sb.AppendLine($"    ... ({uniquePaths.Length - 6} more)");
            }
            sb.AppendLine();
        }

        private static void Write(StringBuilder sb)
        {
            var path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "KayKitAnimDiagnosticReport.txt");
            System.IO.File.WriteAllText(path, sb.ToString());
            Debug.Log($"[KayKitAnimDiagnostic] Wrote report to {path}");
            AssetDatabase.Refresh();
        }
    }
}
