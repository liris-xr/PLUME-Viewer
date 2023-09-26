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
        private readonly AssetBundleCreateRequest _assetBundleCreateRequest;

        public PlayerAssets(string assetBundlePath)
        {
            _assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(assetBundlePath);
        }

        public float GetLoadingProgress()
        {
            return _assetBundleCreateRequest.progress;
        }

        public bool IsLoaded()
        {
            return _assetBundleCreateRequest.isDone;
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
            if (!_assetBundleCreateRequest.isDone)
                throw new Exception("Asset bundle is not fully loaded yet.");
            
            var assets = _assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssets(assetPath, type);
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
                "Cube" => Resources.GetBuiltinResource<Mesh>("Cube.fbx"),
                "Sphere" => Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx"),
                "Plane" => Resources.GetBuiltinResource<Mesh>("New-Plane.fbx"),
                "Capsule" => Resources.GetBuiltinResource<Mesh>("New-Capsule.fbx"),
                "Cylinder" => Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx"),
                "Quad" => Resources.GetBuiltinResource<Mesh>("Quad.fbx"),
                _ => null
            };
        }
    }
}