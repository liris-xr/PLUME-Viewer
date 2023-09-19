using System;
using System.Linq;
using System.Reflection;
using PLUME.Sample.Unity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PLUME
{
    public class PlayerAssets
    {
        private readonly AssetBundle _assetBundle;

        public PlayerAssets(string assetBundlePath)
        {
            _assetBundle = AssetBundle.LoadFromFile(assetBundlePath);
        }

        public T GetOrDefaultAssetByIdentifier<T>(AssetIdentifier identifier) where T : Object
        {
            if (identifier == null)
                return null;

            if (string.IsNullOrEmpty(identifier.Path))
                return null;
            
            var splitAssetIdentifier = identifier.Path.Split(":", 4);

            var assetSource = splitAssetIdentifier[0];
            var assetType = splitAssetIdentifier[1];
            var assetPath = splitAssetIdentifier[2];
            var assetName = splitAssetIdentifier[3];

            var type = Type.GetType(assetType) ?? typeof(Object);

            var asset = assetSource switch
            {
                "Custom" => LoadCustomAsset(type, assetPath, assetName),
                "Builtin" => LoadBuiltinAsset(type, assetName),
                _ => null
            } as T;

            return asset;
        }

        private Object LoadCustomAsset(Type type, string assetPath, string assetName)
        {
            var assets = _assetBundle.LoadAssetWithSubAssets(assetPath, type);
            return assets.FirstOrDefault(asset => asset.name == assetName);
        }

        private Object LoadBuiltinAsset(Type type, string assetName)
        {
            // TODO: embed resources in the asset bundle
            return assetName switch
            {
                "Default-Skybox" => new Material(Shader.Find("Skybox/Procedural")) { name = "Default-Skybox" },
                "Default-Material" => new Material(Shader.Find("Standard")) { name = "Default-Material" },
                "Default-Diffuse" => new Material(Shader.Find("Legacy Shaders/Diffuse")) { name = "Default-Diffuse" },
                "Default-Terrain-Standard" => new Material(Shader.Find("Nature/Terrain/Standard")) { name = "Default-Terrain-Standard" },
                "Cube" => Resources.GetBuiltinResource(type, "Cube.fbx"),
                "Sphere" => Resources.GetBuiltinResource(type, "New-Sphere.fbx"),
                "Plane" => Resources.GetBuiltinResource(type, "New-Plane.fbx"),
                "Capsule" => Resources.GetBuiltinResource(type, "New-Capsule.fbx"),
                "Cylinder" => Resources.GetBuiltinResource(type, "New-Cylinder.fbx"),
                "Quad" => Resources.GetBuiltinResource(type, "Quad.fbx"),
                _ => null
            };
        }
    }
}