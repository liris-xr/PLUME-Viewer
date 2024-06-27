using PLUME.Sample.Unity.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace PLUME.Viewer.Player.Module.Unity
{
    // TODO: this is not replayed so far because player modules are only called for frame data samples
    public class QualitySettingsPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            if (rawSample.Payload is QualitySettingsUpdate qualitySettingsUpdate)
                QualitySettings.renderPipeline =
                    ctx.GetOrDefaultAssetByIdentifier<RenderPipelineAsset>(qualitySettingsUpdate.RenderPipelineAssetId);
        }
    }
}