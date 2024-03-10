using System.Linq;
using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class LightPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case LightCreate lightCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<Light>(lightCreate.Id);
                    break;
                }
                case LightDestroy lightDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(lightDestroy.Id);
                    break;
                }
                case LightUpdate lightUpdate:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdate.Id);

                    if (lightUpdate.HasEnabled)
                    {
                        replayLight.enabled = lightUpdate.Enabled;
                    }

                    if (lightUpdate.HasType)
                    {
                        replayLight.type = lightUpdate.Type.ToEngineType();
                    }

                    if (lightUpdate.HasRange)
                    {
                        replayLight.range = lightUpdate.Range;
                    }

                    if (lightUpdate.Color != null)
                    {
                        replayLight.color = lightUpdate.Color.ToEngineType();
                    }

                    if (lightUpdate.HasColorTemperature)
                    {
                        replayLight.colorTemperature = lightUpdate.ColorTemperature;
                    }

                    if (lightUpdate.HasUseColorTemperature)
                    {
                        replayLight.useColorTemperature = lightUpdate.UseColorTemperature;
                    }

                    if (lightUpdate.HasIntensity)
                    {
                        replayLight.intensity = lightUpdate.Intensity;
                    }

                    if (lightUpdate.HasBounceIntensity)
                    {
                        replayLight.bounceIntensity = lightUpdate.BounceIntensity;
                    }

                    if (lightUpdate.HasShape)
                    {
                        replayLight.shape = lightUpdate.Shape.ToEngineType();
                    }

                    if (lightUpdate.HasSpotAngle)
                    {
                        replayLight.spotAngle = lightUpdate.SpotAngle;
                    }

                    if (lightUpdate.HasInnerSpotAngle)
                    {
                        replayLight.innerSpotAngle = lightUpdate.InnerSpotAngle;
                    }

                    if (lightUpdate.HasShadows)
                    {
                        replayLight.shadows = lightUpdate.Shadows.ToEngineType();
                    }

                    if (lightUpdate.HasShadowBias)
                    {
                        replayLight.shadowBias = lightUpdate.ShadowBias;
                    }

                    if (lightUpdate.HasShadowResolution)
                    {
                        replayLight.shadowResolution = lightUpdate.ShadowResolution.ToEngineType();
                    }

                    if (lightUpdate.HasShadowCustomResolution)
                    {
                        replayLight.shadowCustomResolution = lightUpdate.ShadowCustomResolution;
                    }

                    if (lightUpdate.HasShadowStrength)
                    {
                        replayLight.shadowStrength = lightUpdate.ShadowStrength;
                    }

                    if (lightUpdate.HasShadowNearPlane)
                    {
                        replayLight.shadowNearPlane = lightUpdate.ShadowNearPlane;
                    }

                    if (lightUpdate.HasShadowNormalBias)
                    {
                        replayLight.shadowNormalBias = lightUpdate.ShadowNormalBias;
                    }

                    if (lightUpdate.ShadowMatrixOverride != null)
                    {
                        replayLight.shadowMatrixOverride = lightUpdate.ShadowMatrixOverride.ToEngineType();
                    }

                    if (lightUpdate.LayerShadowCullDistances != null)
                    {
                        replayLight.layerShadowCullDistances = lightUpdate.LayerShadowCullDistances.Distances.ToArray();
                    }

                    if (lightUpdate.HasLightShadowCasterMode)
                    {
                        replayLight.lightShadowCasterMode = lightUpdate.LightShadowCasterMode.ToEngineType();
                    }

                    if (lightUpdate.HasUseShadowMatrixOverride)
                    {
                        replayLight.useShadowMatrixOverride = lightUpdate.UseShadowMatrixOverride;
                    }

                    if (lightUpdate.HasUseViewFrustumForShadowCasterCull)
                    {
                        replayLight.useViewFrustumForShadowCasterCull = lightUpdate.UseViewFrustumForShadowCasterCull;
                    }

                    if (lightUpdate.CookieId != null)
                    {
                        replayLight.cookie = ctx.GetOrDefaultAssetByIdentifier<Texture>(lightUpdate.CookieId);
                        ctx.TryAddAssetIdentifierCorrespondence(lightUpdate.CookieId, replayLight.cookie);
                    }

                    if (lightUpdate.HasCookieSize)
                    {
                        replayLight.cookieSize = lightUpdate.CookieSize;
                    }

                    if (lightUpdate.FlareId != null)
                    {
                        replayLight.flare = ctx.GetOrDefaultAssetByIdentifier<Flare>(lightUpdate.FlareId);
                        ctx.TryAddAssetIdentifierCorrespondence(lightUpdate.FlareId, replayLight.flare);
                    }

                    if (lightUpdate.HasUseBoundingSphereOverride)
                    {
                        replayLight.useBoundingSphereOverride = lightUpdate.UseBoundingSphereOverride;
                    }

                    if (lightUpdate.HasCullingMask)
                    {
                        replayLight.cullingMask = lightUpdate.CullingMask;
                    }

                    if (lightUpdate.HasRenderingLayerMask)
                    {
                        replayLight.renderingLayerMask = lightUpdate.RenderingLayerMask;
                    }

                    break;
                }
            }
        }
    }
}