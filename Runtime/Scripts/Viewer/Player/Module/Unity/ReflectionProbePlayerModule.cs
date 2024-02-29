using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class ReflectionProbePlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case ReflectionProbeCreate reflectionProbeCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<ReflectionProbe>(reflectionProbeCreate.Id);
                    break;
                }
                case ReflectionProbeDestroy reflectionProbeDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(reflectionProbeDestroy.Id);
                    break;
                }
                case ReflectionProbeUpdate reflectionProbeUpdateEnabled:
                {
                    var replayProbe =
                        ctx.GetOrCreateComponentByIdentifier<ReflectionProbe>(reflectionProbeUpdateEnabled.Id);

                    if (reflectionProbeUpdateEnabled.HasEnabled)
                    {
                        replayProbe.enabled = reflectionProbeUpdateEnabled.Enabled;
                    }

                    if (reflectionProbeUpdateEnabled.HasMode)
                    {
                        replayProbe.mode = reflectionProbeUpdateEnabled.Mode.ToEngineType();
                    }

                    if (reflectionProbeUpdateEnabled.HasRefreshMode)
                    {
                        replayProbe.refreshMode = reflectionProbeUpdateEnabled.RefreshMode.ToEngineType();
                    }

                    if (reflectionProbeUpdateEnabled.HasTimeSlicingMode)
                    {
                        replayProbe.timeSlicingMode = reflectionProbeUpdateEnabled.TimeSlicingMode.ToEngineType();
                    }

                    if (reflectionProbeUpdateEnabled.HasClearFlags)
                    {
                        replayProbe.clearFlags = reflectionProbeUpdateEnabled.ClearFlags.ToEngineType();
                    }

                    if (reflectionProbeUpdateEnabled.HasImportance)
                    {
                        replayProbe.importance = reflectionProbeUpdateEnabled.Importance;
                    }

                    if (reflectionProbeUpdateEnabled.HasIntensity)
                    {
                        replayProbe.intensity = reflectionProbeUpdateEnabled.Intensity;
                    }

                    if (reflectionProbeUpdateEnabled.HasNearClipPlane)
                    {
                        replayProbe.nearClipPlane = reflectionProbeUpdateEnabled.NearClipPlane;
                    }

                    if (reflectionProbeUpdateEnabled.HasFarClipPlane)
                    {
                        replayProbe.farClipPlane = reflectionProbeUpdateEnabled.FarClipPlane;
                    }

                    if (reflectionProbeUpdateEnabled.HasRenderDynamicObjects)
                    {
                        replayProbe.renderDynamicObjects = reflectionProbeUpdateEnabled.RenderDynamicObjects;
                    }

                    if (reflectionProbeUpdateEnabled.HasBoxProjection)
                    {
                        replayProbe.boxProjection = reflectionProbeUpdateEnabled.BoxProjection;
                    }

                    if (reflectionProbeUpdateEnabled.HasBlendDistance)
                    {
                        replayProbe.blendDistance = reflectionProbeUpdateEnabled.BlendDistance;
                    }

                    if (reflectionProbeUpdateEnabled.Bounds != null)
                    {
                        replayProbe.center = reflectionProbeUpdateEnabled.Bounds.ToEngineType().center;
                        replayProbe.size = reflectionProbeUpdateEnabled.Bounds.ToEngineType().size;
                    }

                    if (reflectionProbeUpdateEnabled.HasResolution)
                    {
                        replayProbe.resolution = reflectionProbeUpdateEnabled.Resolution;
                    }

                    if (reflectionProbeUpdateEnabled.HasHdr)
                    {
                        replayProbe.hdr = reflectionProbeUpdateEnabled.Hdr;
                    }

                    if (reflectionProbeUpdateEnabled.HasShadowDistance)
                    {
                        replayProbe.shadowDistance = reflectionProbeUpdateEnabled.ShadowDistance;
                    }

                    if (reflectionProbeUpdateEnabled.BackgroundColor != null)
                    {
                        replayProbe.backgroundColor = reflectionProbeUpdateEnabled.BackgroundColor.ToEngineType();
                    }

                    if (reflectionProbeUpdateEnabled.HasCullingMask)
                    {
                        replayProbe.cullingMask = reflectionProbeUpdateEnabled.CullingMask;
                    }

                    if (reflectionProbeUpdateEnabled.CustomBakedTextureId != null)
                    {
                        replayProbe.customBakedTexture =
                            ctx.GetOrDefaultAssetByIdentifier<Texture>(
                                reflectionProbeUpdateEnabled.CustomBakedTextureId);
                    }

                    if (reflectionProbeUpdateEnabled.BakedTextureId != null)
                    {
                        replayProbe.bakedTexture =
                            ctx.GetOrDefaultAssetByIdentifier<Texture>(reflectionProbeUpdateEnabled.BakedTextureId);
                    }

                    break;
                }
            }
        }
    }
}