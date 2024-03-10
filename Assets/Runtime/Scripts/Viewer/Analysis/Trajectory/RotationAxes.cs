using UnityEngine;

namespace PLUME.Viewer.Analysis.Trajectory
{
    [ExecuteInEditMode]
    public class RotationAxes : MonoBehaviour
    {
        private void Update()
        {
            if (transform.hasChanged)
            {
                var lineRenderers = GetComponentsInChildren<LineRenderer>();
                foreach (var lineRenderer in lineRenderers)
                {
                    lineRenderer.startWidth = transform.localScale.x;
                }

                transform.hasChanged = false;
            }
        }
    }
}