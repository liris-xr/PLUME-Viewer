using System.Collections.Generic;

namespace PLUME.Sample
{
    public class FrameSample : ISample
    {
        public readonly List<RawSample> Data;

        public readonly int FrameNumber;

        public FrameSample(ulong timestamp, int frameNumber, List<RawSample> data)
        {
            Timestamp = timestamp;
            FrameNumber = frameNumber;
            Data = data;
        }

        public ulong Timestamp { get; }
        public bool HasTimestamp => true;
    }
}