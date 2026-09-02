namespace DisasterReady.Terrain
{
    /// <summary>
    /// A single point-in-time read of terrain conditions at a world position.
    /// This is the data contract the UI consumes - it is deliberately decoupled
    /// from *how* the numbers were produced (procedural noise today, a real
    /// DEM raster tomorrow).
    /// </summary>
    public readonly struct TerrainInfoSample
    {
        public readonly float ElevationMeters;
        public readonly float SlopeDegrees;
        public readonly TerrainCategory Category;
        public readonly bool IsValid;

        public TerrainInfoSample(float elevationMeters, float slopeDegrees, TerrainCategory category, bool isValid = true)
        {
            ElevationMeters = elevationMeters;
            SlopeDegrees = slopeDegrees;
            Category = category;
            IsValid = isValid;
        }

        public static TerrainInfoSample Invalid => new TerrainInfoSample(0f, 0f, TerrainCategory.Low, false);
    }
}
