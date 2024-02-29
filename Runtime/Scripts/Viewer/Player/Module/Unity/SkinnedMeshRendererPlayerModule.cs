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
                case SkinnedMeshRendererUpdate skinnedMeshRendererUpdate:
                {
                    var skinnedMeshRenderer =
                        ctx.GetOrCreateComponentByIdentifier<SkinnedMeshRenderer>(skinnedMeshRendererUpdate.Id);

                    if (skinnedMeshRendererUpdate.HasEnabled)
                    {
                        skinnedMeshRenderer.enabled = skinnedMeshRendererUpdate.Enabled;
                    }

                    if (skinnedMeshRendererUpdate.Bones != null)
                    {
                        var bones = skinnedMeshRendererUpdate.Bones;
                        skinnedMeshRenderer.rootBone = ctx.GetOrCreateTransformByIdentifier(bones.RootBoneId);
                        skinnedMeshRenderer.bones =
                            bones.BonesIds.Select(ctx.GetOrCreateTransformByIdentifier).ToArray();
                    }

                    if (skinnedMeshRendererUpdate.LocalBounds != null)
                    {
                        skinnedMeshRenderer.localBounds = skinnedMeshRendererUpdate.LocalBounds.ToEngineType();
                    }

                    if (skinnedMeshRendererUpdate.MeshId != null)
                    {
                        skinnedMeshRenderer.sharedMesh =
                            ctx.GetOrDefaultAssetByIdentifier<Mesh>(skinnedMeshRendererUpdate.MeshId);
                        ctx.TryAddAssetIdentifierCorrespondence(skinnedMeshRendererUpdate.MeshId,
                            skinnedMeshRenderer.sharedMesh);
                    }

                    if (skinnedMeshRendererUpdate.Materials != null)
                    {
                        var materials = skinnedMeshRendererUpdate.Materials;
                        skinnedMeshRenderer.sharedMaterials =
                            materials.Ids.Select(ctx.GetOrDefaultAssetByIdentifier<Material>).ToArray();

                        for (var materialIdx = 0;
                             materialIdx < skinnedMeshRenderer.sharedMaterials.Length;
                             ++materialIdx)
                        {
                            ctx.TryAddAssetIdentifierCorrespondence(materials.Ids[materialIdx],
                                skinnedMeshRenderer.sharedMaterials[materialIdx]);
                        }
                    }

                    if (skinnedMeshRendererUpdate.HasLightmapIndex)
                    {
                        skinnedMeshRenderer.lightmapIndex = skinnedMeshRendererUpdate.LightmapIndex;
                    }

                    if (skinnedMeshRendererUpdate.LightmapScaleOffset != null)
                    {
                        skinnedMeshRenderer.lightmapScaleOffset =
                            skinnedMeshRendererUpdate.LightmapScaleOffset.ToEngineType();
                    }

                    if (skinnedMeshRendererUpdate.HasRealtimeLightmapIndex)
                    {
                        skinnedMeshRenderer.realtimeLightmapIndex = skinnedMeshRendererUpdate.RealtimeLightmapIndex;
                    }

                    if (skinnedMeshRendererUpdate.RealtimeLightmapScaleOffset != null)
                    {
                        skinnedMeshRenderer.realtimeLightmapScaleOffset =
                            skinnedMeshRendererUpdate.RealtimeLightmapScaleOffset.ToEngineType();
                    }

                    break;
                }
            }
        }
    }
}