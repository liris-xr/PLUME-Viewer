using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using NUnit.Framework;
using PLUME.Sample;
using Runtime;
using UnityEngine.TestTools;
using Vector3 = PLUME.Sample.Common.Vector3;

namespace Tests
{
    public class SampleLoaderTests
    {
        private Sample _sample1, _sample2;

        private SampleLoader _sampleLoader;
        private Stream _stream;
        private TypeRegistry _typeRegistry;

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

            _stream = new MemoryStream();
            Span<byte> signature = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(signature, (uint)SampleStreamSignature.Uncompressed);

            _stream.Write(signature);
            packedSample1.WriteDelimitedTo(_stream);
            packedSample2.WriteDelimitedTo(_stream);
        }

        [SetUp]
        public void SetUp()
        {
            _stream.Seek(0, SeekOrigin.Begin);
            var sampleStream = SampleStream.Create(_stream, true);
            var sampleReader = new SampleReader(sampleStream, true);
            var sampleParser = new SampleParser(_typeRegistry);
            _sampleLoader = new SampleLoader(sampleReader, sampleParser);
        }

        [TearDown]
        public void TearDown()
        {
            _sampleLoader.Dispose();
        }

        [Test]
        public void LoadSamplesAtTime()
        {
            var samples = _sampleLoader.LoadSamplesAtTime(1);
            Assert.AreEqual(1, samples.Count);
            Assert.AreEqual(_sample1, samples[0]);
        }

        [Test]
        public void LoadSamplesInTimestampRange()
        {
            var samples = _sampleLoader.LoadSamplesInTimeRange(1, 2);
            Assert.AreEqual(2, samples.Count);
            Assert.AreEqual(_sample1, samples[0]);
            Assert.AreEqual(_sample2, samples[1]);
        }

        [UnityTest]
        public IEnumerator LoadSamplesInTimestampRangeAsync()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var samples = new List<Sample>();

                await foreach (var sample in _sampleLoader.LoadSamplesInTimeRangeAsync(1, 2)) samples.Add(sample);

                Assert.AreEqual(2, samples.Count);
                Assert.AreEqual(new List<Sample> { _sample1, _sample2 }, samples);
            });
        }
    }
}