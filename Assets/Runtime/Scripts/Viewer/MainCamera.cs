#define USE_INPUT_SYSTEM
using System;
using System.Linq;
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

        public void FixedUpdate()
        {
            var mainCamera = Camera.main;

            if (mainCamera != _followedMainCamera)
            {
                if (mainCamera != null)
                {
                    // Main camera changed, copy all of its properties except the viewer-owned
                    // output settings (CopyFrom would otherwise clobber them with the recorded
                    // camera's values, e.g. resetting the preview to Display 1 and its clear flags).
                    var prevTargetTexture = _camera.targetTexture;
                    var prevTargetDisplay = _camera.targetDisplay;
                    var prevClearFlags = _camera.clearFlags;
                    var prevBackgroundColor = _camera.backgroundColor;
                    _camera.CopyFrom(mainCamera);
                    _camera.targetTexture = prevTargetTexture;
                    _camera.targetDisplay = prevTargetDisplay;
                    _camera.clearFlags = prevClearFlags;
                    _camera.backgroundColor = prevBackgroundColor;
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