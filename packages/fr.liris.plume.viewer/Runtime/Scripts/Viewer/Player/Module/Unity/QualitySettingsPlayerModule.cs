using PLUME.Sample.Unity.Settings;
using PLUME.Viewer.Analysis;
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
                // While a heatmap owns the render pipeline (switched to URP), do not let the record's replayed
                // pipeline reset it back to HDRP.
                if (HeatmapPipelineSwitcher.IsActive)
                    return;

                QualitySettings.renderPipeline =
                    ctx.GetOrDefaultAssetByIdentifier<RenderPipelineAsset>(qualitySettingsUpdate.RenderPipelineAsset);
            }
        }
    }
}