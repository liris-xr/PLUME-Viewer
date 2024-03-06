using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;
using UnityEngine.Rendering;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class QualitySettingsPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            if (rawSample.Payload is QualitySettingsUpdate qualitySettingsUpdate)
            {
                QualitySettings.renderPipeline =
                    ctx.GetOrDefaultAssetByIdentifier<RenderPipelineAsset>(qualitySettingsUpdate.RenderPipelineAssetId);
            }
        }
    }
}