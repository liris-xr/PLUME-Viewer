using System;
using System.IO;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using K4os.Compression.LZ4.Streams;
using NUnit.Framework;
using PLUME.Sample;
using Runtime;
using Vector3 = PLUME.Sample.Common.Vector3;

namespace Tests
{
    [TestFixture]
    public class TestRecordReaderCreate
    {
        [OneTimeSetUp]
        public void Init()
        {
            _stream = new MemoryStream();
            _stream.Write(BitConverter.GetBytes((int)RecordSignature.LZ4Compressed));
            using var compressedStream = LZ4Stream.Encode(_stream, leaveOpen: true);

            var packedSample1 = new PackedSample
            {
                Timestamp = 1,
                Payload = Any.Pack(new Vector3 { X = 1, Y = 2, Z = 3 })
            };
            var packedSample2 = new PackedSample
            {
                Timestamp = 2,
                Payload = Any.Pack(new Vector3 { X = 4, Y = 5, Z = 6 })
            };
            packedSample1.WriteDelimitedTo(compressedStream);
            packedSample2.WriteDelimitedTo(compressedStream);
            compressedStream.Flush();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _stream.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            _stream.Seek(0, SeekOrigin.Begin);
        }

        private Stream _stream;
        private int _packedSample1Size;
        private int _packedSample2Size;

        [Test]
        public void Create_WithLZ4CompressedStream_ReturnsRecordReader()
        {
            var reader = RecordReaderCreate.Create(_stream);
            Assert.IsNotNull(reader);
            Assert.AreEqual(RecordSignature.LZ4Compressed, reader.RecordSignature);
        }

        [Test]
        public void Create_WithUnreadableStream_ThrowsArgumentException()
        {
            var stream = new MemoryStream(Array.Empty<byte>(), false);
            Assert.Throws<EndOfStreamException>(() => RecordReaderCreate.Create(stream));
        }

        [Test]
        public void Create_WithUnknownSignature_ThrowsInvalidDataException()
        {
            var stream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            Assert.Throws<InvalidDataException>(() => RecordReaderCreate.Create(stream));
        }

        [Test]
        public void Create_WithLZ4CompressedSignature_ReturnsRecordReader()
        {
            var stream = new MemoryStream(BitConverter.GetBytes((int)RecordSignature.LZ4Compressed));
            var reader = RecordReaderCreate.Create(stream);
            Assert.IsNotNull(reader);
        }
    }

    [TestFixture]
    public class TestRecordReaderReadSamples
    {
        [OneTimeSetUp]
        public void Init()
        {
            _stream = new MemoryStream();
            _stream.Write(BitConverter.GetBytes((int)RecordSignature.LZ4Compressed));
            using var compressedStream = LZ4Stream.Encode(_stream, leaveOpen: true);

            var packedSample1 = new PackedSample
            {
                Timestamp = 1,
                Payload = Any.Pack(new Vector3 { X = 1, Y = 2, Z = 3 })
            };
            var packedSample2 = new PackedSample
            {
                Timestamp = 2,
                Payload = Any.Pack(new Vector3 { X = 4, Y = 5, Z = 6 })
            };
            packedSample1.WriteDelimitedTo(compressedStream);
            packedSample2.WriteDelimitedTo(compressedStream);
            compressedStream.Flush();

            _packedSample1Bytes = packedSample1.ToByteArray();
            _packedSample2Bytes = packedSample2.ToByteArray();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _stream.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            _stream.Seek(0, SeekOrigin.Begin);
            _reader = RecordReaderCreate.Create(_stream);
        }

        private Stream _stream;
        private byte[] _packedSample1Bytes;
        private byte[] _packedSample2Bytes;
        private RecordReaderCreate _reader;

        [Test]
        public void ReadNextSample_ReadFirst()
        {
            Span<byte> buffer = stackalloc byte[256];
            var sampleSize = _reader.ReadNextSample(buffer);
            Assert.AreEqual(_packedSample1Bytes.Length, sampleSize);
            Assert.AreEqual(_packedSample1Bytes, buffer[..sampleSize].ToArray());
        }

        [Test]
        public void ReadNextSample_ReadAll()
        {
            Span<byte> buffer = stackalloc byte[256];
            var sample1Size = _reader.ReadNextSample(buffer);
            var sample2Size = _reader.ReadNextSample(buffer[sample1Size..]);
            Assert.AreEqual(_packedSample1Bytes.Length, sample1Size);
            Assert.AreEqual(_packedSample2Bytes.Length, sample2Size);
            Assert.AreEqual(_packedSample1Bytes, buffer[..sample1Size].ToArray());
            Assert.AreEqual(_packedSample2Bytes, buffer.Slice(sample1Size, sample2Size).ToArray());
        }

        [Test]
        public void ReadNextSample_BufferTooSmall()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Span<byte> buffer = stackalloc byte[10];
                _reader.ReadNextSample(buffer);
            });
        }
    }
}