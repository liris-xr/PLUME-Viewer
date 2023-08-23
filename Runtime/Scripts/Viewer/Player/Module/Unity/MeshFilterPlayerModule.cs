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
                case MeshFilterUpdateMesh meshFilterUpdateMesh:
                {
                    var meshFilter = ctx.GetOrCreateComponentByIdentifier<MeshFilter>(meshFilterUpdateMesh.Id);
                    meshFilter.sharedMesh = ctx.GetOrDefaultAssetByIdentifier<Mesh>(meshFilterUpdateMesh.MeshId);
                    ctx.TryAddAssetIdentifierCorrespondence(meshFilterUpdateMesh.MeshId, meshFilter.sharedMesh);
                    break;
                }
            }
        }
    }
}