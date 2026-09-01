#if HDRP_ENABLED
using System;
using UnityEngine.Rendering.HighDefinition;
using HDClearColorMode = PLUME.Sample.Unity.HDRP.HDClearColorMode;
using HDCameraAntialiasingMode = PLUME.Sample.Unity.HDRP.HDCameraAntialiasingMode;
using HDSMAAQualityLevel = PLUME.Sample.Unity.HDRP.HDSMAAQualityLevel;
using HDTAAQualityLevel = PLUME.Sample.Unity.HDRP.HDTAAQualityLevel;

namespace PLUME.Viewer.Player.Module.Unity.HDRP
{
    public static class HDRPPayloadExtensions
    {
        public static HDAdditionalCameraData.ClearColorMode ToEngineType(this HDClearColorMode clearColorMode)
        {
            return clearColorMode switch
            {
                HDClearColorMode.Sky => HDAdditionalCameraData.ClearColorMode.Sky,
                HDClearColorMode.Color => HDAdditionalCameraData.ClearColorMode.Color,
                HDClearColorMode.None => HDAdditionalCameraData.ClearColorMode.None,
                _ => throw new ArgumentOutOfRangeException(nameof(clearColorMode), clearColorMode, null)
            };
        }

        public static HDAdditionalCameraData.AntialiasingMode ToEngineType(
            this HDCameraAntialiasingMode antialiasingMode)
        {
            return antialiasingMode switch
            {
                HDCameraAntialiasingMode.None => HDAdditionalCameraData.AntialiasingMode.None,
                HDCameraAntialiasingMode.FastApproximate => HDAdditionalCameraData.AntialiasingMode
                    .FastApproximateAntialiasing,
                HDCameraAntialiasingMode.Temporal => HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing,
                HDCameraAntialiasingMode.SubpixelMorphological => HDAdditionalCameraData.AntialiasingMode
                    .SubpixelMorphologicalAntiAliasing,
                _ => throw new ArgumentOutOfRangeException(nameof(antialiasingMode), antialiasingMode, null)
            };
        }

        public static HDAdditionalCameraData.SMAAQualityLevel ToEngineType(this HDSMAAQualityLevel smaaQualityLevel)
        {
            return smaaQualityLevel switch
            {
                HDSMAAQualityLevel.Low => HDAdditionalCameraData.SMAAQualityLevel.Low,
                HDSMAAQualityLevel.Medium => HDAdditionalCameraData.SMAAQualityLevel.Medium,
                HDSMAAQualityLevel.High => HDAdditionalCameraData.SMAAQualityLevel.High,
                _ => throw new ArgumentOutOfRangeException(nameof(smaaQualityLevel), smaaQualityLevel, null)
            };
        }

        public static HDAdditionalCameraData.TAAQualityLevel ToEngineType(this HDTAAQualityLevel taaQualityLevel)
        {
            return taaQualityLevel switch
            {
                HDTAAQualityLevel.Low => HDAdditionalCameraData.TAAQualityLevel.Low,
                HDTAAQualityLevel.Medium => HDAdditionalCameraData.TAAQualityLevel.Medium,
                HDTAAQualityLevel.High => HDAdditionalCameraData.TAAQualityLevel.High,
                _ => throw new ArgumentOutOfRangeException(nameof(taaQualityLevel), taaQualityLevel, null)
            };
        }
    }
}
#endif
