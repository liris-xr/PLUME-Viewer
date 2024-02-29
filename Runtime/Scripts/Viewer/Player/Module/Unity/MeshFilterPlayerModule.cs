using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME
{
    public class MeshFilterPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case MeshFilterCreate meshFilterCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<MeshFilter>(meshFilterCreate.Id);
                    break;
                }
                case MeshFilterDestroy meshFilterDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(meshFilterDestroy.Id);
                    break;
                }
                case MeshFilterUpdate meshFilterUpdate:
                {
                    var meshFilter = ctx.GetOrCreateComponentByIdentifier<MeshFilter>(meshFilterUpdate.Id);

                    if (meshFilterUpdate.MeshId != null)
                    {
                        meshFilter.sharedMesh = ctx.GetOrDefaultAssetByIdentifier<Mesh>(meshFilterUpdate.MeshId);
                        ctx.TryAddAssetIdentifierCorrespondence(meshFilterUpdate.MeshId, meshFilter.sharedMesh);
                    }

                    break;
                }
            }
        }
    }
}