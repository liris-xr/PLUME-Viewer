using System.Linq;
using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME
{
    public class MeshRendererPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case MeshRendererCreate meshRendererCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<MeshRenderer>(meshRendererCreate.Id);
                    break;
                }
                case MeshRendererDestroy meshRendererDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(meshRendererDestroy.Id);
                    break;
                }
                case MeshRendererUpdate meshRendererUpdate:
                {
                    var meshRenderer = ctx.GetOrCreateComponentByIdentifier<MeshRenderer>(meshRendererUpdate.Id);

                    if (meshRendererUpdate.HasEnabled)
                    {
                        meshRenderer.enabled = meshRendererUpdate.Enabled;
                    }

                    if (meshRendererUpdate.Materials != null)
                    {
                        meshRenderer.sharedMaterials = meshRendererUpdate.Materials.Ids
                            .Select(ctx.GetOrDefaultAssetByIdentifier<Material>).ToArray();
                    }

                    if (meshRendererUpdate.HasLightmapIndex)
                    {
                        meshRenderer.lightmapIndex = meshRendererUpdate.LightmapIndex;
                    }

                    if (meshRendererUpdate.LightmapScaleOffset != null)
                    {
                        meshRenderer.lightmapScaleOffset = meshRendererUpdate.LightmapScaleOffset.ToEngineType();
                    }

                    if (meshRendererUpdate.HasRealtimeLightmapIndex)
                    {
                        meshRenderer.realtimeLightmapIndex = meshRendererUpdate.RealtimeLightmapIndex;
                    }

                    if (meshRendererUpdate.RealtimeLightmapScaleOffset != null)
                    {
                        meshRenderer.realtimeLightmapScaleOffset =
                            meshRendererUpdate.RealtimeLightmapScaleOffset.ToEngineType();
                    }

                    break;
                }
            }
        }
    }
}