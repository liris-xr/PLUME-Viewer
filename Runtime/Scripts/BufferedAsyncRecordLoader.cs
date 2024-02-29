using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using PLUME.Sample;

namespace PLUME
{
    public class BufferedAsyncRecordLoader : IDisposable
    {
        private readonly RecordReader _reader;
        private readonly TypeRegistry _typeRegistry;

        private readonly List<SemaphoreSlim> _signals;

        private readonly OrderedSamplesList _samplesBuffer;
        private readonly Thread _loadingThread;
        public bool FinishedLoading { get; private set; }
        public ulong SamplesCount { get; private set; }
        public ulong Duration { get; private set; }

        private bool _closed;

        private readonly Func<PackedSample, bool> _filter;

        public BufferedAsyncRecordLoader(RecordReader reader, TypeRegistry typeRegistry,
            Func<PackedSample, bool> filter = null, bool autoStart = true)
        {
            _reader = reader;
            _filter = filter;
            _signals = new List<SemaphoreSlim>();
            _samplesBuffer = new OrderedSamplesList();
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

            if (!readMetaFile)
            {
                throw new NotImplementedException(
                    "Metadata file not found. Need to recompute duration and sample count and verify samples order.");
            }

            if (!recordMetrics.IsSequential)
            {
                throw new NotImplementedException("Record is not sequential and needs to be reordered.");
            }

            Duration = recordMetrics.Duration;
            SamplesCount = recordMetrics.NSamples;

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

                if (_filter != null && !_filter.Invoke(sample)) continue;

                // Skip samples without timestamp
                if (!sample.HasTimestamp)
                    continue;

                var messageDescriptor = _typeRegistry.Find(Any.GetTypeName(sample.Payload.TypeUrl));

                if (messageDescriptor == null)
                {
                    continue;
                }

                var payload = sample.Payload.Unpack(_typeRegistry);

                var unpackedSample = new UnpackedSample(sample.Timestamp, payload);

                lock (_samplesBuffer)
                {
                    _samplesBuffer.Add(unpackedSample);

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

        public async Task<UnpackedSample> SampleAtIndexAsync(int index)
        {
            if (index < 0 || index >= (int)SamplesCount)
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
                lock (_samplesBuffer)
                {
                    if (index < _samplesBuffer.Count)
                    {
                        lock (_signals)
                        {
                            _signals.Remove(signal);
                        }

                        return _samplesBuffer[index] as UnpackedSample;
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

        public async Task<int> FirstSampleIndexAfterOrAtTimeAsync(ulong time)
        {
            var signal = new SemaphoreSlim(0, 1);

            lock (_signals)
            {
                _signals.Add(signal);
            }

            do
            {
                lock (_samplesBuffer)
                {
                    var lastLoadedSample = _samplesBuffer.LastOrDefault();

                    if ((lastLoadedSample != null && lastLoadedSample.Timestamp >= time) || FinishedLoading)
                    {
                        lock (_signals)
                        {
                            _signals.Remove(signal);
                        }

                        return _samplesBuffer.FirstIndexAfterOrAtTime(time);
                    }
                }

                await signal.WaitAsync();
            } while (true);
        }

        public async Task<List<UnpackedSample>> SamplesInTimeRangeAsync(ulong startTime, ulong endTime)
        {
            var samples = new List<UnpackedSample>();
            var startIdx = await FirstSampleIndexAfterOrAtTimeAsync(startTime);

            if (startIdx < 0)
                return samples;

            var idx = startIdx;

            do
            {
                var sample = await SampleAtIndexAsync(idx);

                if (sample == null)
                    return samples;

                if (sample.Timestamp > endTime)
                    return samples;

                samples.Add(sample);
                idx++;
            } while (true);
        }

        public UnpackedSample[] All()
        {
            lock (_samplesBuffer)
            {
                return _samplesBuffer.ToArray();
            }
        }

        public UnpackedSample[] AllOfType<T>() where T : IMessage
        {
            lock (_samplesBuffer)
            {
                return _samplesBuffer.Where(sample => sample.Payload is T).ToArray();
            }
        }

        public void Close()
        {
            if (_closed)
                return;

            _loadingThread.Interrupt();
            lock (_samplesBuffer)
            {
                _samplesBuffer.Clear();
            }

            _closed = true;
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}