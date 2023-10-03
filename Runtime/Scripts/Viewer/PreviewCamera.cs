using System;
using UnityEngine;

namespace PLUME.Viewer
{
    public abstract class PreviewCamera : MonoBehaviour
    {
        protected RenderTexture PreviewRenderTexture;

        public virtual void SetPreviewRenderTexture(RenderTexture previewTexture)
        {
            PreviewRenderTexture = previewTexture;
        }
        
        public abstract Camera GetCamera();

        public abstract void SetEnabled(bool enabled);

        public abstract PreviewCameraType GetCameraType();
    }

    public enum PreviewCameraType
    {
        Free,
        TopView,
        Main
    }
}