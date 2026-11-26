#if HDRP_ENABLED
using PLUME.Sample;
using PLUME.Sample.Unity.HDRP;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace PLUME.Viewer.Player.Module.Unity.HDRP
{
    public class HDAdditionalLightDataPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case HDAdditionalLightDataCreate lightDataCreate:
                {
                    var lightData =
                        ctx.GetOrCreateComponentByIdentifier<HDAdditionalLightData>(lightDataCreate.Component);
                    if (lightData != null)
                        SyncIntensity(lightData.GetComponent<Light>(), lightData);
                    break;
                }
                case HDAdditionalLightDataDestroy lightDataDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(lightDataDestroy.Component);
                    break;
                }
                case HDAdditionalLightDataUpdate lightDataUpdate:
                {
                    var replayLightData =
                        ctx.GetOrCreateComponentByIdentifier<HDAdditionalLightData>(lightDataUpdate.Component);

                    if (lightDataUpdate.HasInteractsWithSky)
                    {
                        replayLightData.interactsWithSky = lightDataUpdate.InteractsWithSky;
                    }

                    if (lightDataUpdate.HasAffectDiffuse)
                    {
                        replayLightData.affectDiffuse = lightDataUpdate.AffectDiffuse;
                    }

                    if (lightDataUpdate.HasAffectSpecular)
                    {
                        replayLightData.affectSpecular = lightDataUpdate.AffectSpecular;
                    }

                    if (lightDataUpdate.HasAffectsVolumetric)
                    {
                        replayLightData.affectsVolumetric = lightDataUpdate.AffectsVolumetric;
                    }

                    if (lightDataUpdate.HasLightDimmer || lightDataUpdate.HasVolumetricDimmer)
                    {
                        var lightDimmer = lightDataUpdate.HasLightDimmer
                            ? lightDataUpdate.LightDimmer
                            : replayLightData.lightDimmer;
                        var volumetricDimmer = lightDataUpdate.HasVolumetricDimmer
                            ? lightDataUpdate.VolumetricDimmer
                            : replayLightData.volumetricDimmer;
                        replayLightData.SetLightDimmer(lightDimmer, volumetricDimmer);
                    }

                    if (lightDataUpdate.HasShadowDimmer || lightDataUpdate.HasVolumetricShadowDimmer)
                    {
                        var shadowDimmer = lightDataUpdate.HasShadowDimmer
                            ? lightDataUpdate.ShadowDimmer
                            : replayLightData.shadowDimmer;
                        var volumetricShadowDimmer = lightDataUpdate.HasVolumetricShadowDimmer
                            ? lightDataUpdate.VolumetricShadowDimmer
                            : replayLightData.volumetricShadowDimmer;
                        replayLightData.SetShadowDimmer(shadowDimmer, volumetricShadowDimmer);
                    }

                    if (lightDataUpdate.HasVolumetricFadeDistance)
                    {
                        replayLightData.volumetricFadeDistance = lightDataUpdate.VolumetricFadeDistance;
                    }

                    if (lightDataUpdate.HasShapeRadius)
                    {
                        replayLightData.shapeRadius = lightDataUpdate.ShapeRadius;
                    }

                    if (lightDataUpdate.HasApplyRangeAttenuation)
                    {
                        replayLightData.applyRangeAttenuation = lightDataUpdate.ApplyRangeAttenuation;
                    }

                    if (lightDataUpdate.SurfaceTint != null)
                    {
                        replayLightData.surfaceTint = lightDataUpdate.SurfaceTint.ToEngineType();
                    }

                    if (lightDataUpdate.ShadowTint != null)
                    {
                        replayLightData.shadowTint = lightDataUpdate.ShadowTint.ToEngineType();
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// HDRP 14 (Unity 2022) overwrites Light.intensity with its additional data's intensity, which is 0 for one
        /// added at runtime: the additional data must hold the light's recorded intensity. Light.intensity is in the
        /// light's native unit. HDRP 17 (Unity 6) reads Light.intensity itself.
        /// </summary>
        internal static void SyncIntensity(Light light, HDAdditionalLightData lightData)
        {
#if !UNITY_6000_0_OR_NEWER
            if (light == null || lightData == null)
                return;

            var nativeUnit = light.type switch
            {
                LightType.Directional => LightUnit.Lux,
                // A box spot lights like a directional light.
                LightType.Spot when lightData.spotLightShape == SpotLightShape.Box => LightUnit.Lux,
                LightType.Point or LightType.Spot => LightUnit.Candela,
                _ => LightUnit.Nits
            };
            lightData.SetIntensity(light.intensity, nativeUnit);
#endif
        }
    }
}
#endif
