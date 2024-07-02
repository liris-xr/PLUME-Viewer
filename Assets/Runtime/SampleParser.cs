using System;
using Google.Protobuf.Reflection;
using PLUME.Sample;

namespace Runtime
{
    // TODO: Add a pool for the PackedSample and Sample to minimize allocations.
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
        ///     Parse the sample bytes in the buffer and return the parsed sample as <see cref="PackedSample" />.
        /// </summary>
        /// <param name="buffer">The buffer that stores the sample bytes.</param>
        /// <returns>The parsed sample as <see cref="PackedSample" />.</returns>
        public PackedSample Parse(ReadOnlySpan<byte> buffer)
        {
            return PackedSample.Parser.ParseFrom(buffer);
        }

        public Sample Unpack(PackedSample packedSample)
        {
            ulong? timestamp = packedSample.HasTimestamp ? packedSample.Timestamp : null;
            var payload = packedSample.Payload.Unpack(_typeRegistry);
            return new Sample(timestamp, payload);
        }
    }
}