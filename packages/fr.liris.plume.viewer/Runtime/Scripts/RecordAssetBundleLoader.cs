using System.IO;
using System.IO.Compression;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PLUME.Viewer.Player
{
    public class BundleLoader
    {
        private readonly string _bundlePath;
        private AssetBundleCreateRequest _assetBundleCreateRequest;
        private AssetBundleCreateRequest _sceneBundleCreateRequest;

        private LoadingStatus _loadingStatus;

        public BundleLoader(string bundlePath)
        {
            _loadingStatus = LoadingStatus.NotLoading;
            _bundlePath = bundlePath;
            
            if (!bundlePath.EndsWith(".zip"))
                throw new System.Exception("Bundle path should be a zip file");
        }

        public async UniTask<RecordAssetBundle> LoadAsync()
        {
            // Unzip the bundlePath zip file in the temporary directory
            var tempDirectory = Path.Combine(Path.GetTempPath(), "plume_bundle");
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);
            Directory.CreateDirectory(tempDirectory);
            
            await UniTask.RunOnThreadPool(() => ZipFile.ExtractToDirectory(_bundlePath, tempDirectory));
            
            var assetBundlePath = Path.Combine(tempDirectory, "plume_assets");
            var sceneBundlePath = Path.Combine(tempDirectory, "plume_scenes");

            // Optional name->hash manifest for diffusion profiles (see RecordAssetBundle.DiffusionProfileHashes).
            // Absent in bundles built before the manifest was introduced.
            DiffusionProfileHashManifest diffusionProfileHashManifest = null;
            var manifestPath = Path.Combine(tempDirectory, "plume_diffusion_hashes.json");
            if (File.Exists(manifestPath))
                diffusionProfileHashManifest =
                    JsonUtility.FromJson<DiffusionProfileHashManifest>(File.ReadAllText(manifestPath));
            
            var assetBundleName = Path.GetFileName(assetBundlePath);
            var assetBundle = AssetBundle.GetAllLoadedAssetBundles()
                .FirstOrDefault(bundle => bundle.name == assetBundleName);

            if (assetBundle == null)
            {
                if (!File.Exists(assetBundlePath))
                    throw new FileNotFoundException(
                        $"The bundle archive {_bundlePath} does not contain an asset bundle.", assetBundlePath);

                _loadingStatus = LoadingStatus.Loading;
                _assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(assetBundlePath);
                // The scene bundle is loaded so that its contents are registered with Unity, but the viewer builds
                // replay scenes itself and never reads assets back out of it.
                _sceneBundleCreateRequest = AssetBundle.LoadFromFileAsync(sceneBundlePath);
                await _assetBundleCreateRequest;
                await _sceneBundleCreateRequest;
                assetBundle = _assetBundleCreateRequest.assetBundle;

                if (assetBundle == null)
                {
                    _loadingStatus = LoadingStatus.NotLoading;
                    throw new InvalidDataException(
                        $"Failed to load the asset bundle from {assetBundlePath}. The bundle is either corrupt or " +
                        $"was built with a different Unity version than this viewer ({Application.unityVersion}).");
                }

                await assetBundle.LoadAllAssetsAsync();
                _loadingStatus = LoadingStatus.Done;
            }

            return new RecordAssetBundle(assetBundle, diffusionProfileHashManifest);
        }

        public float GetLoadingProgress()
        {
            return _loadingStatus switch
            {
                LoadingStatus.Done => 1,
                LoadingStatus.NotLoading => 0,
                _ => (_assetBundleCreateRequest.progress + _sceneBundleCreateRequest.progress) / 2
            };
        }

        public bool IsLoaded()
        {
            return _loadingStatus == LoadingStatus.Done;
        }

        public enum LoadingStatus
        {
            NotLoading,
            Loading,
            Done
        }
    }
}