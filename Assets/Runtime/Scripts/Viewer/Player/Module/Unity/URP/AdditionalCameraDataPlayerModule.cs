using PLUME.Sample;
using PLUME.Sample.Unity.URP;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PLUME.Viewer.Player.Module.Unity.URP
{
    public class AdditionalCameraDataPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case AdditionalCameraDataCreate camDataCreate:
                    ctx.GetOrCreateComponentByIdentifier<UniversalAdditionalCameraData>(camDataCreate.Id);
                    break;
                case AdditionalCameraDataUpdate camDataUpdate:
                    var camData = ctx.GetOrCreateComponentByIdentifier<UniversalAdditionalCameraData>(camDataUpdate.Id);
                    camData.renderShadows = camDataUpdate.RenderShadows;
                    camData.requiresDepthOption = camDataUpdate.RequiresDepthOption.ToEngineType();
                    camData.requiresColorOption = camDataUpdate.RequiresColorOption.ToEngineType();
                    camData.renderType = camDataUpdate.RenderType.ToEngineType();
                    camData.requiresDepthTexture = camDataUpdate.RequiresDepthTexture;
                    camData.requiresColorTexture = camDataUpdate.RequiresColorTexture;
                    camData.volumeLayerMask = camDataUpdate.VolumeLayerMask;
                    camData.volumeTrigger = ctx.GetOrCreateTransformByIdentifier(camDataUpdate.VolumeTriggerId);
                    camData.renderPostProcessing = camDataUpdate.RenderPostProcessing;
                    camData.antialiasing = camDataUpdate.Antialiasing.ToEngineType();
                    camData.antialiasingQuality = camDataUpdate.AntialiasingQuality.ToEngineType();
                    camData.stopNaN = camDataUpdate.StopNan;
                    camData.dithering = camDataUpdate.Dithering;
                    camData.allowXRRendering = camDataUpdate.AllowXrRendering;
                    break;
            }
        }
    }
}