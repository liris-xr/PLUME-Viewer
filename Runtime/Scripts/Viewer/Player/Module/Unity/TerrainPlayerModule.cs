using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME
{
    public class TerrainPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case TerrainCreate terrainCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<Terrain>(terrainCreate.Id);
                    break;
                }
                case TerrainUpdateEnabled terrainUpdateEnabled:
                {
                    var terrain = ctx.GetOrCreateComponentByIdentifier<Terrain>(terrainUpdateEnabled.Id);
                    terrain.enabled = terrainUpdateEnabled.Enabled;
                    break;
                }
                case TerrainUpdate terrainUpdate:
                {
                    var terrain = ctx.GetOrCreateComponentByIdentifier<Terrain>(terrainUpdate.Id);
                    var terrainData = ctx.GetOrDefaultAssetByIdentifier<TerrainData>(terrainUpdate.TerrainDataId);
                    var materialTemplate =
                        ctx.GetOrDefaultAssetByIdentifier<Material>(terrainUpdate.MaterialTemplateId);
                    terrain.terrainData = terrainData;
                    terrain.treeDistance = terrainUpdate.TreeDistance;
                    terrain.treeBillboardDistance = terrainUpdate.TreeBillboardDistance;
                    terrain.treeCrossFadeLength = terrainUpdate.TreeCrossFadeLength;
                    terrain.treeMaximumFullLODCount = terrainUpdate.TreeMaximumFullLodCount;
                    terrain.detailObjectDistance = terrainUpdate.DetailObjectDistance;
                    terrain.detailObjectDensity = terrainUpdate.DetailObjectDensity;
                    terrain.heightmapPixelError = terrainUpdate.HeightmapPixelError;
                    terrain.heightmapMaximumLOD = terrainUpdate.HeightmapMaximumLod;
                    terrain.basemapDistance = terrainUpdate.BasemapDistance;
                    terrain.lightmapIndex = terrainUpdate.LightmapIndex;
                    terrain.realtimeLightmapIndex = terrainUpdate.RealtimeLightmapIndex;
                    terrain.lightmapScaleOffset = terrainUpdate.LightmapScaleOffset.ToEngineType();
                    terrain.realtimeLightmapScaleOffset = terrainUpdate.RealtimeLightmapScaleOffset.ToEngineType();
                    terrain.keepUnusedRenderingResources = terrainUpdate.KeepUnusedRenderingResources;
                    terrain.shadowCastingMode = terrainUpdate.ShadowCastingMode.ToEngineType();
                    terrain.reflectionProbeUsage = terrainUpdate.ReflectionProbeUsage.ToEngineType();
                    terrain.materialTemplate = materialTemplate;
                    terrain.drawHeightmap = terrainUpdate.DrawHeightmap;
                    terrain.allowAutoConnect = terrainUpdate.AllowAutoConnect;
                    terrain.groupingID = terrainUpdate.GroupingId;
                    terrain.drawInstanced = terrainUpdate.DrawInstanced;
                    terrain.drawTreesAndFoliage = terrainUpdate.DrawTreesAndFoliage;
                    terrain.patchBoundsMultiplier = terrainUpdate.PatchBoundsMultiplier.ToEngineType();
                    terrain.treeLODBiasMultiplier = terrainUpdate.TreeLodBiasMultiplier;
                    terrain.collectDetailPatches = terrainUpdate.CollectDetailPatches;
                    break;
                }
            }
        }
    }
}