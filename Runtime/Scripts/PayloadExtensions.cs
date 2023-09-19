using System;
using PLUME.Sample.Unity;
using UnityEngine;
using UnityEngine.Rendering;
using AmbientMode = UnityEngine.Rendering.AmbientMode;
using DefaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode;
using FogMode = UnityEngine.FogMode;
using LightmapData = UnityEngine.LightmapData;
using LightmapsMode = UnityEngine.LightmapsMode;
using LightShadowCasterMode = UnityEngine.LightShadowCasterMode;
using LightShadowResolution = UnityEngine.Rendering.LightShadowResolution;
using LightShadows = UnityEngine.LightShadows;
using LightShape = UnityEngine.LightShape;
using LightType = UnityEngine.LightType;
using ReflectionProbeClearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags;
using ReflectionProbeMode = UnityEngine.Rendering.ReflectionProbeMode;
using ReflectionProbeRefreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode;
using ReflectionProbeTimeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode;
using ReflectionProbeUsage = PLUME.Sample.Unity.ReflectionProbeUsage;
using ShadowCastingMode = PLUME.Sample.Unity.ShadowCastingMode;

namespace PLUME
{
    public static class PayloadExtensions
    {
        public static TransformGameObjectIdentifier ToIdentifierPayload(this Transform t)
        {
            return new TransformGameObjectIdentifier
            {
                TransformId = t.GetHashCode().ToString(),
                GameObjectId = t.gameObject.GetHashCode().ToString()
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
                Sample.Unity.LightType.Area => LightType.Area,
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

        public static FogMode ToEngineType(this Sample.Unity.FogMode fogMode)
        {
            return fogMode switch
            {
                Sample.Unity.FogMode.Linear => FogMode.Linear,
                Sample.Unity.FogMode.Exponential => FogMode.Exponential,
                Sample.Unity.FogMode.ExponentialSquared => FogMode.ExponentialSquared,
                _ => throw new ArgumentOutOfRangeException(nameof(fogMode), fogMode, null)
            };
        }

        public static DefaultReflectionMode ToEngineType(
            this Sample.Unity.DefaultReflectionMode defaultReflectionMode)
        {
            return defaultReflectionMode switch
            {
                Sample.Unity.DefaultReflectionMode.Skybox => DefaultReflectionMode.Skybox,
                Sample.Unity.DefaultReflectionMode.Custom => DefaultReflectionMode.Custom,
                _ => throw new ArgumentOutOfRangeException(nameof(defaultReflectionMode), defaultReflectionMode, null)
            };
        }

        public static AmbientMode ToEngineType(this Sample.Unity.AmbientMode ambientMode)
        {
            return ambientMode switch
            {
                Sample.Unity.AmbientMode.Skybox => AmbientMode.Skybox,
                Sample.Unity.AmbientMode.Trilight => AmbientMode.Trilight,
                Sample.Unity.AmbientMode.Flat => AmbientMode.Flat,
                Sample.Unity.AmbientMode.Custom => AmbientMode.Custom,
                _ => throw new ArgumentOutOfRangeException(nameof(ambientMode), ambientMode, null)
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
        
        public static ReflectionProbeRefreshMode ToEngineType(this Sample.Unity.ReflectionProbeRefreshMode reflectionProbeRefreshMode)
        {
            return reflectionProbeRefreshMode switch
            {
                Sample.Unity.ReflectionProbeRefreshMode.EveryFrame => ReflectionProbeRefreshMode.EveryFrame,
                Sample.Unity.ReflectionProbeRefreshMode.OnAwake => ReflectionProbeRefreshMode.OnAwake,
                Sample.Unity.ReflectionProbeRefreshMode.ViaScripting => ReflectionProbeRefreshMode.ViaScripting,
                _ => throw new ArgumentOutOfRangeException(nameof(reflectionProbeRefreshMode), reflectionProbeRefreshMode, null)
            };
        }
        
        public static ReflectionProbeTimeSlicingMode ToEngineType(this Sample.Unity.ReflectionProbeTimeSlicingMode reflectionProbeTimeSlicingMode)
        {
            return reflectionProbeTimeSlicingMode switch
            {
                Sample.Unity.ReflectionProbeTimeSlicingMode.NoTimeSlicing => ReflectionProbeTimeSlicingMode.NoTimeSlicing,
                Sample.Unity.ReflectionProbeTimeSlicingMode.IndividualFaces => ReflectionProbeTimeSlicingMode.IndividualFaces,
                Sample.Unity.ReflectionProbeTimeSlicingMode.AllFacesAtOnce => ReflectionProbeTimeSlicingMode.AllFacesAtOnce,
                _ => throw new ArgumentOutOfRangeException(nameof(reflectionProbeTimeSlicingMode), reflectionProbeTimeSlicingMode, null)
            };
        }
        
        public static ReflectionProbeClearFlags ToEngineType(this Sample.Unity.ReflectionProbeClearFlags reflectionProbeClearFlags)
        {
            return reflectionProbeClearFlags switch
            {
                Sample.Unity.ReflectionProbeClearFlags.Skybox => ReflectionProbeClearFlags.Skybox,
                Sample.Unity.ReflectionProbeClearFlags.SolidColor => ReflectionProbeClearFlags.SolidColor,
                _ => throw new ArgumentOutOfRangeException(nameof(reflectionProbeClearFlags), reflectionProbeClearFlags, null)
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
        
        public static UnityEngine.Rendering.ShadowCastingMode ToEngineType(this ShadowCastingMode shadowCastingMode)
        {
            return shadowCastingMode switch
            {
                ShadowCastingMode.Off => UnityEngine.Rendering.ShadowCastingMode.Off,
                ShadowCastingMode.On => UnityEngine.Rendering.ShadowCastingMode.On,
                ShadowCastingMode.ShadowsOnly => UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly,
                ShadowCastingMode.TwoSided => UnityEngine.Rendering.ShadowCastingMode.TwoSided,
                _ => throw new ArgumentOutOfRangeException(nameof(shadowCastingMode), shadowCastingMode,
                    null)
            };
        }
        
        public static UnityEngine.Rendering.ReflectionProbeUsage ToEngineType(this ReflectionProbeUsage reflectionProbeUsage)
        {
            return reflectionProbeUsage switch
            {
                ReflectionProbeUsage.Off => UnityEngine.Rendering.ReflectionProbeUsage.Off,
                ReflectionProbeUsage.BlendProbes => UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes,
                ReflectionProbeUsage.BlendProbesAndSkybox => UnityEngine.Rendering.ReflectionProbeUsage.BlendProbesAndSkybox,
                ReflectionProbeUsage.Simple => UnityEngine.Rendering.ReflectionProbeUsage.Simple,
                _ => throw new ArgumentOutOfRangeException(nameof(reflectionProbeUsage), reflectionProbeUsage,
                    null)
            };
        }
    }
}