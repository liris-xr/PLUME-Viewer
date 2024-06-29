using System;
using Google.Protobuf;

namespace Runtime
{
    public class Sample
    {
        public readonly IMessage Payload;
        public readonly ulong? Timestamp;

        public Sample(ulong? timestamp, IMessage payload)
        {
            Timestamp = timestamp;
            Payload = payload;
        }

        public Sample(ulong timestamp, IMessage payload)
        {
            Timestamp = timestamp;
            Payload = payload;
        }

        public Sample(IMessage payload)
        {
            Timestamp = null;
            Payload = payload;
        }

        public override bool Equals(object obj)
        {
            return obj is Sample other && Equals(other);
        }

        public bool Equals(Sample other)
        {
            return Payload.Equals(other.Payload) && Nullable.Equals(Timestamp, other.Timestamp);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Payload, Timestamp);
        }

        public override string ToString()
        {
            var timestampStr = Timestamp.HasValue ? Timestamp.ToString() : "None";
            return $"Sample(Timestamp: {timestampStr}ns, Payload: {Payload})";
        }
    }
}