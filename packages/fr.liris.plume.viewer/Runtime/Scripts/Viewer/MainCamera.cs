#define USE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
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

        private Camera[] _camerasBuffer = new Camera[16];

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
            var mainCamera = FindRecordedMainCamera();

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

        /// <summary>
        /// The camera that was the main camera of the recorded application: the first enabled camera tagged
        /// MainCamera (in hierarchy order) in the scene that was active in the recording. Falls back to such a camera in
        /// the other replay scenes (e.g. an XR rig in DontDestroyOnLoad). Unlike Camera.main, this ignores Unity's active
        /// scene, which the player changes to create objects, and the scenes of other contexts (e.g. a heatmap being
        /// generated).
        /// </summary>
        private Camera FindRecordedMainCamera()
        {
            var player = Player.Player.Instance;
            var ctx = player == null ? null : player.GetMainPlayerContext();

            if (ctx == null)
                return null;

            if (Camera.allCamerasCount > _camerasBuffer.Length)
                _camerasBuffer = new Camera[Mathf.NextPowerOfTwo(Camera.allCamerasCount)];

            var nCameras = Camera.GetAllCameras(_camerasBuffer);
            var activeScene = ctx.GetRecordedActiveScene();

            Camera inActiveScene = null;
            Camera inOtherScene = null;

            for (var i = 0; i < nCameras; ++i)
            {
                var candidate = _camerasBuffer[i];
                _camerasBuffer[i] = null;

                if (candidate == null || candidate == _camera || !candidate.CompareTag("MainCamera"))
                    continue;

                var scene = candidate.gameObject.scene;

                if (activeScene.IsValid() && scene == activeScene)
                {
                    if (inActiveScene == null || IsBeforeInHierarchy(candidate.transform, inActiveScene.transform))
                        inActiveScene = candidate;
                }
                else if (ctx.ContainsScene(scene))
                {
                    if (inOtherScene == null || IsBeforeInHierarchy(candidate.transform, inOtherScene.transform))
                        inOtherScene = candidate;
                }
            }

            return inActiveScene != null ? inActiveScene : inOtherScene;
        }

        // Whether a comes before b in a depth-first walk of the hierarchy (scenes are compared by handle).
        private static bool IsBeforeInHierarchy(Transform a, Transform b)
        {
            if (a.gameObject.scene != b.gameObject.scene)
                return a.gameObject.scene.handle < b.gameObject.scene.handle;

            var pathA = GetSiblingIndexPath(a);
            var pathB = GetSiblingIndexPath(b);

            for (var i = 0; i < Mathf.Min(pathA.Count, pathB.Count); ++i)
            {
                if (pathA[i] != pathB[i])
                    return pathA[i] < pathB[i];
            }

            return pathA.Count < pathB.Count;
        }

        private static List<int> GetSiblingIndexPath(Transform t)
        {
            var path = new List<int>();

            for (var current = t; current != null; current = current.parent)
                path.Add(current.GetSiblingIndex());

            path.Reverse();
            return path;
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