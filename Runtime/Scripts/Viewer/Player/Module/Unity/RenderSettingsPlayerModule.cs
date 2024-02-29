using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME
{
    public class RenderSettingsPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            if (sample.Payload is RenderSettingsUpdate renderSettingsUpdate)
            {
                RenderSettings.skybox = ctx.GetOrDefaultAssetByIdentifier<Material>(renderSettingsUpdate.SkyboxId);
                ctx.TryAddAssetIdentifierCorrespondence(renderSettingsUpdate.SkyboxId, RenderSettings.skybox);

                RenderSettings.ambientEquatorColor = renderSettingsUpdate.AmbientEquatorColor.ToEngineType();
                RenderSettings.ambientGroundColor = renderSettingsUpdate.AmbientGroundColor.ToEngineType();
                RenderSettings.ambientIntensity = renderSettingsUpdate.AmbientIntensity;
                RenderSettings.ambientLight = renderSettingsUpdate.AmbientLight.ToEngineType();
                RenderSettings.ambientMode = renderSettingsUpdate.AmbientMode.ToEngineType();
                RenderSettings.ambientProbe = renderSettingsUpdate.AmbientProbe.ToEngineType();
                RenderSettings.ambientSkyColor = renderSettingsUpdate.AmbientSkyColor.ToEngineType();
#if UNITY_2022_1_OR_NEWER
                RenderSettings.customReflectionTexture =
                    ctx.GetOrDefaultAssetByIdentifier<Texture>(renderSettingsUpdate.CustomReflectionId);
                ctx.TryAddAssetIdentifierCorrespondence(renderSettingsUpdate.CustomReflectionId,
                    RenderSettings.customReflectionTexture);

#else
                RenderSettings.customReflection =
                    ctx.GetOrDefaultAssetByIdentifier<Texture>(renderSettingsUpdate.CustomReflectionId);
                ctx.TryAddAssetIdentifierCorrespondence(renderSettingsUpdate.CustomReflectionId, RenderSettings.customReflection);
#endif
                RenderSettings.defaultReflectionMode = renderSettingsUpdate.DefaultReflectionMode.ToEngineType();
                RenderSettings.defaultReflectionResolution = renderSettingsUpdate.DefaultReflectionResolution;
                RenderSettings.flareFadeSpeed = renderSettingsUpdate.FlareFadeSpeed;
                RenderSettings.flareStrength = renderSettingsUpdate.FlareStrength;
                RenderSettings.fog = renderSettingsUpdate.Fog;
                RenderSettings.fogColor = renderSettingsUpdate.FogColor.ToEngineType();
                RenderSettings.fogDensity = renderSettingsUpdate.FogDensity;
                RenderSettings.fogEndDistance = renderSettingsUpdate.FogEndDistance;
                RenderSettings.fogMode = renderSettingsUpdate.FogMode.ToEngineType();
                RenderSettings.fogStartDistance = renderSettingsUpdate.FogStartDistance;
                RenderSettings.haloStrength = renderSettingsUpdate.HaloStrength;
                RenderSettings.reflectionBounces = renderSettingsUpdate.ReflectionBounces;
                RenderSettings.reflectionIntensity = renderSettingsUpdate.ReflectionIntensity;
                RenderSettings.subtractiveShadowColor = renderSettingsUpdate.SubtractiveShadowColor.ToEngineType();
                RenderSettings.sun = ctx.GetOrCreateComponentByIdentifier<Light>(renderSettingsUpdate.SunId);
            }
        }
    }
}