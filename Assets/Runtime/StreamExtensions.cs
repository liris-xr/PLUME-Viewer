using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime
{
    public static class StreamExtensions
    {
        /// <summary>
        ///     Reads bytes from the stream and advances the position within the stream until the <paramref name="buffer" /> is
        ///     filled.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">
        ///     A region of memory. When this method returns, the region contains the bytes read from the stream.
        /// </param>
        /// <exception cref="TruncatedStreamException">The end of the stream is reached before the bytes are fully read.</exception>
        public static void ReadExactly(this Stream stream, Span<byte> buffer)
        {
            var bytesRead = 0;
            while (bytesRead < buffer.Length)
            {
                var read = stream.Read(buffer[bytesRead..]);
                if (read == 0)
                    throw new TruncatedStreamException();
                bytesRead += read;
            }
        }

        /// <summary>
        ///     Reads <paramref name="count" /> number bytes from the stream and advances the position within the stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">
        ///     An array of bytes. When this method returns, the buffer contains the specified byte array with the
        ///     values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced
        ///     by the bytes read from the stream.
        /// </param>
        /// <param name="offset">
        ///     The byte offset in <paramref name="buffer" /> at which to begin storing the data read from the stream.
        /// </param>
        /// <param name="count">The number of bytes to be read from the stream.</param>
        /// <exception cref="TruncatedStreamException">The end of the stream is reached before the bytes are fully read.</exception>
        public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        {
            var bytesRead = 0;
            while (bytesRead < count)
            {
                var read = stream.Read(buffer, offset + bytesRead, count - bytesRead);
                if (read == 0)
                    throw new TruncatedStreamException();
                bytesRead += read;
            }
        }

        /// <summary>
        ///     Asynchronously reads bytes from the stream and advances the position within the stream until the
        ///     <paramref name="buffer" /> is filled, and monitors cancellation requests.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">
        ///     A region of memory. When this method returns, the region contains the bytes read from the stream.
        /// </param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <exception cref="TruncatedStreamException">The end of the stream is reached before the bytes are fully read.</exception>
        /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
        public static async Task ReadExactlyAsync(this Stream stream, Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var bytesRead = 0;
            while (bytesRead < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer[bytesRead..], cancellationToken);
                if (read == 0)
                    throw new TruncatedStreamException();
                bytesRead += read;
            }
        }

        /// <summary>
        ///     Asynchronously reads <paramref name="count" /> number bytes from the stream and advances the position
        ///     within the stream, and monitors cancellation requests.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">
        ///     An array of bytes. When this method returns, the buffer contains the specified byte array with the
        ///     values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced
        ///     by the bytes read from the stream.
        /// </param>
        /// <param name="offset">
        ///     The byte offset in <paramref name="buffer" /> at which to begin storing the data read from the
        ///     stream.
        /// </param>
        /// <param name="count">The number of bytes to be read from the stream.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <exception cref="TruncatedStreamException">The end of the stream is reached before the bytes are fully read.</exception>
        /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
        public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count,
            CancellationToken cancellationToken = default)
        {
            var bytesRead = 0;
            while (bytesRead < count)
            {
                var read = await stream.ReadAsync(buffer, offset + bytesRead, count - bytesRead, cancellationToken);
                if (read == 0)
                    throw new TruncatedStreamException();
                bytesRead += read;
            }
        }

        /// <summary>
        ///     Reads a VarInt32 from the stream.
        /// </summary>
        /// <param name="input">The stream to read from.</param>
        /// <returns>The 32-bit unsigned integer decoded from the stream.</returns>
        /// <exception cref="TruncatedStreamException">The end of the stream is reached before the VarInt32 is fully read.</exception>
        /// <exception cref="MalformedStreamException.MalformedVarInt">
        ///     The VarInt32 is malformed, for example if the continuation bit is set after reading 32 bits of data or if the total
        ///     number of data bits exceeds 32.
        /// </exception>
        /// <remarks>
        ///     A VarInt32 can have at most 36 bits set:
        ///     - 7*4 bits in the LSBs + 4 bits in the MSB for encoding the 32 bits of actual data
        ///     - 1*4 continuation bits in the LSBs (the continuation bit in the MSB should not be set)
        /// </remarks>
        public static uint ReadRawVarInt32(this Stream input)
        {
            uint value = 0;

            const byte continuationBitMask = 0b10000000;
            const byte dataBitsMask = 0b01111111;

            // At most, we can fit extract 4*7 data bits from 
            for (var byteCount = 0; byteCount < 5; ++byteCount)
            {
                int b;

                try
                {
                    b = input.ReadByte();
                }
                catch (EndOfStreamException)
                {
                    throw new TruncatedStreamException();
                }

                // We reached the end of the stream.
                if (b == -1)
                    throw new TruncatedStreamException();

                var dataBits = b & dataBitsMask;

                if (byteCount == 4 && dataBits > 0xF)
                    throw new MalformedStreamException.MalformedVarInt(
                        "Expected at most 4 bits of data in the most significant byte but more than 4 bits are set.");

                value |= (uint)dataBits << (7 * byteCount);

                // Check the continuation bit to see if we need to read more bytes.
                var continuationBitSet = (b & continuationBitMask) != 0;

                // If the continuation bit is not set, we're done.
                if (!continuationBitSet)
                    return value;
            }

            throw new MalformedStreamException.MalformedVarInt(
                "Continuation bit is set but 32 bits of data have already been read and the 32-bit integer cannot receive more data bits.");
        }

        /// <summary>
        ///     Reads a VarInt64 from the stream.
        /// </summary>
        /// <param name="input">The stream to read from.</param>
        /// <returns>The 64-bit unsigned integer decoded from the stream.</returns>
        /// <exception cref="TruncatedStreamException">The end of the stream is reached before the VarInt64 is fully read.</exception>
        /// <exception cref="MalformedStreamException.MalformedVarInt">
        ///     The VarInt64 is malformed, for example if the continuation bit is set after reading 64 bits of data or if the total
        ///     number of data bits exceeds 64.
        /// </exception>
        /// <remarks>
        ///     A VarInt64 can have at most 73 bits set:
        ///     - 7*9 bits in the LSBs + 1 bit in the MSB for encoding the 64 bits of actual data
        ///     - 1*9 continuation bits in the LSBs (the continuation bit in the MSB should not be set)
        /// </remarks>
        public static ulong ReadRawVarInt64(this Stream input)
        {
            ulong value = 0;

            const byte continuationBitMask = 0b10000000;
            const byte dataBitsMask = 0b01111111;

            for (var byteCount = 0; byteCount < 10; ++byteCount)
            {
                int b;

                try
                {
                    b = input.ReadByte();
                }
                catch (EndOfStreamException)
                {
                    throw new TruncatedStreamException();
                }

                // We reached the end of the stream.
                if (b == -1)
                    throw new TruncatedStreamException();

                var dataBits = (ulong)(b & dataBitsMask);

                if (byteCount == 9 && dataBits > 0x1)
                    throw new MalformedStreamException.MalformedVarInt(
                        "Expected at most 1 bit of data in the most significant byte but more than 1 bit is set.");

                value |= dataBits << (7 * byteCount);

                // Check the continuation bit to see if we need to read more bytes.
                var continuationBitSet = (b & continuationBitMask) != 0;

                // If the continuation bit is not set, we're done.
                if (!continuationBitSet)
                    return value;
            }

            throw new MalformedStreamException.MalformedVarInt(
                "Continuation bit is set but 64 bits of data have already been read and the 64-bit integer cannot receive more data bits.");
        }

        public static int ReadInt32(this Stream input)
        {
            Span<byte> buffer = stackalloc byte[4];
            input.ReadExactly(buffer);
            return BitConverter.ToInt32(buffer);
        }

        public static long ReadInt64(this Stream input)
        {
            Span<byte> buffer = stackalloc byte[8];
            input.ReadExactly(buffer);
            return BitConverter.ToInt64(buffer);
        }

        public static uint ReadUInt32(this Stream input)
        {
            Span<byte> buffer = stackalloc byte[4];
            input.ReadExactly(buffer);
            return BitConverter.ToUInt32(buffer);
        }

        public static ulong ReadUInt64(this Stream input)
        {
            Span<byte> buffer = stackalloc byte[8];
            input.ReadExactly(buffer);
            return BitConverter.ToUInt64(buffer);
        }
    }
}