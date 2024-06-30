using System;
using System.IO;
using Google.Protobuf.Reflection;
using K4os.Compression.LZ4.Streams;

namespace Runtime
{
    // TODO: When reading sample, keep track of the timestamp and the position in the stream to allow faster seeking to a specific timestamp.
    // TODO: Add a pool for the samples to minimize allocations.
    public class SampleLoader : IDisposable
    {
        private readonly SampleParser _parser;
        private readonly SampleReader _reader;

        private SampleLoader(SampleReader reader, SampleParser parser)
        {
            _reader = reader;
            _parser = parser;
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }

        public static SampleLoader Create(Stream baseStream, TypeRegistry typeRegistry, bool leaveOpen = false,
            int bufferSize = 4096)
        {
            if (!baseStream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(baseStream));

            Stream stream = LZ4Stream.Decode(baseStream, leaveOpen: leaveOpen);
            stream = new CachingStream(stream);
            stream = new BufferedStream(stream, bufferSize);

            var reader = new SampleReader(stream, leaveOpen);
            var parser = new SampleParser(typeRegistry);
            return new SampleLoader(reader, parser);
        }
    }
}