using System.IO;

namespace Runtime
{
    public static class ProtobufUtils
    {
        public static uint ReadRawVarInt32(Stream input)
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
                    throw TruncatedMessage();

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
        public static ulong ReadRawVarInt64(Stream input)
        {
            ulong value = 0;

            const byte continuationBitMask = 0b10000000;
            const byte dataBitsMask = 0b01111111;

            for (var byteCount = 0; byteCount < 10; ++byteCount)
            {
                var b = input.ReadByte();

                // We reached the end of the stream.
                if (b == -1)
                    throw TruncatedMessage();

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

        private static InvalidDataException TruncatedMessage()
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