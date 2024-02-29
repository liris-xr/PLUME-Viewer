using System;
using System.IO;
using K4os.Compression.LZ4.Streams;
using PLUME.Sample;

namespace PLUME
{
    public class RecordReader : IDisposable
    {
        private readonly string _recordPath;
        private readonly Stream _samplesStream;

        private bool _closed;

        private readonly RecordMetadata _metadata;

        private const uint LZ4MagicNumber = 0x184D2204;

        public RecordReader(string recordPath)
        {
            _recordPath = recordPath;

            var fileStream = File.Open(recordPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Read magic number
            var magicNumber = new byte[4];
            _ = fileStream.Read(magicNumber, 0, 4);
            fileStream.Seek(0, SeekOrigin.Begin);
            var compressed = BitConverter.ToUInt32(magicNumber, 0) == LZ4MagicNumber;

            if (compressed)
                _samplesStream = LZ4Stream.Decode(fileStream);
            else
                _samplesStream = fileStream;
        }

        public bool TryReadMetaFile(out RecordMetadata metadata, out RecordMetrics metrics)
        {
            var recordMetadataPath = _recordPath + ".meta";

            if (!File.Exists(recordMetadataPath))
            {
                metadata = null;
                metrics = null;
                return false;
            }

            using var metaStream = File.Open(recordMetadataPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            try
            {
                metadata = RecordMetadata.Parser.ParseDelimitedFrom(metaStream);
                metrics = RecordMetrics.Parser.ParseDelimitedFrom(metaStream);
                metaStream.Close();
                return true;
            }
            catch (Exception)
            {
                metaStream.Close();
                metadata = null;
                metrics = null;
                return false;
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
            _samplesStream.Dispose();
        }
    }
}