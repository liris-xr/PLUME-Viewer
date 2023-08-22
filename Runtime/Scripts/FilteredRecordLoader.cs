using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Reflection;
using PLUME.Sample;

namespace PLUME
{
    public class FilteredRecordLoader : IDisposable
    {
        private readonly RecordReader _reader;
        private readonly RecordHeader _recordHeader;
        private readonly TypeRegistry _typeRegistry;

        private readonly OrderedSamplesList _loadedSamples;
        private readonly Func<PackedSample, bool> _filter;

        public int SamplesCount => _recordHeader.SamplesCount;
        public ulong Duration => _recordHeader.Duration;

        public int LoadedSamplesCount { get; private set; }
        
        private bool _loaded;
        private bool _closed;

        public FilteredRecordLoader(RecordReader reader, Func<PackedSample, bool> filter, TypeRegistry typeRegistry)
        {
            _reader = reader;
            _filter = filter;
            _recordHeader = _reader.ReadHeader();
            _loadedSamples = new OrderedSamplesList();
            _typeRegistry = typeRegistry;
        }

        public void Load()
        {
            if (_loaded)
                return;
            
            PackedSample sample;

            do
            {
                sample = _reader.ReadNextSample();

                if (sample != null && _filter.Invoke(sample))
                {
                    // Unpack the sample
                    var payload = sample.Payload.Unpack(_typeRegistry);
                    var unpackedSample = UnpackedSample.InstantiateUnpackedSample(sample.Header, payload);
                    _loadedSamples.Add(unpackedSample);
                    LoadedSamplesCount++;
                }
            } while (sample != null);

            _loaded = true;
        }

        public UnpackedSample SampleAtIndex(int index)
        {
            if(!_loaded)
                Load();
            
            if (index < 0 || index >= SamplesCount)
            {
                return null;
            }

            do
            {
                if (index < _loadedSamples.Count)
                {
                    return _loadedSamples[index] as UnpackedSample;
                }
            } while (true);
        }

        public int FirstSampleIndexAfterOrAtTime(ulong time)
        {
            if(!_loaded)
                Load();
            
            return _loadedSamples.FirstIndexAfterOrAtTime(time);
        }

        public IEnumerable<UnpackedSample> SamplesInTimeRange(ulong startTime, ulong endTime)
        {
            var startIdx = FirstSampleIndexAfterOrAtTime(startTime);

            if (startIdx < 0)
                yield break;

            var idx = startIdx;

            do
            {
                var sample = SampleAtIndex(idx);

                if (sample == null)
                    yield break;

                if (sample.Header.Time > endTime)
                    yield break;

                yield return sample;
                idx++;
            } while (true);
        }

        public void Close()
        {
            if (_closed)
                return;
            
            _loadedSamples.Clear();
            _closed = true;
        }

        public void Dispose()
        {
            Close();
        }
    }
}