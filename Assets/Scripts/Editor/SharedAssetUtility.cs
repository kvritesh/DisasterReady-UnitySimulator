using System.IO;
using UnityEditor;
using UnityEngine;

namespace DisasterReady.EditorTools
{
    /// <summary>Small shared helpers for building materials/textures during scene generation.</summary>
    internal static class SharedAssetUtility
    {
        /// <summary>
        /// AssetDatabase.CreateAsset throws/fails if an asset already exists at
        /// the given path, which makes any unconditional caller of it non-
        /// re-runnable - the very first time this generator is re-run against
        /// its own previous output (e.g. to pick up a scene-generator change),
        /// every such call site hits an already-occupied path and the whole
        /// build aborts before ever reaching EditorSceneManager.SaveScene.
        /// Deleting any pre-existing asset at that path first makes the whole
        /// scene generator safely re-runnable, which is the point of it being
        /// a generator in the first place.
        /// </summary>
        public static void CreateOrReplaceAsset(Object asset, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(asset, path);
        }

        public static Texture2D CreateSolidTexture(Color color, string assetPath, int size = 8)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
            File.WriteAllBytes(assetPath, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        /// <summary>
        /// Like <see cref="CreateSolidTexture"/> but fills the texture with layered
        /// value noise around the base color instead of one flat pixel value, so
        /// terrain (and anything else tiling this at a visible distance) reads as
        /// a textured surface - dirt speckle, grass mottling, rock grain - rather
        /// than a flat placeholder swatch. Deterministic per assetPath (fixed seed)
        /// so re-running the scene generator produces byte-identical output.
        /// Still a tiny, WebGL-safe, zero-external-asset PNG - just 128x128 instead
        /// of 8x8, which is what actually gives the noise room to read at the tile
        /// sizes terrain layers use.
        /// </summary>
        public static Texture2D CreateNoisyTexture(Color baseColor, string assetPath, int size = 128, float variation = 0.09f, int seed = 0)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var rng = new System.Random(seed != 0 ? seed : assetPath.GetHashCode());

            // A handful of random noise-cell offsets at different frequencies,
            // summed together (cheap multi-octave value noise) so the result has
            // both fine speckle and coarser blotchy variation instead of uniform
            // per-pixel static.
            float o1x = (float)rng.NextDouble() * 1000f, o1z = (float)rng.NextDouble() * 1000f;
            float o2x = (float)rng.NextDouble() * 1000f, o2z = (float)rng.NextDouble() * 1000f;
            float o3x = (float)rng.NextDouble() * 1000f, o3z = (float)rng.NextDouble() * 1000f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    float coarse = Mathf.PerlinNoise(o1x + u * 5f, o1z + v * 5f) - 0.5f;
                    float medium = Mathf.PerlinNoise(o2x + u * 16f, o2z + v * 16f) - 0.5f;
                    float fine = Mathf.PerlinNoise(o3x + u * 48f, o3z + v * 48f) - 0.5f;
                    float noise = coarse * 0.55f + medium * 0.32f + fine * 0.13f;

                    float shade = 1f + noise * variation * 2f;
                    var c = new Color(
                        Mathf.Clamp01(baseColor.r * shade),
                        Mathf.Clamp01(baseColor.g * shade),
                        Mathf.Clamp01(baseColor.b * shade),
                        baseColor.a);
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
            File.WriteAllBytes(assetPath, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Shader _litShader;
        public static Shader LitShader => _litShader != null ? _litShader : (_litShader = Shader.Find("Universal Render Pipeline/Lit"));

        /// <summary>
        /// Creates (or reuses) a flat-color URP Lit material saved under
        /// Assets/Art/Materials/{name}.mat. Unlike CreateOrReplaceAsset, this
        /// updates an existing asset IN PLACE rather than deleting and
        /// recreating it. That matters because several call sites in
        /// EnvironmentBuilder invoke this more than once per scene-build pass
        /// with the same material name (e.g. multiple fence runs sharing
        /// "FenceWood") to get one shared material back. Deleting the asset
        /// on the second call would destroy the native Material object that
        /// earlier-built GameObjects already hold a sharedMaterial reference
        /// to, leaving them with a missing material (rendered magenta) even
        /// though nothing else about them is wrong.
        /// </summary>
        public static Material CreateColorMaterial(string name, Color color, float smoothness = 0.15f)
        {
            string path = $"Assets/Art/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(LitShader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != LitShader)
            {
                mat.shader = LitShader;
            }
            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material CreateEmissiveMaterial(string name, Color color, Color emission)
        {
            var mat = CreateColorMaterial(name, color);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return mat;
        }
    }
}
