#if HDRP_ENABLED
using PLUME.Sample;
using PLUME.Sample.Unity.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace PLUME.Viewer.Player.Module.Unity.HDRP
{
    /// <summary>
    /// Replays the record's default volume profile, which gives the sky, exposure and other volume defaults of every
    /// scene. Without it (not recorded, not in the bundle), the viewer's own defaults are used.
    /// </summary>
    public class HDRPGlobalSettingsPlayerModule : PlayerModule
    {
#if UNITY_6000_0_OR_NEWER
        private VolumeProfile _recordProfile;
        private VolumeProfile _viewerProfile;
        private bool _profileReplaced;
#else
        private Volume _defaultVolume;
#endif

        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            if (rawSample.Payload is not HDRPGlobalSettingsUpdate { DefaultVolumeProfile: not null } settingsUpdate)
                return;

            var profile = ctx.GetOrDefaultAssetByIdentifier<VolumeProfile>(settingsUpdate.DefaultVolumeProfile);

            if (profile == null)
                return;

#if UNITY_6000_0_OR_NEWER
            if (!_profileReplaced)
            {
                _viewerProfile = VolumeManager.instance.globalDefaultProfile;
                _profileReplaced = true;
            }

            _recordProfile = profile;
            VolumeManager.instance.SetGlobalDefaultProfile(profile);
#else
            // HDRP 14 (Unity 2022) has no public default profile: a global volume just above HDRP's own default volume
            // (at the lowest priority) acts as one, overridden by the scenes' volumes as the default profile is.
            if (_defaultVolume == null)
            {
                var go = new GameObject("Record Default Volume Profile");
                go.transform.SetParent(transform, false);
                _defaultVolume = go.AddComponent<Volume>();
                _defaultVolume.isGlobal = true;
                _defaultVolume.priority = float.MinValue / 2;
            }

            _defaultVolume.sharedProfile = profile;
#endif
        }

#if UNITY_6000_0_OR_NEWER
        // Recreating the render pipeline (e.g. switched for a heatmap) resets the default profile to the viewer's.
        private void OnEnable()
        {
            RenderPipelineManager.activeRenderPipelineCreated += ReapplyRecordProfile;
        }

        private void OnDisable()
        {
            RenderPipelineManager.activeRenderPipelineCreated -= ReapplyRecordProfile;
        }

        private void ReapplyRecordProfile()
        {
            if (_recordProfile != null)
                VolumeManager.instance.SetGlobalDefaultProfile(_recordProfile);
        }

        private void OnDestroy()
        {
            // In the editor, the default profile would otherwise stay the record's after play mode.
            if (!_profileReplaced || _viewerProfile == null)
                return;

            try
            {
                VolumeManager.instance.SetGlobalDefaultProfile(_viewerProfile);
            }
            catch (System.Exception e)
            {
                // The pipeline may already be torn down when quitting: nothing left to restore then.
                Debug.LogWarning($"The viewer's default volume profile could not be restored: {e.Message}");
            }
        }
#endif
    }
}
#endif
