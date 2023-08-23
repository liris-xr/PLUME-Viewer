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
                case MeshRendererUpdateEnabled meshRendererUpdateEnabled:
                {
                    var meshRenderer = ctx.GetOrCreateComponentByIdentifier<MeshRenderer>(meshRendererUpdateEnabled.Id);
                    meshRenderer.enabled = meshRendererUpdateEnabled.Enabled;
                    break;
                }
                case MeshRendererUpdateMaterials meshRendererUpdateInstanceMaterials:
                {
                    var meshRenderer =
                        ctx.GetOrCreateComponentByIdentifier<MeshRenderer>(meshRendererUpdateInstanceMaterials.Id);
                    meshRenderer.sharedMaterials = meshRendererUpdateInstanceMaterials.MaterialsIds
                        .Select(ctx.GetOrDefaultAssetByIdentifier<Material>).ToArray();

                    for (var materialIdx = 0; materialIdx < meshRenderer.sharedMaterials.Length; ++materialIdx)
                    {
                        ctx.TryAddAssetIdentifierCorrespondence(
                            meshRendererUpdateInstanceMaterials.MaterialsIds[materialIdx],
                            meshRenderer.sharedMaterials[materialIdx]);
                    }

                    break;
                }
            }
        }
    }
}