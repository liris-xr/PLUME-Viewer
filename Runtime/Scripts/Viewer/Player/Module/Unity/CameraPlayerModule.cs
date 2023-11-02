using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME
{
    public class CameraPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case CameraCreate cameraCreate:
                    ctx.GetOrCreateComponentByIdentifier<Camera>(cameraCreate.Id);
                    break;
                case CameraUpdate cameraUpdate:
                    var cam = ctx.GetOrCreateComponentByIdentifier<Camera>(cameraUpdate.Id);
                    cam.nearClipPlane = cameraUpdate.NearClipPlane;
                    cam.farClipPlane = cameraUpdate.FarClipPlane;
                    cam.fieldOfView = cameraUpdate.FieldOfView;
                    cam.renderingPath = cameraUpdate.RenderingPath.ToEngineType();
                    cam.allowHDR = cameraUpdate.AllowHdr;
                    cam.allowMSAA = cameraUpdate.AllowMsaa;
                    cam.allowDynamicResolution = cameraUpdate.AllowDynamicResolution;
                    cam.forceIntoRenderTexture = cameraUpdate.ForceIntoRenderTexture;
                    cam.orthographicSize = cameraUpdate.OrthographicSize;
                    cam.orthographic = cameraUpdate.Orthographic;
                    cam.opaqueSortMode = cameraUpdate.OpaqueSortMode.ToEngineType();
                    cam.transparencySortMode = cameraUpdate.TransparencySortMode.ToEngineType();
                    cam.transparencySortAxis = cameraUpdate.TransparencySortAxis.ToEngineType();
                    cam.depth = cameraUpdate.Depth;
                    cam.aspect = cameraUpdate.Aspect;
                    cam.cullingMask = cameraUpdate.CullingMask;
                    cam.eventMask = cameraUpdate.EventMask;
                    cam.layerCullSpherical = cameraUpdate.LayerCullSpherical;
                    cam.cameraType = cameraUpdate.CameraType.ToEngineType();
                    cam.layerCullDistances = cameraUpdate.LayerCullDistances.ToEngineType();
                    cam.useOcclusionCulling = cameraUpdate.UseOcclusionCulling;
                    cam.cullingMatrix = cameraUpdate.CullingMatrix.ToEngineType();
                    cam.backgroundColor = cameraUpdate.BackgroundColor.ToEngineType();
                    cam.clearFlags = cameraUpdate.ClearFlags.ToEngineType();
                    cam.depthTextureMode = cameraUpdate.DepthTextureMode.ToEngineType();
                    cam.clearStencilAfterLightingPass = cameraUpdate.ClearStencilAfterLightingPass;
                    cam.usePhysicalProperties = cameraUpdate.UsePhysicalProperties;
                    cam.sensorSize = cameraUpdate.SensorSize.ToEngineType();
                    cam.lensShift = cameraUpdate.LensShift.ToEngineType();
                    cam.focalLength = cameraUpdate.FocalLength;
                    cam.gateFit = cameraUpdate.GateFit.ToEngineType();
                    cam.rect = cameraUpdate.Rect.ToEngineType();
                    cam.pixelRect = cameraUpdate.PixelRect.ToEngineType();
                    
                    // TODO: support render textures living outside of assets (dynamic RT)
                    // cam.targetTexture = cameraUpdate.TargetTextureId;
                    
                    cam.targetDisplay = cameraUpdate.TargetDisplay;
                    cam.worldToCameraMatrix = cameraUpdate.WorldToCameraMatrix.ToEngineType();
                    cam.projectionMatrix = cameraUpdate.ProjectionMatrix.ToEngineType();
                    cam.nonJitteredProjectionMatrix = cameraUpdate.NonJitteredProjectionMatrix.ToEngineType();
                    cam.useJitteredProjectionMatrixForTransparentRendering = cameraUpdate.UseJitteredProjectionMatrixForTransparentRendering;
                    // cam.scene = cameraUpdate.SceneIdx;
                    cam.stereoSeparation = cameraUpdate.StereoSeparation;
                    cam.stereoConvergence = cameraUpdate.StereoConvergence;
                    cam.stereoTargetEye = cameraUpdate.StereoTargetEye.ToEngineType();
                    break;
            }
        }
    }
}