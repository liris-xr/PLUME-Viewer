using System;
using System.IO;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams;

namespace Runtime
{
    public class RecordReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly bool _leaveOpen;

        private RecordReader(Stream stream, bool leaveOpen = false)
        {
            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Create a new <see cref="RecordReader"/> from the given stream.
        /// </summary>
        /// <param name="stream">The stream to create the reader from.</param>
        /// <param name="leaveOpen">Whether to leave the stream open when the reader is disposed.</param>
        /// <returns>A new <see cref="RecordReader"/> instance.</returns>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="InvalidDataException">The signature is unknown.</exception>
        public static RecordReader Create(Stream stream, bool leaveOpen = false)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));

            var signature = ReadSignature(stream);

            switch (signature)
            {
                case RecordSignature.LZ4Compressed:
                    var compressedStream = LZ4Stream.Decode(stream, leaveOpen: leaveOpen);
                    return new RecordReader(compressedStream, leaveOpen);
                case RecordSignature.Uncompressed:
                    return new RecordReader(stream, leaveOpen);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public static async Task<RecordReader> CreateAsync(Stream stream, bool leaveOpen = false)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));

            var signature = await ReadSignatureAsync(stream);

            switch (signature)
            {
                case RecordSignature.LZ4Compressed:
                    var compressedStream = LZ4Stream.Decode(stream, leaveOpen: leaveOpen);
                    return new RecordReader(compressedStream, leaveOpen);
                case RecordSignature.Uncompressed:
                    return new RecordReader(stream, leaveOpen);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Read and return the signature of the record from the stream.
        /// </summary>
        /// <returns>The <see cref="RecordSignature"/> of the record.</returns>
        /// <exception cref="InvalidDataException">The signature is unknown.</exception>
        private static RecordSignature ReadSignature(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadExactly(buffer);
            var signature = BitConverter.ToInt32(buffer);
            if (!Enum.IsDefined(typeof(RecordSignature), signature))
                throw new InvalidDataException($"Unknown signature 0x{signature:X}");
            return (RecordSignature)signature;
        }
        
        private static async Task<RecordSignature> ReadSignatureAsync(Stream stream)
        {
            Memory<byte> buffer = new byte[4];
            await stream.ReadExactlyAsync(buffer);
            var signature = BitConverter.ToInt32(buffer.Span);
            if (!Enum.IsDefined(typeof(RecordSignature), signature))
                throw new InvalidDataException($"Unknown signature 0x{signature:X}");
            return (RecordSignature)signature;
        }

        public void Dispose()
        {
            if (!_leaveOpen)
                _stream.Dispose();
        }
    }

    public enum RecordSignature
    {
        LZ4Compressed = 0x184D2204,
        Uncompressed = 0x00000000
    }
}