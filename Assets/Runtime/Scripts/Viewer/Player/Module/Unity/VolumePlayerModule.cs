﻿using PLUME.Sample.Unity.URP;
using UnityEngine.Rendering;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class VolumePlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case VolumeCreate volumeCreate:
                    ctx.GetOrCreateComponentByIdentifier<Volume>(volumeCreate.Id);
                    break;
                case VolumeUpdateEnabled volumeUpdateEnabled:
                {
                    var volume = ctx.GetOrCreateComponentByIdentifier<Volume>(volumeUpdateEnabled.Id);
                    volume.enabled = volumeUpdateEnabled.Enabled;
                    break;
                }
                case VolumeUpdate volumeUpdate:
                {
                    var volume = ctx.GetOrCreateComponentByIdentifier<Volume>(volumeUpdate.Id);
                    volume.isGlobal = volumeUpdate.IsGlobal;
                    volume.blendDistance = volumeUpdate.BlendDistance;
                    volume.weight = volumeUpdate.Weight;
                    volume.priority = volumeUpdate.Priority;
                    volume.sharedProfile =
                        ctx.GetOrDefaultAssetByIdentifier<VolumeProfile>(volumeUpdate.SharedProfileId);
                    break;
                }
            }
        }
    }
}