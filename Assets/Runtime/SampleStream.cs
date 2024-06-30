using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams;
using UnityEngine.Assertions;

namespace Runtime
{
    public class SampleStream : IDisposable
    {
        private readonly DelimitedMessageReader _delimitedMessageReader;
        private readonly Stream _stream;

        private SampleStream(Stream stream, bool leaveOpen = false)
        {
            Assert.IsTrue(stream.CanSeek, "Expected stream to be seekable.");
            Assert.IsTrue(stream.CanRead, "Expected stream to be readable.");

            _stream = stream;
            _delimitedMessageReader = new DelimitedMessageReader(stream, leaveOpen);
        }

        public void Dispose()
        {
            _delimitedMessageReader.Dispose();
        }

        /// <summary>
        ///     Reads the next sample's bytes from the stream and advances the position within the stream.
        /// </summary>
        /// <param name="bufferWriter">
        ///     Output sink into which the sample's bytes are written.
        /// </param>
        /// <returns>The number of bytes read into the buffer.</returns>
        /// <exception cref="InvalidDataException">The sample's size is malformed or truncated.</exception>
        /// <exception cref="ArgumentException">The buffer is too small to read the sample's bytes.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        public int ReadSample(IBufferWriter<byte> bufferWriter)
        {
            return _delimitedMessageReader.ReadDelimitedMessage(bufferWriter);
        }

        /// <summary>
        ///     Asynchronously reads the next sample's bytes from the stream and advances the position within the stream.
        /// </summary>
        /// <param name="bufferWriter">
        ///     Output sink into which the sample's bytes are written.
        /// </param>
        /// <returns>The number of bytes read into the buffer.</returns>
        /// <exception cref="InvalidDataException">The sample's size is malformed or truncated.</exception>
        /// <exception cref="ArgumentException">The buffer is too small to read the sample's bytes.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        public async Task<int> ReadSampleAsync(IBufferWriter<byte> bufferWriter)
        {
            return await _delimitedMessageReader.ReadDelimitedMessageAsync(bufferWriter);
        }

        public static SampleStream Create(Stream baseStream, bool leaveOpen = false, int bufferSize = 4096)
        {
            if (!baseStream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(baseStream));

            Stream stream = LZ4Stream.Decode(baseStream, leaveOpen: leaveOpen);
            stream = new CachingStream(stream);
            stream = new BufferedStream(stream, bufferSize);
            return new SampleStream(stream, leaveOpen);
        }
    }
}