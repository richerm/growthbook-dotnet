using NUnit.Framework;

namespace GrowthBook.Tests
{
    public class GrowthBookUtilTests
    {
        [Test]
        [TestCase("a", 220)]
        [TestCase("b", 77)]
        [TestCase("ab", 946)]
        [TestCase("def", 652)]
        [TestCase("8952klfjas09ujkasdf", 549)]
        [TestCase("123", 11)]
        [TestCase("___)((*\":&", 563)]
        public void TestHashing(string valueToHash, int hashValue)
        {
            var result = GrowthBookUtil.HashFnv32a(valueToHash) % 1000;
            Assert.AreEqual(hashValue, result);

        }

        [Test]
        [TestCase(1, new object[] { new double[] { 0.0, 0.5 }, new double[] { 0.5, 1.0 } })]
        [TestCase(0, new object[] { new double[] { 0.0, 0.0 }, new double[] { 0.5, 0.5 } })]
        [TestCase(0.5, new object[] { new double[] { 0.0, 0.25 }, new double[] { 0.5, 0.75 } })]
        public void BucketRanges_DefaultWeight(double coverage, object[] expectedResults)
        {
            var weights = new double[] { 0.5d, 0.5d };

            var results = GrowthBookUtil.GetBucketRanges(weights, coverage);

            Assert.AreEqual(expectedResults.Length, results.Length);
            for (var i=0; i<expectedResults.Length; i++)
            {
                var expected = expectedResults[i] as double[];
                Assert.AreEqual(expected[0], results[i].Start);
                Assert.AreEqual(expected[1], results[i].End);
            }
        }

    }
}
