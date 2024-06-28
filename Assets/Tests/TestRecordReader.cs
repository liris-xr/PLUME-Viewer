using System;
using System.IO;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using K4os.Compression.LZ4.Streams;
using NUnit.Framework;
using PLUME.Sample;
using PLUME.Sample.Common;
using Runtime;

namespace Tests
{
    public class TestRecordReader
    {
        private byte[] _recordData;

        [SetUp]
        public void SetUp()
        {
            using var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes((int)RecordSignature.LZ4Compressed));
            var compressedStream = LZ4Stream.Encode(stream);

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
            _recordData = stream.ToArray();
        }

        [Test]
        public void Create_WithLZ4CompressedStream_ReturnsRecordReader()
        {
            using var stream = new MemoryStream(_recordData);
            var reader = RecordReader.Create(stream);
            Assert.IsNotNull(reader);
            Assert.AreEqual(RecordSignature.LZ4Compressed, reader.RecordSignature);
        }

        [Test]
        public void Create_WithUnreadableStream_ThrowsArgumentException()
        {
            var stream = new MemoryStream(Array.Empty<byte>(), false);
            Assert.Throws<EndOfStreamException>(() => RecordReader.Create(stream));
        }

        [Test]
        public void Create_WithUnknownSignature_ThrowsInvalidDataException()
        {
            var stream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            Assert.Throws<InvalidDataException>(() => RecordReader.Create(stream));
        }

        [Test]
        public void Create_WithLZ4CompressedSignature_ReturnsRecordReader()
        {
            var stream = new MemoryStream(BitConverter.GetBytes((int)RecordSignature.LZ4Compressed));
            var reader = RecordReader.Create(stream);
            Assert.IsNotNull(reader);
        }
    }
}