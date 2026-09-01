// VolumeManager.SetGlobalDefaultProfile is core RP 17+ (Unity 6). Guarded so the
// shared package still compiles under the Unity 2022 (HDRP 14) app shell.
#if HDRP_ENABLED && UNITY_6000_0_OR_NEWER
using PLUME.Sample;
using PLUME.Sample.Unity.Settings;
using UnityEngine.Rendering;

namespace PLUME.Viewer.Player.Module.Unity.HDRP
{
    public class HDRPGlobalSettingsPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case HDRPGlobalSettingsUpdate settingsUpdate:
                {
                    if (settingsUpdate.DefaultVolumeProfile != null)
                    {
                        var profile =
                            ctx.GetOrDefaultAssetByIdentifier<VolumeProfile>(settingsUpdate.DefaultVolumeProfile);

                        if (profile != null)
                        {
                            VolumeManager.instance.SetGlobalDefaultProfile(profile);
                        }
                    }

                    break;
                }
            }
        }
    }
}
#endif
