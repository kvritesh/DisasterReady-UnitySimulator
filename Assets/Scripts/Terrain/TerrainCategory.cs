namespace DisasterReady.Terrain
{
    /// <summary>
    /// Coarse elevation bucket used by the HUD/terrain-info panel.
    /// Kept intentionally simple so a future DEM-backed provider can
    /// reuse the same thresholds without changes to the UI layer.
    /// </summary>
    public enum TerrainCategory
    {
        Low,
        Medium,
        High
    }
}
