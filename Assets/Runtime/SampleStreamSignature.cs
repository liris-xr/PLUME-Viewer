namespace Runtime
{
    /// <summary>
    ///     Represents the signature of a sample stream. The signature is written at the beginning of the stream in little
    ///     endian format.
    ///     Little endian:
    ///     - Uncompressed: 00 DB 98 34
    ///     - LZ4 compressed: 01 DB 98 34
    ///     Big endian:
    ///     - Uncompressed: 34 98 DB 00
    ///     - LZ4 compressed: 34 98 DB 01
    /// </summary>
    public enum SampleStreamSignature : uint
    {
        Uncompressed = 0x3498DB00,
        LZ4Compressed = 0x3498DB01
    }
}