using System;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using NUnit.Framework;
using PLUME.Sample;
using Runtime;
using Vector3 = PLUME.Sample.Common.Vector3;

namespace Tests
{
    [TestFixture]
    public class SampleParserTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            _typeRegistry = TypeRegistry.FromMessages(Vector3.Descriptor);

            _sample1 = new Sample(1, new Vector3 { X = 1, Y = 2, Z = 3 });
            _sample2 = new Sample(2, new Vector3 { X = 4, Y = 5, Z = 6 });

            var packedSample1 = new PackedSample
            {
                Timestamp = _sample1.Timestamp!.Value,
                Payload = Any.Pack(_sample1.Payload)
            };

            var packedSample2 = new PackedSample
            {
                Timestamp = _sample2.Timestamp!.Value,
                Payload = Any.Pack(_sample2.Payload)
            };

            _packedSample1Bytes = packedSample1.ToByteArray();
            _packedSample2Bytes = packedSample2.ToByteArray();
        }

        [SetUp]
        public void SetUp()
        {
            _sampleParser = new SampleParser(_typeRegistry);
        }

        private SampleParser _sampleParser;
        private Sample _sample1, _sample2;
        private byte[] _packedSample1Bytes;
        private byte[] _packedSample2Bytes;
        private TypeRegistry _typeRegistry;

        [Test]
        public void Parse_SingleSample_ReturnsExpectedResult()
        {
            var result = _sampleParser.Parse(_packedSample1Bytes);
            Assert.AreEqual(_sample1, result);
        }

        [Test]
        public void Parse_MultipleSamples_ReturnsExpectedResult()
        {
            var buffer = new SampleBytesBuffer();
            buffer.AddSampleBytes(_packedSample1Bytes);
            buffer.AddSampleBytes(_packedSample2Bytes);

            var results = _sampleParser.Parse(buffer);
            CollectionAssert.AreEquivalent(new[] { _sample1, _sample2 }, results);
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