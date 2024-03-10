using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class TerrainColliderPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case TerrainColliderCreate terrainColliderCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<TerrainCollider>(terrainColliderCreate.Id);
                    break;
                }
                case TerrainColliderDestroy terrainColliderDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(terrainColliderDestroy.Id);
                    break;
                }
                case TerrainColliderUpdate terrainColliderUpdate:
                {
                    var terrainCollider =
                        ctx.GetOrCreateComponentByIdentifier<TerrainCollider>(terrainColliderUpdate.Id);

                    if (terrainColliderUpdate.HasEnabled)
                    {
                        terrainCollider.enabled = terrainColliderUpdate.Enabled;
                    }

                    if (terrainColliderUpdate.TerrainDataId != null)
                    {
                        var terrainData =
                            ctx.GetOrDefaultAssetByIdentifier<TerrainData>(terrainColliderUpdate.TerrainDataId);
                        terrainCollider.terrainData = terrainData;
                    }

                    if (terrainColliderUpdate.MaterialId != null)
                    {
                        var material =
                            ctx.GetOrDefaultAssetByIdentifier<PhysicMaterial>(terrainColliderUpdate.MaterialId);
                        terrainCollider.sharedMaterial = material;
                    }

                    break;
                }
            }
        }
    }
}