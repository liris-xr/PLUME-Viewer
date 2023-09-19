using System.Linq;
using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME
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
                case ReflectionProbeUpdateEnabled reflectionProbeUpdateEnabled:
                {
                    var replayProbe = ctx.GetOrCreateComponentByIdentifier<ReflectionProbe>(reflectionProbeUpdateEnabled.Id);
                    replayProbe.enabled = reflectionProbeUpdateEnabled.Enabled;
                    break;
                }
                case ReflectionProbeUpdate reflectionProbeUpdate:
                {
                    var replayProbe = ctx.GetOrCreateComponentByIdentifier<ReflectionProbe>(reflectionProbeUpdate.Id);
                    replayProbe.mode = reflectionProbeUpdate.Mode.ToEngineType();
                    replayProbe.refreshMode = reflectionProbeUpdate.RefreshMode.ToEngineType();
                    replayProbe.timeSlicingMode = reflectionProbeUpdate.TimeSlicingMode.ToEngineType();
                    replayProbe.clearFlags = reflectionProbeUpdate.ClearFlags.ToEngineType();
                    replayProbe.importance = reflectionProbeUpdate.Importance;
                    replayProbe.intensity = reflectionProbeUpdate.Intensity;
                    replayProbe.nearClipPlane = reflectionProbeUpdate.NearClipPlane;
                    replayProbe.farClipPlane = reflectionProbeUpdate.FarClipPlane;
                    replayProbe.renderDynamicObjects = reflectionProbeUpdate.RenderDynamicObjects;
                    replayProbe.boxProjection = reflectionProbeUpdate.BoxProjection;
                    replayProbe.blendDistance = reflectionProbeUpdate.BlendDistance;
                    replayProbe.center = reflectionProbeUpdate.Bounds.ToEngineType().center;
                    replayProbe.size = reflectionProbeUpdate.Bounds.ToEngineType().size;
                    replayProbe.resolution = reflectionProbeUpdate.Resolution;
                    replayProbe.hdr = reflectionProbeUpdate.Hdr;
                    replayProbe.shadowDistance = reflectionProbeUpdate.ShadowDistance;
                    replayProbe.backgroundColor = reflectionProbeUpdate.BackgroundColor.ToEngineType();
                    replayProbe.cullingMask = reflectionProbeUpdate.CullingMask;
                    replayProbe.customBakedTexture = ctx.GetOrDefaultAssetByIdentifier<Texture>(reflectionProbeUpdate.CustomBakedTextureId);
                    replayProbe.bakedTexture = ctx.GetOrDefaultAssetByIdentifier<Texture>(reflectionProbeUpdate.BakedTextureId);
                    break;
                }
            }
        }
    }
}