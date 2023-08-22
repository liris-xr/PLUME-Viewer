using System.Linq;
using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME
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
                case SkinnedMeshRendererUpdateEnabled skinnedMeshRendererUpdateEnabled:
                {
                    var skinnedMeshRenderer =
                        ctx.GetOrCreateComponentByIdentifier<SkinnedMeshRenderer>(skinnedMeshRendererUpdateEnabled.Id);
                    skinnedMeshRenderer.enabled = skinnedMeshRendererUpdateEnabled.Enabled;
                    break;
                }
                case SkinnedMeshRendererUpdateBones skinnedMeshRendererUpdateBones:
                {
                    var skinnedMeshRenderer =
                        ctx.GetOrCreateComponentByIdentifier<SkinnedMeshRenderer>(skinnedMeshRendererUpdateBones.Id);
                    skinnedMeshRenderer.rootBone = ctx.GetOrCreateTransformByIdentifier(skinnedMeshRendererUpdateBones.RootBoneId);
                    skinnedMeshRenderer.bones = skinnedMeshRendererUpdateBones.BonesIds.Select(ctx.GetOrCreateTransformByIdentifier).ToArray();
                    break;
                }
                case SkinnedMeshRendererUpdateBounds skinnedMeshRendererUpdateBounds:
                {
                    var skinnedMeshRenderer =
                        ctx.GetOrCreateComponentByIdentifier<SkinnedMeshRenderer>(skinnedMeshRendererUpdateBounds.Id);
                    skinnedMeshRenderer.localBounds = skinnedMeshRendererUpdateBounds.LocalBounds.ToEngineType();
                    break;
                }
                case SkinnedMeshRendererUpdateMesh skinnedMeshRendererUpdateSharedMesh:
                {
                    var skinnedMeshRenderer =
                        ctx.GetOrCreateComponentByIdentifier<SkinnedMeshRenderer>(skinnedMeshRendererUpdateSharedMesh.Id);
                    skinnedMeshRenderer.sharedMesh = ctx.GetOrDefaultAssetByIdentifier<Mesh>(skinnedMeshRendererUpdateSharedMesh.MeshId);
                    ctx.TryAddAssetIdentifierCorrespondence(skinnedMeshRendererUpdateSharedMesh.MeshId, skinnedMeshRenderer.sharedMesh);
                    break;
                }
                case SkinnedMeshRendererUpdateMaterials skinnedMeshRendererUpdateSharedMaterials:
                {
                    var skinnedMeshRenderer =
                        ctx.GetOrCreateComponentByIdentifier<SkinnedMeshRenderer>(skinnedMeshRendererUpdateSharedMaterials.Id);
                    skinnedMeshRenderer.sharedMaterials = skinnedMeshRendererUpdateSharedMaterials.MaterialsIds.Select(ctx.GetOrDefaultAssetByIdentifier<Material>).ToArray();
                    
                    for (var materialIdx = 0; materialIdx < skinnedMeshRenderer.sharedMaterials.Length; ++materialIdx)
                    {
                        ctx.TryAddAssetIdentifierCorrespondence(skinnedMeshRendererUpdateSharedMaterials.MaterialsIds[materialIdx], skinnedMeshRenderer.sharedMaterials[materialIdx]);
                    }
                    break;
                }
            }
        }
    }
}