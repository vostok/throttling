using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests.Quotas
{
    [TestFixture]
    internal class MaximumFractionForGivenConsumersQuota_Tests
    {
        private MaximumFractionForGivenConsumersQuota quota;

        [SetUp]
        public void TestSetup()
        {
            quota = new MaximumFractionForGivenConsumersQuota(
                new Dictionary<string, double>
                {
                    {"foo", 0.5},
                    {"bar", 0.2}
                });
        }

        [Test]
        public void Should_allow_unknown_consumers()
        {
            quota.Allows("baz", 10, 10).Should().BeTrue();
        }

        [Test]
        public void Should_allow_a_consumer_if_its_individual_quota_allows_it()
        {
            quota.Allows("foo", 4, 10).Should().BeTrue();
            quota.Allows("bar", 2, 10).Should().BeTrue();
        }

        [Test]
        public void Should_disallow_a_consumer_if_its_individual_quota_disallows_it()
        {
            quota.Allows("foo", 6, 10).Should().BeFalse();
            quota.Allows("bar", 3, 10).Should().BeFalse();
        }
    }
}