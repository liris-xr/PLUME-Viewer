using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime
{
    public static class StreamUtils
    {
        /// <summary>
        ///     Reads bytes from the current stream and advances the position within the stream until the
        ///     <paramref name="buffer" /> is filled.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">
        ///     A region of memory. When this method returns, the contents of this region are replaced by the
        ///     bytes read from the current stream.
        /// </param>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before filling the <paramref name="buffer" />.</exception>
        public static void ReadExactly(this Stream stream, Span<byte> buffer)
        {
            var bytesRead = 0;
            while (bytesRead < buffer.Length)
            {
                var read = stream.Read(buffer[bytesRead..]);
                if (read == 0)
                    throw new EndOfStreamException();
                bytesRead += read;
            }
        }

        /// <summary>
        ///     Reads <paramref name="count" /> number bytes from the current stream and advances the position within the stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">
        ///     An array of bytes. When this method returns, the buffer contains the specified byte array with the
        ///     values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced
        ///     by the bytes read from the current stream.
        /// </param>
        /// <param name="offset">
        ///     The byte offset in <paramref name="buffer" /> at which to begin storing the data read from the
        ///     current stream.
        /// </param>
        /// <param name="count">The number of bytes to be read from the current stream.</param>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before filling the <paramref name="buffer" />.</exception>
        public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        {
            var bytesRead = 0;
            while (bytesRead < count)
            {
                var read = stream.Read(buffer, offset + bytesRead, count - bytesRead);
                if (read == 0)
                    throw new EndOfStreamException();
                bytesRead += read;
            }
        }

        /// <summary>
        ///     Asynchronously reads bytes from the current stream and advances the position within the stream until the
        ///     <paramref name="buffer" /> is filled, and monitors cancellation requests.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">
        ///     A region of memory. When this method returns, the contents of this region are replaced by the
        ///     bytes read from the current stream.
        /// </param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before filling the <paramref name="buffer" />.</exception>
        /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
        public static async Task ReadExactlyAsync(this Stream stream, Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var bytesRead = 0;
            while (bytesRead < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer[bytesRead..], cancellationToken);
                if (read == 0)
                    throw new EndOfStreamException();
                bytesRead += read;
            }
        }

        /// <summary>
        ///     Asynchronously reads <paramref name="count" /> number bytes from the current stream and advances the position
        ///     within the stream, and monitors cancellation requests.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">
        ///     An array of bytes. When this method returns, the buffer contains the specified byte array with the
        ///     values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced
        ///     by the bytes read from the current stream.
        /// </param>
        /// <param name="offset">
        ///     The byte offset in <paramref name="buffer" /> at which to begin storing the data read from the
        ///     current stream.
        /// </param>
        /// <param name="count">The number of bytes to be read from the current stream.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before filling the <paramref name="buffer" />.</exception>
        /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
        public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count,
            CancellationToken cancellationToken = default)
        {
            var bytesRead = 0;
            while (bytesRead < count)
            {
                var read = await stream.ReadAsync(buffer, offset + bytesRead, count - bytesRead, cancellationToken);
                if (read == 0)
                    throw new EndOfStreamException();
                bytesRead += read;
            }
        }
    }
}