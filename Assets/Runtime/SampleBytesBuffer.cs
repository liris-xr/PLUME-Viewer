using System;
using System.Buffers;
using System.Collections.Generic;

namespace Runtime
{
    /// <summary>
    ///     A thread-safe buffer that stores samples bytes in a circular buffer.
    /// </summary>
    /// <remarks>
    ///     The sample bytes are stored contiguously (i.e. without splitting a sample across the buffer boundary).
    /// </remarks>
    public class SampleBytesBuffer
    {
        private readonly object _lock = new();
        private readonly List<SampleInfo> _samples;
        private byte[] _buffer;
        private int _end;
        private int _start;

        public SampleBytesBuffer(int initialCapacity = 256)
        {
            _buffer = new byte[initialCapacity];
            _samples = new List<SampleInfo>();
            _start = 0;
            _end = 0;
        }

        public int Capacity
        {
            get
            {
                lock (_lock)
                {
                    return _buffer.Length;
                }
            }
        }

        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return _samples.Count == 0;
                }
            }
        }

        /// <summary>
        ///     Take the first available sample bytes from the buffer and write them to the <paramref name="buffer" />.
        /// </summary>
        /// <param name="buffer">The buffer to write the sample bytes to.</param>
        /// <param name="sampleSize">The number of bytes written to the <paramref name="buffer" />.</param>
        /// <returns>Whether the sample bytes were successfully taken from the buffer.</returns>
        public bool TakeSampleBytes(IBufferWriter<byte> buffer, out int sampleSize)
        {
            lock (_lock)
            {
                if (IsEmpty)
                {
                    sampleSize = -1;
                    return false;
                }

                var sampleInfo = _samples[0];
                buffer.Write(_buffer.AsSpan(sampleInfo.Offset, sampleInfo.Length));
                _samples.RemoveAt(0);
                _start = sampleInfo.Offset + sampleInfo.Length;
                sampleSize = sampleInfo.Length;
                return true;
            }
        }

        /// <summary>
        ///     Add the sample bytes to the buffer.
        /// </summary>
        /// <param name="bytes">The sample bytes to add.</param>
        /// <returns>
        ///     The <see cref="SampleInfo" /> of the added sample bytes containing the offset and length of the sample bytes
        ///     in the buffer.
        /// </returns>
        /// <remarks>
        ///     The sample bytes are stored contiguously (i.e. without splitting a sample across the buffer boundary) in the
        ///     internal circular buffer. If there is not enough space in the buffer, the buffer size is automatically doubled.
        /// </remarks>
        public SampleInfo AddSampleBytes(Span<byte> bytes)
        {
            lock (_lock)
            {
                // If we have enough space at the end of the buffer, insert the bytes there.
                if (_end + bytes.Length <= _buffer.Length)
                {
                    var sampleInfo = new SampleInfo { Offset = _end, Length = bytes.Length };
                    _samples.Add(sampleInfo);
                    bytes.CopyTo(_buffer.AsSpan(_end));
                    _end += bytes.Length;
                    return sampleInfo;
                }

                // Check if we have enough space at the beginning of the buffer.
                if (_start >= bytes.Length)
                {
                    var sampleInfo = new SampleInfo { Offset = 0, Length = bytes.Length };
                    _samples.Add(sampleInfo);
                    bytes.CopyTo(_buffer.AsSpan(0));
                    _end = bytes.Length;
                    return sampleInfo;
                }
                else
                {
                    // If we failed to insert the bytes at the beginning or end of the buffer, we increase the buffer size and insert at the end.
                    var newBuffer = new byte[_buffer.Length * 2];
                    _buffer.AsSpan(0, _buffer.Length).CopyTo(newBuffer);
                    _buffer = newBuffer;

                    var sampleInfo = new SampleInfo { Offset = _end, Length = bytes.Length };
                    _samples.Add(sampleInfo);
                    bytes.CopyTo(newBuffer.AsSpan(_end));
                    _end += bytes.Length;
                    return sampleInfo;
                }
            }
        }
    }

    public struct SampleInfo
    {
        public int Offset;
        public int Length;
    }
}