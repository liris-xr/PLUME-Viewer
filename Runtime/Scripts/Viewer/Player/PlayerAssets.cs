using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PLUME
{
    // TODO: only load assets when needed.
    // For this we need to keep a correspondence between asset ID/GUID and name (can be built in the editor once along with the asset bundle)
    public class PlayerAssets
    {
        // TODO: move this to a different class
        // TODO: add one dictionary for each asset type to lower the risks of collisions?
        private readonly Dictionary<int, Object> _assetsRegistry = new();

        public void RegisterBuiltinAssets()
        {
            // Add builtin resources manually as we can't add them in the asset bundle during export
            // TODO: export a custom builtin asset bundle to make sure those are not stripped from the build
            RegisterAsset(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
            RegisterAsset(Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx"));
            RegisterAsset(Resources.GetBuiltinResource<Mesh>("New-Plane.fbx"));
            RegisterAsset(Resources.GetBuiltinResource<Mesh>("New-Capsule.fbx"));
            RegisterAsset(Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx"));
            RegisterAsset(Resources.GetBuiltinResource<Mesh>("Quad.fbx"));
            RegisterAsset(new Material(Shader.Find("Legacy Shaders/Diffuse")) { name = "Default-Diffuse" });
            RegisterAsset(new Material(Shader.Find("Standard")) { name = "Default-Material" });
            RegisterAsset(new Material(Shader.Find("Skybox/Procedural")) { name = "Default-Skybox" });
        }

        public void RegisterAllAssetsFromBundle(string assetBundlePath)
        {
            if (!File.Exists(assetBundlePath)) throw new Exception($"Asset bundle '{assetBundlePath}' not found.");

            var assetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            var assets = assetBundle.LoadAllAssets();

            foreach (var asset in assets)
            {
                RegisterAsset(asset);
            }
        }

        public void RegisterAsset(Object asset)
        {
            var hash = asset.GetRecorderHash();
            
            if (hash != 0)
            {
                if (asset is Mesh mesh)
                {
                    mesh.UploadMeshData(true);
                }
                
                if (!_assetsRegistry.TryAdd(hash, asset))
                {
                    Debug.LogWarning($"Hash collision detected for {asset}.");
                }
            }
        }

        public T FindAssetByHash<T>(int assetReferenceHash) where T : Object
        {
            _assetsRegistry.TryGetValue(assetReferenceHash, out var obj);
            return (T)obj;
        }
    }
}