using System.Collections.Generic;

namespace PLUME.Sample
{
    public class FrameSample : ISample
    {
        public ulong Timestamp { get; }
        public bool HasTimestamp => true;

        public readonly int FrameNumber;
        public readonly List<RawSample> Data;

        public FrameSample(ulong timestamp, int frameNumber, List<RawSample> data)
        {
            Timestamp = timestamp;
            FrameNumber = frameNumber;
            Data = data;
        }
    }
}