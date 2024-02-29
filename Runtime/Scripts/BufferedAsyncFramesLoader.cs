using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;
using PLUME.Sample;
using PLUME.Sample.Unity;

namespace PLUME
{
    public class BufferedAsyncFramesLoader : IDisposable
    {
        private readonly RecordReader _reader;
        private readonly TypeRegistry _typeRegistry;

        private readonly List<SemaphoreSlim> _signals;

        private readonly List<UnpackedFrame> _frames;
        private readonly Thread _loadingThread;
        public bool FinishedLoading { get; private set; }
        public ulong FramesCount { get; private set; }
        public ulong Duration { get; private set; }

        private bool _closed;

        private readonly Func<PackedSample, bool> _filter;

        public BufferedAsyncFramesLoader(RecordReader reader, TypeRegistry typeRegistry, bool autoStart = true)
        {
            _reader = reader;
            _signals = new List<SemaphoreSlim>();
            _frames = new List<UnpackedFrame>();
            _typeRegistry = typeRegistry;

            _loadingThread = new Thread(Run);

            if (autoStart)
                StartLoading();
        }

        public void StartLoading()
        {
            if (_loadingThread.ThreadState == ThreadState.Running)
                return;

            _loadingThread.Start();
        }

        private void Run()
        {
            var readMetaFile = _reader.TryReadMetaFile(out var recordMetadata, out var recordMetrics);

            var recordHeader = _reader.ReadNextSample().Payload.Unpack<RecordMetadata>();

            var versionStr =
                $"{recordHeader.RecorderVersion.Name} v{recordHeader.RecorderVersion.Major}.{recordHeader.RecorderVersion.Minor}.{recordHeader.RecorderVersion.Patch}";

            // if (!readMetaFile)
            // {
            //     throw new NotImplementedException(
            //         "Metadata file not found. Need to recompute duration and sample count and verify samples order.");
            // }

            // if (!recordMetrics.IsSequential)
            // {
            //     throw new NotImplementedException("Record is not sequential and needs to be reordered.");
            // }

            Duration = recordMetrics.Duration;
            FramesCount = recordMetrics.NSamples;

            PackedSample sample;

            while (!FinishedLoading)
            {
                try
                {
                    sample = _reader.ReadNextSample();
                }
                catch (Exception)
                {
                    FinishedLoading = true;
                    break;
                }

                if (!sample.Payload.Is(Frame.Descriptor))
                    continue;

                var frame = sample.Payload.Unpack<Frame>();
                var frameData = new List<UnpackedSample>();
                frameData.AddRange(
                    frame.Data.Select(d => new UnpackedSample(sample.Timestamp, d.Unpack(_typeRegistry))));
                var unpackedFrame = new UnpackedFrame(sample.Timestamp, frame.FrameNumber, frameData);

                lock (_frames)
                {
                    _frames.Add(unpackedFrame);

                    lock (_signals)
                    {
                        foreach (var signal in _signals)
                        {
                            if (signal.CurrentCount == 0)
                                signal.Release();
                        }
                    }
                }
            }
        }

        public async Task<UnpackedFrame> FrameAtIndexAsync(int index)
        {
            if (index < 0 || index >= (int)FramesCount)
            {
                return null;
            }

            var signal = new SemaphoreSlim(0, 1);

            lock (_signals)
            {
                _signals.Add(signal);
            }

            do
            {
                lock (_frames)
                {
                    if (index < _frames.Count)
                    {
                        lock (_signals)
                        {
                            _signals.Remove(signal);
                        }

                        return _frames[index];
                    }

                    if (FinishedLoading)
                    {
                        lock (_signals)
                        {
                            _signals.Remove(signal);
                        }

                        return null;
                    }
                }

                await signal.WaitAsync();
            } while (true);
        }

        public async Task<int> FirstFrameIndexAfterOrAtTimeAsync(ulong time)
        {
            var signal = new SemaphoreSlim(0, 1);

            lock (_signals)
            {
                _signals.Add(signal);
            }

            do
            {
                lock (_frames)
                {
                    var lastLoadedSample = _frames.LastOrDefault();

                    if ((lastLoadedSample != null && lastLoadedSample.Timestamp >= time) || FinishedLoading)
                    {
                        lock (_signals)
                        {
                            _signals.Remove(signal);
                        }

                        var lookupFrame = new UnpackedFrame(time, 0, null);

                        var idx = _frames.BinarySearch(lookupFrame, FrameTimestampComparer.Instance);

                        if (idx >= 0)
                        {
                            return idx;
                        }

                        return ~idx;
                    }
                }

                await signal.WaitAsync();
            } while (true);
        }

        public async Task<List<UnpackedFrame>> FramesInTimeRangeAsync(ulong startTime, ulong endTime)
        {
            var samples = new List<UnpackedFrame>();
            var startIdx = await FirstFrameIndexAfterOrAtTimeAsync(startTime);

            if (startIdx < 0)
                return samples;

            var idx = startIdx;

            do
            {
                var sample = await FrameAtIndexAsync(idx);

                if (sample == null)
                    return samples;

                if (sample.Timestamp > endTime)
                    return samples;

                samples.Add(sample);
                idx++;
            } while (true);
        }

        public UnpackedFrame[] All()
        {
            lock (_frames)
            {
                return _frames.ToArray();
            }
        }

        public void Close()
        {
            if (_closed)
                return;

            _loadingThread.Interrupt();
            lock (_frames)
            {
                _frames.Clear();
            }

            _closed = true;
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}