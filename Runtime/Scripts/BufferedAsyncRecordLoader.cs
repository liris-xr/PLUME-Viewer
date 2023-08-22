using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using PLUME.Sample;

namespace PLUME
{
    public class BufferedAsyncRecordLoader : IDisposable
    {
        private readonly RecordReader _reader;
        private readonly RecordHeader _recordHeader;
        private readonly TypeRegistry _typeRegistry;

        private readonly List<SemaphoreSlim> _signals;

        private readonly OrderedSamplesList _samplesBuffer;
        private readonly Thread _loadingThread;
        private bool _finishedLoading;

        public int SamplesCount => _recordHeader.SamplesCount;
        public ulong Duration => _recordHeader.Duration;

        public int ReadSamplesCount { get; private set; }
        public int UnpackedSamplesCount { get; private set; }

        private bool _closed;

        public BufferedAsyncRecordLoader(RecordReader reader, MessageDescriptor[] descriptors, bool autoStart = true) :
            this(reader, TypeRegistry.FromMessages(descriptors), autoStart)
        {
        }

        public BufferedAsyncRecordLoader(RecordReader reader, TypeRegistry typeRegistry, bool autoStart = true)
        {
            _reader = reader;
            _recordHeader = _reader.ReadHeader();
            _signals = new List<SemaphoreSlim>();
            _samplesBuffer = new OrderedSamplesList();
            _typeRegistry = typeRegistry;
            _loadingThread = new Thread(SamplesLoadingTask);

            if (autoStart)
                StartLoading();
        }

        public void StartLoading()
        {
            if (_loadingThread.ThreadState == ThreadState.Running)
                return;

            _loadingThread.Start();
        }

        private void SamplesLoadingTask()
        {
            while (!_finishedLoading)
            {
                var sample = _reader.ReadNextSample();
                ReadSamplesCount++;

                if (sample == null)
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
                var unpackedSample = UnpackedSample.InstantiateUnpackedSample(sample.Header, payload);

                lock (_samplesBuffer)
                {
                    _samplesBuffer.Add(unpackedSample);
                    UnpackedSamplesCount++;

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
            if (index < 0 || index >= SamplesCount)
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