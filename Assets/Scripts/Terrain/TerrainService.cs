using UnityEngine;

namespace DisasterReady.Terrain
{
    /// <summary>
    /// Small service locator so gameplay/UI code can reach "the active terrain data
    /// provider" without holding a direct scene reference. Swapping the procedural
    /// provider for a DEM-backed one only requires that new provider to call
    /// Register() the same way.
    /// </summary>
    public static class TerrainService
    {
        public static ITerrainDataProvider Active { get; private set; }

        public static void Register(ITerrainDataProvider provider)
        {
            Active = provider;
        }

        public static void Unregister(ITerrainDataProvider provider)
        {
            if (Active == provider) Active = null;
        }

        public static bool TryGetSample(Vector3 worldPosition, out TerrainInfoSample sample)
        {
            if (Active == null)
            {
                sample = TerrainInfoSample.Invalid;
                return false;
            }
            sample = Active.Sample(worldPosition);
            return true;
        }

        public static Vector3 GetHighestPointWorldPosition()
        {
            return Active != null ? Active.HighestPointWorldPosition : Vector3.zero;
        }
    }
}
