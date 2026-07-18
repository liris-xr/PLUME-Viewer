using System.Linq;
using PLUME.Sample;
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
                    ctx.GetOrCreateComponentByIdentifier<SkinnedMeshRenderer>(skinnedMeshRendererCreate.Component);
                    break;
                }
                case SkinnedMeshRendererDestroy skinnedMeshRendererDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(skinnedMeshRendererDestroy.Component);
                    break;
                }
                case SkinnedMeshRendererUpdate skinnedMeshRendererUpdate:
                {
                    var skinnedMeshRenderer =
                        ctx.GetOrCreateComponentByIdentifier<SkinnedMeshRenderer>(skinnedMeshRendererUpdate.Component);

                    if (skinnedMeshRendererUpdate.RootBone != null)
                    {
                        skinnedMeshRenderer.rootBone =
                            ctx.GetOrCreateTransformByIdentifier(skinnedMeshRendererUpdate.RootBone);
                    }

                    if (skinnedMeshRendererUpdate.Bones != null)
                    {
                        skinnedMeshRenderer.bones = skinnedMeshRendererUpdate.Bones.Ids
                            .Select(ctx.GetOrCreateTransformByIdentifier)
                            .ToArray();
                    }

                    if (skinnedMeshRendererUpdate.Mesh != null)
                    {
                        skinnedMeshRenderer.sharedMesh =
                            ctx.GetOrDefaultAssetByIdentifier<Mesh>(skinnedMeshRendererUpdate.Mesh);
                        ctx.TryAddAssetIdentifierCorrespondence(skinnedMeshRendererUpdate.Mesh,
                            skinnedMeshRenderer.sharedMesh);
                    }

                    break;
                }
                case SkinnedMeshRendererBlendShapeUpdate skinnedMeshRendererBlendShapeUpdate:
                {
                    var skinnedMeshRenderer =
                        ctx.GetOrCreateComponentByIdentifier<SkinnedMeshRenderer>(
                            skinnedMeshRendererBlendShapeUpdate.Component);

                    var sharedMesh = skinnedMeshRenderer.sharedMesh;

                    if (sharedMesh == null)
                        break;

                    // Weights are packed in shape index order (0..blendShapeCount-1); the index is implicit.
                    var weights = skinnedMeshRendererBlendShapeUpdate.Weights;
                    var count = Mathf.Min(weights.Count, sharedMesh.blendShapeCount);
                    for (var i = 0; i < count; i++)
                    {
                        skinnedMeshRenderer.SetBlendShapeWeight(i, weights[i]);
                    }

                    break;
                }
            }
        }
    }
}