using UnityEngine;

namespace DisasterReady.POI
{
    /// <summary>Rotates a world-space UI label to always face the active camera.</summary>
    public class BillboardLabel : MonoBehaviour
    {
        private Camera _cam;

        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);
        }
    }
}
