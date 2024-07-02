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

            _packedSample1 = new PackedSample
            {
                Timestamp = _sample1.Timestamp!.Value,
                Payload = Any.Pack(_sample1.Payload)
            };

            _packedSample2 = new PackedSample
            {
                Timestamp = _sample2.Timestamp!.Value,
                Payload = Any.Pack(_sample2.Payload)
            };

            _packedSample1Bytes = _packedSample1.ToByteArray();
            _packedSample2Bytes = _packedSample2.ToByteArray();
        }

        [SetUp]
        public void SetUp()
        {
            _sampleParser = new SampleParser(_typeRegistry);
        }

        private SampleParser _sampleParser;
        private PackedSample _packedSample1, _packedSample2;
        private Sample _sample1, _sample2;
        private byte[] _packedSample1Bytes;
        private byte[] _packedSample2Bytes;
        private TypeRegistry _typeRegistry;

        [Test]
        [Order(1)]
        public void Parse_ReturnsExpectedResult()
        {
            var result = _sampleParser.Parse(_packedSample1Bytes);
            Assert.AreEqual(_packedSample1, result);
        }

        [Test]
        [Order(2)]
        public void Unpack_ReturnsExpectedResult()
        {
            var result = _sampleParser.Unpack(_packedSample1);
            Assert.AreEqual(_sample1, result);
        }
    }
}