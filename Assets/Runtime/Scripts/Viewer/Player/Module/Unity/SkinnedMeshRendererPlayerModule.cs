using System.Linq;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class SkinnedMeshRendererPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case SkinnedMeshRendererCreate skinnedMeshRendererCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<SkinnedMeshRenderer>(skinnedMeshRendererCreate.Id);
                    break;
                }
                case SkinnedMeshRendererDestroy skinnedMeshRendererDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(skinnedMeshRendererDestroy.Id);
                    break;
                }
                case SkinnedMeshRendererUpdate skinnedMeshRendererUpdate:
                {
                    var skinnedMeshRenderer =
                        ctx.GetOrCreateComponentByIdentifier<SkinnedMeshRenderer>(skinnedMeshRendererUpdate.Id);

                    if (skinnedMeshRendererUpdate.RootBoneId != null)
                        skinnedMeshRenderer.rootBone =
                            ctx.GetOrCreateTransformByIdentifier(skinnedMeshRendererUpdate.RootBoneId);

                    if (skinnedMeshRendererUpdate.Bones != null)
                        skinnedMeshRenderer.bones = skinnedMeshRendererUpdate.Bones.Ids
                            .Select(ctx.GetOrCreateTransformByIdentifier)
                            .ToArray();

                    if (skinnedMeshRendererUpdate.MeshId != null)
                    {
                        skinnedMeshRenderer.sharedMesh =
                            ctx.GetOrDefaultAssetByIdentifier<Mesh>(skinnedMeshRendererUpdate.MeshId);
                        ctx.TryAddAssetIdentifierCorrespondence(skinnedMeshRendererUpdate.MeshId,
                            skinnedMeshRenderer.sharedMesh);
                    }

                    break;
                }
            }
        }
    }
}