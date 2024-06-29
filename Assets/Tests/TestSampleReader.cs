using System;
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
    public class TestSampleReaderCreate
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
        private int _packedSample1Size;
        private int _packedSample2Size;

        [Test]
        public void Create_WithLZ4CompressedStream_ReturnsSampleReader()
        {
            var reader = SampleReader.Create(_stream);
            Assert.IsNotNull(reader);
            Assert.AreEqual(SampleStreamSignature.LZ4Compressed, reader.SampleStreamSignature);
        }

        [Test]
        public void Create_WithUnreadableStream_ThrowsArgumentException()
        {
            var stream = new MemoryStream(Array.Empty<byte>(), false);
            Assert.Throws<EndOfStreamException>(() => SampleReader.Create(stream));
        }

        [Test]
        public void Create_WithUnknownSignature_ThrowsMalformedSignatureException()
        {
            var stream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            Assert.Throws<MalformedStreamException.UnknownSampleStreamSignature>(() => SampleReader.Create(stream));
        }

        [Test]
        public void Create_WithLZ4CompressedSignature_ReturnsSampleReader()
        {
            var stream = new MemoryStream(BitConverter.GetBytes((int)SampleStreamSignature.LZ4Compressed));
            var reader = SampleReader.Create(stream);
            Assert.IsNotNull(reader);
        }
    }

    [TestFixture]
    public class TestSampleReaderReadNext
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
            _reader = SampleReader.Create(_stream);
        }

        private Stream _stream;
        private byte[] _packedSample1Bytes;
        private byte[] _packedSample2Bytes;
        private SampleReader _reader;

        [Test]
        public void ReadNextSample_ReadFirst()
        {
            ArrayBufferWriter<byte> bufferWriter = new(256);
            var sampleSize = _reader.ReadNextSample(bufferWriter);
            Assert.AreEqual(_packedSample1Bytes.Length, sampleSize);
            Assert.AreEqual(_packedSample1Bytes, bufferWriter.WrittenSpan[..sampleSize].ToArray());
        }

        [Test]
        public void ReadNextSample_ReadAll()
        {
            ArrayBufferWriter<byte> bufferWriter = new(256);
            var sample1Size = _reader.ReadNextSample(bufferWriter);
            var sample2Size = _reader.ReadNextSample(bufferWriter);
            Assert.AreEqual(_packedSample1Bytes.Length, sample1Size);
            Assert.AreEqual(_packedSample2Bytes.Length, sample2Size);
            Assert.AreEqual(_packedSample1Bytes, bufferWriter.WrittenSpan[..sample1Size].ToArray());
            Assert.AreEqual(_packedSample2Bytes, bufferWriter.WrittenSpan.Slice(sample1Size, sample2Size).ToArray());
        }

        [Test]
        public void ReadNextSample_ReadTooMany_ThrowsTruncatedStreamException()
        {
            ArrayBufferWriter<byte> bufferWriter = new(256);
            _reader.ReadNextSample(bufferWriter);
            _reader.ReadNextSample(bufferWriter);
            Assert.Throws<TruncatedStreamException>(() => _reader.ReadNextSample(bufferWriter));
        }

        [UnityTest]
        public IEnumerator ReadNextSampleAsync_ReadFirst()
        {
            return UniTask.ToCoroutine(async () =>
            {
                ArrayBufferWriter<byte> bufferWriter = new(256);
                var sampleSize = await _reader.ReadNextSampleAsync(bufferWriter);
                Assert.AreEqual(_packedSample1Bytes.Length, sampleSize);
                Assert.AreEqual(_packedSample1Bytes, bufferWriter.WrittenSpan[..sampleSize].ToArray());
            });
        }

        [UnityTest]
        public IEnumerator ReadNextSampleAsync_ReadAll()
        {
            return UniTask.ToCoroutine(async () =>
            {
                ArrayBufferWriter<byte> bufferWriter = new(256);
                var sample1Size = await _reader.ReadNextSampleAsync(bufferWriter);
                var sample2Size = await _reader.ReadNextSampleAsync(bufferWriter);
                Assert.AreEqual(_packedSample1Bytes.Length, sample1Size);
                Assert.AreEqual(_packedSample2Bytes.Length, sample2Size);
                Assert.AreEqual(_packedSample1Bytes, bufferWriter.WrittenSpan[..sample1Size].ToArray());
                Assert.AreEqual(_packedSample2Bytes,
                    bufferWriter.WrittenSpan.Slice(sample1Size, sample2Size).ToArray());
            });
        }

        [UnityTest]
        public IEnumerator ReadNextSampleAsync_ReadTooMany_ThrowsTruncatedStreamException()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // The test sample stream only contains two samples. Reading a third sample should throw an exception.
                ArrayBufferWriter<byte> bufferWriter = new(256);
                await _reader.ReadNextSampleAsync(bufferWriter);
                await _reader.ReadNextSampleAsync(bufferWriter);
                Assert.Throws<TruncatedStreamException>(() => _reader.ReadNextSample(bufferWriter));
            });
        }
    }
}