using System;
using System.Buffers.Binary;
using System.IO;
using K4os.Compression.LZ4.Streams;

namespace Runtime
{
    public class SampleStream : Stream
    {
        private readonly SampleStreamSignature _signature;
        private readonly Stream _stream;

        private SampleStream(Stream stream, SampleStreamSignature signature)
        {
            _stream = stream;
            _signature = signature;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public static SampleStream Create(Stream stream, bool leaveOpen = false, string cacheFilePath = null,
            bool destroyCacheOnClose = true, int bufferSize = 4096)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable.", nameof(stream));

            if (!stream.CanSeek || cacheFilePath != null)
            {
                cacheFilePath ??= Path.GetTempFileName();
                var cacheFileStream = new FileStream(cacheFilePath, FileMode.Create, FileAccess.ReadWrite,
                    FileShare.None, 4096, destroyCacheOnClose ? FileOptions.DeleteOnClose : FileOptions.None);
                stream = new CachingStream(stream, cacheFileStream, leaveOpen);
            }

            var signature = ReadStreamSignature(stream);

            switch (signature)
            {
                case SampleStreamSignature.LZ4Compressed:
                {
                    var decoderStream = LZ4Stream.Decode(stream, leaveOpen: leaveOpen);
                    var bufferedStream = new BufferedStream(decoderStream, bufferSize);
                    return new SampleStream(bufferedStream, signature);
                }
                case SampleStreamSignature.Uncompressed:
                {
                    var bufferedStream = new BufferedStream(stream, bufferSize);
                    return new SampleStream(bufferedStream, signature);
                }
                default:
                    throw new NotSupportedException($"Unsupported sample stream signature 0x{signature:X}");
            }
        }

        private static SampleStreamSignature ReadStreamSignature(Stream stream)
        {
            Span<byte> signatureBytes = stackalloc byte[4];
            stream.ReadExactly(signatureBytes);

            var signature = BinaryPrimitives.ReadUInt32LittleEndian(signatureBytes);

            if (!Enum.IsDefined(typeof(SampleStreamSignature), signature))
                throw new MalformedStreamException.UnknownSampleStreamSignature(signature);

            return (SampleStreamSignature)signature;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}