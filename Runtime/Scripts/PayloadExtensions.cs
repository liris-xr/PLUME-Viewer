using System;
using PLUME.Sample.Common;
using PLUME.Sample.Unity;
using UnityEngine;
using Bounds = PLUME.Sample.Common.Bounds;
using Color = PLUME.Sample.Common.Color;
using FogMode = PLUME.Sample.Unity.FogMode;
using LightShadowCasterMode = PLUME.Sample.Unity.LightShadowCasterMode;
using LightShadows = PLUME.Sample.Unity.LightShadows;
using LightShape = PLUME.Sample.Unity.LightShape;
using LightType = PLUME.Sample.Unity.LightType;
using Matrix4x4 = PLUME.Sample.Common.Matrix4x4;
using Quaternion = PLUME.Sample.Common.Quaternion;
using Vector2 = PLUME.Sample.Common.Vector2;
using Vector3 = PLUME.Sample.Common.Vector3;
using Vector4 = PLUME.Sample.Common.Vector4;

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

        public static UnityEngine.Vector2 ToEngineType(this Vector2 vec)
        {
            return new UnityEngine.Vector2
            {
                x = vec.X,
                y = vec.Y,
            };
        }

        public static UnityEngine.Vector3 ToEngineType(this Vector3 vec)
        {
            return new UnityEngine.Vector3
            {
                x = vec.X,
                y = vec.Y,
                z = vec.Z
            };
        }

        public static UnityEngine.Vector4 ToEngineType(this Vector4 vec)
        {
            return new UnityEngine.Vector4
            {
                x = vec.X,
                y = vec.Y,
                z = vec.Z,
                w = vec.W
            };
        }

        public static UnityEngine.Quaternion ToEngineType(this Quaternion vec)
        {
            return new UnityEngine.Quaternion
            {
                x = vec.X,
                y = vec.Y,
                z = vec.Z,
                w = vec.W
            };
        }

        public static UnityEngine.Color ToEngineType(this Color color)
        {
            return new UnityEngine.Color
            {
                r = color.R,
                g = color.G,
                b = color.B,
                a = color.A
            };
        }

        public static UnityEngine.Rendering.SphericalHarmonicsL2 ToEngineType(
            this SphericalHarmonicsL2 sphericalHarmonicsL2)
        {
            var shl2 = new UnityEngine.Rendering.SphericalHarmonicsL2
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

        public static UnityEngine.LightType ToEngineType(this LightType lightType)
        {
            return lightType switch
            {
                LightType.Point => UnityEngine.LightType.Point,
                LightType.Directional => UnityEngine.LightType.Directional,
                LightType.Spot => UnityEngine.LightType.Spot,
                LightType.Rectangle => UnityEngine.LightType.Rectangle,
                LightType.Disc => UnityEngine.LightType.Disc,
                LightType.Area => UnityEngine.LightType.Area,
                _ => throw new ArgumentOutOfRangeException(nameof(lightType), lightType, null)
            };
        }

        public static UnityEngine.LightShape ToEngineType(this LightShape lightShape)
        {
            return lightShape switch
            {
                LightShape.Cone => UnityEngine.LightShape.Cone,
                LightShape.Pyramid => UnityEngine.LightShape.Pyramid,
                LightShape.Box => UnityEngine.LightShape.Box,
                _ => throw new ArgumentOutOfRangeException(nameof(lightShape), lightShape, null)
            };
        }

        public static UnityEngine.LightShadows ToEngineType(this LightShadows lightShadows)
        {
            return lightShadows switch
            {
                LightShadows.None => UnityEngine.LightShadows.None,
                LightShadows.Hard => UnityEngine.LightShadows.Hard,
                LightShadows.Soft => UnityEngine.LightShadows.Soft,
                _ => throw new ArgumentOutOfRangeException(nameof(lightShadows), lightShadows, null)
            };
        }

        public static UnityEngine.Rendering.LightShadowResolution ToEngineType(
            this LightShadowResolution lightShadowResolution)
        {
            return lightShadowResolution switch
            {
                LightShadowResolution.FromQualitySettings => UnityEngine.Rendering.LightShadowResolution
                    .FromQualitySettings,
                LightShadowResolution.Low => UnityEngine.Rendering.LightShadowResolution.Low,
                LightShadowResolution.Medium => UnityEngine.Rendering.LightShadowResolution.Medium,
                LightShadowResolution.High => UnityEngine.Rendering.LightShadowResolution.High,
                LightShadowResolution.VeryHigh => UnityEngine.Rendering.LightShadowResolution.VeryHigh,
                _ => throw new ArgumentOutOfRangeException(nameof(lightShadowResolution), lightShadowResolution, null)
            };
        }

        public static UnityEngine.LightShadowCasterMode ToEngineType(this LightShadowCasterMode lightShadowCasterMode)
        {
            return lightShadowCasterMode switch
            {
                LightShadowCasterMode.Default => UnityEngine.LightShadowCasterMode.Default,
                LightShadowCasterMode.NonLightmappedOnly => UnityEngine.LightShadowCasterMode.NonLightmappedOnly,
                LightShadowCasterMode.Everything => UnityEngine.LightShadowCasterMode.Everything,
                _ => throw new ArgumentOutOfRangeException(nameof(lightShadowCasterMode), lightShadowCasterMode, null)
            };
        }

        public static UnityEngine.FogMode ToEngineType(this FogMode fogMode)
        {
            return fogMode switch
            {
                FogMode.Linear => UnityEngine.FogMode.Linear,
                FogMode.Exponential => UnityEngine.FogMode.Exponential,
                FogMode.ExponentialSquared => UnityEngine.FogMode.ExponentialSquared,
                _ => throw new ArgumentOutOfRangeException(nameof(fogMode), fogMode, null)
            };
        }

        public static UnityEngine.Rendering.DefaultReflectionMode ToEngineType(
            this DefaultReflectionMode defaultReflectionMode)
        {
            return defaultReflectionMode switch
            {
                DefaultReflectionMode.Skybox => UnityEngine.Rendering.DefaultReflectionMode.Skybox,
                DefaultReflectionMode.Custom => UnityEngine.Rendering.DefaultReflectionMode.Custom,
                _ => throw new ArgumentOutOfRangeException(nameof(defaultReflectionMode), defaultReflectionMode, null)
            };
        }

        public static UnityEngine.Rendering.AmbientMode ToEngineType(this AmbientMode ambientMode)
        {
            return ambientMode switch
            {
                AmbientMode.Skybox => UnityEngine.Rendering.AmbientMode.Skybox,
                AmbientMode.Trilight => UnityEngine.Rendering.AmbientMode.Trilight,
                AmbientMode.Flat => UnityEngine.Rendering.AmbientMode.Flat,
                AmbientMode.Custom => UnityEngine.Rendering.AmbientMode.Custom,
                _ => throw new ArgumentOutOfRangeException(nameof(ambientMode), ambientMode, null)
            };
        }
        
        public static UnityEngine.Bounds ToEngineType(this Bounds bounds)
        {
            return new UnityEngine.Bounds
            {
                center = bounds.Center.ToEngineType(),
                extents = bounds.Extents.ToEngineType()
            };
        }

        public static UnityEngine.Matrix4x4 ToEngineType(this Matrix4x4 mtx)
        {
            return new UnityEngine.Matrix4x4
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
    }
}