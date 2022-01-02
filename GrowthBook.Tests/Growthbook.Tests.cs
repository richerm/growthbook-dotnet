using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrowthBook.Tests
{
    public class GrowthbookTests
    {
        private Experiment exp;
        private Context ctx;

        [SetUp]
        public void SetUp()
        {
            exp = new Experiment()
            {
                Key = "my-test",
                Variations = new object[] { 0, 1 }
            };

            ctx = new Context()
            {
                Enabled = true,
                User = new Dictionary<string, object>()
                {
                    { "id" , "1" }
                }
            };
        }


        [Test]
        public void SimpleExperiment()
        {
            var result = GrowthBook.Run(ctx, exp);
            Assert.IsTrue(result.InExperiment);
        }


        [Test]
        [TestCase(1, 1)]
        [TestCase(2, 0)]
        [TestCase(3, 0)]
        [TestCase(4, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 1)]
        [TestCase(7, 0)]
        [TestCase(8, 1)]
        [TestCase(9, 0)]
        public void TestDefaultWeights(int userId, int expectedVariation)
        {
            ctx.User["id"] = userId.ToString();

            var result = GrowthBook.Run(ctx, exp);

            Assert.AreEqual(expectedVariation, result?.Value, $"Failed to compare weights for user id <{userId}>.  Expected: {expectedVariation}.  Received: {result?.Value}");
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 0)]
        [TestCase(4, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 1)]
        [TestCase(7, 0)]
        [TestCase(8, 1)]
        [TestCase(9, 1)]
        public void TestUnevenWeights(int userId, int expectedVariation)
        {
            exp.Weights = new double[] { 0.1f, 0.9f };
            ctx.User["id"] = userId.ToString();

            var result = GrowthBook.Run(ctx, exp);

            Assert.AreEqual(expectedVariation, result?.Value, $"Failed to compare uneven weights for user id <{userId}>.  Expected: {expectedVariation}.  Received: {result?.Value}");
        }

        [Test]
        [TestCase(1, -1)]
        [TestCase(2, 0)]
        [TestCase(3, 0)]
        [TestCase(4, -1)]
        [TestCase(5, 1)]
        [TestCase(6, -1)]
        [TestCase(7, 0)]
        [TestCase(8, 1)]
        [TestCase(9, -1)]
        public void TestCoverage(int userId, int expectedVariation)
        {
            exp.Coverage = 0.4;
            ctx.User["id"] = userId.ToString();

            var result = GrowthBook.Run(ctx, exp);

            var actualVariation = (result.InExperiment) ? result.VariationIndex : -1;
            Assert.AreEqual(expectedVariation, actualVariation, $"Failed to test coverage for user id <{userId}>.  Expected: {expectedVariation}.  Received: {actualVariation}");
        }

        [Test]
        [TestCase(1, 2)]
        [TestCase(2, 0)]
        [TestCase(3, 0)]
        [TestCase(4, 2)]
        [TestCase(5, 1)]
        [TestCase(6, 2)]
        [TestCase(7, 0)]
        [TestCase(8, 1)]
        [TestCase(9, 0)]
        public void ThreeWayTest(int userId, int expectedValue)
        {
            exp.Variations = new object[] { 0, 1, 2 };
            ctx.User["id"] = userId.ToString();

            var result = GrowthBook.Run(ctx, exp);

            var actualValue = result.Value;
            Assert.AreEqual(expectedValue, actualValue, $"Failed for three way test for user id <{userId}>.  Expected: {expectedValue}.  Received: {actualValue}");
        }

        [Test]
        [TestCase("my-test", 1)]
        [TestCase("my-test-3", 0)]
        public void TestName(string name, int expectedValue)
        {
            exp.Key = name;

            var result = GrowthBook.Run(ctx, exp);

            var actualValue = result.Value;
            Assert.AreEqual(expectedValue, actualValue, $"Failed for three way test for name <{name}>.  Expected: {expectedValue}.  Received: {actualValue}");
        }

        [Test]
        public void MissingId()
        {
            ctx.User["id"] = string.Empty;

            var result = GrowthBook.Run(ctx, exp);

            Assert.IsFalse(result.InExperiment);
        }

        [Test]
        public void InsufficientVariations()
        {
            exp.Variations = new object[] { 0 };

            var result = GrowthBook.Run(ctx, exp);

            Assert.IsFalse(result.InExperiment);

        }

        [Test]
        public void DisabledContext()
        {
            ctx.Enabled = false;

            var result = GrowthBook.Run(ctx, exp);

            Assert.IsFalse(result.InExperiment);
        }

        [Test]
        public void NullExperiment()
        {
            Assert.Throws<ArgumentNullException>(() => GrowthBook.Run(ctx, null));
        }

        [Test]
        public void ExperimentInUrl()
        {
            ctx.Url = "http://localhost/?my-test=1";

            var result = GrowthBook.Run(ctx, exp);

            Assert.IsFalse(result.InExperiment);
            Assert.AreEqual(result.VariationIndex, 1);
        }

        [Test]
        public void ForcedVariationContext()
        {
            ctx.ForcedVariations = new Dictionary<string, int>();
            ctx.ForcedVariations.Add("my-test", 1);

            var result = GrowthBook.Run(ctx, exp);

            Assert.IsFalse(result.InExperiment);
            Assert.AreEqual(1, result.VariationIndex);
        }

        [Test]
        [TestCase("draft")]
        [TestCase("stopped")]
        public void StatusChecks(string status)
        {
            exp.Status = status;

            var result = GrowthBook.Run(ctx, exp);

            Assert.IsFalse(result.InExperiment);
        }

        [Test]
        public void QaMode()
        {
            ctx.QaMode = true;

            var result = GrowthBook.Run(ctx, exp);

            Assert.IsFalse(result.InExperiment);
        }

        [Test]
        public void IncludeActionReturnsFalse()
        {
            exp.Include = new Func<bool>(() => { return false; });

            var result = GrowthBook.Run(ctx, exp);
            Assert.IsFalse(result.InExperiment);
            Assert.AreEqual(0, result.VariationIndex);
        }

        [Test]
        public void IncludeActionThrows()
        {
            exp.Include = new Func<bool>(() => { throw new Exception(); });

            var result = GrowthBook.Run(ctx, exp);
            Assert.IsFalse(result.InExperiment);
            Assert.AreEqual(0, result.VariationIndex);
        }

        [Test]
        public void NotInGroups()
        {
            exp.Groups = new string[] { "A", "B", "C" };
            ctx.Groups = new Dictionary<string, bool>();
            ctx.Groups.Add("A", false);
            ctx.Groups.Add("B", false);

            var result = GrowthBook.Run(ctx, exp);
            Assert.IsFalse(result.InExperiment);
            Assert.AreEqual(0, result.VariationIndex);
        }

        [Test]
        public void IsInGroups()
        {
            exp.Groups = new string[] { "A", "B", "C" };
            ctx.Groups = new Dictionary<string, bool>();
            ctx.Groups.Add("A", true);
            ctx.Groups.Add("B", false);

            var result = GrowthBook.Run(ctx, exp);
            Assert.True(result.InExperiment);
        }

        [Test]
        [TestCase(null, true)]
        [TestCase("", true)]
        [TestCase("a/b", true)]
        [TestCase("a/c", false)]
        [TestCase(@"c\d+", true)]
        [TestCase(@"a\d+", false)]
        public void UrlRegexTests(string expUrlRegex, bool expectedInExperiment)
        {
            ctx.Url = "http://localhost/a/b/c123";
            exp.Url = expUrlRegex;

            var result = GrowthBook.Run(ctx, exp);
            Assert.AreEqual(expectedInExperiment, result.InExperiment);
        }

        [Test]
        public void ForcedVariationExperiment()
        {
            exp.Force = 0;

            var result = GrowthBook.Run(ctx, exp);
            Assert.IsFalse(result.InExperiment);
            Assert.AreEqual(0, result.VariationIndex);
        }

        [Test]
        public void ForcedVariationExperimentEvenInStoppedState()
        {
            exp.Status = "stopped";
            exp.Force = 0;

            var result = GrowthBook.Run(ctx, exp);
            Assert.IsFalse(result.InExperiment);
            Assert.AreEqual(0, result.VariationIndex);
        }

        [Test]
        public void CallbackFires()
        {
            Experiment receivedExp = null;
            ExperimentResult receivedResult = null;
            ctx.TrackingCallback = (exp, result) => { receivedExp = exp; receivedResult = result; return Task.CompletedTask;  };

            GrowthBook.Run(ctx, exp);

            Assert.AreEqual(exp, receivedExp);
            Assert.NotNull(receivedResult);
            Assert.IsTrue(receivedResult.InExperiment);
            Assert.AreEqual(1, receivedResult.VariationIndex);
        }

        [Test]
        public void CallbackFailuresAreHandledGracefully()
        {
            ctx.TrackingCallback = (exp, result) => { throw new Exception();  };

            var result = GrowthBook.Run(ctx, exp);

            Assert.IsTrue(result.InExperiment);
            Assert.AreEqual(1, result.VariationIndex);
        }

        [Test]
        public void OverridesAreMergedIn()
        {
            ctx.Overrides.Add(new Experiment
            {
                Key = "my-test",
                Coverage = 0
            });

            var result = GrowthBook.Run(ctx, exp);

            Assert.IsFalse(result.InExperiment);
        }

        [Test]
        public void VariationDataIsIncludedInResult()
        {
            var variationA = new { name = "Test A", value = 0 };
            var variationB = new { name = "Test B", value = 1 };

            exp.Variations = new object[] { variationA, variationB };

            var result = GrowthBook.Run(ctx, exp);

            Assert.IsTrue(result.InExperiment);
            Assert.AreEqual(1, result.VariationIndex);
            Assert.AreEqual(result.Value, variationB);
        }

        [Test]
        public void VariationDataIsFallbackWhenNotInExperiment()
        {
            var variationA = new { name = "Test A", value = 0 };
            var variationB = new { name = "Test B", value = 1 };

            exp.Variations = new object[] { variationA, variationB };
            exp.Status = "draft";

            var result = GrowthBook.Run(ctx, exp);

            Assert.IsFalse(result.InExperiment);
            Assert.AreEqual(0, result.VariationIndex);
            Assert.AreEqual(result.Value, variationA);
        }
    }
}