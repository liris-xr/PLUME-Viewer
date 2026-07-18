#if HDRP_ENABLED
using PLUME.Sample;
using PLUME.Sample.Unity.HDRP;
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
                    ctx.GetOrCreateComponentByIdentifier<HDAdditionalLightData>(lightDataCreate.Component);
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
    }
}
#endif
