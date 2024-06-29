using System.Buffers;
using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NUnit.Framework;
using PLUME.Sample;
using PLUME.Sample.Common;
using Runtime;
using UnityEngine.TestTools;

namespace Tests
{
    public class DelimitedMessageReaderTests
    {
        private byte[] _packedSample1Bytes;
        private byte[] _packedSample2Bytes;

        private Stream _stream;
        private DelimitedMessageReader _streamReader;

        [OneTimeSetUp]
        public void Init()
        {
            _stream = new MemoryStream();

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
            packedSample1.WriteDelimitedTo(_stream);
            packedSample2.WriteDelimitedTo(_stream);
            _stream.Flush();

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
            _streamReader = new DelimitedMessageReader(_stream, true);
        }

        [TearDown]
        public void TearDown()
        {
            _streamReader.Dispose();
        }

        [Test]
        public void ReadDelimitedMessage_ReadFirst()
        {
            ArrayBufferWriter<byte> bufferWriter = new(256);
            var sampleSize = _streamReader.ReadDelimitedMessage(bufferWriter);
            Assert.AreEqual(_packedSample1Bytes.Length, sampleSize);
            Assert.AreEqual(_packedSample1Bytes, bufferWriter.WrittenSpan[..sampleSize].ToArray());
        }

        [Test]
        public void ReadDelimitedMessage_ReadAll()
        {
            ArrayBufferWriter<byte> bufferWriter = new(256);
            var sample1Size = _streamReader.ReadDelimitedMessage(bufferWriter);
            var sample2Size = _streamReader.ReadDelimitedMessage(bufferWriter);
            Assert.AreEqual(_packedSample1Bytes.Length, sample1Size);
            Assert.AreEqual(_packedSample2Bytes.Length, sample2Size);
            Assert.AreEqual(_packedSample1Bytes, bufferWriter.WrittenSpan[..sample1Size].ToArray());
            Assert.AreEqual(_packedSample2Bytes, bufferWriter.WrittenSpan.Slice(sample1Size, sample2Size).ToArray());
        }

        [Test]
        public void ReadDelimitedMessage_ReadTooMany_ThrowsTruncatedStreamException()
        {
            ArrayBufferWriter<byte> bufferWriter = new(256);
            _streamReader.ReadDelimitedMessage(bufferWriter);
            _streamReader.ReadDelimitedMessage(bufferWriter);
            Assert.Throws<TruncatedStreamException>(() => _streamReader.ReadDelimitedMessage(bufferWriter));
        }

        [UnityTest]
        public IEnumerator ReadDelimitedMessageAsync_ReadFirst()
        {
            return UniTask.ToCoroutine(async () =>
            {
                ArrayBufferWriter<byte> bufferWriter = new(256);
                var sampleSize = await _streamReader.ReadDelimitedMessageAsync(bufferWriter);
                Assert.AreEqual(_packedSample1Bytes.Length, sampleSize);
                Assert.AreEqual(_packedSample1Bytes, bufferWriter.WrittenSpan[..sampleSize].ToArray());
            });
        }

        [UnityTest]
        public IEnumerator ReadDelimitedMessageAsync_ReadAll()
        {
            return UniTask.ToCoroutine(async () =>
            {
                ArrayBufferWriter<byte> bufferWriter = new(256);
                var sample1Size = await _streamReader.ReadDelimitedMessageAsync(bufferWriter);
                var sample2Size = await _streamReader.ReadDelimitedMessageAsync(bufferWriter);
                Assert.AreEqual(_packedSample1Bytes.Length, sample1Size);
                Assert.AreEqual(_packedSample2Bytes.Length, sample2Size);
                Assert.AreEqual(_packedSample1Bytes, bufferWriter.WrittenSpan[..sample1Size].ToArray());
                Assert.AreEqual(_packedSample2Bytes,
                    bufferWriter.WrittenSpan.Slice(sample1Size, sample2Size).ToArray());
            });
        }

        [UnityTest]
        public IEnumerator ReadDelimitedMessageAsync_ReadTooMany_ThrowsTruncatedStreamException()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // The test sample stream only contains two samples. Reading a third sample should throw an exception.
                ArrayBufferWriter<byte> bufferWriter = new(256);
                await _streamReader.ReadDelimitedMessageAsync(bufferWriter);
                await _streamReader.ReadDelimitedMessageAsync(bufferWriter);
                Assert.Throws<TruncatedStreamException>(() => _streamReader.ReadDelimitedMessage(bufferWriter));
            });
        }
    }
}