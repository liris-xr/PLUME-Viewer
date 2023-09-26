using System;
using System.IO;
using System.IO.Compression;
using Google.Protobuf;
using JetBrains.Annotations;
using PLUME.Sample;
using UnityEngine;

namespace PLUME
{
    public class RecordReader : IDisposable
    {
        private readonly string _recordPath;
        private readonly Stream _samplesStream;

        private bool _closed;

        private readonly RecordMetadata _metadata;
        
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
                return null;
            }
            
            using var metadataStream = File.Open(recordMetadataPath, FileMode.Open, FileAccess.Read, FileShare.Read);

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

        public bool TryReadNextSample(out PackedSample sample)
        {
            try
            {
                sample = PackedSample.Parser.ParseDelimitedFrom(_samplesStream);
                return true;
            }
            catch (Exception)
            {
                sample = null;
                return false;
            }
        }
        
        public PackedSample ReadNextSample()
        {
            return PackedSample.Parser.ParseDelimitedFrom(_samplesStream);
        }

        public void Close()
        {
            if (_closed)
                return;
            _closed = true;
            
            _samplesStream.Close();
        }

        public void Dispose()
        {
            Close();
        }
    }
}