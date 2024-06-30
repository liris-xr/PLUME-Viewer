using System;
using System.IO;

namespace Tests
{
    public class NonSeekableMemoryStream : Stream
    {
        private readonly MemoryStream _innerStream;

        public NonSeekableMemoryStream(byte[] data)
        {
            _innerStream = new MemoryStream(data);
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => false; // Not seekable
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => throw new NotSupportedException("Stream is not seekable.");
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Stream is not seekable.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Stream length cannot be changed.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _innerStream.Dispose();
            base.Dispose(disposing);
        }
    }
}