using System;
using UnityEngine;

namespace PLUME.Viewer
{
    public abstract class PreviewCamera : MonoBehaviour
    {
        [NonSerialized]
        public RenderTexture PreviewRenderTexture;

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