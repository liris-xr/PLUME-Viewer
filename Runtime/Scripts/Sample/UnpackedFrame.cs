using System.Collections.Generic;

namespace PLUME.Sample
{
    public class UnpackedFrame
    {
        public readonly ulong Timestamp;
        public readonly int FrameNumber;
        public readonly List<UnpackedSample> Data;

        public UnpackedFrame(ulong timestamp, int frameNumber, List<UnpackedSample> data)
        {
            Timestamp = timestamp;
            FrameNumber = frameNumber;
            Data = data;
        }

        protected bool Equals(UnpackedFrame other)
        {
            return FrameNumber == other.FrameNumber;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((UnpackedFrame)obj);
        }

        public override int GetHashCode()
        {
            return FrameNumber;
        }
    }

    public class FrameTimestampComparer : IComparer<UnpackedFrame>
    {
        public static FrameTimestampComparer Instance { get; } = new();

        public int Compare(UnpackedFrame x, UnpackedFrame y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            return x.Timestamp.CompareTo(y.Timestamp);
        }
    }
}