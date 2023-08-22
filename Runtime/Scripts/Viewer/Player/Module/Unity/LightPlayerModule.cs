using System.Linq;
using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME
{
    public class LightPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
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
                case LightUpdateEnabled lightUpdateEnabled:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateEnabled.Id);
                    replayLight.enabled = lightUpdateEnabled.Enabled;
                    break;
                }
                case LightUpdateType lightUpdateType:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateType.Id);
                    replayLight.type = lightUpdateType.Type.ToEngineType();
                    break;
                }
                case LightUpdateRange lightUpdateRange:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateRange.Id);
                    replayLight.range = lightUpdateRange.Range;
                    break;
                }
                case LightUpdateColor lightUpdateColor:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateColor.Id);
                    replayLight.color = lightUpdateColor.Color.ToEngineType();
                    replayLight.colorTemperature = lightUpdateColor.ColorTemperature;
                    replayLight.useColorTemperature = lightUpdateColor.UseColorTemperature;
                    break;
                }
                case LightUpdateIntensity lightUpdateIntensity:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateIntensity.Id);
                    replayLight.intensity = lightUpdateIntensity.Intensity;
                    break;
                }
                case LightUpdateBounceIntensity lightUpdateBounceIntensity:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateBounceIntensity.Id);
                    replayLight.bounceIntensity = lightUpdateBounceIntensity.BounceIntensity;
                    break;
                }
                case LightUpdateShape lightUpdateShape:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateShape.Id);
                    replayLight.shape = lightUpdateShape.Shape.ToEngineType();
                    break;
                }
                case LightUpdateSpotAngle lightUpdateSpotAngle:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateSpotAngle.Id);
                    replayLight.spotAngle = lightUpdateSpotAngle.SpotAngle;
                    replayLight.innerSpotAngle = lightUpdateSpotAngle.InnerSpotAngle;
                    break;
                }
                case LightUpdateShadows lightUpdateShadows:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateShadows.Id);
                    replayLight.shadows = lightUpdateShadows.Shadows.ToEngineType();
                    replayLight.shadowBias = lightUpdateShadows.ShadowBias;
                    replayLight.shadowResolution = lightUpdateShadows.ShadowResolution.ToEngineType();
                    replayLight.shadowCustomResolution = lightUpdateShadows.ShadowCustomResolution;
                    replayLight.shadowStrength = lightUpdateShadows.ShadowStrength;
                    replayLight.shadowNearPlane = lightUpdateShadows.ShadowNearPlane;
                    replayLight.shadowNormalBias = lightUpdateShadows.ShadowNormalBias;
                    replayLight.shadowMatrixOverride = lightUpdateShadows.ShadowMatrixOverride.ToEngineType();
                    replayLight.layerShadowCullDistances = lightUpdateShadows.LayerShadowCullDistances.ToArray();
                    replayLight.lightShadowCasterMode = lightUpdateShadows.LightShadowCasterMode.ToEngineType();
                    replayLight.useShadowMatrixOverride = lightUpdateShadows.UseShadowMatrixOverride;
                    replayLight.useViewFrustumForShadowCasterCull = lightUpdateShadows.UseViewFrustumForShadowCasterCull;
                    break;
                }
                case LightUpdateCookie lightUpdateCookie:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateCookie.Id);
                    replayLight.cookie = ctx.GetOrDefaultAssetByIdentifier<Texture>(lightUpdateCookie.CookieId);
                    replayLight.cookieSize = lightUpdateCookie.CookieSize;
                    ctx.TryAddAssetIdentifierCorrespondence(lightUpdateCookie.CookieId, replayLight.cookie);
                    break;
                }
                case LightUpdateFlare lightUpdateFlare:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateFlare.Id);
                    replayLight.flare = ctx.GetOrDefaultAssetByIdentifier<Flare>(lightUpdateFlare.FlareId);;
                    ctx.TryAddAssetIdentifierCorrespondence(lightUpdateFlare.FlareId, replayLight.flare);
                    break;
                }
                case LightUpdateCulling lightUpdateCulling:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateCulling.Id);
                    replayLight.useBoundingSphereOverride = lightUpdateCulling.UseBoundingSphereOverride;
                    replayLight.cullingMask = lightUpdateCulling.CullingMask;
                    break;
                }
                case LightUpdateRenderingLayerMask lightUpdateRenderingLayerMask:
                {
                    var replayLight = ctx.GetOrCreateComponentByIdentifier<Light>(lightUpdateRenderingLayerMask.Id);
                    replayLight.renderingLayerMask = lightUpdateRenderingLayerMask.RenderingLayerMask;
                    break;
                }
            }
        }
    }
}