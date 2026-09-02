using UnityEngine;

namespace DisasterReady.Terrain
{
    /// <summary>
    /// Abstraction over "where does terrain data come from".
    ///
    /// PROTOTYPE: implemented today by <see cref="ProceduralTerrainDataProvider"/>,
    /// which reads a procedurally generated Unity Terrain.
    ///
    /// PRODUCTION PATH: a future `DemTerrainDataProvider` can implement this
    /// same interface backed by a real heightmap/DEM raster (e.g. for
    /// Aizawl, Mizoram) without any changes to gameplay or UI code - everything
    /// in this vertical slice talks to terrain exclusively through this
    /// interface via <see cref="TerrainService"/>.
    /// </summary>
    public interface ITerrainDataProvider
    {
        /// <summary>World-space Y (meters) of the terrain surface below the given XZ position.</summary>
        float GetElevation(Vector3 worldPosition);

        /// <summary>Slope of the terrain surface, in degrees, at the given XZ position.</summary>
        float GetSlope(Vector3 worldPosition);

        /// <summary>Coarse LOW/MEDIUM/HIGH bucket for the given XZ position.</summary>
        TerrainCategory GetCategory(Vector3 worldPosition);

        /// <summary>Convenience: elevation + slope + category in one query.</summary>
        TerrainInfoSample Sample(Vector3 worldPosition);

        /// <summary>World-space position of the highest point on the generated terrain.</summary>
        Vector3 HighestPointWorldPosition { get; }

        /// <summary>Lowest elevation present anywhere on the terrain, meters.</summary>
        float MinElevation { get; }

        /// <summary>Highest elevation present anywhere on the terrain, meters.</summary>
        float MaxElevation { get; }

        /// <summary>Human readable label shown in the terrain-info panel, e.g. "DEMONSTRATION TERRAIN - PROTOTYPE DATA".</summary>
        string DataSourceLabel { get; }
    }
}
