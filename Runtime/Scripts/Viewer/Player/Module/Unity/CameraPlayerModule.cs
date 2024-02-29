using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
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
                case CameraDestroy cameraDestroy:
                    ctx.TryDestroyComponentByIdentifier(cameraDestroy.Id);
                    break;
                case CameraUpdate cameraUpdate:
                    var cam = ctx.GetOrCreateComponentByIdentifier<Camera>(cameraUpdate.Id);

                    if (cameraUpdate.HasNearClipPlane)
                        cam.nearClipPlane = cameraUpdate.NearClipPlane;

                    if (cameraUpdate.HasFarClipPlane)
                        cam.farClipPlane = cameraUpdate.FarClipPlane;

                    if (cameraUpdate.HasFieldOfView)
                        cam.fieldOfView = cameraUpdate.FieldOfView;

                    if (cameraUpdate.HasRenderingPath)
                        cam.renderingPath = cameraUpdate.RenderingPath.ToEngineType();

                    if (cameraUpdate.HasAllowHdr)
                        cam.allowHDR = cameraUpdate.AllowHdr;

                    if (cameraUpdate.HasAllowMsaa)
                        cam.allowMSAA = cameraUpdate.AllowMsaa;

                    if (cameraUpdate.HasAllowDynamicResolution)
                        cam.allowDynamicResolution = cameraUpdate.AllowDynamicResolution;

                    if (cameraUpdate.HasForceIntoRenderTexture)
                        cam.forceIntoRenderTexture = cameraUpdate.ForceIntoRenderTexture;

                    if (cameraUpdate.HasOrthographicSize)
                        cam.orthographicSize = cameraUpdate.OrthographicSize;

                    if (cameraUpdate.HasOrthographic)
                        cam.orthographic = cameraUpdate.Orthographic;

                    if (cameraUpdate.HasOpaqueSortMode)
                        cam.opaqueSortMode = cameraUpdate.OpaqueSortMode.ToEngineType();

                    if (cameraUpdate.HasTransparencySortMode)
                        cam.transparencySortMode = cameraUpdate.TransparencySortMode.ToEngineType();

                    if (cameraUpdate.TransparencySortAxis != null)
                        cam.transparencySortAxis = cameraUpdate.TransparencySortAxis.ToEngineType();

                    if (cameraUpdate.HasDepth)
                        cam.depth = cameraUpdate.Depth;

                    if (cameraUpdate.HasAspect)
                        cam.aspect = cameraUpdate.Aspect;

                    if (cameraUpdate.HasCullingMask)
                        cam.cullingMask = cameraUpdate.CullingMask;

                    if (cameraUpdate.HasEventMask)
                        cam.eventMask = cameraUpdate.EventMask;

                    if (cameraUpdate.HasLayerCullSpherical)
                        cam.layerCullSpherical = cameraUpdate.LayerCullSpherical;

                    if (cameraUpdate.HasCameraType)
                        cam.cameraType = cameraUpdate.CameraType.ToEngineType();

                    if (cameraUpdate.LayerCullDistances != null)
                        cam.layerCullDistances = cameraUpdate.LayerCullDistances.Distances.ToEngineType();

                    if (cameraUpdate.HasUseOcclusionCulling)
                        cam.useOcclusionCulling = cameraUpdate.UseOcclusionCulling;

                    if (cameraUpdate.CullingMatrix != null)
                        cam.cullingMatrix = cameraUpdate.CullingMatrix.ToEngineType();

                    if (cameraUpdate.BackgroundColor != null)
                        cam.backgroundColor = cameraUpdate.BackgroundColor.ToEngineType();

                    if (cameraUpdate.HasClearFlags)
                        cam.clearFlags = cameraUpdate.ClearFlags.ToEngineType();

                    if (cameraUpdate.HasDepthTextureMode)
                        cam.depthTextureMode = cameraUpdate.DepthTextureMode.ToEngineType();

                    if (cameraUpdate.HasClearStencilAfterLightingPass)
                        cam.clearStencilAfterLightingPass = cameraUpdate.ClearStencilAfterLightingPass;

                    if (cameraUpdate.HasUsePhysicalProperties)
                        cam.usePhysicalProperties = cameraUpdate.UsePhysicalProperties;

                    if (cameraUpdate.SensorSize != null)
                        cam.sensorSize = cameraUpdate.SensorSize.ToEngineType();

                    if (cameraUpdate.LensShift != null)
                        cam.lensShift = cameraUpdate.LensShift.ToEngineType();

                    if (cameraUpdate.HasFocalLength)
                        cam.focalLength = cameraUpdate.FocalLength;

                    if (cameraUpdate.HasGateFit)
                        cam.gateFit = cameraUpdate.GateFit.ToEngineType();

                    if (cameraUpdate.Rect != null)
                        cam.rect = cameraUpdate.Rect.ToEngineType();

                    if (cameraUpdate.PixelRect != null)
                        cam.pixelRect = cameraUpdate.PixelRect.ToEngineType();

                    if (cameraUpdate.HasTargetDisplay)
                        cam.targetDisplay = cameraUpdate.TargetDisplay;

                    if (cameraUpdate.WorldToCameraMatrix != null)
                        cam.worldToCameraMatrix = cameraUpdate.WorldToCameraMatrix.ToEngineType();

                    if (cameraUpdate.ProjectionMatrix != null)
                        cam.projectionMatrix = cameraUpdate.ProjectionMatrix.ToEngineType();

                    if (cameraUpdate.NonJitteredProjectionMatrix != null)
                        cam.nonJitteredProjectionMatrix = cameraUpdate.NonJitteredProjectionMatrix.ToEngineType();

                    if (cameraUpdate.HasUseJitteredProjectionMatrixForTransparentRendering)
                        cam.useJitteredProjectionMatrixForTransparentRendering =
                            cameraUpdate.UseJitteredProjectionMatrixForTransparentRendering;

                    if (cameraUpdate.HasStereoSeparation)
                        cam.stereoSeparation = cameraUpdate.StereoSeparation;

                    if (cameraUpdate.HasStereoConvergence)
                        cam.stereoConvergence = cameraUpdate.StereoConvergence;

                    if (cameraUpdate.HasStereoTargetEye)
                        cam.stereoTargetEye = cameraUpdate.StereoTargetEye.ToEngineType();

                    break;
            }
        }
    }
}