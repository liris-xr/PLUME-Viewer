#define USE_INPUT_SYSTEM
using PLUME.Viewer.Player;
using UnityEngine;

namespace PLUME.Viewer
{
    [RequireComponent(typeof(Camera))]
    public class MainCamera : PreviewCamera
    {
        // The camera we will copy the settings to.
        private Camera _camera;

        private Camera _followedMainCamera;

        public void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        public override Camera GetCamera()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();

            return _camera;
        }

        private Camera GetMainCamera()
        {
            var ctx = PlayerContext.GetActiveContext();

            if (ctx == null)
                return null;

            foreach (var component in ctx.GetAllComponents())
            {
                if (component == null)
                    continue;

                if (component is Camera c && ctx.GetGameObjectTag(c.gameObject.GetInstanceID()) == "MainCamera")
                    return c;
            }

            return null;
        }

        public void FixedUpdate()
        {
            var mainCamera = GetMainCamera();

            if (mainCamera != _followedMainCamera)
            {
                if (mainCamera != null)
                {
                    // Main camera changed, copy all of its properties except target texture.
                    var prevTargetTexture = _camera.targetTexture;
                    _camera.CopyFrom(mainCamera);
                    _camera.targetTexture = prevTargetTexture;
                }

                _followedMainCamera = mainCamera;
            }
        }

        public void LateUpdate()
        {
            if (_followedMainCamera != null)
            {
                // Copy world transform.
                _followedMainCamera.transform.GetPositionAndRotation(out var position, out var rotation);
                transform.SetPositionAndRotation(position, rotation);
            }
        }

        public override void SetEnabled(bool enabled)
        {
            GetCamera().targetTexture = enabled ? PreviewRenderTexture : null;
            GetCamera().enabled = enabled;
        }

        public override PreviewCameraType GetCameraType()
        {
            return PreviewCameraType.Main;
        }

        public override void ResetView()
        {
        }
    }
}