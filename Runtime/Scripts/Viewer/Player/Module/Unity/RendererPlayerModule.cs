using System.Linq;
using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class RendererPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            if (sample.Payload is RendererUpdate rendererUpdate)
            {
                var r = ctx.GetOrCreateComponentByIdentifier<Renderer>(rendererUpdate.Id);

                if (rendererUpdate.HasEnabled)
                {
                    r.enabled = rendererUpdate.Enabled;
                }

                if (rendererUpdate.LocalBounds != null)
                {
                    r.localBounds = rendererUpdate.LocalBounds.ToEngineType();
                }

                if (rendererUpdate.Materials != null)
                {
                    var materials = rendererUpdate.Materials;
                    r.sharedMaterials = materials.Ids.Select(ctx.GetOrDefaultAssetByIdentifier<Material>).ToArray();

                    for (var materialIdx = 0; materialIdx < r.sharedMaterials.Length; ++materialIdx)
                    {
                        ctx.TryAddAssetIdentifierCorrespondence(materials.Ids[materialIdx],
                            r.sharedMaterials[materialIdx]);
                    }
                }

                if (rendererUpdate.HasLightmapIndex)
                {
                    r.lightmapIndex = rendererUpdate.LightmapIndex;
                }

                if (rendererUpdate.LightmapScaleOffset != null)
                {
                    r.lightmapScaleOffset = rendererUpdate.LightmapScaleOffset.ToEngineType();
                }

                if (rendererUpdate.HasRealtimeLightmapIndex)
                {
                    r.realtimeLightmapIndex = rendererUpdate.RealtimeLightmapIndex;
                }

                if (rendererUpdate.RealtimeLightmapScaleOffset != null)
                {
                    r.realtimeLightmapScaleOffset = rendererUpdate.RealtimeLightmapScaleOffset.ToEngineType();
                }
            }
        }
    }
}