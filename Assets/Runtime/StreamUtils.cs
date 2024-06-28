using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime
{
    public static class StreamUtils
    {
        /// <summary>
        ///     Reads bytes from the stream and advances the position within the stream until the
        ///     <paramref name="buffer" /> is filled.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">
        ///     A region of memory. When this method returns, the region contains the bytes read from the stream.
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
        ///     Asynchronously reads bytes from the stream and advances the position within the stream until the
        ///     <paramref name="buffer" /> is filled, and monitors cancellation requests.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">
        ///     A region of memory. When this method returns, the region contains the bytes read from the stream.
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

        public static uint ReadRawVarInt32(this Stream input)
        {
            uint value = 0;

            const byte continuationBitMask = 0b10000000;
            const byte dataBitsMask = 0b01111111;

            // At most, we can fit extract 4*7 data bits from 
            for (var byteCount = 0; byteCount < 5; ++byteCount)
            {
                var num = input.ReadByte();

                // We reached the end of the stream.
                if (num == -1)
                    throw TruncatedData();

                // If the MSB have more than 4 data bits set, then the total number of data bits exceeds 32. This is invalid.
                if (byteCount == 4 && num > 0xF)
                    throw MalformedVarInt();

                value |= (uint)(num & dataBitsMask) << (7 * byteCount);

                // Check the continuation bit to see if we need to read more bytes.
                var continuationBitSet = (num & continuationBitMask) != 0;

                // If the continuation bit is not set, we're done.
                if (!continuationBitSet)
                    return value;
            }

            // If we reach this point, we have read 32 bits of data but the continuation bit is still set.
            // This is invalid as it will not fit in a 32-bit integer.
            throw MalformedVarInt();
        }

        /// <summary>
        ///     Reads a VarInt64 from the stream.
        /// </summary>
        /// <param name="input">The stream to read from.</param>
        /// <returns>The 64-bit unsigned integer decoded from the stream.</returns>
        /// <remarks>
        ///     A VarInt64 can have at most 73 bits set:
        ///     - (7*9+1) data bits for encoding the 64bits of actual data
        ///     - (1*9) continuation bits
        /// </remarks>
        public static ulong ReadRawVarInt64(this Stream input)
        {
            ulong value = 0;

            const byte continuationBitMask = 0b10000000;
            const byte dataBitsMask = 0b01111111;

            for (var byteCount = 0; byteCount < 10; ++byteCount)
            {
                var b = input.ReadByte();

                // We reached the end of the stream.
                if (b == -1)
                    throw TruncatedData();

                // If the MSB have more than 1 data bit set, then the total number of data bits exceeds 64. This is invalid.
                if (byteCount == 9 && b > 0x1)
                    throw MalformedVarInt();

                var data = (ulong)(b & dataBitsMask);

                value |= data << (7 * byteCount);

                // Check the continuation bit to see if we need to read more bytes.
                var continuationBitSet = (b & continuationBitMask) != 0;

                // If the continuation bit is not set, we're done.
                if (!continuationBitSet)
                    return value;
            }

            // If we reach this point, we have read 64 bits of data but the continuation bit is still set.
            // This is invalid as it will not fit in a 64-bit integer.
            throw MalformedVarInt();
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

        private static InvalidDataException TruncatedData()
        {
            return new InvalidDataException(
                "While parsing a protocol message, the input ended unexpectedly in the middle of a field. This could mean either that the input has been truncated or that an embedded message misreported its own length.");
        }

        private static InvalidDataException MalformedVarInt()
        {
            return new InvalidDataException("Stream encountered a malformed varint.");
        }
    }
}