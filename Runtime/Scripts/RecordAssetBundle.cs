using System;
using System.Linq;
using PLUME.Sample.Unity;
using PLUME.Viewer.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PLUME
{
    public class RecordAssetBundle
    {
        private readonly AssetBundle _assetBundle;

        public RecordAssetBundle(AssetBundle assetBundle)
        {
            _assetBundle = assetBundle;
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
            var assets = _assetBundle.LoadAssetWithSubAssets(assetPath, assetType);
            return assets.FirstOrDefault(asset => asset.name == assetName);
        }

        private static Object LoadBuiltinAsset(Type assetType, string assetPath, string assetName)
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

            if (assetType == typeof(Sprite))
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

            if (assetType == typeof(Font))
            {
                return assetName switch
                {
                    "LegacyRuntime" => builtinAssets.legacyRuntime,
                    _ => null
                };
            }

            return null;
        }
    }
}