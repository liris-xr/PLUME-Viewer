#if HDRP_ENABLED
using PLUME.Sample;
using PLUME.Sample.Unity.HDRP;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace PLUME.Viewer.Player.Module.Unity.HDRP
{
    public class HDAdditionalCameraDataPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case HDAdditionalCameraDataCreate camDataCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<HDAdditionalCameraData>(camDataCreate.Component);
                    break;
                }
                case HDAdditionalCameraDataDestroy camDataDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(camDataDestroy.Component);
                    break;
                }
                case HDAdditionalCameraDataUpdate camDataUpdate:
                {
                    var replayCamData =
                        ctx.GetOrCreateComponentByIdentifier<HDAdditionalCameraData>(camDataUpdate.Component);

                    if (camDataUpdate.HasClearColorMode)
                    {
                        replayCamData.clearColorMode = camDataUpdate.ClearColorMode.ToEngineType();
                    }

                    if (camDataUpdate.BackgroundColorHdr != null)
                    {
                        replayCamData.backgroundColorHDR = camDataUpdate.BackgroundColorHdr.ToEngineType();
                    }

                    if (camDataUpdate.HasClearDepth)
                    {
                        replayCamData.clearDepth = camDataUpdate.ClearDepth;
                    }

                    if (camDataUpdate.HasVolumeLayerMask)
                    {
                        replayCamData.volumeLayerMask = camDataUpdate.VolumeLayerMask;
                    }

                    if (camDataUpdate.VolumeAnchorOverride != null)
                    {
                        replayCamData.volumeAnchorOverride =
                            ctx.GetOrCreateTransformByIdentifier(camDataUpdate.VolumeAnchorOverride);
                    }

                    if (camDataUpdate.HasAntialiasing)
                    {
                        replayCamData.antialiasing = camDataUpdate.Antialiasing.ToEngineType();
                    }

                    if (camDataUpdate.HasSmaaQuality)
                    {
                        replayCamData.SMAAQuality = camDataUpdate.SmaaQuality.ToEngineType();
                    }

                    if (camDataUpdate.HasTaaQuality)
                    {
                        replayCamData.TAAQuality = camDataUpdate.TaaQuality.ToEngineType();
                    }

                    if (camDataUpdate.HasDithering)
                    {
                        replayCamData.dithering = camDataUpdate.Dithering;
                    }

                    if (camDataUpdate.HasStopNans)
                    {
                        replayCamData.stopNaNs = camDataUpdate.StopNans;
                    }

                    if (camDataUpdate.HasCustomRenderingSettings)
                    {
                        replayCamData.customRenderingSettings = camDataUpdate.CustomRenderingSettings;
                    }

                    if (camDataUpdate.HasXrRendering)
                    {
                        replayCamData.xrRendering = camDataUpdate.XrRendering;
                    }

                    // FocusDistance / Iso / ShutterSpeed / Aperture are physical camera properties that live on
                    // UnityEngine.Camera itself (HDAdditionalLightData.physicalParameters is obsolete since
                    // HDRP migrated these to Camera, see HDAdditionalCameraData.cs), not on HDAdditionalCameraData.
                    // HDAdditionalCameraData is always co-located with a Camera on the same GameObject.
                    if (camDataUpdate.HasFocusDistance || camDataUpdate.HasIso ||
                        camDataUpdate.HasShutterSpeed || camDataUpdate.HasAperture)
                    {
                        var go = ctx.GetOrCreateGameObjectByIdentifier(camDataUpdate.Component.GameObject);
                        var cam = go.GetComponent<Camera>();
                        if (cam == null)
                        {
                            cam = go.AddComponent<Camera>();
                        }

                        if (camDataUpdate.HasFocusDistance)
                        {
                            cam.focusDistance = camDataUpdate.FocusDistance;
                        }

                        if (camDataUpdate.HasIso)
                        {
                            cam.iso = camDataUpdate.Iso;
                        }

                        if (camDataUpdate.HasShutterSpeed)
                        {
                            cam.shutterSpeed = camDataUpdate.ShutterSpeed;
                        }

                        if (camDataUpdate.HasAperture)
                        {
                            cam.aperture = camDataUpdate.Aperture;
                        }
                    }

                    break;
                }
            }
        }
    }
}
#endif
