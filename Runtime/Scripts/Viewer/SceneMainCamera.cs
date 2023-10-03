#define USE_INPUT_SYSTEM
using System;
using System.Linq;
using PLUME.Viewer;
using UnityEngine;

namespace PLUME
{
    public class SceneMainCamera : PreviewCamera
    {
        public override Camera GetCamera()
        {
            var ctx = Player.Instance.GetPlayerContext();
            var mainCamera = ctx?.GetAllComponents().FirstOrDefault(c => c is Camera && ctx.GetGameObjectTag(c.gameObject.GetInstanceID()) == "MainCamera") as Camera;

            if (mainCamera != null && mainCamera.targetTexture != Player.Instance.PreviewRenderTexture)
            {
                mainCamera.targetTexture = Player.Instance.PreviewRenderTexture;
            }
            
            return mainCamera;
        }

        public void FixedUpdate()
        {
            var cam = GetCamera();
            if (cam != null)
            {
                cam.enabled = true;
            }
        }

        public override void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
            
            var cam = GetCamera();
            if (cam != null)
            {
                cam.enabled = enabled;
            }
        }

        public override PreviewCameraType GetCameraType()
        {
            return PreviewCameraType.Main;
        }
    }
}