using System;
using System.IO;
using System.Net.Sockets;
using JetBrains.Annotations;
using K4os.Compression.LZ4.Streams;

namespace Runtime
{
    /// <summary>
    ///     Read-only stream that caches all data read from the base stream to a temporary local disk file or a custom stream.
    ///     This streams allows for seeking and reading data from originally non-seekable streams like
    ///     <see cref="NetworkStream" />, <see cref="LZ4Stream" />, etc.
    /// </summary>
    public class CachingStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly Stream _cacheStream;

        private readonly bool _leaveBaseStreamOpen;
        private readonly bool _leaveCacheStreamOpen;
        [CanBeNull] private readonly string _tmpCachePath;

        public CachingStream(Stream baseStream, Stream cacheStream, bool leaveBaseStreamOpen = false,
            bool leaveCacheStreamOpen = false)
        {
            if (!cacheStream.CanSeek || !cacheStream.CanWrite)
                throw new ArgumentException("The cache stream must be seekable and writable.", nameof(cacheStream));

            _baseStream = baseStream;
            _leaveBaseStreamOpen = leaveBaseStreamOpen;
            _leaveCacheStreamOpen = leaveCacheStreamOpen;
            _cacheStream = cacheStream;
        }

        public CachingStream(Stream baseStream, bool leaveOpen = false)
        {
            _baseStream = baseStream;
            _leaveBaseStreamOpen = leaveOpen;
            _leaveCacheStreamOpen = false;
            _tmpCachePath = Path.GetTempFileName();

            _tmpCachePath = Path.GetTempFileName();
            _cacheStream = new FileStream(_tmpCachePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override long Length => _cacheStream.Length;

        public override long Position
        {
            get => _cacheStream.Position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush()
        {
            _cacheStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readFromCache = _cacheStream.Read(buffer, offset, count);
            if (readFromCache == count) return readFromCache;

            var remainingBytes = count - readFromCache;
            var readFromBaseStream = _baseStream.Read(buffer, offset + readFromCache, remainingBytes);

            // Add the uncached bytes to the disk cache.
            _cacheStream.Write(buffer, offset + readFromCache, readFromBaseStream);
            return readFromCache + readFromBaseStream;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return origin switch
            {
                SeekOrigin.Begin when offset > Length => throw new ArgumentOutOfRangeException(nameof(offset),
                    "Cannot seek past the end of the cache."),
                SeekOrigin.Begin when offset < 0 => throw new ArgumentOutOfRangeException(nameof(offset),
                    "Cannot seek before the beginning of the cache."),
                SeekOrigin.End when offset > 0 => throw new ArgumentOutOfRangeException(nameof(offset),
                    "Cannot seek past the end of the cache."),
                SeekOrigin.End when offset < -Length => throw new ArgumentOutOfRangeException(nameof(offset),
                    "Cannot seek before the beginning of the cache."),
                SeekOrigin.Current when Position + offset < 0 => throw new ArgumentOutOfRangeException(nameof(offset),
                    "Cannot seek before the beginning of the cache."),
                SeekOrigin.Current when Position + offset > Length => throw new ArgumentOutOfRangeException(
                    nameof(offset), "Cannot seek past the end of the cache."),
                _ => _cacheStream.Seek(offset, origin)
            };
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_leaveBaseStreamOpen) _baseStream.Dispose();
                if (!_leaveCacheStreamOpen) _cacheStream.Dispose();

                if (_tmpCachePath != null)
                    File.Delete(_tmpCachePath);
            }

            base.Dispose(disposing);
        }
    }
}