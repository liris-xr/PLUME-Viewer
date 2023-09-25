using System;
using System.IO;
using System.IO.Compression;
using Google.Protobuf;
using PLUME.Sample;

namespace PLUME
{
    public class RecordReader : IDisposable
    {
        private readonly Stream _fileStream;
        private readonly Stream _samplesStream;

        private bool _closed;

        private readonly RecordMetadata _metadata;

        public RecordReader(string recordPath)
        {
            _fileStream = File.Open(recordPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var archive = new ZipArchive(_fileStream, ZipArchiveMode.Read);
            _samplesStream = archive.GetEntry("samples")!.Open();

            using var metadataStream = archive.GetEntry("metadata")!.Open();
            _metadata = RecordMetadata.Parser.ParseDelimitedFrom(metadataStream);
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

        public RecordMetadata GetMetadata()
        {
            return _metadata;
        }

        public void Close()
        {
            if (_closed)
                return;

            _fileStream.Close();

            _closed = true;
        }

        public void Dispose()
        {
            Close();
        }
    }
}