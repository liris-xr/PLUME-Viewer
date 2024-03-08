using System;
using Google.Protobuf.Collections;
using PLUME.Sample.Common;
using PLUME.Sample.Unity;
using PLUME.Sample.Unity.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using AnimationCurve = UnityEngine.AnimationCurve;
using Bounds = UnityEngine.Bounds;
using Color = UnityEngine.Color;
using ColorSpace = UnityEngine.ColorSpace;
using FontStyle = UnityEngine.FontStyle;
using HorizontalWrapMode = UnityEngine.HorizontalWrapMode;
using LightmapsMode = UnityEngine.LightmapsMode;
using LightShadowCasterMode = UnityEngine.LightShadowCasterMode;
using LightShadowResolution = UnityEngine.Rendering.LightShadowResolution;
using LightShadows = UnityEngine.LightShadows;
using LightShape = UnityEngine.LightShape;
using LightType = UnityEngine.LightType;
using Matrix4x4 = UnityEngine.Matrix4x4;
using OpaqueSortMode = UnityEngine.Rendering.OpaqueSortMode;
using Quaternion = UnityEngine.Quaternion;
using Rect = UnityEngine.Rect;
using ReflectionProbeClearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags;
using ReflectionProbeMode = UnityEngine.Rendering.ReflectionProbeMode;
using ReflectionProbeRefreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode;
using ReflectionProbeTimeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode;
using ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage;
using RenderingPath = UnityEngine.RenderingPath;
using RenderMode = UnityEngine.RenderMode;
using ScaleMode = PLUME.Sample.Unity.UI.ScaleMode;
using ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode;
using SphericalHarmonicsL2 = UnityEngine.Rendering.SphericalHarmonicsL2;
using StandaloneRenderResize = UnityEngine.StandaloneRenderResize;
using TextAnchor = UnityEngine.TextAnchor;
using TransparencySortMode = UnityEngine.TransparencySortMode;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
using VerticalWrapMode = UnityEngine.VerticalWrapMode;
using WeightedMode = UnityEngine.WeightedMode;

namespace PLUME
{
    public static class PayloadExtensions
    {
        public static GameObjectIdentifier ToIdentifierPayload(this GameObject go)
        {
            return new GameObjectIdentifier
            {
                GameObjectId = go.GetHashCode().ToString(),
                TransformId = go.transform.GetHashCode().ToString()
            };
        }

        public static ComponentIdentifier ToIdentifierPayload(this Component component)
        {
            return new ComponentIdentifier
            {
                ComponentId = component.GetHashCode().ToString(),
                ParentId = component.gameObject.ToIdentifierPayload()
            };
        }

        public static Vector2 ToEngineType(this Sample.Common.Vector2 vec)
        {
            return new Vector2
            {
                x = vec.X,
                y = vec.Y,
            };
        }

        public static Vector3 ToEngineType(this Sample.Common.Vector3 vec)
        {
            return new Vector3
            {
                x = vec.X,
                y = vec.Y,
                z = vec.Z
            };
        }

        public static Vector4 ToEngineType(this Sample.Common.Vector4 vec)
        {
            return new Vector4
            {
                x = vec.X,
                y = vec.Y,
                z = vec.Z,
                w = vec.W
            };
        }

        public static Quaternion ToEngineType(this Sample.Common.Quaternion vec)
        {
            return new Quaternion
            {
                x = vec.X,
                y = vec.Y,
                z = vec.Z,
                w = vec.W
            };
        }

        public static Color ToEngineType(this Sample.Common.Color color)
        {
            return new Color
            {
                r = color.R,
                g = color.G,
                b = color.B,
                a = color.A
            };
        }

        public static SphericalHarmonicsL2 ToEngineType(
            this Sample.Common.SphericalHarmonicsL2 sphericalHarmonicsL2)
        {
            var shl2 = new SphericalHarmonicsL2
            {
                [0, 0] = sphericalHarmonicsL2.Shr0,
                [0, 1] = sphericalHarmonicsL2.Shr1,
                [0, 2] = sphericalHarmonicsL2.Shr2,
                [0, 3] = sphericalHarmonicsL2.Shr3,
                [0, 4] = sphericalHarmonicsL2.Shr4,
                [0, 5] = sphericalHarmonicsL2.Shr5,
                [0, 6] = sphericalHarmonicsL2.Shr6,
                [0, 7] = sphericalHarmonicsL2.Shr7,
                [0, 8] = sphericalHarmonicsL2.Shr8,
                [0, 9] = sphericalHarmonicsL2.Shg0,
                [0, 10] = sphericalHarmonicsL2.Shg1,
                [0, 11] = sphericalHarmonicsL2.Shg2,
                [0, 12] = sphericalHarmonicsL2.Shg3,
                [0, 13] = sphericalHarmonicsL2.Shg4,
                [0, 14] = sphericalHarmonicsL2.Shg5,
                [0, 15] = sphericalHarmonicsL2.Shg6,
                [0, 16] = sphericalHarmonicsL2.Shg7,
                [0, 17] = sphericalHarmonicsL2.Shg8,
                [0, 18] = sphericalHarmonicsL2.Shb0,
                [0, 19] = sphericalHarmonicsL2.Shb1,
                [0, 20] = sphericalHarmonicsL2.Shb2,
                [0, 21] = sphericalHarmonicsL2.Shb3,
                [0, 22] = sphericalHarmonicsL2.Shb4,
                [0, 23] = sphericalHarmonicsL2.Shb5,
                [0, 24] = sphericalHarmonicsL2.Shb6,
                [0, 25] = sphericalHarmonicsL2.Shb7,
                [0, 26] = sphericalHarmonicsL2.Shb8
            };
            return shl2;
        }

        public static LightType ToEngineType(this Sample.Unity.LightType lightType)
        {
            return lightType switch
            {
                Sample.Unity.LightType.Point => LightType.Point,
                Sample.Unity.LightType.Directional => LightType.Directional,
                Sample.Unity.LightType.Spot => LightType.Spot,
                Sample.Unity.LightType.Rectangle => LightType.Rectangle,
                Sample.Unity.LightType.Disc => LightType.Disc,
                // Sample.Unity.LightType.Area => LightType.Area,
                _ => throw new ArgumentOutOfRangeException(nameof(lightType), lightType, null)
            };
        }

        public static LightShape ToEngineType(this Sample.Unity.LightShape lightShape)
        {
            return lightShape switch
            {
                Sample.Unity.LightShape.Cone => LightShape.Cone,
                Sample.Unity.LightShape.Pyramid => LightShape.Pyramid,
                Sample.Unity.LightShape.Box => LightShape.Box,
                _ => throw new ArgumentOutOfRangeException(nameof(lightShape), lightShape, null)
            };
        }

        public static LightShadows ToEngineType(this Sample.Unity.LightShadows lightShadows)
        {
            return lightShadows switch
            {
                Sample.Unity.LightShadows.None => LightShadows.None,
                Sample.Unity.LightShadows.Hard => LightShadows.Hard,
                Sample.Unity.LightShadows.Soft => LightShadows.Soft,
                _ => throw new ArgumentOutOfRangeException(nameof(lightShadows), lightShadows, null)
            };
        }

        public static LightShadowResolution ToEngineType(
            this Sample.Unity.LightShadowResolution lightShadowResolution)
        {
            return lightShadowResolution switch
            {
                Sample.Unity.LightShadowResolution.FromQualitySettings => LightShadowResolution
                    .FromQualitySettings,
                Sample.Unity.LightShadowResolution.Low => LightShadowResolution.Low,
                Sample.Unity.LightShadowResolution.Medium => LightShadowResolution.Medium,
                Sample.Unity.LightShadowResolution.High => LightShadowResolution.High,
                Sample.Unity.LightShadowResolution.VeryHigh => LightShadowResolution.VeryHigh,
                _ => throw new ArgumentOutOfRangeException(nameof(lightShadowResolution), lightShadowResolution, null)
            };
        }

        public static LightShadowCasterMode ToEngineType(this Sample.Unity.LightShadowCasterMode lightShadowCasterMode)
        {
            return lightShadowCasterMode switch
            {
                Sample.Unity.LightShadowCasterMode.Default => LightShadowCasterMode.Default,
                Sample.Unity.LightShadowCasterMode.NonLightmappedOnly => LightShadowCasterMode.NonLightmappedOnly,
                Sample.Unity.LightShadowCasterMode.Everything => LightShadowCasterMode.Everything,
                _ => throw new ArgumentOutOfRangeException(nameof(lightShadowCasterMode), lightShadowCasterMode, null)
            };
        }

        public static Bounds ToEngineType(this Sample.Common.Bounds bounds)
        {
            return new Bounds
            {
                center = bounds.Center.ToEngineType(),
                extents = bounds.Extents.ToEngineType()
            };
        }

        public static Rect ToEngineType(this Sample.Common.Rect rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static Matrix4x4 ToEngineType(this Sample.Common.Matrix4x4 mtx)
        {
            return new Matrix4x4
            {
                m00 = mtx.M00,
                m10 = mtx.M10,
                m20 = mtx.M20,
                m30 = mtx.M30,
                m01 = mtx.M01,
                m11 = mtx.M11,
                m21 = mtx.M21,
                m31 = mtx.M31,
                m02 = mtx.M02,
                m12 = mtx.M12,
                m22 = mtx.M22,
                m32 = mtx.M32,
                m03 = mtx.M03,
                m13 = mtx.M13,
                m23 = mtx.M23,
                m33 = mtx.M33
            };
        }

        public static T[] ToEngineType<T>(this RepeatedField<T> repeatedField)
        {
            var arr = new T[repeatedField.Count];
            for (var i = 0; i < repeatedField.Count; ++i)
            {
                arr[i] = repeatedField[i];
            }

            return arr;
        }

        public static ReflectionProbeMode ToEngineType(this Sample.Unity.ReflectionProbeMode reflectionProbeMode)
        {
            return reflectionProbeMode switch
            {
                Sample.Unity.ReflectionProbeMode.Custom => ReflectionProbeMode.Custom,
                Sample.Unity.ReflectionProbeMode.Baked => ReflectionProbeMode.Baked,
                Sample.Unity.ReflectionProbeMode.Realtime => ReflectionProbeMode.Realtime,
                _ => throw new ArgumentOutOfRangeException(nameof(reflectionProbeMode), reflectionProbeMode, null)
            };
        }

        public static ReflectionProbeRefreshMode ToEngineType(
            this Sample.Unity.ReflectionProbeRefreshMode reflectionProbeRefreshMode)
        {
            return reflectionProbeRefreshMode switch
            {
                Sample.Unity.ReflectionProbeRefreshMode.EveryFrame => ReflectionProbeRefreshMode.EveryFrame,
                Sample.Unity.ReflectionProbeRefreshMode.OnAwake => ReflectionProbeRefreshMode.OnAwake,
                Sample.Unity.ReflectionProbeRefreshMode.ViaScripting => ReflectionProbeRefreshMode.ViaScripting,
                _ => throw new ArgumentOutOfRangeException(nameof(reflectionProbeRefreshMode),
                    reflectionProbeRefreshMode, null)
            };
        }

        public static ReflectionProbeTimeSlicingMode ToEngineType(
            this Sample.Unity.ReflectionProbeTimeSlicingMode reflectionProbeTimeSlicingMode)
        {
            return reflectionProbeTimeSlicingMode switch
            {
                Sample.Unity.ReflectionProbeTimeSlicingMode.NoTimeSlicing => ReflectionProbeTimeSlicingMode
                    .NoTimeSlicing,
                Sample.Unity.ReflectionProbeTimeSlicingMode.IndividualFaces => ReflectionProbeTimeSlicingMode
                    .IndividualFaces,
                Sample.Unity.ReflectionProbeTimeSlicingMode.AllFacesAtOnce => ReflectionProbeTimeSlicingMode
                    .AllFacesAtOnce,
                _ => throw new ArgumentOutOfRangeException(nameof(reflectionProbeTimeSlicingMode),
                    reflectionProbeTimeSlicingMode, null)
            };
        }

        public static ReflectionProbeClearFlags ToEngineType(
            this Sample.Unity.ReflectionProbeClearFlags reflectionProbeClearFlags)
        {
            return reflectionProbeClearFlags switch
            {
                Sample.Unity.ReflectionProbeClearFlags.Skybox => ReflectionProbeClearFlags.Skybox,
                Sample.Unity.ReflectionProbeClearFlags.SolidColor => ReflectionProbeClearFlags.SolidColor,
                _ => throw new ArgumentOutOfRangeException(nameof(reflectionProbeClearFlags), reflectionProbeClearFlags,
                    null)
            };
        }

        public static LightmapsMode ToEngineType(this Sample.Unity.LightmapsMode lightmapsMode)
        {
            return lightmapsMode switch
            {
                Sample.Unity.LightmapsMode.NonDirectional => LightmapsMode.NonDirectional,
                Sample.Unity.LightmapsMode.CombinedDirectional => LightmapsMode.CombinedDirectional,
                _ => throw new ArgumentOutOfRangeException(nameof(lightmapsMode), lightmapsMode, null)
            };
        }

        public static ShadowCastingMode ToEngineType(this Sample.Unity.ShadowCastingMode shadowCastingMode)
        {
            return shadowCastingMode switch
            {
                Sample.Unity.ShadowCastingMode.Off => ShadowCastingMode.Off,
                Sample.Unity.ShadowCastingMode.On => ShadowCastingMode.On,
                Sample.Unity.ShadowCastingMode.ShadowsOnly => ShadowCastingMode.ShadowsOnly,
                Sample.Unity.ShadowCastingMode.TwoSided => ShadowCastingMode.TwoSided,
                _ => throw new ArgumentOutOfRangeException(nameof(shadowCastingMode), shadowCastingMode,
                    null)
            };
        }

        public static ReflectionProbeUsage ToEngineType(
            this Sample.Unity.ReflectionProbeUsage reflectionProbeUsage)
        {
            return reflectionProbeUsage switch
            {
                Sample.Unity.ReflectionProbeUsage.Off => ReflectionProbeUsage.Off,
                Sample.Unity.ReflectionProbeUsage.BlendProbes => ReflectionProbeUsage.BlendProbes,
                Sample.Unity.ReflectionProbeUsage.BlendProbesAndSkybox => ReflectionProbeUsage
                    .BlendProbesAndSkybox,
                Sample.Unity.ReflectionProbeUsage.Simple => ReflectionProbeUsage.Simple,
                _ => throw new ArgumentOutOfRangeException(nameof(reflectionProbeUsage), reflectionProbeUsage,
                    null)
            };
        }

        public static OpaqueSortMode ToEngineType(this Sample.Unity.OpaqueSortMode opaqueSortMode)
        {
            return opaqueSortMode switch
            {
                Sample.Unity.OpaqueSortMode.Default => OpaqueSortMode.Default,
                Sample.Unity.OpaqueSortMode.NoDistanceSort => OpaqueSortMode.NoDistanceSort,
                Sample.Unity.OpaqueSortMode.FrontToBack => OpaqueSortMode.FrontToBack,
                _ => throw new ArgumentOutOfRangeException(nameof(opaqueSortMode), opaqueSortMode,
                    null)
            };
        }

        public static TransparencySortMode ToEngineType(this Sample.Unity.TransparencySortMode transparencySortMode)
        {
            return transparencySortMode switch
            {
                Sample.Unity.TransparencySortMode.Default => TransparencySortMode.Default,
                Sample.Unity.TransparencySortMode.Orthographic => TransparencySortMode.Orthographic,
                Sample.Unity.TransparencySortMode.Perspective => TransparencySortMode.Perspective,
                Sample.Unity.TransparencySortMode.CustomAxis => TransparencySortMode.CustomAxis,
                _ => throw new ArgumentOutOfRangeException(nameof(transparencySortMode), transparencySortMode,
                    null)
            };
        }

        public static RenderingPath ToEngineType(this Sample.Unity.RenderingPath renderingPath)
        {
            return renderingPath switch
            {
                Sample.Unity.RenderingPath.DeferredLighting => RenderingPath.DeferredShading,
                Sample.Unity.RenderingPath.DeferredShading => RenderingPath.DeferredShading,
                Sample.Unity.RenderingPath.Forward => RenderingPath.Forward,
                Sample.Unity.RenderingPath.VertexLit => RenderingPath.VertexLit,
                Sample.Unity.RenderingPath.UsePlayerSettings => RenderingPath.UsePlayerSettings,
                _ => throw new ArgumentOutOfRangeException(nameof(renderingPath), renderingPath,
                    null)
            };
        }

        public static Camera.GateFitMode ToEngineType(this CameraGateFitMode gateFitMode)
        {
            return gateFitMode switch
            {
                CameraGateFitMode.None => Camera.GateFitMode.None,
                CameraGateFitMode.Fill => Camera.GateFitMode.Fill,
                CameraGateFitMode.Horizontal => Camera.GateFitMode.Horizontal,
                CameraGateFitMode.Vertical => Camera.GateFitMode.Vertical,
                CameraGateFitMode.Overscan => Camera.GateFitMode.Overscan,
                _ => throw new ArgumentOutOfRangeException(nameof(gateFitMode), gateFitMode,
                    null)
            };
        }

        public static CameraClearFlags ToEngineType(this CameraClearFlags cameraClearFlags)
        {
            return cameraClearFlags switch
            {
                CameraClearFlags.Nothing => CameraClearFlags.Nothing,
                CameraClearFlags.Skybox => CameraClearFlags.Skybox,
                CameraClearFlags.SolidColor => CameraClearFlags.SolidColor,
                CameraClearFlags.Depth => CameraClearFlags.Depth,
                _ => throw new ArgumentOutOfRangeException(nameof(cameraClearFlags), cameraClearFlags,
                    null)
            };
        }

        public static DepthTextureMode ToEngineType(this DepthTextureMode depthTextureMode)
        {
            return depthTextureMode switch
            {
                DepthTextureMode.None => DepthTextureMode.None,
                DepthTextureMode.Depth => DepthTextureMode.Depth,
                DepthTextureMode.DepthNormals => DepthTextureMode.DepthNormals,
                DepthTextureMode.MotionVectors => DepthTextureMode.MotionVectors,
                _ => throw new ArgumentOutOfRangeException(nameof(depthTextureMode), depthTextureMode,
                    null)
            };
        }

        public static StereoTargetEyeMask ToEngineType(this CameraStereoTargetEyeMask stereoTargetEyeMask)
        {
            return stereoTargetEyeMask switch
            {
                CameraStereoTargetEyeMask.None => StereoTargetEyeMask.None,
                CameraStereoTargetEyeMask.Both => StereoTargetEyeMask.Both,
                CameraStereoTargetEyeMask.Left => StereoTargetEyeMask.Left,
                CameraStereoTargetEyeMask.Right => StereoTargetEyeMask.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(stereoTargetEyeMask), stereoTargetEyeMask,
                    null)
            };
        }

        public static CameraOverrideOption ToEngineType(this Sample.Unity.URP.CameraOverrideOption cameraOverrideOption)
        {
            return cameraOverrideOption switch
            {
                Sample.Unity.URP.CameraOverrideOption.Off => CameraOverrideOption.Off,
                Sample.Unity.URP.CameraOverrideOption.On => CameraOverrideOption.On,
                Sample.Unity.URP.CameraOverrideOption.UsePipelineSettings => CameraOverrideOption.UsePipelineSettings,
                _ => throw new ArgumentOutOfRangeException(nameof(cameraOverrideOption), cameraOverrideOption,
                    null)
            };
        }

        public static CameraRenderType ToEngineType(this Sample.Unity.URP.CameraRenderType cameraRenderType)
        {
            return cameraRenderType switch
            {
                Sample.Unity.URP.CameraRenderType.Base => CameraRenderType.Base,
                Sample.Unity.URP.CameraRenderType.Overlay => CameraRenderType.Overlay,
                _ => throw new ArgumentOutOfRangeException(nameof(cameraRenderType), cameraRenderType,
                    null)
            };
        }

        public static AntialiasingMode ToEngineType(this Sample.Unity.URP.AntialiasingMode antialiasingMode)
        {
            return antialiasingMode switch
            {
                Sample.Unity.URP.AntialiasingMode.None => AntialiasingMode.None,
                Sample.Unity.URP.AntialiasingMode.FastApproximateAntialiasing => AntialiasingMode
                    .FastApproximateAntialiasing,
                Sample.Unity.URP.AntialiasingMode.SubpixelMorphologicalAntiAliasing => AntialiasingMode
                    .SubpixelMorphologicalAntiAliasing,
                _ => throw new ArgumentOutOfRangeException(nameof(antialiasingMode), antialiasingMode,
                    null)
            };
        }

        public static AntialiasingQuality ToEngineType(this Sample.Unity.URP.AntialiasingQuality antialiasingQuality)
        {
            return antialiasingQuality switch
            {
                Sample.Unity.URP.AntialiasingQuality.Low => AntialiasingQuality.Low,
                Sample.Unity.URP.AntialiasingQuality.Medium => AntialiasingQuality.Medium,
                Sample.Unity.URP.AntialiasingQuality.High => AntialiasingQuality.High,
                _ => throw new ArgumentOutOfRangeException(nameof(antialiasingQuality), antialiasingQuality,
                    null)
            };
        }

        public static CanvasScaler.ScaleMode ToEngineType(this ScaleMode scaleMode)
        {
            return scaleMode switch
            {
                ScaleMode.ConstantPixelSize => CanvasScaler.ScaleMode.ConstantPixelSize,
                ScaleMode.ScaleWithScreenSize => CanvasScaler.ScaleMode.ScaleWithScreenSize,
                ScaleMode.ConstantPhysicalSize => CanvasScaler.ScaleMode.ConstantPhysicalSize,
                _ => throw new ArgumentOutOfRangeException(nameof(scaleMode), scaleMode,
                    null)
            };
        }

        public static CanvasScaler.ScreenMatchMode ToEngineType(this ScreenMatchMode screenMatchMode)
        {
            return screenMatchMode switch
            {
                ScreenMatchMode.MatchWidthOrHeight => CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,
                ScreenMatchMode.Expand => CanvasScaler.ScreenMatchMode.Expand,
                ScreenMatchMode.Shrink => CanvasScaler.ScreenMatchMode.Shrink,
                _ => throw new ArgumentOutOfRangeException(nameof(screenMatchMode), screenMatchMode,
                    null)
            };
        }

        public static CanvasScaler.Unit ToEngineType(this Unit unit)
        {
            return unit switch
            {
                Unit.Centimeters => CanvasScaler.Unit.Centimeters,
                Unit.Millimeters => CanvasScaler.Unit.Millimeters,
                Unit.Inches => CanvasScaler.Unit.Inches,
                Unit.Points => CanvasScaler.Unit.Points,
                Unit.Picas => CanvasScaler.Unit.Picas,
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit,
                    null)
            };
        }

        public static RenderMode ToEngineType(this Sample.Unity.UI.RenderMode renderMode)
        {
            return renderMode switch
            {
                Sample.Unity.UI.RenderMode.ScreenSpaceOverlay => RenderMode.ScreenSpaceOverlay,
                Sample.Unity.UI.RenderMode.ScreenSpaceCamera => RenderMode.ScreenSpaceCamera,
                Sample.Unity.UI.RenderMode.WorldSpace => RenderMode.WorldSpace,
                _ => throw new ArgumentOutOfRangeException(nameof(renderMode), renderMode,
                    null)
            };
        }

        public static StandaloneRenderResize ToEngineType(
            this Sample.Unity.UI.StandaloneRenderResize standaloneRenderResize)
        {
            return standaloneRenderResize switch
            {
                Sample.Unity.UI.StandaloneRenderResize.Enabled => StandaloneRenderResize.Enabled,
                Sample.Unity.UI.StandaloneRenderResize.Disabled => StandaloneRenderResize.Disabled,
                _ => throw new ArgumentOutOfRangeException(nameof(standaloneRenderResize), standaloneRenderResize,
                    null)
            };
        }

        public static FontStyle ToEngineType(this Sample.Unity.UI.FontStyle fontStyle)
        {
            return fontStyle switch
            {
                Sample.Unity.UI.FontStyle.Normal => FontStyle.Normal,
                Sample.Unity.UI.FontStyle.Bold => FontStyle.Bold,
                Sample.Unity.UI.FontStyle.Italic => FontStyle.Italic,
                Sample.Unity.UI.FontStyle.BoldAndItalic => FontStyle.BoldAndItalic,
                _ => throw new ArgumentOutOfRangeException(nameof(fontStyle), fontStyle,
                    null)
            };
        }

        public static TextAnchor ToEngineType(this Sample.Unity.UI.TextAnchor textAnchor)
        {
            return textAnchor switch
            {
                Sample.Unity.UI.TextAnchor.UpperLeft => TextAnchor.UpperLeft,
                Sample.Unity.UI.TextAnchor.UpperCenter => TextAnchor.UpperCenter,
                Sample.Unity.UI.TextAnchor.UpperRight => TextAnchor.UpperRight,
                Sample.Unity.UI.TextAnchor.MiddleLeft => TextAnchor.MiddleLeft,
                Sample.Unity.UI.TextAnchor.MiddleCenter => TextAnchor.MiddleCenter,
                Sample.Unity.UI.TextAnchor.MiddleRight => TextAnchor.MiddleRight,
                Sample.Unity.UI.TextAnchor.LowerLeft => TextAnchor.LowerLeft,
                Sample.Unity.UI.TextAnchor.LowerCenter => TextAnchor.LowerCenter,
                Sample.Unity.UI.TextAnchor.LowerRight => TextAnchor.LowerRight,
                _ => throw new ArgumentOutOfRangeException(nameof(textAnchor), textAnchor,
                    null)
            };
        }

        public static HorizontalWrapMode ToEngineType(this Sample.Unity.UI.HorizontalWrapMode horizontalWrapMode)
        {
            return horizontalWrapMode switch
            {
                Sample.Unity.UI.HorizontalWrapMode.Wrap => HorizontalWrapMode.Wrap,
                Sample.Unity.UI.HorizontalWrapMode.Overflow => HorizontalWrapMode.Overflow,
                _ => throw new ArgumentOutOfRangeException(nameof(horizontalWrapMode), horizontalWrapMode,
                    null)
            };
        }

        public static VerticalWrapMode ToEngineType(this Sample.Unity.UI.VerticalWrapMode verticalWrapMode)
        {
            return verticalWrapMode switch
            {
                Sample.Unity.UI.VerticalWrapMode.Truncate => VerticalWrapMode.Truncate,
                Sample.Unity.UI.VerticalWrapMode.Overflow => VerticalWrapMode.Overflow,
                _ => throw new ArgumentOutOfRangeException(nameof(verticalWrapMode), verticalWrapMode,
                    null)
            };
        }

        public static Image.Type ToEngineType(this ImageType imageType)
        {
            return imageType switch
            {
                ImageType.Simple => Image.Type.Simple,
                ImageType.Sliced => Image.Type.Sliced,
                ImageType.Tiled => Image.Type.Tiled,
                ImageType.Filled => Image.Type.Filled,
                _ => throw new ArgumentOutOfRangeException(nameof(imageType), imageType,
                    null)
            };
        }

        public static LineAlignment ToEngineType(this Alignment lineAlignment)
        {
            return lineAlignment switch
            {
                Alignment.View => LineAlignment.View,
                Alignment.TransformZ => LineAlignment.TransformZ,
                _ => throw new ArgumentOutOfRangeException(nameof(lineAlignment), lineAlignment, null)
            };
        }
        
        public static LineTextureMode ToEngineType(this TextureMode lineTextureMode)
        {
            return lineTextureMode switch
            {
                TextureMode.Stretch => LineTextureMode.Stretch,
                TextureMode.Tile => LineTextureMode.Tile,
                TextureMode.DistributePerSegment => LineTextureMode.DistributePerSegment,
                TextureMode.RepeatPerSegment => LineTextureMode.RepeatPerSegment,
                TextureMode.Static => LineTextureMode.Static,
                _ => throw new ArgumentOutOfRangeException(nameof(lineTextureMode), lineTextureMode, null)
            };
        }
        
        public static SpriteMaskInteraction ToEngineType(this MaskInteraction spriteMaskInteraction)
        {
            return spriteMaskInteraction switch
            {
                MaskInteraction.None => SpriteMaskInteraction.None,
                MaskInteraction.VisibleInside => SpriteMaskInteraction.VisibleInsideMask,
                MaskInteraction.VisibleOutside => SpriteMaskInteraction.VisibleOutsideMask,
                _ => throw new ArgumentOutOfRangeException(nameof(spriteMaskInteraction), spriteMaskInteraction, null)
            };
        }

        public static GradientMode ToEngineType(this ColorGradient.Types.GradientMode gradientMode)
        {
            return gradientMode switch
            {
                ColorGradient.Types.GradientMode.Blend => GradientMode.Blend,
                ColorGradient.Types.GradientMode.Fixed => GradientMode.Fixed,
                _ => throw new ArgumentOutOfRangeException(nameof(gradientMode), gradientMode, null)
            };
        }
        
        public static ColorSpace ToEngineType(this Sample.Common.ColorSpace colorSpace)
        {
            return colorSpace switch
            {
                Sample.Common.ColorSpace.Uninitialized => ColorSpace.Uninitialized,
                Sample.Common.ColorSpace.Gamma => ColorSpace.Gamma,
                Sample.Common.ColorSpace.Linear => ColorSpace.Linear,
                _ => throw new ArgumentOutOfRangeException(nameof(colorSpace), colorSpace, null)
            };
        }
        
        public static WeightedMode ToEngineType(this Sample.Common.WeightedMode weightedMode)
        {
            return weightedMode switch
            {
                Sample.Common.WeightedMode.None => WeightedMode.None,
                Sample.Common.WeightedMode.In => WeightedMode.In,
                Sample.Common.WeightedMode.Out => WeightedMode.Out,
                Sample.Common.WeightedMode.Both => WeightedMode.Both,
                _ => throw new ArgumentOutOfRangeException(nameof(weightedMode), weightedMode, null)
            };
        }
        
        public static AnimationCurve ToEngineType(this Sample.Common.AnimationCurve curve)
        {
            var animationCurve = new AnimationCurve();

            foreach (var keyframe in curve.Keyframes)
            {
                animationCurve.AddKey(new Keyframe
                {
                    time = keyframe.Time,
                    value = keyframe.Value,
                    inTangent = keyframe.InTangent,
                    outTangent = keyframe.OutTangent,
                    inWeight = keyframe.InWeight,
                    outWeight = keyframe.OutWeight,
                    weightedMode = keyframe.WeightedMode.ToEngineType()
                });
            }
            
            return animationCurve;
        }
        
        public static Gradient ToEngineType(this ColorGradient gradient)
        {
            var colorKeys = new GradientColorKey[gradient.ColorKeys.Count];
            for (var i = 0; i < gradient.ColorKeys.Count; ++i)
            {
                colorKeys[i] = new GradientColorKey
                {
                    color = gradient.ColorKeys[i].Color.ToEngineType(),
                    time = gradient.ColorKeys[i].Time
                };
            }

            var alphaKeys = new GradientAlphaKey[gradient.AlphaKeys.Count];
            for (var i = 0; i < gradient.AlphaKeys.Count; ++i)
            {
                alphaKeys[i] = new GradientAlphaKey
                {
                    alpha = gradient.AlphaKeys[i].Alpha,
                    time = gradient.AlphaKeys[i].Time
                };
            }

            return new Gradient
            {
                colorKeys = colorKeys,
                alphaKeys = alphaKeys,
                colorSpace = gradient.ColorSpace.ToEngineType(),
                mode = gradient.Mode.ToEngineType()
            };
        }
    }
}