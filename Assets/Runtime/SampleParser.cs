using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf.Reflection;
using PLUME.Sample;

namespace Runtime
{
    // TODO: Add a pool for the samples to minimize allocations.
    public class SampleParser
    {
        private readonly TypeRegistry _typeRegistry;

        /// <summary>
        ///     Create a new instance of <see cref="SampleParser" /> with the given <paramref name="typeRegistry" />.
        /// </summary>
        /// <param name="typeRegistry">The type registry that is used to unpack samples payloads.</param>
        public SampleParser(TypeRegistry typeRegistry)
        {
            _typeRegistry = typeRegistry;
        }

        /// <summary>
        ///     Parse the sample bytes in the buffer and return the parsed sample as <see cref="Sample" />.
        /// </summary>
        /// <param name="buffer">The buffer that stores the sample bytes.</param>
        /// <returns>The parsed sample as <see cref="Sample" />.</returns>
        public Sample Parse(ReadOnlySpan<byte> buffer)
        {
            var packedSample = PackedSample.Parser.ParseFrom(buffer);
            ulong? timestamp = packedSample.HasTimestamp ? packedSample.Timestamp : null;
            var payload = packedSample.Payload.Unpack(_typeRegistry);
            return new Sample(timestamp, payload);
        }

        /// <summary>
        ///     Parse the sample bytes in the buffer in parallel using <paramref name="nWorkers" /> threads and return the parsed
        ///     samples as <see cref="PackedSample" />.
        /// </summary>
        /// <param name="buffer">The buffer that stores the sample bytes.</param>
        /// <param name="nWorkers">The number of threads to use for parsing the sample bytes.</param>
        /// <returns>The parsed samples as <see cref="PackedSample" />. The order of the samples is not guaranteed.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="nWorkers" /> is less than or equal to 0.</exception>
        public IEnumerable<Sample> Parse(SampleBytesBuffer buffer, int nWorkers = 4)
        {
            if (nWorkers <= 0)
                throw new ArgumentOutOfRangeException(nameof(nWorkers),
                    "The number of workers must be greater than 0.");

            var concurrentBag = new ConcurrentBag<Sample>();
            var workers = new Thread[nWorkers];

            for (var i = 0; i < nWorkers; i++)
            {
                workers[i] = new Thread(() =>
                {
                    var sampleBytesBuffer = new ArrayBufferWriter<byte>();

                    while (!buffer.IsEmpty)
                    {
                        var sampleSize = buffer.TakeSampleBytes(sampleBytesBuffer);
                        var packedSample = Parse(sampleBytesBuffer.WrittenSpan[..sampleSize]);
                        concurrentBag.Add(packedSample);
                    }
                });
                workers[i].Start();
            }

            foreach (var worker in workers) worker.Join();

            return concurrentBag;
        }
    }
}