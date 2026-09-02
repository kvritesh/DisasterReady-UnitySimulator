using UnityEngine;
using UnityEngine.UI;
using DisasterReady.Terrain;

namespace DisasterReady.UI
{
    /// <summary>
    /// Updates the small terrain-info readout each frame from the player's current
    /// position. Talks only to ITerrainDataProvider via TerrainService, so it will
    /// work unchanged once a real DEM-backed provider is registered.
    /// </summary>
    public class TerrainInfoPanelController : MonoBehaviour
    {
        public Transform TrackedTarget;
        public Text ElevationText;
        public Text SlopeText;
        public Text CategoryText;
        public Text SourceLabelText;
        public float UpdateInterval = 0.2f;

        private float _timer;

        private void Update()
        {
            if (TrackedTarget == null) return;
            _timer += Time.deltaTime;
            if (_timer < UpdateInterval) return;
            _timer = 0f;

            if (!TerrainService.TryGetSample(TrackedTarget.position, out var sample) || !sample.IsValid)
                return;

            if (ElevationText != null) ElevationText.text = $"Elevation: {sample.ElevationMeters:0} m";
            if (SlopeText != null) SlopeText.text = $"Slope: {sample.SlopeDegrees:0}°";
            if (CategoryText != null) CategoryText.text = $"Terrain: {sample.Category.ToString().ToUpperInvariant()}";
            if (SourceLabelText != null && TerrainService.Active != null) SourceLabelText.text = TerrainService.Active.DataSourceLabel;
        }
    }
}
