using System;
using System.Buffers;
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
    public class SampleStreamReaderCreateTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            _stream = new MemoryStream();
            _stream.Write(BitConverter.GetBytes((int)SampleStreamSignature.LZ4Compressed));
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

        [Test]
        public void Create_WithLZ4CompressedStream_ReturnsSampleStreamReader()
        {
            var reader = SampleStreamReader.Create(_stream);
            Assert.IsNotNull(reader);
            Assert.AreEqual(SampleStreamSignature.LZ4Compressed, reader.SampleStreamSignature);
        }

        [Test]
        public void Create_WithEmptyStream_ThrowsTruncatedStreamException()
        {
            var stream = new MemoryStream();
            Assert.Throws<TruncatedStreamException>(() => SampleStreamReader.Create(stream));
        }

        [Test]
        public void Create_WithUnknownSignature_ThrowsMalformedSignatureException()
        {
            var stream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            Assert.Throws<MalformedStreamException.UnknownSampleStreamSignature>(
                () => SampleStreamReader.Create(stream));
        }

        [Test]
        public void Create_WithLZ4CompressedSignature_ReturnsSampleStreamReader()
        {
            var stream = new MemoryStream(BitConverter.GetBytes((int)SampleStreamSignature.LZ4Compressed));
            var reader = SampleStreamReader.Create(stream);
            Assert.IsNotNull(reader);
        }
    }

    [TestFixture]
    public class SampleStreamReaderReadTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            _stream = new MemoryStream();
            _stream.Write(BitConverter.GetBytes((int)SampleStreamSignature.LZ4Compressed));
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
            _reader = SampleStreamReader.Create(_stream, leaveOpen: true);
        }

        [TearDown]
        public void TearDown()
        {
            _reader.Dispose();
        }

        private Stream _stream;
        private SampleStreamReader _reader;
        private byte[] _packedSample1Bytes;
        private byte[] _packedSample2Bytes;

        [Test]
        public void ReadSample_ReadFirst()
        {
            var buffer = new ArrayBufferWriter<byte>(256);
            var sampleSize = _reader.ReadSample(buffer);
            Assert.AreEqual(_packedSample1Bytes.Length, sampleSize);
            Assert.AreEqual(_packedSample1Bytes, buffer.WrittenSpan.ToArray());
        }

        [Test]
        public void ReadSample_ReadAll()
        {
            var buffer = new ArrayBufferWriter<byte>(256);
            var sampleSize1 = _reader.ReadSample(buffer);
            var sampleSize2 = _reader.ReadSample(buffer);
            Assert.AreEqual(_packedSample1Bytes.Length, sampleSize1);
            Assert.AreEqual(_packedSample2Bytes.Length, sampleSize2);
            Assert.AreEqual(_packedSample1Bytes, buffer.WrittenSpan[..sampleSize1].ToArray());
            Assert.AreEqual(_packedSample2Bytes, buffer.WrittenSpan.Slice(sampleSize1, sampleSize2).ToArray());
        }
    }
}