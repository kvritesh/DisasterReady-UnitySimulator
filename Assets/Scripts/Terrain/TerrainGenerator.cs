using UnityEngine;

namespace DisasterReady.Terrain
{
    /// <summary>
    /// Builds a Unity <see cref="TerrainData"/> heightmap from any <see cref="IHeightSource"/>.
    /// This is the "Mesh/Terrain API" generation step called once (by the editor-time
    /// SceneBuilder) to bake the demo terrain. Because it only depends on the
    /// IHeightSource abstraction, swapping in real DEM data later means writing a
    /// new IHeightSource - this class does not need to change.
    /// </summary>
    public static class TerrainGenerator
    {
        /// <summary>
        /// Fills heightData (resolution x resolution, values 0..1) by sampling source.
        /// </summary>
        public static float[,] BuildHeights(IHeightSource source, int resolution)
        {
            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            {
                float nz = (float)z / (resolution - 1);
                for (int x = 0; x < resolution; x++)
                {
                    float nx = (float)x / (resolution - 1);
                    // Unity's TerrainData heightmap is indexed [y, x] i.e. [z, x].
                    heights[z, x] = source.GetHeight01(nx, nz);
                }
            }
            return heights;
        }

        /// <summary>Applies a generated heightmap to a TerrainData asset in-place.</summary>
        public static void Apply(TerrainData terrainData, IHeightSource source, int resolution, Vector3 size)
        {
            terrainData.heightmapResolution = resolution;
            terrainData.size = size;
            var heights = BuildHeights(source, resolution);
            terrainData.SetHeights(0, 0, heights);
        }

        /// <summary>
        /// Scans the baked heightmap for the single highest sample and returns it in
        /// LOCAL (terrain-relative) world space. Shared by the editor-time scene
        /// builder and the runtime ProceduralTerrainDataProvider so both agree on
        /// exactly the same "highest point" used for Mission 1.
        /// </summary>
        public static Vector3 FindHighestPointLocal(TerrainData terrainData)
        {
            int res = terrainData.heightmapResolution;
            var heights = terrainData.GetHeights(0, 0, res, res);
            float max = float.MinValue;
            int bestX = 0, bestZ = 0;
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float h = heights[z, x];
                    if (h > max) { max = h; bestX = x; bestZ = z; }
                }
            }
            float worldX = ((float)bestX / (res - 1)) * terrainData.size.x;
            float worldZ = ((float)bestZ / (res - 1)) * terrainData.size.z;
            float worldY = max * terrainData.size.y;
            return new Vector3(worldX, worldY, worldZ);
        }
    }
}
