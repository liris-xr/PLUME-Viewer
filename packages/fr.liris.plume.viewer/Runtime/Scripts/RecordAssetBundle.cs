using System;
using System.Linq;
using PLUME.Sample.Unity;
using PLUME.Viewer.Player;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace PLUME
{
    /// <summary>
    /// Name-to-hash manifest for the diffusion profiles packed in the bundle, written by the recorder next to the
    /// bundle files. HDRP derives a profile's hash from its asset GUID in the editor and bakes it into materials;
    /// when a profile is loaded from a bundle in the editor, HDRP re-derives the hash from the (nonexistent)
    /// AssetDatabase path and zeroes it, breaking the material match. The viewer restores hashes from this manifest.
    /// </summary>
    [Serializable]
    public class DiffusionProfileHashManifest
    {
        public System.Collections.Generic.List<DiffusionProfileHashEntry> entries = new();
    }

    [Serializable]
    public class DiffusionProfileHashEntry
    {
        public string name;
        public uint hash;
    }

    public class RecordAssetBundle
    {
        private readonly AssetBundle _assetBundle;
        private readonly DiffusionProfileHashManifest _diffusionProfileHashManifest;

        public RecordAssetBundle(AssetBundle assetBundle,
            DiffusionProfileHashManifest diffusionProfileHashManifest = null)
        {
            _assetBundle = assetBundle;
            _diffusionProfileHashManifest = diffusionProfileHashManifest;
        }

        /// <summary>
        /// Loads every <see cref="DiffusionProfileSettings"/> packed in the record's asset bundle. The recorder
        /// adds them explicitly (HDRP references them by GUID/hash, so the bundle dependency walk misses them), and
        /// the viewer must register them at runtime or subsurface materials fall back to the neutral profile and
        /// render red. Returns an empty array for non-HDRP records, whose bundles contain no diffusion profiles.
        /// When running in the editor, HDRP zeroes the hash of bundle-loaded profiles on load
        /// (no AssetDatabase path to derive it from), so the recorded hashes are restored from the bundle's
        /// manifest before returning; without them, materials cannot match the profiles and skin renders red.
        /// </summary>
        public DiffusionProfileSettings[] LoadAllDiffusionProfiles()
        {
            if (_assetBundle == null)
                return Array.Empty<DiffusionProfileSettings>();

            var profiles = _assetBundle.LoadAllAssets<DiffusionProfileSettings>();
            RestoreDiffusionProfileHashes(profiles);
            return profiles;
        }

        private void RestoreDiffusionProfileHashes(DiffusionProfileSettings[] profiles)
        {
            if (_diffusionProfileHashManifest == null || _diffusionProfileHashManifest.entries.Count == 0)
                return;

            var profileField = typeof(DiffusionProfileSettings).GetField("profile",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            foreach (var settings in profiles)
            {
                var entry = _diffusionProfileHashManifest.entries.Find(e => e.name == settings.name);
                if (entry == null)
                {
                    Debug.LogWarning($"No diffusion profile hash manifest entry for '{settings.name}'; " +
                                     "subsurface materials using it will not match it.");
                    continue;
                }

                var diffusionProfile = profileField?.GetValue(settings);
                var hashField = diffusionProfile?.GetType().GetField("hash",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (hashField == null)
                {
                    Debug.LogWarning("Could not reflect DiffusionProfile.hash in Unity " + Application.unityVersion +
                                     "; no diffusion profile hash was restored, so subsurface materials (skin in " +
                                     "particular) will not match their profile and render red.");
                    return;
                }

                hashField.SetValue(diffusionProfile, entry.hash);
            }
        }

        public T GetOrDefaultAssetByIdentifier<T>(AssetIdentifier identifier) where T : Object
        {
            // Null whenever the record left the corresponding message field unset.
            if (identifier == null)
                return null;

            if (string.IsNullOrEmpty(identifier.Guid) || Guid.Parse(identifier.Guid) == Guid.Empty)
                return null;

            if (string.IsNullOrEmpty(identifier.AssetBundlePath))
                return null;

            var splitAssetIdentifier = identifier.AssetBundlePath.Split(":", 4);

            if (splitAssetIdentifier.Length < 4)
            {
                Debug.LogWarning($"Asset {identifier.Guid} not loaded: its bundle path " +
                                 $"'{identifier.AssetBundlePath}' has {splitAssetIdentifier.Length} of the 4 expected " +
                                 "'source:type:path:name' parts.");
                return null;
            }

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
            // Instance first: this runs once per builtin asset referenced by the record, and a scene-wide search per
            // asset is what the singleton exists to avoid. The search remains as a fallback in case no Awake ran yet.
            var builtinAssets = BuiltinAssets.Instance;

            if (builtinAssets == null)
                builtinAssets = Object.FindAnyObjectByType<BuiltinAssets>();

            if (builtinAssets == null)
            {
                Debug.LogWarning($"Cannot load the builtin asset '{assetName}' ({assetType.Name}) from '{assetPath}': " +
                                 "no BuiltinAssets component exists in the loaded scenes.");
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
                    "Default-Line" => builtinAssets.defaultLine,
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