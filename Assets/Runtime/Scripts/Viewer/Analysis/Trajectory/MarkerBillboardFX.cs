using UnityEngine;

namespace PLUME.Viewer.Analysis.Trajectory
{
    public class MarkerBillboardFX : MonoBehaviour
    {
        private Quaternion _originalRotation;
        public new Camera camera;

        private void Start()
        {
            _originalRotation = transform.rotation;
        }

        private void Update()
        {
            if (camera != null)
                transform.rotation = camera.transform.rotation * _originalRotation;
        }
    }
}