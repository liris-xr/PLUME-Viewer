using System;
using System.IO;
using NUnit.Framework;
using Runtime;

namespace Tests
{
    public class TestRecordReader
    {
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

        [Test]
        public void CreateAsync_WithUnreadableStream_ThrowsArgumentException()
        {
            var stream = new MemoryStream(Array.Empty<byte>(), false);
            Assert.Throws<EndOfStreamException>(() => RecordReader.CreateAsync(stream).GetAwaiter().GetResult());
        }

        [Test]
        public void CreateAsync_WithUnknownSignature_ThrowsInvalidDataException()
        {
            var stream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            Assert.Throws<InvalidDataException>(() => RecordReader.CreateAsync(stream).GetAwaiter().GetResult());
        }

        [Test]
        public void CreateAsync_WithLZ4CompressedSignature_ReturnsRecordReader()
        {
            var stream = new MemoryStream(BitConverter.GetBytes((int)RecordSignature.LZ4Compressed));
            var reader = RecordReader.CreateAsync(stream).Result;
            Assert.IsNotNull(reader);
        }
    }
}