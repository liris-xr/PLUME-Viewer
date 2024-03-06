using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PLUME.Viewer.Player
{
    public class AssetBundleLoader
    {
        private readonly string _assetBundlePath;
        private AssetBundleCreateRequest _assetBundleCreateRequest;

        private LoadingStatus _loadingStatus;

        public AssetBundleLoader(string assetBundlePath)
        {
            _loadingStatus = LoadingStatus.NotLoading;
            _assetBundlePath = assetBundlePath;
        }

        public async UniTask<RecordAssetBundle> LoadAsync()
        {
            var assetBundleName = Path.GetFileName(_assetBundlePath);
            var assetBundle = AssetBundle.GetAllLoadedAssetBundles()
                .FirstOrDefault(bundle => bundle.name == assetBundleName);

            if (assetBundle == null)
            {
                _loadingStatus = LoadingStatus.Loading;
                _assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(_assetBundlePath);
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

        public enum LoadingStatus
        {
            NotLoading,
            Loading,
            Done
        }
    }
}