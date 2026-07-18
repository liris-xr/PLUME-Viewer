using System.Linq;
using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;
using UnityEngine.Rendering;

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
                    ctx.GetOrCreateComponentByIdentifier<Light>(lightCreate.Component);
                    break;
                }
                case LightDestroy lightDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(lightDestroy.Component);
                    break;
                }
                case LightUpdate lightUpdate:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdate.Component);

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

#if UNITY_6000_0_OR_NEWER
                    if (lightUpdate.HasLightUnit)
                    {
                        replayLight.lightUnit = lightUpdate.LightUnit.ToEngineType();
                    }
#endif

                    if (lightUpdate.HasShape)
                    {
#if UNITY_6000_0_OR_NEWER
                        replayLight.type = lightUpdate.Shape.ToEngineShapeType();
#else
                        replayLight.shape = lightUpdate.Shape.ToEngineType();
#endif
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

                    // shadowResolution / shadowCustomResolution are Built-in RP only APIs: setting them under
                    // SRP (URP/HDRP) logs a per-frame warning and has no effect.
                    if (GraphicsSettings.defaultRenderPipeline == null)
                    {
                        if (lightUpdate.HasShadowResolution)
                        {
                            replayLight.shadowResolution = lightUpdate.ShadowResolution.ToEngineType();
                        }

                        if (lightUpdate.HasShadowCustomResolution)
                        {
                            replayLight.shadowCustomResolution = lightUpdate.ShadowCustomResolution;
                        }
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

                    if (lightUpdate.Cookie != null)
                    {
                        replayLight.cookie = ctx.GetOrDefaultAssetByIdentifier<Texture>(lightUpdate.Cookie);
                        ctx.TryAddAssetIdentifierCorrespondence(lightUpdate.Cookie, replayLight.cookie);
                    }

                    if (lightUpdate.HasCookieSize)
                    {
#if UNITY_6000_0_OR_NEWER
                        replayLight.cookieSize2D = new Vector2(lightUpdate.CookieSize, lightUpdate.CookieSize);
#else
                        replayLight.cookieSize = lightUpdate.CookieSize;
#endif
                    }

                    if (lightUpdate.Flare != null)
                    {
                        replayLight.flare = ctx.GetOrDefaultAssetByIdentifier<Flare>(lightUpdate.Flare);
                        ctx.TryAddAssetIdentifierCorrespondence(lightUpdate.Flare, replayLight.flare);
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