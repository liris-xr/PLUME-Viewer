using System;
using System.Globalization;
using System.IO;
using System.Threading;
using PLUME.Sample.Common;
using PLUME.Sample.LSL;
using UnityEngine;

namespace PLUME
{
    [DisallowMultipleComponent]
    public class Player : MonoBehaviour
    {
        public TypeRegistryProvider typeRegistryProvider;

        public string recordPath;
        public bool loop;

        private float _playSpeed = 1;

        private PlayerAssets _assets;
        public PlayerModule[] PlayerModules { get; private set; }

        private BufferedAsyncRecordLoader _recordLoader;
        private FilteredRecordLoader _markersLoader;
        private FilteredRecordLoader _physioSignalsLoader;

        private PlayerContext _playerContext;
        private bool _isPlaying;
        private ulong _currentTimeNanoseconds;

        private bool _isLoading;

        public Camera currentCamera;

        public Camera freeCamera;
        public Camera topViewCamera;
        public Camera sceneMainCamera;

        [RuntimeInitializeOnLoadMethod]
        public static void OnInitialize()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }
        
        private void Awake()
        {
            PlayerModules = FindObjectsOfType<PlayerModule>();
            _assets = new PlayerAssets(Path.Combine(Application.streamingAssetsPath, "plume_asset_bundle_windows"));
            
            _markersLoader = new FilteredRecordLoader(new RecordReader(recordPath),
                sample => sample.Payload.Is(Marker.Descriptor), typeRegistryProvider.GetTypeRegistry());
            _markersLoader.Load();
            
            _physioSignalsLoader = new FilteredRecordLoader(new RecordReader(recordPath),
                sample => sample.Payload.Is(StreamOpen.Descriptor)
                          || sample.Payload.Is(StreamClose.Descriptor)
                          || sample.Payload.Is(StreamSample.Descriptor),
                typeRegistryProvider.GetTypeRegistry());
            _physioSignalsLoader.Load();
            
            _recordLoader = new BufferedAsyncRecordLoader(new RecordReader(recordPath), typeRegistryProvider.GetTypeRegistry());
            _recordLoader.StartLoading();

            _playerContext = PlayerContext.NewContext("MainPlayerContext", _assets);

            transform.parent = null;
            DontDestroyOnLoad(this);
        }

        public FilteredRecordLoader GetPhysiologicalSignalsLoader()
        {
            return _physioSignalsLoader;
        }
        
        public FilteredRecordLoader GetMarkersLoader()
        {
            return _markersLoader;
        }

        public PlayerAssets GetPlayerAssets()
        {
            return _assets;
        }

        private void FixedUpdate()
        {
            if (_isPlaying)
            {
                PlayForward((ulong) (Time.fixedDeltaTime * _playSpeed * 1_000_000_000));
            }
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

            _currentTimeNanoseconds = Math.Clamp(endTime + 1, 0, _recordLoader.Duration + 1);

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

        public ulong GetCurrentPlayTimeInNanoseconds()
        {
            return _currentTimeNanoseconds;
        }

        public PlayerContext GetPlayerContext()
        {
            return _playerContext;
        }

    }
}