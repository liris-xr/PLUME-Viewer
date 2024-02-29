using System.Linq;
using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class SkinnedMeshRendererPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
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

                    if (skinnedMeshRendererUpdate.Bones != null)
                    {
                        var bones = skinnedMeshRendererUpdate.Bones;
                        skinnedMeshRenderer.rootBone = ctx.GetOrCreateTransformByIdentifier(bones.RootBoneId);
                        skinnedMeshRenderer.bones =
                            bones.BonesIds.Select(ctx.GetOrCreateTransformByIdentifier).ToArray();
                    }

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