using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine.Rendering;

namespace PLUME
{
    public class RenderPipelinePlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            if (sample.Payload is RenderPipelineUpdate renderPipelineUpdate)
            {
                GraphicsSettings.renderPipelineAsset =
                    ctx.GetOrDefaultAssetByIdentifier<RenderPipelineAsset>(renderPipelineUpdate.AssetId);
            }
        }
    }
}