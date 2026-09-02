namespace DisasterReady.Terrain
{
    /// <summary>
    /// Pure function contract for "how tall is the terrain at this normalized point".
    /// Swappable seam requested by the brief: <see cref="ProceduralHeightSource"/> is the
    /// prototype implementation; a future implementation could sample a real DEM raster
    /// and return normalized (0..1) heights the exact same way, letting
    /// <see cref="TerrainGenerator"/> stay unchanged.
    /// </summary>
    public interface IHeightSource
    {
        /// <summary>
        /// Returns a normalized height (0..1) for a point given in normalized
        /// terrain-space coordinates (nx, nz each 0..1).
        /// </summary>
        float GetHeight01(float nx, float nz);

        /// <summary>Short label describing where this height data came from.</summary>
        string SourceLabel { get; }
    }
}
