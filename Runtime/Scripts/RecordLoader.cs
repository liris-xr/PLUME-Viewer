using System;
using System.Collections.Generic;
using Google.Protobuf.Reflection;
using PLUME.Sample;
using UnityEngine;

namespace PLUME
{
    public class RecordLoader : IDisposable
    {
        private readonly RecordReader _reader;
        private readonly TypeRegistry _typeRegistry;
        private readonly OrderedSamplesList _loadedSamples;
        
        public ulong Duration { get; private set; }
        public ulong SampleCount { get; private set; }

        private bool _closed;
        private bool _loaded;

        public RecordLoader(RecordReader reader, MessageDescriptor[] descriptors) :
            this(reader, TypeRegistry.FromMessages(descriptors))
        {
        }

        public RecordLoader(RecordReader reader, TypeRegistry typeRegistry)
        {
            _reader = reader;
            _loadedSamples = new OrderedSamplesList();
            _typeRegistry = typeRegistry;
        }

        public void Load()
        {
            if (_loaded)
                return;

            PackedSample sample;

            while((sample = _reader.ReadNextSample()) != null)
            {
                var payload = sample.Payload.Unpack(_typeRegistry);

                // Unpacking might fail if the message descriptor is not found in the type registry.
                if (payload == null)
                {
                    Debug.LogWarning($"Could not load payload with type {sample.Payload.TypeUrl}");
                    continue;
                }

                var unpackedSample = UnpackedSample.InstantiateUnpackedSample(sample.Header, payload);

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

                SampleCount++;
                Duration = Math.Max(Duration, sample.Header.Time);
            }

            _loaded = true;
        }

        public UnpackedSample SampleAtIndex(int index)
        {
            if (!_loaded)
                Load();

            if (index < 0 || index >= (int)SampleCount)
            {
                return null;
            }

            if (index < _loadedSamples.Count)
            {
                return _loadedSamples[index] as UnpackedSample;
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
            if (!_loaded)
                Load();

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