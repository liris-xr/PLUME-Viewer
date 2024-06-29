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

        public void TakeSampleBytes(IBufferWriter<byte> buffer)
        {
            lock (_lock)
            {
                if (IsEmpty) throw new InvalidOperationException("No sample bytes in the buffer.");

                var sample = _samples[0];
                buffer.Write(_buffer.AsSpan(sample.Offset, sample.Length));
                _samples.RemoveAt(0);
                _start = sample.Offset + sample.Length;
            }
        }

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