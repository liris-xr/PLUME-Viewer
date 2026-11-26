using PLUME.Sample.Unity.Settings;
using PLUME.Viewer.Analysis;
using UnityEngine;
using UnityEngine.Rendering;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class QualitySettingsPlayerModule : PlayerModule
    {
        // Restored on destroy: in the editor, a render pipeline switched in play mode stays switched afterwards.
        private RenderPipelineAsset _viewerRenderPipeline;
        private bool _renderPipelineReplaced;

        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            if (rawSample.Payload is QualitySettingsUpdate qualitySettingsUpdate)
            {
                // While a heatmap owns the render pipeline (switched to URP), do not let the record's replayed
                // pipeline reset it back to HDRP.
                if (HeatmapPipelineSwitcher.IsActive)
                    return;

                if (!_renderPipelineReplaced)
                {
                    _viewerRenderPipeline = QualitySettings.renderPipeline;
                    _renderPipelineReplaced = true;
                }

                QualitySettings.renderPipeline =
                    ctx.GetOrDefaultAssetByIdentifier<RenderPipelineAsset>(qualitySettingsUpdate.RenderPipelineAsset);
            }
        }

        private void OnDestroy()
        {
            if (_renderPipelineReplaced && !HeatmapPipelineSwitcher.IsActive)
                QualitySettings.renderPipeline = _viewerRenderPipeline;
        }
    }
}