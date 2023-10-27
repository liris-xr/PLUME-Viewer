using System;
using System.Globalization;
using System.Threading;
using PLUME.Sample.Common;
using PLUME.Sample.LSL;
using PLUME.Viewer;
using UnityEngine;
using Color = UnityEngine.Color;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace PLUME
{
    [DisallowMultipleComponent]
    public class Player : SingletonMonoBehaviour<Player>, IDisposable
    {
        public TypeRegistryProvider typeRegistryProvider;

        public string recordPath;
        public string assetBundlePath;

        public bool loop;

        private float _playSpeed = 1;

        private PlayerAssets _assets;
        public PlayerModule[] PlayerModules { get; private set; }

        private BufferedAsyncRecordLoader _recordLoader;
        private BufferedAsyncRecordLoader _markersLoader;
        private BufferedAsyncRecordLoader _physioSignalsLoader;

        private PlayerContext _playerContext;
        private bool _isPlaying;
        private ulong _currentTimeNanoseconds;

        private bool _isLoading;

        public RenderTexture PreviewRenderTexture { get; private set; }

        private FreeCamera _freeCamera;
        private TopViewCamera _topViewCamera;
        private MainCamera _mainCamera;

        private PreviewCamera _currentCamera;

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

            PreviewRenderTexture = RenderTexture.GetTemporary(1920, 1080);

            var t = transform;
            var freeCameraGo = new GameObject("FreeCamera") { transform = { parent = t } };
            var topViewCameraGo = new GameObject("TopViewCamera") { transform = { parent = t } };
            var sceneMainCameraGo = new GameObject("SceneMainCamera") { transform = { parent = t } };
            _freeCamera = freeCameraGo.AddComponent<FreeCamera>();
            _topViewCamera = topViewCameraGo.AddComponent<TopViewCamera>();
            _mainCamera = sceneMainCameraGo.AddComponent<MainCamera>();

            _freeCamera.PreviewRenderTexture = PreviewRenderTexture;
            _topViewCamera.PreviewRenderTexture = PreviewRenderTexture;
            _mainCamera.PreviewRenderTexture = PreviewRenderTexture;
            SetCurrentPreviewCamera(_mainCamera);

            _freeCamera.transform.position = new Vector3(-2.24f, 1.84f, 0.58f);
            _freeCamera.transform.rotation = Quaternion.Euler(25f, -140f, 0f);
            _topViewCamera.transform.position = new Vector3(0, 3.25f, -4);
            _topViewCamera.GetCamera().orthographicSize = 7;

            PlayerModules = FindObjectsOfType<PlayerModule>();
            _assets = new PlayerAssets(assetBundlePath);

            _markersLoader = new BufferedAsyncRecordLoader(new RecordReader(recordPath),
                typeRegistryProvider.GetTypeRegistry(),
                sample => sample.Payload.Is(Marker.Descriptor));

            _physioSignalsLoader = new BufferedAsyncRecordLoader(new RecordReader(recordPath),
                typeRegistryProvider.GetTypeRegistry(),
                sample => sample.Payload.Is(StreamOpen.Descriptor)
                          || sample.Payload.Is(StreamClose.Descriptor)
                          || sample.Payload.Is(StreamSample.Descriptor)
            );

            _recordLoader =
                new BufferedAsyncRecordLoader(new RecordReader(recordPath), typeRegistryProvider.GetTypeRegistry());

            _markersLoader.StartLoading();
            _physioSignalsLoader.StartLoading();
            _recordLoader.StartLoading();

            _playerContext = PlayerContext.NewContext("MainPlayerContext", _assets);
        }

        public void SetCurrentPreviewCamera(PreviewCamera camera)
        {
            var rt = RenderTexture.active;
            RenderTexture.active = PreviewRenderTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rt;

            _freeCamera.SetEnabled(false);
            _topViewCamera.SetEnabled(false);
            _mainCamera.SetEnabled(false);
            _currentCamera = camera;
            camera.SetEnabled(true);
        }

        public PreviewCamera GetCurrentPreviewCamera()
        {
            return _currentCamera;
        }

        public PlayerAssets GetPlayerAssets()
        {
            return _assets;
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

            _recordLoader?.Dispose();
            _markersLoader?.Dispose();
            _physioSignalsLoader?.Dispose();
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
            _playerContext.Reset();

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
                _playerContext.Reset();

                _currentTimeNanoseconds = 0;
                PlayForward(time);
            }
        }

        private void PlayForward(ulong durationNanoseconds)
        {
            var endTime = _currentTimeNanoseconds + durationNanoseconds;

            _isLoading = true;
            var samples = _recordLoader.SamplesInTimeRangeAsync(_currentTimeNanoseconds, endTime).Result;
            _isLoading = false;

            _playerContext.PlaySamples(PlayerModules, samples);

            _currentTimeNanoseconds = Math.Clamp(endTime, 0, _recordLoader.Duration + 1);

            if (endTime > _recordLoader.Duration)
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

        public bool IsLoading()
        {
            return _isLoading;
        }

        public ulong GetRecordDurationInNanoseconds()
        {
            return _recordLoader.Duration;
        }

        public BufferedAsyncRecordLoader GetRecordLoader()
        {
            return _recordLoader;
        }

        public BufferedAsyncRecordLoader GetMarkersLoader()
        {
            return _markersLoader;
        }

        public BufferedAsyncRecordLoader GetPhysiologicalSignalsLoader()
        {
            return _physioSignalsLoader;
        }

        public ulong GetCurrentPlayTimeInNanoseconds()
        {
            return _currentTimeNanoseconds;
        }

        public PlayerContext GetPlayerContext()
        {
            return _playerContext;
        }

        public FreeCamera GetFreeCamera()
        {
            return _freeCamera;
        }

        public TopViewCamera GetTopViewCamera()
        {
            return _topViewCamera;
        }

        public MainCamera GetMainCamera()
        {
            return _mainCamera;
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
            _recordLoader?.Dispose();
            _markersLoader?.Dispose();
            _physioSignalsLoader?.Dispose();
        }
    }
}