using System;

namespace Runtime
{
    public class TruncatedStreamException : Exception
    {
        public TruncatedStreamException() : base(
            "While reading a stream, the input ended unexpectedly in the middle of a field." +
            "This could mean either that the input has been truncated or that an embedded message misreported its own length.")
        {
        }
    }
}