using System;
using System.IO;
using NUnit.Framework;
using Runtime;

namespace Tests
{
    public class CachingStreamTests
    {
        private byte[] _buffer;
        private byte[] _data;
        private Stream _nonSeekableStream;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _data = new byte[]
            {
                0x01, 0x02,
                0x01, 0x03,
                0x01, 0x04
            };

            _buffer = new byte[_data.Length];
        }

        [SetUp]
        public void SetUp()
        {
            _nonSeekableStream = new NonSeekableMemoryStream(_data);
        }

        [TearDown]
        public void Teardown()
        {
            _nonSeekableStream.Dispose();
        }

        [Test]
        [Order(1)]
        public void Create_Succeeds()
        {
            Assert.DoesNotThrow(() => { _ = new CachingStream(_nonSeekableStream); });
        }

        [Test]
        [Order(2)]
        public void Read_FirstTime_CachedContentIdentical()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);

            var buffer = new byte[6];

            // We start with an empty cache. We expect the cache to grow as we read from the base stream.
            Assert.AreEqual(0, cachingStream.Length);
            var nBytes = cachingStream.Read(buffer, 0, _data.Length);
            Assert.AreEqual(_data.Length, nBytes);
            // The cache should now have the same data as the base stream.
            Assert.AreEqual(_data, buffer);
        }

        [Test]
        [Order(3)]
        public void GetPosition_Succeeds()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);

            Assert.AreEqual(0, cachingStream.Position);
            _ = cachingStream.Read(_buffer, 0, _data.Length);
            Assert.AreEqual(_data.Length, cachingStream.Position);
        }

        [Test]
        [Order(4)]
        public void Seek_Begin_InRange_Succeeds()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);
            _ = cachingStream.Read(_buffer, 0, _data.Length);
            cachingStream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(0, cachingStream.Position);
        }

        [Test]
        [Order(4)]
        public void Seek_Begin_OutOfRangeBeforeBeginning_ThrowsArgumentOutOfRangeException()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);
            _ = cachingStream.Read(_buffer, 0, _data.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => cachingStream.Seek(-1, SeekOrigin.Begin));
        }

        [Test]
        [Order(4)]
        public void Seek_Begin_OutOfRangeAfterEnd_ThrowsArgumentOutOfRangeException()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);
            _ = cachingStream.Read(_buffer, 0, _data.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => cachingStream.Seek(_data.Length + 1, SeekOrigin.Begin));
        }

        [Test]
        [Order(4)]
        public void Seek_Current_InRange_Succeeds()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);
            _ = cachingStream.Read(_buffer, 0, _data.Length);
            cachingStream.Seek(-1, SeekOrigin.Current);
            Assert.AreEqual(_data.Length - 1, cachingStream.Position);
        }

        [Test]
        [Order(4)]
        public void Seek_Current_OutOfRangeBeforeBeginning_ThrowsArgumentOutOfRangeException()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);
            _ = cachingStream.Read(_buffer, 0, _data.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => cachingStream.Seek(-_data.Length - 1, SeekOrigin.Current));
        }

        [Test]
        [Order(4)]
        public void Seek_Current_OutOfRangeAfterEnd_ThrowsArgumentOutOfRangeException()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);
            _ = cachingStream.Read(_buffer, 0, _data.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => cachingStream.Seek(1, SeekOrigin.Current));
        }

        [Test]
        [Order(4)]
        public void Seek_End_InRange_Succeeds()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);
            _ = cachingStream.Read(_buffer, 0, _data.Length);
            cachingStream.Seek(-1, SeekOrigin.End);
            Assert.AreEqual(_data.Length - 1, cachingStream.Position);
        }

        [Test]
        [Order(4)]
        public void Seek_End_OutOfRangeBeforeBeginning_ThrowsArgumentOutOfRangeException()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);
            _ = cachingStream.Read(_buffer, 0, _data.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => cachingStream.Seek(-_data.Length - 1, SeekOrigin.End));
        }

        [Test]
        [Order(4)]
        public void Seek_End_OutOfRangeAfterEnd_ThrowsArgumentOutOfRangeException()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);
            _ = cachingStream.Read(_buffer, 0, _data.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => cachingStream.Seek(1, SeekOrigin.End));
        }

        [Test]
        [Order(5)]
        public void Read_SecondTime_CachedContentIdentical()
        {
            using var cachingStream = new CachingStream(_nonSeekableStream);

            var buffer = new byte[6];

            _ = cachingStream.Read(buffer, 0, _data.Length);

            // We expect the cache to be used when reading the same data again, and still get the same data.
            cachingStream.Seek(0, SeekOrigin.Begin);
            var nBytes = cachingStream.Read(buffer, 0, _data.Length);
            Assert.AreEqual(_data.Length, nBytes);
            Assert.AreEqual(_data, buffer);
            // The cache should still have the same length.
            Assert.AreEqual(_data.Length, cachingStream.Length);
        }
    }
}