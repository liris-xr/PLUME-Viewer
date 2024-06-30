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
    public class SampleStreamTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            _stream = new MemoryStream();
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

            _buffer = new ArrayBufferWriter<byte>(256);
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
            _reader = SampleStream.Create(_stream, true);
            _buffer.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _reader.Dispose();
        }

        private Stream _stream;
        private SampleStream _reader;
        private byte[] _packedSample1Bytes;
        private byte[] _packedSample2Bytes;
        private ArrayBufferWriter<byte> _buffer;

        [Test]
        public void ReadSample_ReadFirst()
        {
            var sampleSize = _reader.ReadSample(_buffer);
            Assert.AreEqual(_packedSample1Bytes.Length, sampleSize);
            Assert.AreEqual(_packedSample1Bytes, _buffer.WrittenSpan.ToArray());
        }

        [Test]
        public void ReadSample_ReadAll()
        {
            var sampleSize1 = _reader.ReadSample(_buffer);
            var sampleSize2 = _reader.ReadSample(_buffer);
            Assert.AreEqual(_packedSample1Bytes.Length, sampleSize1);
            Assert.AreEqual(_packedSample2Bytes.Length, sampleSize2);
            Assert.AreEqual(_packedSample1Bytes, _buffer.WrittenSpan[..sampleSize1].ToArray());
            Assert.AreEqual(_packedSample2Bytes,
                _buffer.WrittenSpan[sampleSize1..(sampleSize1 + sampleSize2)].ToArray());
        }
    }
}