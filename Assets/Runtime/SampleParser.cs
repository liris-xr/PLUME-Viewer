using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PLUME.Sample;

namespace Runtime
{
    public class SampleParser
    {
        /// <summary>
        ///     Parse the sample bytes in the buffer and return the parsed sample as <see cref="PackedSample" />.
        /// </summary>
        /// <param name="buffer">The buffer that stores the sample bytes.</param>
        /// <returns>The parsed sample as <see cref="PackedSample" />.</returns>
        public PackedSample Parse(ReadOnlySpan<byte> buffer)
        {
            return PackedSample.Parser.ParseFrom(buffer);
        }

        /// <summary>
        ///     Parse the sample bytes in the buffer in parallel using <paramref name="nWorkers" /> threads and return the parsed
        ///     samples as <see cref="PackedSample" />.
        /// </summary>
        /// <param name="buffer">The buffer that stores the sample bytes.</param>
        /// <param name="nWorkers">The number of threads to use for parsing the sample bytes.</param>
        /// <returns>The parsed samples as <see cref="PackedSample" />. The order of the samples is not guaranteed.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="nWorkers" /> is less than or equal to 0.</exception>
        public IEnumerable<PackedSample> Parse(SampleBytesBuffer buffer, int nWorkers = 4)
        {
            if (nWorkers <= 0)
                throw new ArgumentOutOfRangeException(nameof(nWorkers),
                    "The number of workers must be greater than 0.");

            var concurrentBag = new ConcurrentBag<PackedSample>();
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