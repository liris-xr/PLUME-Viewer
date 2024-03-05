﻿using System;
using System.Globalization;
using System.Threading;
using PLUME.Sample.Common;
using PLUME.Sample.LSL;
using PLUME.Viewer.Analysis;
using UnityEngine;
using UnityEngine.Serialization;
using Color = UnityEngine.Color;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace PLUME.Viewer.Player
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

        private BufferedAsyncFramesLoader _framesLoader;
        private BufferedAsyncRecordLoader _markersLoader;
        private BufferedAsyncRecordLoader _physioSignalsLoader;

        private PlayerContext _playerContext;
        private bool _isPlaying;
        private ulong _currentTimeNanoseconds;

        private bool _isLoading;

        public RenderTexture PreviewRenderTexture { get; private set; }

        public FreeCamera freeCamera;
        public TopViewCamera topViewCamera;
        public MainCamera mainCamera;

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
            freeCamera.PreviewRenderTexture = PreviewRenderTexture;
            topViewCamera.PreviewRenderTexture = PreviewRenderTexture;
            mainCamera.PreviewRenderTexture = PreviewRenderTexture;
            SetCurrentPreviewCamera(mainCamera);

            freeCamera.transform.position = new Vector3(-2.24f, 1.84f, 0.58f);
            freeCamera.transform.rotation = Quaternion.Euler(25f, -140f, 0f);
            topViewCamera.transform.position = new Vector3(0, 3.25f, -4);
            topViewCamera.GetCamera().orthographicSize = 7;

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

            _framesLoader =
                new BufferedAsyncFramesLoader(new RecordReader(recordPath), typeRegistryProvider.GetTypeRegistry());

            _markersLoader.StartLoading();
            _physioSignalsLoader.StartLoading();
            _framesLoader.StartLoading();

            _playerContext = PlayerContext.NewContext("MainPlayerContext", _assets);
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

            _framesLoader?.Dispose();
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
            var frames = _framesLoader.FramesInTimeRangeAsync(_currentTimeNanoseconds, endTime).Result;
            _isLoading = false;

            _playerContext.PlayFrames(PlayerModules, frames);

            _currentTimeNanoseconds = Math.Clamp(endTime, 0, _framesLoader.Duration + 1);

            if (endTime > _framesLoader.Duration)
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
            return _framesLoader.Duration;
        }

        public BufferedAsyncFramesLoader GetFramesLoader()
        {
            return _framesLoader;
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
            _framesLoader?.Dispose();
            _markersLoader?.Dispose();
            _physioSignalsLoader?.Dispose();
        }
    }
}