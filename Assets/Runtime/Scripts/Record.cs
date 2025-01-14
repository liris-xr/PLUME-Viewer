using System;
using System.IO;
using PLUME.Sample;
using PLUME.Sample.Common;
using PLUME.Sample.LSL;
using PLUME.Sample.Unity.Settings;
using PLUME.Sample.Unity;

namespace PLUME
{
    public class Record
    {
        public readonly RecordMetadata metadata;
        public readonly GraphicsSettings graphicsSettings;

        /// Duration in nanoseconds
        public ulong Duration { get; private set; }

        public readonly IReadOnlySamplesSortedList<FrameSample> Frames;
        public readonly IReadOnlySamplesSortedList<RawSample<Marker>> Markers;
        public readonly IReadOnlySamplesSortedList<RawSample<InputAction>> InputActions;
        public readonly IReadOnlySamplesSortedList<RawSample<StreamSample>> LslStreamSamples;
        public readonly IReadOnlySamplesSortedList<RawSample<StreamOpen>> LslStreamOpenSamples;
        public readonly IReadOnlySamplesSortedList<RawSample<StreamClose>> LslStreamCloseSamples;
        public readonly IReadOnlySamplesSortedList<RawSample> OtherSamples;

        private readonly SamplesSortedList<FrameSample> _frames;
        private readonly SamplesSortedList<RawSample<Marker>> _markers;
        private readonly SamplesSortedList<RawSample<InputAction>> _inputActions;

        // LSL samples
        private readonly SamplesSortedList<RawSample<StreamSample>> _lslStreamSamples;
        private readonly SamplesSortedList<RawSample<StreamOpen>> _lslStreamOpenSamples;
        private readonly SamplesSortedList<RawSample<StreamClose>> _lslStreamCloseSamples;

        private readonly SamplesSortedList<RawSample> _otherSamples;

        internal Record(RecordMetadata metadata, GraphicsSettings graphicsSettings)
        {
            this.metadata = metadata;
            this.graphicsSettings = graphicsSettings;

            _frames = new SamplesSortedList<FrameSample>();
            _markers = new SamplesSortedList<RawSample<Marker>>();
            _inputActions = new SamplesSortedList<RawSample<InputAction>>();
            _lslStreamSamples = new SamplesSortedList<RawSample<StreamSample>>();
            _lslStreamOpenSamples = new SamplesSortedList<RawSample<StreamOpen>>();
            _lslStreamCloseSamples = new SamplesSortedList<RawSample<StreamClose>>();
            _otherSamples = new SamplesSortedList<RawSample>();

            Frames = _frames.AsReadOnly();
            Markers = _markers.AsReadOnly();
            InputActions = _inputActions.AsReadOnly();
            LslStreamSamples = _lslStreamSamples.AsReadOnly();
            LslStreamOpenSamples = _lslStreamOpenSamples.AsReadOnly();
            LslStreamCloseSamples = _lslStreamCloseSamples.AsReadOnly();
            OtherSamples = _otherSamples.AsReadOnly();
        }

        internal void AddFrame(FrameSample frame)
        {
            _frames.Add(frame);
            Duration = Math.Max(Duration, frame.Timestamp);
        }

        internal void AddMarkerSample(RawSample<Marker> marker)
        {
            _markers.Add(marker);
            Duration = Math.Max(Duration, marker.Timestamp);
        }

        internal void AddInputActionSample(RawSample<InputAction> inputAction)
        {
            _inputActions.Add(inputAction);
            Duration = Math.Max(Duration, inputAction.Timestamp);
        }

        internal void AddStreamSample(RawSample<StreamSample> streamRawSample)
        {
            _lslStreamSamples.Add(streamRawSample);
            Duration = Math.Max(Duration, streamRawSample.Timestamp);
        }

        internal void AddStreamOpenSample(RawSample<StreamOpen> streamOpen)
        {
            _lslStreamOpenSamples.Add(streamOpen);
            Duration = Math.Max(Duration, streamOpen.Timestamp);
        }

        internal void AddStreamCloseSample(RawSample<StreamClose> streamClose)
        {
            _lslStreamCloseSamples.Add(streamClose);
            Duration = Math.Max(Duration, streamClose.Timestamp);
        }

        internal void AddOtherSample(RawSample sample)
        {
            _otherSamples.Add(sample);
            Duration = Math.Max(Duration, sample.Timestamp);
        }
        
        public string ToSafeString()
        {
            var recordName = metadata.Name;
            var recordStartTime = metadata.StartTime.ToDateTime();
            var formattedStartTime = recordStartTime.ToString("yyyy-MM-dd_HH-mm-ss");
            var recordSafeName = string.Join("_", recordName.Split(Path.GetInvalidFileNameChars())) + "_" + formattedStartTime;
            return recordSafeName;
        }
    }
}