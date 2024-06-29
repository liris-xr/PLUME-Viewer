using System.IO;
using NUnit.Framework;
using Runtime;

namespace Tests
{
    public class TestStreamExtensions
    {
        [Test]
        public void ReadRawVarInt32_EmptyStream_ThrowsTruncated()
        {
            using var stream = CreateStream();
            Assert.Throws<TruncatedStreamException>(() => stream.ReadRawVarInt32());
        }

        [Test]
        public void ReadRawVarInt64_EmptyStream_ThrowsTruncated()
        {
            using var stream = CreateStream();
            Assert.Throws<TruncatedStreamException>(() => stream.ReadRawVarInt64());
        }

        [Test]
        public void ReadRawVarInt32_MissingBytes_ThrowsTruncated()
        {
            // Set the continuation bit on the first byte, but don't provide any more bytes.
            using var stream = CreateStream(0x80);
            Assert.Throws<TruncatedStreamException>(() => stream.ReadRawVarInt32());
        }

        [Test]
        public void ReadRawVarInt64_MissingBytes_ThrowsTruncated()
        {
            // Set the continuation bit on the first byte, but don't provide any more bytes.
            using var stream = CreateStream(0x80);
            Assert.Throws<TruncatedStreamException>(() => stream.ReadRawVarInt64());
        }

        [Test]
        public void ReadRawVarInt32_InvalidContinuationBit_ThrowsMalformed()
        {
            // The continuation bit is set in the most significant byte, but we already read 32 bits of data.
            //                                         v
            // LSB -> 11111111 [...] 11111111 11111111 10001111 <- MSB
            using var stream = CreateStream(0xFF, 0xFF, 0xFF, 0xFF, 0x9F);
            Assert.Throws<MalformedStreamException.MalformedVarInt>(() => stream.ReadRawVarInt32());
        }

        [Test]
        public void ReadRawVarInt64_InvalidContinuationBit_ThrowsMalformed()
        {
            // The continuation bit is set in the most significant byte, but we already read 64 bits of data.
            //                                         v
            // LSB -> 11111111 [...] 11111111 11111111 10000001 <- MSB
            using var stream = CreateStream(0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x81);
            Assert.Throws<MalformedStreamException.MalformedVarInt>(() => stream.ReadRawVarInt64());
        }

        [Test]
        public void ReadRawVarInt32_DataOverflow_ThrowsMalformed()
        {
            // If the MSB have more than 4 data bits set, then the total number of data bits exceeds 32. This is invalid.
            //                                            v
            // LSB -> 11111111 [...] 11111111 11111111 00011111 <- MSB
            using var stream = CreateStream(0xFF, 0xFF, 0xFF, 0xFF, 0x1F);
            Assert.Throws<MalformedStreamException.MalformedVarInt>(() => stream.ReadRawVarInt32());
        }

        [Test]
        public void ReadRawVarInt64_DataOverflow_ThrowsMalformed()
        {
            // If the MSB have more than 1 data bits set, then the total number of data bits exceeds 64. This is invalid.
            //                                               v
            // LSB -> 11111111 [...] 11111111 11111111 00000011 <- MSB
            using var stream = CreateStream(0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x03);
            Assert.Throws<MalformedStreamException.MalformedVarInt>(() => stream.ReadRawVarInt64());
        }

        [Test]
        public void ReadRawVarInt32_WithContinuationBit_1()
        {
            // Reads a value that requires more than one byte to be encoded.
            using var stream = CreateStream(0x96, 0x01);
            var result = stream.ReadRawVarInt32();
            Assert.AreEqual(150, result);
        }

        [Test]
        public void ReadRawVarInt32_WithContinuationBit_2()
        {
            // Reads a value that requires more than one byte to be encoded.
            using var stream = CreateStream(0x80, 0x01);
            var result = stream.ReadRawVarInt32();
            Assert.AreEqual(128, result);
        }

        [Test]
        public void ReadRawVarInt64_WithContinuationBit_1()
        {
            // Reads a value that requires more than one byte to be encoded.
            using var stream = CreateStream(0x96, 0x01);
            var result = stream.ReadRawVarInt64();
            Assert.AreEqual(150, result);
        }

        [Test]
        public void ReadRawVarInt64_WithContinuationBit_2()
        {
            // Reads a value that requires more than one byte to be encoded.
            using var stream = CreateStream(0x80, 0x01);
            var result = stream.ReadRawVarInt64();
            Assert.AreEqual(128, result);
        }

        [Test]
        public void ReadRawVarInt32_NoContinuationBit()
        {
            // Only one byte representing a value smaller than 128.
            using var stream = CreateStream(0x7F);
            var result = stream.ReadRawVarInt32();
            Assert.AreEqual(127, result);
        }

        [Test]
        public void ReadRawVarInt64_NoContinuationBit()
        {
            // Only one byte representing a value smaller than 128.
            using var stream = CreateStream(0x7F);
            var result = stream.ReadRawVarInt64();
            Assert.AreEqual(127, result);
        }

        [Test]
        public void ReadRawVarInt32_MaxValue()
        {
            // 5 bytes representing uint.MaxValue as a varint
            // LSB -> 11111111 [...] 11111111 11111111 00001111 <- MSB
            using var stream = CreateStream(0xFF, 0xFF, 0xFF, 0xFF, 0x0F);
            var result = stream.ReadRawVarInt32();
            Assert.AreEqual(uint.MaxValue, result);
        }

        [Test]
        public void ReadRawVarInt64_MaxValue()
        {
            // 10 bytes representing ulong.MaxValue as a varint
            // LSB -> 11111111 [...] 11111111 11111111 00000001 <- MSB
            using var stream = CreateStream(0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01);
            var result = stream.ReadRawVarInt64();
            Assert.AreEqual(ulong.MaxValue, result);
        }

        private static MemoryStream CreateStream(params byte[] bytes)
        {
            var stream = new MemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}