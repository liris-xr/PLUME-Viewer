using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace PLUME.Sample
{
    public class UnpackedSample : UnpackedSample<IMessage>
    {
        public UnpackedSample(ulong? timestamp, IMessage payload) : base(timestamp, payload)
        {
        }
    }

    public abstract class UnpackedSample<T> where T : IMessage
    {
        public readonly ulong? Timestamp;
        public readonly T Payload;

        public UnpackedSample(ulong? timestamp, T payload)
        {
            Timestamp = timestamp;
            Payload = payload;
        }

        protected bool Equals(UnpackedSample<T> other)
        {
            return Timestamp == other.Timestamp && EqualityComparer<T>.Default.Equals(Payload, other.Payload);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnpackedSample<T>)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Timestamp, Payload);
        }
    }
}