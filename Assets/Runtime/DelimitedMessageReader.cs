using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime
{
    /// <summary>
    ///     Reads delimited <see cref="Google.Protobuf.IMessage" /> message bytes from a stream.
    /// </summary>
    public class DelimitedMessageReader : IDisposable
    {
        private readonly bool _leaveOpen;
        private readonly Stream _stream;

        public DelimitedMessageReader(Stream stream, bool leaveOpen = false)
        {
            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        public void Dispose()
        {
            if (!_leaveOpen)
                _stream.Dispose();
        }

        /// <summary>
        ///     Reads the next delimited message's bytes from the stream and advances the position within the stream.
        /// </summary>
        /// <param name="bufferWriter">
        ///     Output sink into which the message bytes are written.
        /// </param>
        /// <returns>The number of bytes read into the buffer.</returns>
        /// <exception cref="TruncatedStreamException">
        ///     The message's size is truncated or the end of the stream is reached before
        ///     the message is fully read.
        /// </exception>
        /// <exception cref="MalformedStreamException.MalformedVarInt">The message's size is malformed.</exception>
        public int ReadDelimitedMessage(IBufferWriter<byte> bufferWriter)
        {
            var nBytes = (int)_stream.ReadRawVarInt32();
            var buffer = bufferWriter.GetSpan(nBytes);
            _stream.ReadExactly(buffer[..nBytes]);
            bufferWriter.Advance(nBytes);
            return nBytes;
        }

        /// <summary>
        ///     Asynchronously reads the next delimited message's bytes from the stream and advances the position within the
        ///     stream.
        /// </summary>
        /// <param name="bufferWriter">
        ///     Output sink into which the sample's bytes are written.
        /// </param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The number of bytes read into the buffer.</returns>
        /// <exception cref="TruncatedStreamException">
        ///     The message's size is truncated or the end of the stream is reached before
        ///     the message is fully read.
        /// </exception>
        /// <exception cref="MalformedStreamException.MalformedVarInt">The message's size is malformed.</exception>
        public async Task<int> ReadDelimitedMessageAsync(IBufferWriter<byte> bufferWriter,
            CancellationToken cancellationToken = default)
        {
            var nBytes = (int)_stream.ReadRawVarInt32();
            var buffer = bufferWriter.GetMemory(nBytes);
            await _stream.ReadExactlyAsync(buffer[..nBytes], cancellationToken);
            bufferWriter.Advance(nBytes);
            return nBytes;
        }
    }
}