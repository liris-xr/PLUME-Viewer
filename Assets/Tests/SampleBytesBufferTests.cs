using System.Buffers;
using NUnit.Framework;
using Runtime;

namespace Tests
{
    public class SampleBytesBufferTests
    {
        [Test]
        [Order(1)]
        public void AddSampleBytes_EnoughSpaceAtEnd()
        {
            var buffer = new SampleBytesBuffer(10);
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            var sampleInfo = buffer.AddSampleBytes(bytes);
            Assert.AreEqual(0, sampleInfo.Offset);
            Assert.AreEqual(5, sampleInfo.Length);
            // Check that the buffer was not resized
            Assert.AreEqual(10, buffer.Capacity);
        }

        [Test]
        [Order(1)]
        public void AddSampleBytes_JustEnoughSpaceAtEnd()
        {
            var buffer = new SampleBytesBuffer(5);
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            var sampleInfo = buffer.AddSampleBytes(bytes);
            Assert.AreEqual(0, sampleInfo.Offset);
            Assert.AreEqual(5, sampleInfo.Length);
            // Check that the buffer was not resized
            Assert.AreEqual(5, buffer.Capacity);
        }

        [Test]
        [Order(1)]
        public void AddSampleBytes_NotEnoughSpaceAtEnd()
        {
            var buffer = new SampleBytesBuffer(5);
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6 };
            var sampleInfo = buffer.AddSampleBytes(bytes);
            Assert.AreEqual(0, sampleInfo.Offset);
            Assert.AreEqual(6, sampleInfo.Length);
            // Check that the buffer was resized
            Assert.IsTrue(buffer.Capacity >= bytes.Length);
        }

        [Test]
        [Order(2)]
        public void TakeSampleBytes()
        {
            var buffer = new SampleBytesBuffer(10);
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            buffer.AddSampleBytes(bytes);

            var dstBuffer = new ArrayBufferWriter<byte>();
            var sampleSize = buffer.TakeSampleBytes(dstBuffer);

            Assert.AreEqual(5, dstBuffer.WrittenCount);
            Assert.AreEqual(bytes, dstBuffer.WrittenSpan.ToArray());
            Assert.AreEqual(bytes.Length, sampleSize);
            Assert.IsTrue(buffer.IsEmpty);
        }

        [Test]
        [Order(3)]
        public void AddSampleBytes_NotEnoughSpaceAtEndButEnoughAtStart()
        {
            var buffer = new SampleBytesBuffer(10);
            buffer.AddSampleBytes(new byte[] { 1, 2, 3, 4, 5 });
            buffer.AddSampleBytes(new byte[] { 6, 7, 8, 9, 10 });

            // Remove the first sample to make space at the start
            var tmpBuffer = new ArrayBufferWriter<byte>();
            buffer.TakeSampleBytes(tmpBuffer);

            var bytes = new byte[] { 11, 12, 13 };
            var sampleInfo = buffer.AddSampleBytes(bytes);
            Assert.AreEqual(0, sampleInfo.Offset);
            Assert.AreEqual(3, sampleInfo.Length);
            // Check that the buffer was not resized
            Assert.AreEqual(10, buffer.Capacity);
        }

        [Test]
        [Order(3)]
        public void AddSampleBytes_NotEnoughSpaceAtEndButJustEnoughAtStart()
        {
            var buffer = new SampleBytesBuffer(10);
            buffer.AddSampleBytes(new byte[] { 1, 2, 3, 4, 5 });
            buffer.AddSampleBytes(new byte[] { 6, 7, 8, 9, 10 });

            // Remove the first sample to make space at the start
            var tmpBuffer = new ArrayBufferWriter<byte>();
            buffer.TakeSampleBytes(tmpBuffer);

            var bytes = new byte[] { 11, 12, 13, 14, 15 };
            var sampleInfo = buffer.AddSampleBytes(bytes);
            Assert.AreEqual(0, sampleInfo.Offset);
            Assert.AreEqual(5, sampleInfo.Length);
            // Check that the buffer was not resized
            Assert.AreEqual(10, buffer.Capacity);
        }
    }
}