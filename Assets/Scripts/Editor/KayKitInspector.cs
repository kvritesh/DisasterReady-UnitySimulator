using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DisasterReady.EditorTools
{
    /// <summary>
    /// One-off diagnostic dump of the actually-imported KayKit assets, written
    /// so the integration pass never has to guess a filename, clip name, or
    /// mesh size. Writes a plain text report to KayKitInspectionReport.txt at
    /// the project root (outside Assets/, so it never gets reimported) and
    /// also logs a short summary to the Console. Safe to delete once the
    /// KayKit integration is complete - it doesn't touch the scene or any
    /// gameplay code.
    /// </summary>
    internal static class KayKitInspector
    {
        [MenuItem("DisasterReady/KayKit/Dump Asset Report")]
        public static void DumpReport()
        {
            var sb = new StringBuilder();

            DumpCharacterHierarchy(sb, "Assets/KayKit/Adventurers/Characters/fbx/Rogue_Hooded.fbx");
            DumpCharacterHierarchy(sb, "Assets/KayKit/Adventurers/Characters/fbx/Ranger.fbx");

            DumpAnimationClips(sb, "Assets/KayKit/Adventurers/Animations/fbx/Rig_Medium/Rig_Medium_MovementBasic.fbx");
            DumpAnimationClips(sb, "Assets/KayKit/Adventurers/Animations/fbx/Rig_Medium/Rig_Medium_General.fbx");
            DumpAnimationClips(sb, "Assets/KayKit/CharacterAnimations/Animations/fbx/Rig_Medium/Rig_Medium_MovementBasic.fbx");
            DumpAnimationClips(sb, "Assets/KayKit/CharacterAnimations/Animations/fbx/Rig_Medium/Rig_Medium_MovementAdvanced.fbx");
            DumpAnimationClips(sb, "Assets/KayKit/CharacterAnimations/Animations/fbx/Rig_Medium/Rig_Medium_General.fbx");

            foreach (var letter in new[] { "A", "B", "C", "D", "E", "F", "G", "H" })
            {
                DumpMeshBounds(sb, $"Assets/KayKit/CityBuilder/Assets/fbx (unity)/building_{letter}.fbx");
                DumpMeshBounds(sb, $"Assets/KayKit/CityBuilder/Assets/fbx (unity)/building_{letter}_withoutBase.fbx");
            }

            DumpMeshBounds(sb, "Assets/KayKit/Forest/Assets/fbx(unity)/Tree_1_A_Color1.fbx");
            DumpMeshBounds(sb, "Assets/KayKit/Forest/Assets/fbx(unity)/Tree_2_A_Color1.fbx");
            DumpMeshBounds(sb, "Assets/KayKit/Forest/Assets/fbx(unity)/Tree_3_A_Color1.fbx");
            DumpMeshBounds(sb, "Assets/KayKit/Forest/Assets/fbx(unity)/Tree_4_A_Color1.fbx");
            DumpMeshBounds(sb, "Assets/KayKit/Forest/Assets/fbx(unity)/Tree_Bare_1_A_Color1.fbx");
            DumpMeshBounds(sb, "Assets/KayKit/Forest/Assets/fbx(unity)/Rock_1_A_Color1.fbx");
            DumpMeshBounds(sb, "Assets/KayKit/Forest/Assets/fbx(unity)/Rock_2_A_Color1.fbx");
            DumpMeshBounds(sb, "Assets/KayKit/Forest/Assets/fbx(unity)/Rock_3_A_Color1.fbx");
            DumpMeshBounds(sb, "Assets/KayKit/Forest/Assets/fbx(unity)/Bush_1_A_Color1.fbx");

            // Materials/shaders used by the imported FBX materials, to check
            // URP/WebGL compatibility (Phase 1 requirement 4).
            DumpMaterials(sb, "Assets/KayKit/Adventurers/Characters/fbx/Rogue_Hooded.fbx");
            DumpMaterials(sb, "Assets/KayKit/CityBuilder/Assets/fbx (unity)/building_A.fbx");
            DumpMaterials(sb, "Assets/KayKit/Forest/Assets/fbx(unity)/Tree_1_A_Color1.fbx");

            string outPath = Path.Combine(Application.dataPath, "..", "KayKitInspectionReport.txt");
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[KayKitInspector] Report written to {outPath} ({sb.Length} chars)");
        }

        private static void DumpCharacterHierarchy(StringBuilder sb, string path)
        {
            sb.AppendLine($"=== HIERARCHY: {path} ===");
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) { sb.AppendLine("  (NOT FOUND)"); return; }
            void Walk(Transform t, int depth)
            {
                var renderer = t.GetComponent<SkinnedMeshRenderer>() ?? (Component)t.GetComponent<MeshRenderer>();
                string tag = renderer != null ? "  [has renderer]" : "";
                sb.AppendLine(new string(' ', depth * 2) + t.name + tag);
                foreach (Transform child in t) Walk(child, depth + 1);
            }
            Walk(go.transform, 1);
            var bounds = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds);
            if (bounds.Any())
            {
                var b = bounds.Aggregate((a, c) => { a.Encapsulate(c); return a; });
                sb.AppendLine($"  Combined renderer bounds (local import space): size={b.size} center={b.center}");
            }
            sb.AppendLine();
        }

        private static void DumpAnimationClips(StringBuilder sb, string path)
        {
            sb.AppendLine($"=== ANIM CLIPS: {path} ===");
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0) { sb.AppendLine("  (NOT FOUND or empty)"); sb.AppendLine(); return; }
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    sb.AppendLine($"  clip: {clip.name}  length={clip.length:F2}s  frameRate={clip.frameRate}  isLooping={clip.isLooping}");
                }
            }
            sb.AppendLine();
        }

        private static void DumpMeshBounds(StringBuilder sb, string path)
        {
            sb.AppendLine($"=== MESH: {path} ===");
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) { sb.AppendLine("  (NOT FOUND)"); sb.AppendLine(); return; }
            var filters = go.GetComponentsInChildren<MeshFilter>();
            if (filters.Length == 0) { sb.AppendLine("  (no MeshFilter found)"); sb.AppendLine(); return; }
            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds; // local to that mesh's own transform
                var worldCenter = mf.transform.TransformPoint(b.center);
                var worldSize = Vector3.Scale(b.size, mf.transform.lossyScale);
                sb.AppendLine($"  part '{mf.name}' localBounds size={b.size} | worldSize~={worldSize} worldCenter~={worldCenter}");
            }
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var wb = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) wb.Encapsulate(renderers[i].bounds);
                sb.AppendLine($"  TOTAL combined renderer bounds: size={wb.size} center={wb.center}");
            }
            sb.AppendLine();
        }

        private static void DumpMaterials(StringBuilder sb, string path)
        {
            sb.AppendLine($"=== MATERIALS: {path} ===");
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) { sb.AppendLine("  (NOT FOUND)"); sb.AppendLine(); return; }
            var renderers = go.GetComponentsInChildren<Renderer>();
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var r in renderers)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) { sb.AppendLine("  (null material slot)"); continue; }
                    string key = m.name;
                    if (!seen.Add(key)) continue;
                    string shaderName = m.shader != null ? m.shader.name : "(null shader)";
                    sb.AppendLine($"  material '{m.name}' shader='{shaderName}' mainTexture={(m.mainTexture != null ? m.mainTexture.name : "none")}");
                }
            }
            sb.AppendLine();
        }
    }
}
