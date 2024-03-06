using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using K4os.Compression.LZ4.Streams;
using PLUME.Sample;
using PLUME.Sample.Common;
using PLUME.Sample.LSL;
using PLUME.Sample.Unity;
using PLUME.Sample.Unity.XRITK;
using UnityEngine;
using UnityEngine.Profiling;

namespace PLUME
{
    public class RecordLoader : IDisposable
    {
        private const uint LZ4MagicNumber = 0x184D2204;

        private Stream _baseStream;
        private Stream _stream;

        public float Progress { get; private set; }

        public LoadingStatus Status { get; private set; }

        private readonly TypeRegistry _sampleTypeRegistry;

        public RecordLoader(string recordPath, TypeRegistry sampleTypeRegistry)
        {
            Status = LoadingStatus.NotLoading;
            Progress = 0;

            _sampleTypeRegistry = sampleTypeRegistry;

            _baseStream = File.Open(recordPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (IsLZ4Compressed(_baseStream))
                _stream = LZ4Stream.Decode(_baseStream);
            else
                _stream = _baseStream;
        }

        public async UniTask<Record> LoadAsync()
        {
            Status = LoadingStatus.Loading;
            Progress = 0;

            var packedMetadata = PackedSample.Parser.ParseDelimitedFrom(_stream);
            var metadata = packedMetadata.Payload.Unpack<RecordMetadata>();

            var record = new Record(metadata);

            var loadingThread = new Thread(() =>
            {
                Profiler.BeginThreadProfiling("PLUME", "RecordLoader.LoadAsync");

                while (_baseStream.Position < _baseStream.Length)
                {
                    try
                    {
                        var packedSample = PackedSample.Parser.ParseDelimitedFrom(_stream);
                        ulong? timestamp = packedSample.HasTimestamp ? packedSample.Timestamp : null;
                        var payload = packedSample.Payload;
                        var unpackedSample = RawSampleUtils.UnpackAsRawSample(timestamp, payload, _sampleTypeRegistry);

                        switch (unpackedSample)
                        {
                            case RawSample<Frame> frame:
                                // Unpack frame
                                var frameSample = UnpackFrame(frame);
                                record.AddFrame(frameSample);
                                break;
                            case RawSample<Marker> marker:
                                record.AddMarkerSample(marker);
                                break;
                            case RawSample<InputAction> inputAction:
                                record.AddInputActionSample(inputAction);
                                break;
                            case RawSample<StreamSample> streamSample:
                                record.AddStreamSample(streamSample);
                                break;
                            case RawSample<StreamOpen> streamOpen:
                                record.AddStreamOpenSample(streamOpen);
                                break;
                            case RawSample<StreamClose> streamClose:
                                record.AddStreamCloseSample(streamClose);
                                break;
                            default:
                                record.AddOtherSample(unpackedSample);
                                break;
                        }

                        Progress = _baseStream.Position / (float)_baseStream.Length;
                    }
                    catch (InvalidProtocolBufferException)
                    {
                        break;
                    }
                }

                Profiler.EndThreadProfiling();
            })
            {
                Name = "RecordLoader.LoadAsync"
            };

            loadingThread.Start();

            // Wait until thread finishes loading the record.
            await UniTask.WaitUntil(() => !loadingThread.IsAlive);

            Status = LoadingStatus.Done;
            Progress = 1;

            _stream.Close();
            _stream = null;

            return record;
        }

        private FrameSample UnpackFrame(ISample<Frame> frame)
        {
            var unpackedFrameData = frame.Payload.Data.Select(frameData =>
                RawSampleUtils.UnpackAsRawSample(frame.Timestamp, frameData, _sampleTypeRegistry)).ToList();
            return new FrameSample(frame.Timestamp, frame.Payload.FrameNumber, unpackedFrameData);
        }

        private static bool IsLZ4Compressed(Stream fileStream)
        {
            // Read magic number
            var magicNumber = new byte[4];
            _ = fileStream.Read(magicNumber, 0, 4);
            fileStream.Seek(0, SeekOrigin.Begin);
            var compressed = BitConverter.ToUInt32(magicNumber, 0) == LZ4MagicNumber;
            return compressed;
        }

        public void Dispose()
        {
            _stream?.Dispose();
        }

        public enum LoadingStatus
        {
            NotLoading,
            Loading,
            Done
        }
    }
}