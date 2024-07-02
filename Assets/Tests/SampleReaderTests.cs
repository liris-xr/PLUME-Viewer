using System.Buffers;
using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using K4os.Compression.LZ4.Streams;
using NUnit.Framework;
using PLUME.Sample;
using Runtime;
using UnityEngine.TestTools;
using Vector3 = PLUME.Sample.Common.Vector3;

namespace Tests
{
    [TestFixture]
    public class SampleReaderTests
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
            _decoderStream = LZ4Stream.Decode(_stream, leaveOpen: true);
            _reader = new SampleReader(_decoderStream, true);
            _buffer.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _reader.Dispose();
        }

        private Stream _stream;
        private Stream _decoderStream;
        private SampleReader _reader;
        private byte[] _packedSample1Bytes;
        private byte[] _packedSample2Bytes;
        private ArrayBufferWriter<byte> _buffer;

        [Test]
        public void ReadSample_ReadFirst()
        {
            var sampleSize = _reader.ReadSampleBytes(_buffer);
            Assert.AreEqual(_packedSample1Bytes.Length, sampleSize);
            Assert.AreEqual(_packedSample1Bytes, _buffer.WrittenSpan.ToArray());
        }

        [Test]
        public void ReadSample_ReadAll()
        {
            var sampleSize1 = _reader.ReadSampleBytes(_buffer);
            var sampleSize2 = _reader.ReadSampleBytes(_buffer);
            Assert.AreEqual(_packedSample1Bytes.Length, sampleSize1);
            Assert.AreEqual(_packedSample2Bytes.Length, sampleSize2);
            Assert.AreEqual(_packedSample1Bytes, _buffer.WrittenSpan[..sampleSize1].ToArray());
            Assert.AreEqual(_packedSample2Bytes,
                _buffer.WrittenSpan[sampleSize1..(sampleSize1 + sampleSize2)].ToArray());
        }

        [Test]
        public void ReadDelimitedMessage_ReadTooMany_ReturnsZero()
        {
            ArrayBufferWriter<byte> bufferWriter = new(256);
            _reader.ReadSampleBytes(bufferWriter);
            _reader.ReadSampleBytes(bufferWriter);
            Assert.AreEqual(0, _reader.ReadSampleBytes(bufferWriter));
        }

        [UnityTest]
        public IEnumerator ReadSampleAsync_ReadFirst()
        {
            return UniTask.ToCoroutine(async () =>
            {
                ArrayBufferWriter<byte> bufferWriter = new(256);
                var sampleSize = await _reader.ReadSampleBytesAsync(bufferWriter);
                Assert.AreEqual(_packedSample1Bytes.Length, sampleSize);
                Assert.AreEqual(_packedSample1Bytes, bufferWriter.WrittenSpan[..sampleSize].ToArray());
            });
        }

        [UnityTest]
        public IEnumerator ReadSampleAsync_ReadAll()
        {
            return UniTask.ToCoroutine(async () =>
            {
                ArrayBufferWriter<byte> bufferWriter = new(256);
                var sampleSize1 = await _reader.ReadSampleBytesAsync(bufferWriter);
                var sampleSize2 = await _reader.ReadSampleBytesAsync(bufferWriter);
                Assert.AreEqual(_packedSample1Bytes.Length, sampleSize1);
                Assert.AreEqual(_packedSample2Bytes.Length, sampleSize2);
                Assert.AreEqual(_packedSample1Bytes, bufferWriter.WrittenSpan[..sampleSize1].ToArray());
                Assert.AreEqual(_packedSample2Bytes,
                    bufferWriter.WrittenSpan[sampleSize1..(sampleSize1 + sampleSize2)].ToArray());
            });
        }

        [UnityTest]
        public IEnumerator ReadSampleAsync_ReadTooMany_ReturnsZero()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // The test sample stream only contains two samples. Reading a third sample should throw an exception.
                ArrayBufferWriter<byte> bufferWriter = new(256);
                await _reader.ReadSampleBytesAsync(bufferWriter);
                await _reader.ReadSampleBytesAsync(bufferWriter);
                Assert.AreEqual(0, await _reader.ReadSampleBytesAsync(bufferWriter));
            });
        }
    }
}