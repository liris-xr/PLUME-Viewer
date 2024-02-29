using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;
using LightmapData = UnityEngine.LightmapData;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class LightmapsPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case LightmapsUpdate lightmapsUpdate:
                {
                    var lightmaps = new LightmapData[lightmapsUpdate.LightmapsData.Count];

                    for (var i = 0; i < lightmapsUpdate.LightmapsData.Count; ++i)
                    {
                        var payload = lightmapsUpdate.LightmapsData[i];
                        lightmaps[i] = new LightmapData
                        {
                            lightmapColor =
                                ctx.GetOrDefaultAssetByIdentifier<Texture2D>(payload.LightmapColorTextureId),
                            lightmapDir = ctx.GetOrDefaultAssetByIdentifier<Texture2D>(payload.LightmapDirTextureId),
                            shadowMask =
                                ctx.GetOrDefaultAssetByIdentifier<Texture2D>(payload.LightmapShadowMaskTextureId)
                        };
                    }

                    LightmapSettings.lightmapsMode = lightmapsUpdate.LightmapsMode.ToEngineType();
                    LightmapSettings.lightmaps = lightmaps;

                    break;
                }
            }
        }
    }
}