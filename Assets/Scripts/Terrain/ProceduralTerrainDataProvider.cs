using UnityEngine;

namespace DisasterReady.Terrain
{
    /// <summary>
    /// Runtime ITerrainDataProvider backed by a real Unity Terrain component whose
    /// heightmap was baked from an IHeightSource (see TerrainGenerator).
    ///
    /// This is the piece that gets swapped for a DEM-backed provider in production -
    /// everything else (missions, UI, player) only ever talks to
    /// <see cref="ITerrainDataProvider"/> via <see cref="TerrainService"/>.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Terrain))]
    public class ProceduralTerrainDataProvider : MonoBehaviour, ITerrainDataProvider
    {
        [Header("Elevation category thresholds (0..1 of Min..Max elevation)")]
        [Range(0f, 1f)] public float LowMediumThreshold = 0.35f;
        [Range(0f, 1f)] public float MediumHighThreshold = 0.68f;

        [Header("Diagnostics label shown in the Terrain Info panel")]
        public string DemonstrationLabel = "DEMONSTRATION TERRAIN — PROTOTYPE DATA";

        private UnityEngine.Terrain _terrain;
        private TerrainData _data;
        private float _minElevation;
        private float _maxElevation;
        private Vector3 _highestPointWorld;
        private bool _boundsCalculated;

        public string DataSourceLabel => DemonstrationLabel;
        public float MinElevation => _minElevation;
        public float MaxElevation => _maxElevation;
        public Vector3 HighestPointWorldPosition => _highestPointWorld;

        private void Awake()
        {
            _terrain = GetComponent<UnityEngine.Terrain>();
            _data = _terrain.terrainData;
            CalculateBounds();
            TerrainService.Register(this);
        }

        private void OnDestroy()
        {
            TerrainService.Unregister(this);
        }

        private void CalculateBounds()
        {
            int res = _data.heightmapResolution;
            float min = float.MaxValue;
            float max = float.MinValue;
            var heights = _data.GetHeights(0, 0, res, res);
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float h = heights[z, x] * _data.size.y;
                    if (h < min) min = h;
                    if (h > max) max = h;
                }
            }
            _minElevation = min;
            _maxElevation = max;

            Vector3 local = TerrainGenerator.FindHighestPointLocal(_data);
            _highestPointWorld = transform.TransformPoint(local);
            _boundsCalculated = true;
        }

        public float GetElevation(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - transform.position;
            float nx = Mathf.Clamp01(local.x / _data.size.x);
            float nz = Mathf.Clamp01(local.z / _data.size.z);
            return _data.GetInterpolatedHeight(nx, nz);
        }

        public float GetSlope(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - transform.position;
            float nx = Mathf.Clamp01(local.x / _data.size.x);
            float nz = Mathf.Clamp01(local.z / _data.size.z);
            Vector3 normal = _data.GetInterpolatedNormal(nx, nz);
            return Vector3.Angle(normal, Vector3.up);
        }

        public TerrainCategory GetCategory(Vector3 worldPosition)
        {
            if (!_boundsCalculated || Mathf.Approximately(_maxElevation, _minElevation))
                return TerrainCategory.Low;

            float elevation = GetElevation(worldPosition);
            float t = Mathf.InverseLerp(_minElevation, _maxElevation, elevation);
            if (t < LowMediumThreshold) return TerrainCategory.Low;
            if (t < MediumHighThreshold) return TerrainCategory.Medium;
            return TerrainCategory.High;
        }

        public TerrainInfoSample Sample(Vector3 worldPosition)
        {
            return new TerrainInfoSample(GetElevation(worldPosition), GetSlope(worldPosition), GetCategory(worldPosition));
        }
    }
}
