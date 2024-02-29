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
                case TerrainDestroy terrainDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(terrainDestroy.Id);
                    break;
                }
                case TerrainUpdate terrainUpdate:
                {
                    var terrain = ctx.GetOrCreateComponentByIdentifier<Terrain>(terrainUpdate.Id);

                    if (terrainUpdate.HasEnabled)
                    {
                        terrain.enabled = terrainUpdate.Enabled;
                    }

                    if (terrainUpdate.TerrainDataId != null)
                    {
                        var terrainData = ctx.GetOrDefaultAssetByIdentifier<TerrainData>(terrainUpdate.TerrainDataId);
                        terrain.terrainData = terrainData;
                    }

                    if (terrainUpdate.MaterialTemplateId != null)
                    {
                        var materialTemplate =
                            ctx.GetOrDefaultAssetByIdentifier<Material>(terrainUpdate.MaterialTemplateId);
                        terrain.materialTemplate = materialTemplate;
                    }

                    if (terrainUpdate.HasTreeDistance)
                    {
                        terrain.treeDistance = terrainUpdate.TreeDistance;
                    }

                    if (terrainUpdate.HasTreeBillboardDistance)
                    {
                        terrain.treeBillboardDistance = terrainUpdate.TreeBillboardDistance;
                    }

                    if (terrainUpdate.HasTreeCrossFadeLength)
                    {
                        terrain.treeCrossFadeLength = terrainUpdate.TreeCrossFadeLength;
                    }

                    if (terrainUpdate.HasTreeMaximumFullLodCount)
                    {
                        terrain.treeMaximumFullLODCount = terrainUpdate.TreeMaximumFullLodCount;
                    }

                    if (terrainUpdate.HasDetailObjectDistance)
                    {
                        terrain.detailObjectDistance = terrainUpdate.DetailObjectDistance;
                    }

                    if (terrainUpdate.HasDetailObjectDensity)
                    {
                        terrain.detailObjectDensity = terrainUpdate.DetailObjectDensity;
                    }

                    if (terrainUpdate.HasHeightmapPixelError)
                    {
                        terrain.heightmapPixelError = terrainUpdate.HeightmapPixelError;
                    }

                    if (terrainUpdate.HasHeightmapMaximumLod)
                    {
                        terrain.heightmapMaximumLOD = terrainUpdate.HeightmapMaximumLod;
                    }

                    if (terrainUpdate.HasBasemapDistance)
                    {
                        terrain.basemapDistance = terrainUpdate.BasemapDistance;
                    }

                    if (terrainUpdate.HasLightmapIndex)
                    {
                        terrain.lightmapIndex = terrainUpdate.LightmapIndex;
                    }

                    if (terrainUpdate.HasRealtimeLightmapIndex)
                    {
                        terrain.realtimeLightmapIndex = terrainUpdate.RealtimeLightmapIndex;
                    }

                    if (terrainUpdate.LightmapScaleOffset != null)
                    {
                        terrain.lightmapScaleOffset = terrainUpdate.LightmapScaleOffset.ToEngineType();
                    }

                    if (terrainUpdate.RealtimeLightmapScaleOffset != null)
                    {
                        terrain.realtimeLightmapScaleOffset = terrainUpdate.RealtimeLightmapScaleOffset.ToEngineType();
                    }

                    if (terrainUpdate.HasKeepUnusedRenderingResources)
                    {
                        terrain.keepUnusedRenderingResources = terrainUpdate.KeepUnusedRenderingResources;
                    }

                    if (terrainUpdate.HasShadowCastingMode)
                    {
                        terrain.shadowCastingMode = terrainUpdate.ShadowCastingMode.ToEngineType();
                    }

                    if (terrainUpdate.HasReflectionProbeUsage)
                    {
                        terrain.reflectionProbeUsage = terrainUpdate.ReflectionProbeUsage.ToEngineType();
                    }

                    if (terrainUpdate.HasDrawHeightmap)
                    {
                        terrain.drawHeightmap = terrainUpdate.DrawHeightmap;
                    }

                    if (terrainUpdate.HasAllowAutoConnect)
                    {
                        terrain.allowAutoConnect = terrainUpdate.AllowAutoConnect;
                    }

                    if (terrainUpdate.HasGroupingId)
                    {
                        terrain.groupingID = terrainUpdate.GroupingId;
                    }

                    if (terrainUpdate.HasDrawInstanced)
                    {
                        terrain.drawInstanced = terrainUpdate.DrawInstanced;
                    }

                    if (terrainUpdate.HasDrawTreesAndFoliage)
                    {
                        terrain.drawTreesAndFoliage = terrainUpdate.DrawTreesAndFoliage;
                    }

                    if (terrainUpdate.PatchBoundsMultiplier != null)
                    {
                        terrain.patchBoundsMultiplier = terrainUpdate.PatchBoundsMultiplier.ToEngineType();
                    }

                    if (terrainUpdate.HasTreeLodBiasMultiplier)
                    {
                        terrain.treeLODBiasMultiplier = terrainUpdate.TreeLodBiasMultiplier;
                    }

                    if (terrainUpdate.HasCollectDetailPatches)
                    {
                        terrain.collectDetailPatches = terrainUpdate.CollectDetailPatches;
                    }

                    break;
                }
            }
        }
    }
}