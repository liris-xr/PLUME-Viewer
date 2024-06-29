using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NUnit.Framework;
using PLUME.Sample;
using PLUME.Sample.Common;
using Runtime;

namespace Tests
{
    [TestFixture]
    public class TestSampleParser
    {
        [OneTimeSetUp]
        public void Init()
        {
            _packedSample1 = new PackedSample
            {
                Timestamp = 1,
                Payload = Any.Pack(new Vector3 { X = 1, Y = 2, Z = 3 })
            };
            _packedSample2 = new PackedSample
            {
                Timestamp = 2,
                Payload = Any.Pack(new Vector3 { X = 4, Y = 5, Z = 6 })
            };

            _packedSample1Bytes = _packedSample1.ToByteArray();
            _packedSample2Bytes = _packedSample2.ToByteArray();
        }

        [SetUp]
        public void SetUp()
        {
            _sampleParser = new SampleParser();
        }

        private SampleParser _sampleParser;
        private PackedSample _packedSample1;
        private PackedSample _packedSample2;
        private byte[] _packedSample1Bytes;
        private byte[] _packedSample2Bytes;

        [Test]
        public void Parse_SingleSample_ReturnsExpectedResult()
        {
            var result = _sampleParser.Parse(_packedSample1Bytes);
            Assert.AreEqual(_packedSample1, result);
        }

        [Test]
        public void Parse_MultipleSamples_ReturnsExpectedResult()
        {
            var buffer = new SampleBytesBuffer();
            buffer.AddSampleBytes(_packedSample1Bytes);
            buffer.AddSampleBytes(_packedSample2Bytes);

            var results = _sampleParser.Parse(buffer);
            CollectionAssert.AreEquivalent(new[] { _packedSample1, _packedSample2 }, results);
        }

        [Test]
        public void Parse_InvalidNWorkers_ThrowsArgumentOutOfRangeException()
        {
            var buffer = new SampleBytesBuffer();
            buffer.AddSampleBytes(_packedSample1Bytes);

            // nWorkers can't be less than or equal to 0.
            Assert.Throws<ArgumentOutOfRangeException>(() => _sampleParser.Parse(buffer, 0));
        }
    }
}