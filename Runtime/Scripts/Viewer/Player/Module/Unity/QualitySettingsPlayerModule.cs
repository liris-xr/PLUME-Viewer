using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;
using UnityEngine.Rendering;

namespace PLUME
{
    public class QualitySettingsPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            if (sample.Payload is QualitySettingsUpdate qualitySettingsUpdate)
            {
                QualitySettings.renderPipeline =
                    ctx.GetOrDefaultAssetByIdentifier<RenderPipelineAsset>(qualitySettingsUpdate.RenderPipelineAssetId);
            }
        }
    }
}