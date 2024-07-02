using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using PLUME.Sample;

namespace Runtime
{
    // TODO: When reading sample, keep track of the timestamp and the position in the stream to allow faster seeking to a specific timestamp.
    // TODO: When reading sample, also keep track of its type to allow filtering samples by type.
    // TODO: Add a pool for the samples to minimize allocations.
    public class SampleLoader : IDisposable
    {
        private readonly ArrayBufferWriter<byte> _bytesBuffer;
        private readonly SampleParser _parser;
        private readonly SampleReader _reader;

        private readonly Stream _stream;

        public SampleLoader(SampleReader reader, SampleParser parser)
        {
            _reader = reader;
            _parser = parser;
            _bytesBuffer = new ArrayBufferWriter<byte>();
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }

        public async IAsyncEnumerable<Sample> LoadSamplesAtTimeAsync(ulong time)
        {
            var sampleSize = await _reader.ReadSampleBytesAsync(_bytesBuffer);
            var packedSample = _parser.Parse(_bytesBuffer.WrittenSpan[..sampleSize]);

            while (packedSample.Timestamp <= time)
            {
                sampleSize = await _reader.ReadSampleBytesAsync(_bytesBuffer);
                packedSample = _parser.Parse(_bytesBuffer.WrittenSpan[..sampleSize]);

                if (packedSample.Timestamp != time) continue;

                var sample = _parser.Unpack(packedSample);
                yield return sample;
            }
        }

        public async IAsyncEnumerable<Sample> LoadSamplesInTimeRangeAsync(ulong startTime, ulong endTime,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var dataChannel = Channel.CreateSingleConsumerUnbounded<Sample>();

            UniTask.RunOnThreadPool(() =>
            {
                try
                {
                    PackedSample packedSample;

                    do
                    {
                        _bytesBuffer.Clear();
                        var sampleSize = _reader.ReadSampleBytes(_bytesBuffer);

                        if (sampleSize == 0)
                        {
                            dataChannel.Writer.Complete();
                            return;
                        }

                        packedSample = _parser.Parse(_bytesBuffer.WrittenSpan[..sampleSize]);

                        if (packedSample.Timestamp < startTime)
                            continue;

                        var sample = _parser.Unpack(packedSample);
                        dataChannel.Writer.TryWrite(sample);
                    } while (packedSample.Timestamp <= endTime);
                }
                catch (Exception e)
                {
                    dataChannel.Writer.Complete(e);
                }
            }, cancellationToken: cancellationToken);

            await foreach (var sample in dataChannel.Reader.ReadAllAsync(cancellationToken))
                yield return sample;
        }

        public List<Sample> LoadSamplesAtTime(ulong time)
        {
            var samples = new List<Sample>();

            var sampleSize = _reader.ReadSampleBytes(_bytesBuffer);

            if (sampleSize == 0) return samples;

            var packedSample = _parser.Parse(_bytesBuffer.WrittenSpan[..sampleSize]);

            while (packedSample.Timestamp <= time)
            {
                sampleSize = _reader.ReadSampleBytes(_bytesBuffer);

                if (sampleSize == 0) return samples;

                packedSample = _parser.Parse(_bytesBuffer.WrittenSpan[..sampleSize]);

                if (packedSample.Timestamp != time) continue;

                var sample = _parser.Unpack(packedSample);
                samples.Add(sample);
            }

            return samples;
        }

        public List<Sample> LoadSamplesInTimeRange(ulong startTime, ulong endTime)
        {
            var samples = new List<Sample>();

            PackedSample packedSample;

            do
            {
                _bytesBuffer.Clear();
                var sampleSize = _reader.ReadSampleBytes(_bytesBuffer);

                if (sampleSize == 0) break;

                packedSample = PackedSample.Parser.ParseFrom(_bytesBuffer.WrittenSpan[..sampleSize]);

                if (packedSample.Timestamp < startTime)
                    continue;

                var sample = _parser.Unpack(packedSample);
                samples.Add(sample);
            } while (packedSample.Timestamp <= endTime);

            return samples;
        }
    }
}