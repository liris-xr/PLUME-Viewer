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

        public ulong Duration => _duration;

        private int _sampleCount;
        private ulong _duration;

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

            Debug.Log("Record metadata: " + _reader.GetMetadata());
            
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

                    if (idx == -1)
                    {
                        _loadedSamples.Add(unpackedSample);
                    }
                    else
                    {
                        _loadedSamples.Insert(idx + 1, unpackedSample);
                    }
                }
                else
                {
                    _loadedSamples.Add(unpackedSample);
                }

                _sampleCount++;
                _duration = Math.Max(_duration, sample.Header.Time);
            }

            _loaded = true;
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