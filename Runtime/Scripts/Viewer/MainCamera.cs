#define USE_INPUT_SYSTEM
using System.Linq;
using PLUME.Viewer;
using UnityEngine;

namespace PLUME
{
    public class MainCamera : PreviewCamera
    {
        public override Camera GetCamera()
        {
            var ctx = PlayerContext.GetActiveContext();
            var mainCamera = ctx?.GetAllComponents().FirstOrDefault(c => c is Camera && ctx.GetGameObjectTag(c.gameObject.GetInstanceID()) == "MainCamera") as Camera;
            return mainCamera;
        }

        public void FixedUpdate()
        {
            var cam = GetCamera();
            
            if (cam != null)
            {
                cam.targetTexture = Player.Instance.GetCurrentPreviewCamera() == this ? PreviewRenderTexture : null;
                cam.enabled = Player.Instance.GetCurrentPreviewCamera() == this;
            }
        }
        
        public override void SetEnabled(bool enabled)
        {
        }

        public override PreviewCameraType GetCameraType()
        {
            return PreviewCameraType.Main;
        }
    }
}