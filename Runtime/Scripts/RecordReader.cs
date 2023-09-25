using System;
using System.IO;
using System.IO.Compression;
using Google.Protobuf;
using PLUME.Sample;
using UnityEngine;

namespace PLUME
{
    public class RecordReader : IDisposable
    {
        private readonly string _recordPath;
        private readonly Stream _samplesStream;

        private bool _closed;

        public RecordReader(string recordPath)
        {
            _recordPath = recordPath;
            _samplesStream = new GZipStream(File.Open(recordPath, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);
        }

        public RecordMetadata ReadMetadata()
        {
            var recordMetadataPath = _recordPath + ".meta";

            if (!File.Exists(recordMetadataPath))
            {
                Debug.LogWarning($"Metadata file not found '{recordMetadataPath}'");
                return null;
            }
                
            using var metadataStream = new GZipStream(File.Open(recordMetadataPath, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);

            try
            {
                return RecordMetadata.Parser.ParseDelimitedFrom(metadataStream);
            }
            catch (EndOfStreamException)
            {
                return null;
            }
            catch (InvalidProtocolBufferException)
            {
                return null;
            }
        }

        public PackedSample ReadNextSample()
        {
            try
            {
                return PackedSample.Parser.ParseDelimitedFrom(_samplesStream);
            }
            catch (EndOfStreamException)
            {
                return null;
            }
            catch (InvalidProtocolBufferException)
            {
                return null;
            }
        }

        public void Close()
        {
            if (_closed)
                return;

            _samplesStream.Close();

            _closed = true;
        }

        public void Dispose()
        {
            Close();
        }
    }
}