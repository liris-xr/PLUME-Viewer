using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using PLUME.Sample;
using UnityEngine;

namespace PLUME
{
    public class BufferedAsyncRecordLoader : IDisposable
    {
        private readonly RecordReader _reader;
        private readonly TypeRegistry _typeRegistry;

        private readonly List<SemaphoreSlim> _signals;

        private readonly OrderedSamplesList _samplesBuffer;
        private readonly Thread _loadingThread;
        private bool _finishedLoading;

        public ulong SamplesCount { get; private set; }
        public ulong Duration { get; private set; }

        private bool _closed;

        public BufferedAsyncRecordLoader(RecordReader reader, MessageDescriptor[] descriptors, bool autoStart = true) :
            this(reader, TypeRegistry.FromMessages(descriptors), autoStart)
        {
        }

        public BufferedAsyncRecordLoader(RecordReader reader, TypeRegistry typeRegistry, bool autoStart = true)
        {
            _reader = reader;
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
            var recordMetadata = _reader.ReadMetadata();
            var recordHeader = _reader.ReadNextSample().Payload.Unpack<RecordHeader>();

            var versionStr =
                $"{recordHeader.RecorderVersion.Name} v{recordHeader.RecorderVersion.Major}.{recordHeader.RecorderVersion.Minor}.{recordHeader.RecorderVersion.Patch}";
            Debug.Log($"Loading record {recordHeader.Identifier}. Recorded with version {versionStr}");

            if (recordMetadata == null)
            {
                throw new NotImplementedException(
                    "Metadata file not found. Need to recompute duration and sample count and verify samples order.");
            }

            if (!recordMetadata.Sequential)
            {
                throw new NotImplementedException("Record is not sequential and needs to be reordered.");
            }

            Duration = recordMetadata.Duration;
            SamplesCount = recordMetadata.SamplesCount;

            PackedSample sample;

            while (!_finishedLoading)
            {
                try
                {
                    sample = _reader.ReadNextSample();
                }
                catch (Exception)
                {
                    _finishedLoading = true;
                    break;
                }

                var messageDescriptor = _typeRegistry.Find(Any.GetTypeName(sample.Payload.TypeUrl));

                if (messageDescriptor == null)
                {
                    continue;
                }

                var payload = sample.Payload.Unpack(_typeRegistry);
                var unpackedSample = new UnpackedSample
                {
                    Header = sample.Header,
                    Payload = payload
                };

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

                    if (_finishedLoading)
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
                    var finishedLoading = _finishedLoading;

                    if ((lastLoadedSample != null && lastLoadedSample.Header.Time >= time) || finishedLoading)
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

                if (sample.Header.Time > endTime)
                    return samples;

                samples.Add(sample);
                idx++;
            } while (true);
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
            Close();
        }
    }
}