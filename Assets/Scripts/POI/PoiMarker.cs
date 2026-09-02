using UnityEngine;

namespace DisasterReady.POI
{
    /// <summary>Tags a GameObject as a point of interest and gives it a small bobbing icon for readability.</summary>
    public class PoiMarker : MonoBehaviour
    {
        public PoiType Type;
        public string Label = "POI";
        public Transform IconPivot;
        public float BobAmplitude = 0.25f;
        public float BobSpeed = 1.5f;

        private Vector3 _iconStartLocalPos;

        private void Start()
        {
            if (IconPivot != null) _iconStartLocalPos = IconPivot.localPosition;
        }

        private void Update()
        {
            if (IconPivot == null) return;
            float y = Mathf.Sin(Time.time * BobSpeed) * BobAmplitude;
            IconPivot.localPosition = _iconStartLocalPos + new Vector3(0f, y, 0f);
            IconPivot.Rotate(Vector3.up, 40f * Time.deltaTime, Space.World);
        }
    }
}
