using System;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;

namespace PLUME
{
    public interface ISample
    {
        ulong Timestamp { get; }
        bool HasTimestamp { get; }
    }

    public interface ISample<out TP> : ISample
    {
        TP Payload { get; }
    }

    public static class RawSampleUtils
    {
        public static RawSample UnpackAsRawSample(ulong? timestamp, Any payload, TypeRegistry sampleTypeRegistry)
        {
            var unpackedPayload = payload.Unpack(sampleTypeRegistry);

            if (unpackedPayload == null)
            {
                Debug.LogWarning($"Failed to unpack payload of type {payload.TypeUrl}");
                return null;
            }

            var rawSampleType = typeof(RawSample<>).MakeGenericType(unpackedPayload.GetType());
            return (RawSample)Activator.CreateInstance(rawSampleType, timestamp, unpackedPayload);
        }
    }

    public abstract class RawSample : ISample<IMessage>
    {
        public ulong Timestamp { get; }
        public bool HasTimestamp { get; }

        public IMessage Payload { get; }

        protected RawSample(ulong? timestamp, IMessage payload)
        {
            Timestamp = timestamp ?? default;
            HasTimestamp = timestamp.HasValue;
            Payload = payload;
        }
    }

    public class RawSample<TP> : RawSample, ISample<TP> where TP : IMessage
    {
        public new TP Payload => (TP)base.Payload;

        public RawSample(ulong? timestamp, TP payload) : base(timestamp, payload)
        {
        }
    }
}