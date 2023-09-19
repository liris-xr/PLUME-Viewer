using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME
{
    public class TerrainColliderPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case TerrainColliderCreate terrainColliderCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<TerrainCollider>(terrainColliderCreate.Id);
                    break;
                }
                case TerrainColliderUpdateEnabled terrainColliderUpdateEnabled:
                {
                    var terrainCollider = ctx.GetOrCreateComponentByIdentifier<TerrainCollider>(terrainColliderUpdateEnabled.Id);
                    terrainCollider.enabled = terrainColliderUpdateEnabled.Enabled;
                    break;
                }
                case TerrainColliderUpdate terrainColliderUpdate:
                {
                    var terrainCollider = ctx.GetOrCreateComponentByIdentifier<TerrainCollider>(terrainColliderUpdate.Id);
                    var terrainData = ctx.GetOrDefaultAssetByIdentifier<TerrainData>(terrainColliderUpdate.TerrainDataId);
                    var material = ctx.GetOrDefaultAssetByIdentifier<PhysicMaterial>(terrainColliderUpdate.MaterialId);
                    terrainCollider.terrainData = terrainData;
                    terrainCollider.sharedMaterial = material;
                    break;
                }
            }
        }
    }
}