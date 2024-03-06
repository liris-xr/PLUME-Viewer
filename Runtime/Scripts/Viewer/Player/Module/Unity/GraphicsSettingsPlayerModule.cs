using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine.Rendering;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class GraphicsSettingsPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            if (rawSample.Payload is GraphicsSettingsUpdate graphicsSettingsUpdate)
            {
                GraphicsSettings.defaultRenderPipeline =
                    ctx.GetOrDefaultAssetByIdentifier<RenderPipelineAsset>(graphicsSettingsUpdate
                        .DefaultRenderPipelineAssetId);
            }
        }
    }
}