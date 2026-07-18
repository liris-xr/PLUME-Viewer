using PLUME.Sample.Unity.Settings;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class RenderSettingsPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case RenderSettingsUpdate settingsUpdate:
                {
                    if (settingsUpdate.HasAmbientMode)
                    {
                        RenderSettings.ambientMode = settingsUpdate.AmbientMode.ToEngineType();
                    }
                    
                    if (settingsUpdate.HasFog)
                    {
                        RenderSettings.fog = settingsUpdate.Fog;
                    }
                    
                    if (settingsUpdate.HasFogMode)
                    {
                        RenderSettings.fogMode = settingsUpdate.FogMode.ToEngineType();
                    }
                    
                    if (settingsUpdate.HasDefaultReflectionMode)
                    {
                        RenderSettings.defaultReflectionMode = settingsUpdate.DefaultReflectionMode.ToEngineType();
                    }
                    
                    if (settingsUpdate.HasDefaultReflectionResolution)
                    {
                        RenderSettings.defaultReflectionResolution = settingsUpdate.DefaultReflectionResolution;
                    }

                    if (settingsUpdate.HasReflectionBounces)
                    {
                        RenderSettings.reflectionBounces = settingsUpdate.ReflectionBounces;
                    }

                    if (settingsUpdate.HasReflectionIntensity)
                    {
                        RenderSettings.reflectionIntensity = settingsUpdate.ReflectionIntensity;
                    }

                    if (settingsUpdate.HasHaloStrength)
                    {
                        RenderSettings.haloStrength = settingsUpdate.HaloStrength;
                    }

                    if (settingsUpdate.HasFlareStrength)
                    {
                        RenderSettings.flareStrength = settingsUpdate.FlareStrength;
                    }

                    if (settingsUpdate.HasFlareFadeSpeed)
                    {
                        RenderSettings.flareFadeSpeed = settingsUpdate.FlareFadeSpeed;
                    }
                    
                    if (settingsUpdate.Sun != null)
                    {
                        RenderSettings.sun = ctx.GetOrCreateComponentByIdentifier<Light>(settingsUpdate.Sun);
                    }
                    
                    if (settingsUpdate.FogColor != null)
                    {
                        RenderSettings.fogColor = settingsUpdate.FogColor.ToEngineType();
                    }
                    
                    if (settingsUpdate.HasFogDensity)
                    {
                        RenderSettings.fogDensity = settingsUpdate.FogDensity;
                    }
                    
                    if (settingsUpdate.HasFogStartDistance)
                    {
                        RenderSettings.fogStartDistance = settingsUpdate.FogStartDistance;
                    }

                    if (settingsUpdate.HasFogEndDistance)
                    {
                        RenderSettings.fogEndDistance = settingsUpdate.FogEndDistance;
                    }

                    if (settingsUpdate.AmbientLightColor != null)
                    {
                        RenderSettings.ambientLight = settingsUpdate.AmbientLightColor.ToEngineType();
                    }

                    if (settingsUpdate.AmbientEquatorColor != null)
                    {
                        RenderSettings.ambientEquatorColor = settingsUpdate.AmbientEquatorColor.ToEngineType();
                    }

                    if (settingsUpdate.AmbientGroundColor != null)
                    {
                        RenderSettings.ambientGroundColor = settingsUpdate.AmbientGroundColor.ToEngineType();
                    }

                    if (settingsUpdate.AmbientSkyColor != null)
                    {
                        RenderSettings.ambientSkyColor = settingsUpdate.AmbientSkyColor.ToEngineType();
                    }

                    if (settingsUpdate.HasAmbientIntensity)
                    {
                        RenderSettings.ambientIntensity = settingsUpdate.AmbientIntensity;
                    }

                    if (settingsUpdate.AmbientProbe != null)
                    {
                        RenderSettings.ambientProbe = settingsUpdate.AmbientProbe.ToEngineType();
                    }
                    
                    if (settingsUpdate.CustomReflectionTexture != null)
                    {
                        RenderSettings.customReflectionTexture = ctx.GetOrDefaultAssetByIdentifier<Texture>(settingsUpdate.CustomReflectionTexture);
                    }

                    if (settingsUpdate.SubtractiveShadowColor != null)
                    {
                        RenderSettings.subtractiveShadowColor = settingsUpdate.SubtractiveShadowColor.ToEngineType();
                    }
                    
                    if (settingsUpdate.Skybox != null)
                    {
                        var skybox = ctx.GetOrDefaultAssetByIdentifier<Material>(settingsUpdate.Skybox);
                        RenderSettings.skybox = skybox;
                    }

                    break;
                }
            }
        }
    }
}