using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PLUME.Viewer.Player
{
    public class BundleLoader
    {
        public enum LoadingStatus
        {
            NotLoading,
            Loading,
            Done
        }

        private readonly string _bundlePath;
        private AssetBundleCreateRequest _assetBundleCreateRequest;

        private LoadingStatus _loadingStatus;

        public BundleLoader(string bundlePath)
        {
            _loadingStatus = LoadingStatus.NotLoading;
            _bundlePath = bundlePath;

            if (!bundlePath.EndsWith(".zip"))
                throw new Exception("Bundle path should be a zip file");
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

            var assetBundleName = Path.GetFileName(assetBundlePath);
            var assetBundle = AssetBundle.GetAllLoadedAssetBundles()
                .FirstOrDefault(bundle => bundle.name == assetBundleName);

            if (assetBundle == null)
            {
                _loadingStatus = LoadingStatus.Loading;
                _assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(assetBundlePath);
                await _assetBundleCreateRequest;
                assetBundle = _assetBundleCreateRequest.assetBundle;
                await assetBundle.LoadAllAssetsAsync();
                _loadingStatus = LoadingStatus.Done;
            }

            return new RecordAssetBundle(assetBundle);
        }

        public float GetLoadingProgress()
        {
            return _loadingStatus switch
            {
                LoadingStatus.Done => 1,
                LoadingStatus.NotLoading => 0,
                _ => _assetBundleCreateRequest.progress
            };
        }

        public bool IsLoaded()
        {
            return _loadingStatus == LoadingStatus.Done;
        }
    }
}