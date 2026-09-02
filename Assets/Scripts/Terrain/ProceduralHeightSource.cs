using UnityEngine;

namespace DisasterReady.Terrain
{
    /// <summary>
    /// Deterministic procedural stand-in for real Aizawl, Mizoram DEM data.
    /// Produces two flanking ridgelines with a lower valley running between them,
    /// plus a single unmistakable summit bump used for the "highest point" mission.
    /// All values are normalized 0..1; <see cref="TerrainGenerator"/> scales the
    /// result to world-space meters.
    /// </summary>
    [System.Serializable]
    public class ProceduralHeightSource : IHeightSource
    {
        public int Seed = 1337;
        public float RidgeSharpness = 2.2f;
        public float NoiseStrength = 0.06f;
        public float NoiseScale = 3.2f;

        // Summit bump, in normalized terrain space. Placed on the right-hand ridge.
        public Vector2 SummitPosition01 = new Vector2(0.80f, 0.62f);
        public float SummitRadius01 = 0.16f;
        public float SummitHeightBoost = 0.42f;

        private readonly float _noiseOffsetX;
        private readonly float _noiseOffsetZ;

        public string SourceLabel => "Procedural Prototype Generator";

        public ProceduralHeightSource(int seed)
        {
            Seed = seed;
            var rng = new System.Random(seed);
            _noiseOffsetX = (float)rng.NextDouble() * 1000f;
            _noiseOffsetZ = (float)rng.NextDouble() * 1000f;
        }

        public float GetHeight01(float nx, float nz)
        {
            // Valley runs along Z. X = 0 and X = 1 are the flanking ridgelines,
            // X = 0.5 is the valley floor.
            float distFromCenterX = Mathf.Abs(nx - 0.5f) * 2f; // 0 at center, 1 at edges

            // Two ridges (left + right), each a smooth hill using a shaped falloff.
            float ridgeShape = Mathf.Pow(distFromCenterX, RidgeSharpness);
            float baseHeight = Mathf.Lerp(0.06f, 0.72f, ridgeShape);

            // Gentle undulation along the valley so it doesn't read as a perfectly
            // straight trench, plus a slight secondary cross-ridge for a 3rd hill.
            float valleyWave = Mathf.Sin(nz * Mathf.PI * 1.6f) * 0.035f;
            float crossRidge = Mathf.Exp(-Mathf.Pow((nz - 0.28f) / 0.10f, 2f)) * 0.18f * (1f - distFromCenterX);

            float h = baseHeight + valleyWave + crossRidge;

            // Fractal-ish noise for natural roughness.
            float n1 = Mathf.PerlinNoise(nx * NoiseScale + _noiseOffsetX, nz * NoiseScale + _noiseOffsetZ);
            float n2 = Mathf.PerlinNoise(nx * NoiseScale * 3.1f + _noiseOffsetX, nz * NoiseScale * 3.1f + _noiseOffsetZ);
            h += (n1 - 0.5f) * NoiseStrength + (n2 - 0.5f) * NoiseStrength * 0.4f;

            // Unmistakable summit bump - guarantees a single, clear highest point.
            float summitDist = Vector2.Distance(new Vector2(nx, nz), SummitPosition01) / SummitRadius01;
            if (summitDist < 1f)
            {
                float falloff = Mathf.SmoothStep(1f, 0f, summitDist);
                h += falloff * SummitHeightBoost;
            }

            // Flatten a small pad at the very edges so terrain seams read as ground, not cliffs.
            float edgeFade = Mathf.Min(nx, 1f - nx, nz, 1f - nz);
            if (edgeFade < 0.02f)
            {
                h *= Mathf.Clamp01(edgeFade / 0.02f) * 0.5f + 0.5f;
            }

            return Mathf.Clamp01(h);
        }
    }
}
