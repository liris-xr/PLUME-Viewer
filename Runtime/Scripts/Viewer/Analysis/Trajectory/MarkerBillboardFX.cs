using UnityEngine;

public class MarkerBillboardFX : MonoBehaviour
{
    public new Camera camera;

    private Quaternion _originalRotation;

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