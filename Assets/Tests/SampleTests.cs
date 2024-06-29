using NUnit.Framework;
using PLUME.Sample.Common;
using Runtime;

namespace Tests
{
    public class SampleTests
    {
        [Test]
        public void Equals_ReturnsTrue()
        {
            var sample1 = new Sample(1, new Vector3 { X = 1, Y = 2, Z = 3 });
            var sample2 = new Sample(1, new Vector3 { X = 1, Y = 2, Z = 3 });
            Assert.AreEqual(sample1, sample2);
        }

        [Test]
        public void Equals_DifferentTimestamps_ReturnsFalse()
        {
            var sample1 = new Sample(1, new Vector3 { X = 1, Y = 2, Z = 3 });
            var sample2 = new Sample(2, new Vector3 { X = 1, Y = 2, Z = 3 });
            Assert.AreNotEqual(sample1, sample2);
        }

        [Test]
        public void Equals_OneNullTimestamp_ReturnsFalse()
        {
            var sample1 = new Sample(1, new Vector3 { X = 1, Y = 2, Z = 3 });
            var sample2 = new Sample(null, new Vector3 { X = 1, Y = 2, Z = 3 });
            Assert.AreNotEqual(sample1, sample2);
        }

        [Test]
        public void Equals_DifferentPayloads_ReturnsFalse()
        {
            var sample1 = new Sample(1, new Vector3 { X = 1, Y = 2, Z = 3 });
            var sample2 = new Sample(1, new Vector3 { X = 4, Y = 5, Z = 6 });
            Assert.AreNotEqual(sample1, sample2);
        }

        [Test]
        public void GetHashCode_ReturnsExpectedResult()
        {
            var sample1 = new Sample(1, new Vector3 { X = 1, Y = 2, Z = 3 });
            var sample2 = new Sample(1, new Vector3 { X = 1, Y = 2, Z = 3 });
            Assert.AreEqual(sample1.GetHashCode(), sample2.GetHashCode());
        }

        [Test]
        public void GetHashCode_DifferentTimestamps_ReturnsFalse()
        {
            var sample1 = new Sample(1, new Vector3 { X = 1, Y = 2, Z = 3 });
            var sample2 = new Sample(2, new Vector3 { X = 1, Y = 2, Z = 3 });
            Assert.AreNotEqual(sample1.GetHashCode(), sample2.GetHashCode());
        }

        [Test]
        public void GetHashCode_OneNullTimestamp_ReturnsFalse()
        {
            var sample1 = new Sample(1, new Vector3 { X = 1, Y = 2, Z = 3 });
            var sample2 = new Sample(null, new Vector3 { X = 1, Y = 2, Z = 3 });
            Assert.AreNotEqual(sample1.GetHashCode(), sample2.GetHashCode());
        }

        [Test]
        public void GetHashCode_DifferentPayloads_ReturnsFalse()
        {
            var sample1 = new Sample(1, new Vector3 { X = 1, Y = 2, Z = 3 });
            var sample2 = new Sample(1, new Vector3 { X = 4, Y = 5, Z = 6 });
            Assert.AreNotEqual(sample1.GetHashCode(), sample2.GetHashCode());
        }
    }
}