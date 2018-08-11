using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests.Quotas
{
    [TestFixture]
    internal class MaximumFractionForGivenPropertyGivenValuesQuota_Tests
    {
        private MaximumFractionForGivenPropertyGivenValuesQuota quota;
        private string key;

        [SetUp]
        public void TestSetup()
        {
            key = "k1";
            quota = new MaximumFractionForGivenPropertyGivenValuesQuota(
                key,
                new Dictionary<string, double>
                {
                    {"foo", 0.5},
                    {"bar", 0.2}
                });
        }

        [Test]
        public void Should_allow_unknown_value()
        {
            Allows(new Dictionary<string, ConsumedByValue>() {{"k1", new ConsumedByValue("baz", 10)}}, 10).Should().BeTrue();
        }

        [Test]
        public void Should_allow_unknown_property()
        {
            Allows(new Dictionary<string, ConsumedByValue>() {{"another_prop", new ConsumedByValue("foo", 10)}}, 10).Should().BeTrue();
        }

        [Test]
        public void Should_allow_empty_properties()
        {
            Allows(new Dictionary<string, ConsumedByValue>(), 10).Should().BeTrue();
        }

        [Test]
        public void Should_allow_if_its_individual_quota_allows_it()
        {
            Allows(new Dictionary<string, ConsumedByValue>() {{"k1", new ConsumedByValue("foo", 4)}}, 10).Should().BeTrue();
            Allows(new Dictionary<string, ConsumedByValue>() {{"k1", new ConsumedByValue("bar", 2)}}, 10).Should().BeTrue();
        }

        [Test]
        public void Should_disallow_if_its_individual_quota_disallows_it()
        {
            Allows(new Dictionary<string, ConsumedByValue>() {{"k1", new ConsumedByValue("foo", 6)}}, 10).Should().BeFalse();
            Allows(new Dictionary<string, ConsumedByValue>() {{"k1", new ConsumedByValue("bar", 3)}}, 10).Should().BeFalse();
        }

        private bool Allows(Dictionary<string, ConsumedByValue> consumed, int totalCapacity)
        {
            return quota.Allows(consumed, totalCapacity);
        }
    }
}