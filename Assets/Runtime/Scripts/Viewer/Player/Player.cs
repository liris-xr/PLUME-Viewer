#if UNITY_STANDALONE_WIN
#endif
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using PLUME.Viewer.Analysis;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Application = UnityEngine.Application;

namespace PLUME.Viewer.Player
{
    // TODO: decouple the player from the record loading process
    [DisallowMultipleComponent]
    public class Player : SingletonMonoBehaviour<Player>, IDisposable
    {
        public TypeRegistryProvider typeRegistryProvider;

        public bool loop;

        private float _playSpeed = 1;

        public PlayerModule[] PlayerModules { get; private set; }

        private RecordLoader _recordLoader;
        public Record Record { get; private set; }
        public bool IsRecordLoaded => Record != null;

        private BundleLoader _bundleLoader;
        public RecordAssetBundle RecordAssetBundle { get; private set; }
        public bool IsRecordAssetBundleLoaded => RecordAssetBundle != null;

        private PlayerContext _mainPlayerContext;
        private bool _isPlaying;
        private ulong _currentTimeNanoseconds;

        public RenderTexture PreviewRenderTexture { get; private set; }

        public event Action OnFinishLoading = delegate { };

        public FreeCamera freeCamera;
        public TopViewCamera topViewCamera;
        public MainCamera mainCamera;

        private PreviewCamera _currentCamera;
        
        public Action<IHierarchyUpdateEvent> mainContextUpdatedHierarchy;

        private AnalysisModule _generatingModule;
        private AnalysisModule _visibleHeatmapModule;

        public Action<AnalysisModule> onGeneratingModuleChanged;
        public Action<AnalysisModule> onVisibleHeatmapModuleChanged;

        [RuntimeInitializeOnLoadMethod]
        public static void OnInitialize()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        private new void Awake()
        {
            base.Awake();

            var recordPath = GetRecordPath();
            var bundlePath = GetBundlePath(recordPath);

            PreviewRenderTexture = RenderTexture.GetTemporary(1920, 1080);
            freeCamera.PreviewRenderTexture = PreviewRenderTexture;
            topViewCamera.PreviewRenderTexture = PreviewRenderTexture;
            mainCamera.PreviewRenderTexture = PreviewRenderTexture;
            freeCamera.transform.position = new Vector3(-2.24f, 1.84f, 0.58f);
            freeCamera.transform.rotation = Quaternion.Euler(25f, -140f, 0f);
            topViewCamera.transform.position = new Vector3(0, 3.25f, -4);
            topViewCamera.GetCamera().orthographicSize = 7;
            SetCurrentPreviewCamera(mainCamera);

            PlayerModules = FindObjectsOfType<PlayerModule>();
            _bundleLoader = new BundleLoader(bundlePath);

            var assetBundleLoadTask = _bundleLoader.LoadAsync().ContinueWith(async recordAssetBundle =>
            {
                RecordAssetBundle = recordAssetBundle;
                _mainPlayerContext = await PlayerContext.CreateMainPlayerContext(recordAssetBundle);
                _mainPlayerContext.updatedHierarchy += mainContextUpdatedHierarchy;
            });

            _recordLoader = new RecordLoader(recordPath, typeRegistryProvider.GetTypeRegistry());

            var recordLoadTask = _recordLoader.LoadAsync().ContinueWith(record => { Record = record; });

            OnFinishLoading += () =>
            {
                GraphicsSettings.defaultRenderPipeline =
                    RecordAssetBundle.GetOrDefaultAssetByIdentifier<RenderPipelineAsset>(Record.graphicsSettings
                        .DefaultRenderPipelineAssetId);
            };

            UniTask.WhenAll(recordLoadTask, assetBundleLoadTask).ContinueWith(() =>
            {
                OnFinishLoading();
            }).Forget();
        }

        private static string GetRecordPath()
        {
            // Try to get the record file path from the command line arguments
            var arguments = Environment.GetCommandLineArgs();

            if (arguments.Length > 0)
            {
                var lastArgument = arguments[^1];

                if (lastArgument.EndsWith(".plm") && File.Exists(lastArgument))
                {
                    return lastArgument;
                }
            }

#if UNITY_EDITOR
            var filePath = EditorUtility.OpenFilePanel("Open record file", Application.dataPath, "plm");

            if (!string.IsNullOrEmpty(filePath))
            {
                return filePath;
            }
#elif UNITY_STANDALONE_WIN
            using (var fd = new OpenFileDialog())
            {
                fd.Title = "Open record file";
                fd.Filter = "plm files (*.plm)|*.plm";
                fd.FilterIndex = 0;

                if (fd.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    return fd.FileName;
                }
            }
#endif

            Application.Quit(127);
            throw new FileNotFoundException("Failed to open record file.");
        }

        private static string GetBundlePath(string recordFilePath = null)
        {
            if (recordFilePath != null)
            {
                // Try to find the asset bundle file from the same directory as the record file
                var recordDirectory = Path.GetDirectoryName(recordFilePath);

                if (recordDirectory == null)
                {
                    Application.Quit(127);
                    throw new DirectoryNotFoundException("Failed to find the directory of the record file.");
                }

                var path = Path.Combine(recordDirectory, "plume_bundle.zip");

                if (File.Exists(path))
                {
                    return path;
                }
            }

#if UNITY_EDITOR
            var bundlePath = EditorUtility.OpenFilePanel("Open bundle", Environment.CurrentDirectory, "");

            if (!string.IsNullOrEmpty(bundlePath))
            {
                return bundlePath;
            }
#elif UNITY_STANDALONE_WIN
            using (var fd = new OpenFileDialog())
            {
                fd.Title = "Open asset bundle file";
                fd.Filter = "asset bundle (*.zip)|*.zip";
                fd.FilterIndex = 0;

                if (fd.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    return fd.FileName;
                }
            }
#endif

            Application.Quit(127);
            throw new FileNotFoundException("Failed to open bundle file.");
        }

        public void SetCurrentPreviewCamera(PreviewCamera camera)
        {
            var rt = RenderTexture.active;
            RenderTexture.active = PreviewRenderTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rt;

            freeCamera.SetEnabled(false);
            topViewCamera.SetEnabled(false);
            mainCamera.SetEnabled(false);
            _currentCamera = camera;
            camera.SetEnabled(true);
        }

        public PreviewCamera GetCurrentPreviewCamera()
        {
            return _currentCamera;
        }

        private void FixedUpdate()
        {
            if (_isPlaying)
            {
                PlayForward((ulong)(Time.fixedDeltaTime * _playSpeed * 1_000_000_000));
            }

            if (GetModuleGenerating() != null && _isPlaying)
            {
                PausePlaying();
            }
        }

        public void OnDestroy()
        {
            PreviewRenderTexture.Release();
            _recordLoader.Dispose();
        }

        public bool TogglePlaying()
        {
            return _isPlaying ? PausePlaying() : StartPlaying();
        }

        public bool StartPlaying()
        {
            if (_isPlaying) return false;

            _isPlaying = true;

            foreach (var playerModule in PlayerModules) playerModule.Reset();

            return true;
        }

        public bool PausePlaying()
        {
            if (!_isPlaying) return false;

            _isPlaying = false;

            return true;
        }

        public bool StopPlaying()
        {
            _isPlaying = false;
            _currentTimeNanoseconds = 0;

            foreach (var playerModule in PlayerModules) playerModule.Reset();
            _mainPlayerContext.Reset();

            return true;
        }

        public void JumpToTime(ulong time)
        {
            if (time == _currentTimeNanoseconds)
            {
                return;
            }

            if (time > _currentTimeNanoseconds)
            {
                PlayForward(time - _currentTimeNanoseconds);
            }
            else
            {
                foreach (var playerModule in PlayerModules) playerModule.Reset();
                _mainPlayerContext.Reset();

                _currentTimeNanoseconds = 0;
                PlayForward(time);
            }
        }

        private void PlayForward(ulong durationNanoseconds)
        {
            var endTime = _currentTimeNanoseconds + durationNanoseconds;

            var frames = Record.Frames.GetInTimeRange(_currentTimeNanoseconds, endTime);

            _mainPlayerContext.PlayFrames(PlayerModules, frames);

            _currentTimeNanoseconds = Math.Clamp(endTime, 0, Record.Duration + 1);

            if (endTime > Record.Duration)
            {
                if (loop)
                {
                    JumpToTime(0);
                }
                else
                {
                    _isPlaying = false;
                }
            }
        }

        public float GetRecordLoadingProgress()
        {
            return _recordLoader.Progress;
        }

        public float GetRecordAssetBundleLoadingProgress()
        {
            return _bundleLoader.GetLoadingProgress();
        }

        public void SetPlaySpeed(float playSpeed)
        {
            if (playSpeed < 0)
            {
                _playSpeed = 0;
            }
            else
            {
                _playSpeed = playSpeed;
            }
        }

        public float GetPlaySpeed()
        {
            return _playSpeed;
        }

        public bool IsPlaying()
        {
            return _isPlaying;
        }

        public ulong GetCurrentPlayTimeInNanoseconds()
        {
            return _currentTimeNanoseconds;
        }

        public PlayerContext GetMainPlayerContext()
        {
            return _mainPlayerContext;
        }

        public FreeCamera GetFreeCamera()
        {
            return freeCamera;
        }

        public TopViewCamera GetTopViewCamera()
        {
            return topViewCamera;
        }

        public MainCamera GetMainCamera()
        {
            return mainCamera;
        }

        public void SetModuleGenerating(AnalysisModule module)
        {
            _generatingModule = module;
            onGeneratingModuleChanged?.Invoke(module);
        }

        public AnalysisModule GetModuleGenerating()
        {
            return _generatingModule;
        }

        public void SetVisibleHeatmapModule(AnalysisModule module)
        {
            _visibleHeatmapModule = module;
            onVisibleHeatmapModuleChanged?.Invoke(module);
        }

        public AnalysisModule GetVisibleHeatmapModule()
        {
            return _visibleHeatmapModule;
        }

        public void Dispose()
        {
            _recordLoader.Dispose();
        }
    }
}