using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams;

namespace Runtime
{
    /// <summary>
    ///     Reads delimited <see cref="Google.Protobuf.IMessage" /> message bytes from a stream.
    /// </summary>
    public class SampleStreamReader : IDisposable
    {
        private readonly DelimitedMessageReader _delimitedMessageReader;
        public readonly SampleStreamSignature SampleStreamSignature;

        private SampleStreamReader(Stream stream, SampleStreamSignature sampleStreamSignature, bool leaveOpen = false)
        {
            SampleStreamSignature = sampleStreamSignature;
            _delimitedMessageReader = new DelimitedMessageReader(stream, leaveOpen);
        }

        public void Dispose()
        {
            _delimitedMessageReader.Dispose();
        }

        /// <summary>
        ///     Create a new <see cref="SampleStreamReader" /> from the given stream.
        /// </summary>
        /// <param name="stream">The stream to create the reader from.</param>
        /// <param name="bufferSize">The size of the buffer for the buffered stream.</param>
        /// <param name="leaveOpen">Whether to leave the stream open when the reader is disposed.</param>
        /// <returns>A new <see cref="SampleStreamReader" /> instance.</returns>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="InvalidDataException">The signature is unknown.</exception>
        public static SampleStreamReader Create(Stream stream, int bufferSize = 4096, bool leaveOpen = false)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));

            var signatureValue = stream.ReadInt32();

            if (!Enum.IsDefined(typeof(SampleStreamSignature), signatureValue))
                throw new MalformedStreamException.UnknownSampleStreamSignature(signatureValue);

            // The signature is a 4-byte integer at the beginning of the stream that indicates the type of record and
            // how it is compressed to determine how to read it.
            var signature = (SampleStreamSignature)signatureValue;

            switch (signature)
            {
                case SampleStreamSignature.LZ4Compressed:
                {
                    var compressedStream = LZ4Stream.Decode(stream, leaveOpen: leaveOpen);
                    var bufferedStream = new BufferedStream(compressedStream, bufferSize);
                    return new SampleStreamReader(bufferedStream, signature, leaveOpen);
                }
                case SampleStreamSignature.Uncompressed:
                {
                    var bufferedStream = new BufferedStream(stream, bufferSize);
                    return new SampleStreamReader(bufferedStream, signature, leaveOpen);
                }
                default:
                    throw new NotSupportedException($"Unsupported signature 0x{signatureValue:X}");
            }
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
    }
}