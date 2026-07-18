using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using PhysicsMaterial = UnityEngine.PhysicsMaterial;
#else
using PhysicsMaterial = UnityEngine.PhysicMaterial;
#endif

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
                    ctx.GetOrCreateComponentByIdentifier<TerrainCollider>(terrainColliderCreate.Component);
                    break;
                }
                case TerrainColliderDestroy terrainColliderDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(terrainColliderDestroy.Component);
                    break;
                }
                case TerrainColliderUpdate terrainColliderUpdate:
                {
                    var terrainCollider =
                        ctx.GetOrCreateComponentByIdentifier<TerrainCollider>(terrainColliderUpdate.Component);

                    if (terrainColliderUpdate.HasEnabled)
                    {
                        terrainCollider.enabled = terrainColliderUpdate.Enabled;
                    }

                    if (terrainColliderUpdate.TerrainData != null)
                    {
                        var terrainData =
                            ctx.GetOrDefaultAssetByIdentifier<TerrainData>(terrainColliderUpdate.TerrainData);
                        terrainCollider.terrainData = terrainData;
                    }

                    if (terrainColliderUpdate.Material != null)
                    {
                        var material =
                            ctx.GetOrDefaultAssetByIdentifier<PhysicsMaterial>(terrainColliderUpdate.Material);
                        terrainCollider.sharedMaterial = material;
                    }

                    break;
                }
            }
        }
    }
}