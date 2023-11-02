using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine.Rendering;

namespace PLUME
{
    public class VolumePlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
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
                    volume.sharedProfile = ctx.GetOrDefaultAssetByIdentifier<VolumeProfile>(volumeUpdate.SharedProfileId);
                    break;
                }
            }
        }
    }
}