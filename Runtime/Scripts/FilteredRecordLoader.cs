using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using PLUME.Sample;
using UnityEngine;

namespace PLUME
{
    public class FilteredRecordLoader : IDisposable
    {
        private readonly RecordReader _reader;
        private readonly TypeRegistry _typeRegistry;

        private readonly OrderedSamplesList _loadedSamples;
        private readonly Func<PackedSample, bool> _filter;

        private int _sampleCount;
        private bool _loaded;
        private bool _closed;

        public FilteredRecordLoader(RecordReader reader, Func<PackedSample, bool> filter, TypeRegistry typeRegistry)
        {
            _reader = reader;
            _filter = filter;
            _loadedSamples = new OrderedSamplesList();
            _typeRegistry = typeRegistry;
        }

        public void Load()
        {
            if (_loaded)
                return;

            var recordHeader = _reader.ReadNextSample().Payload.Unpack<RecordHeader>();
            
            while(_reader.TryReadNextSample(out var sample))
            {
                // Skip samples without timestamp
                if (sample.Header == null)
                    continue;
                
                if (!_filter.Invoke(sample)) continue;

                // Unpack the sample
                var payload = sample.Payload.Unpack(_typeRegistry);
                
                // Unpacking might fail if the message descriptor is not found in the type registry.
                if (payload == null)
                {
                    Debug.LogWarning($"Could not load payload with type {sample.Payload.TypeUrl}");
                    continue;
                }

                var unpackedSample = new UnpackedSample
                {
                    Header = sample.Header,
                    Payload = payload
                };
                
                if (_loadedSamples.Count > 0)
                {
                    var idx = _loadedSamples.FirstIndexAfterOrAtTime(sample.Header.Time);
                    
                    // No samples after or at the current sample's timestamp, inserting at the end
                    if (idx == -1)
                    {
                        _loadedSamples.Add(unpackedSample);
                    }
                    else
                    {
                        _loadedSamples.Insert(idx, unpackedSample);
                    }
                }
                else
                {
                    // No samples, inserting at the beginning
                    _loadedSamples.Add(unpackedSample);
                }

                _sampleCount++;
            }

            _loaded = true;
        }

        public UnpackedSample[] All()
        {
            return _loadedSamples.ToArray();
        }
        
        public UnpackedSample[] AllOfType<T>() where T : IMessage
        {
            return _loadedSamples.Where(sample => sample.Payload is T).ToArray();
        }
        
        public UnpackedSample SampleAtIndex(int index)
        {
            if (!_loaded)
                Load();

            if (index < 0 || index >= _sampleCount)
            {
                return null;
            }

            if (index < _loadedSamples.Count)
            {
                return _loadedSamples[index];
            }

            return null;
        }

        public int FirstSampleIndexAfterOrAtTime(ulong time)
        {
            if (!_loaded)
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
            _reader.Close();
            
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