using System;
using System.IO;
using System.Linq;
using PLUME.Sample.Unity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PLUME.Viewer.Player
{
    public class PlayerAssets
    {
        private AssetBundle _assetBundle;
        private readonly string _assetBundlePath;
        private readonly AssetBundleCreateRequest _assetBundleCreateRequest;

        public PlayerAssets(string assetBundlePath)
        {
            _assetBundlePath = assetBundlePath;
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
            if (identifier.Id == "00000000000000000000000000000000")
                return null;

            if (string.IsNullOrEmpty(identifier.Path))
                return null;

            var splitAssetIdentifier = identifier.Path.Split(":", 4);

            var assetSource = splitAssetIdentifier[0];
            var assetTypeName = splitAssetIdentifier[1];
            var assetPath = splitAssetIdentifier[2];
            var assetName = splitAssetIdentifier[3];

            var assetType = Type.GetType(assetTypeName) ?? typeof(Object);

            var asset = assetSource switch
            {
                "Custom" => LoadCustomAsset(assetType, assetPath, assetName),
                "Builtin" => LoadBuiltinAsset(assetType, assetPath, assetName),
                _ => null
            } as T;

            return asset;
        }

        private Object LoadCustomAsset(Type assetType, string assetPath, string assetName)
        {
            if (!_assetBundleCreateRequest.isDone)
                throw new Exception("Asset bundle is not fully loaded yet.");

            var assets = GetAssetBundle().LoadAssetWithSubAssets(assetPath, assetType);
            return assets.FirstOrDefault(asset => asset.name == assetName);
        }

        private Object LoadBuiltinAsset(Type assetType, string assetPath, string assetName)
        {
            var builtinAssets = Object.FindAnyObjectByType<BuiltinAssets>();

            if (builtinAssets == null)
            {
                Debug.LogWarning("BuiltinAssets not found. Cannot load builtin asset.");
                return null;
            }
            
            // TODO: embed resources in the asset bundle
            
            if (assetType == typeof(Material))
            {
                return assetName switch
                {
                    "Default-Skybox" => builtinAssets.defaultSkybox,
                    "Default-Material" => builtinAssets.defaultMaterial,
                    "Default-Diffuse" => builtinAssets.defaultDiffuse,
                    "Default-Terrain-Standard" => builtinAssets.defaultTerrainStandard,
                    _ => null
                };
            }

            if (assetType == typeof(Mesh))
            {
                return assetName switch
                {
                    "Cube" => builtinAssets.cube,
                    "Sphere" => builtinAssets.sphere,
                    "Plane" => builtinAssets.plane,
                    "Capsule" => builtinAssets.capsule,
                    "Cylinder" => builtinAssets.cylinder,
                    "Quad" => builtinAssets.quad,
                    _ => null
                };
            }
            
            if(assetType == typeof(Sprite))
            {
                return assetName switch
                {
                    "Background" => builtinAssets.background,
                    "Checkmark" => builtinAssets.checkmark,
                    "DropdownArrow" => builtinAssets.dropdownArrow,
                    "InputFieldBackground" => builtinAssets.inputFieldBackground,
                    "Knob" => builtinAssets.knob,
                    "UISprite" => builtinAssets.uiSprite,
                    "UIMask" => builtinAssets.uiSprite,
                    _ => null
                };
            }
            
            if(assetType == typeof(Font))
            {
                return assetName switch
                {
                    "LegacyRuntime" => builtinAssets.legacyRuntime,
                    _ => null
                };
            }

            return null;
        }

        public AssetBundle GetAssetBundle()
        {
            if (_assetBundle != null) return _assetBundle;

            if (!_assetBundleCreateRequest.isDone)
            {
                return null;
            }

            var assetBundleName = Path.GetFileName(_assetBundlePath);
            _assetBundle = AssetBundle.GetAllLoadedAssetBundles()
                .FirstOrDefault(bundle => bundle.name == assetBundleName);
            return _assetBundle;
        }
    }
}