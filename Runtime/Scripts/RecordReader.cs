using System;
using System.IO;
using System.IO.Compression;
using PLUME.Sample;

namespace PLUME
{
    public class RecordReader : IDisposable
    {
        private readonly Stream _stream;

        private bool _closed;

        public RecordReader(Stream stream)
        {
            _stream = stream;
        }

        public RecordReader(string recordPath, bool isRecordCompressed)
        {
            if (isRecordCompressed)
                _stream = new GZipStream(new FileStream(recordPath, FileMode.Open, FileAccess.Read), CompressionMode.Decompress, false);
            else
                _stream = new FileStream(recordPath, FileMode.Open, FileAccess.Read);
        }

        public RecordHeader ReadHeader()
        {
            try
            {
                return IsEndOfStream() ? null : RecordHeader.Parser.ParseDelimitedFrom(_stream);
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }

        public PackedSample ReadNextSample()
        {
            try
            {
                return IsEndOfStream() ? null : PackedSample.Parser.ParseDelimitedFrom(_stream);
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }

        private bool IsEndOfStream()
        {
            if (_stream is GZipStream gZipStream)
            {
                if (gZipStream.BaseStream.Position == gZipStream.BaseStream.Length)
                {
                    return true;
                }
            }
            else
            {
                if (_stream.Position == _stream.Length)
                {
                    return true;
                }
            }

            return false;
        }

        public void Close()
        {
            if (_closed)
                return;
            
            _stream.Close();

            _closed = true;
        }
        
        public void Dispose()
        {
            Close();
        }
    }
}