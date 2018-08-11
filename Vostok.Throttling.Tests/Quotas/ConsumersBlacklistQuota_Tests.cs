using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests.Quotas
{
    [TestFixture]
    internal class ConsumersBlacklistQuota_Tests
    {
        private ConsumersBlacklistQuota quota;

        [SetUp]
        public void TestSetup()
        {
            quota = new ConsumersBlacklistQuota(new HashSet<string> { "foo", "bar" });
        }

        [Test]
        public void Should_disallow_blacklisted_consumers()
        {
            quota.Allows("foo", 1, 10).Should().BeFalse();
            quota.Allows("bar", 1, 10).Should().BeFalse();
        }

        [Test]
        public void Should_allow_unknown_consumers()
        {
            quota.Allows("baz", 10, 10).Should().BeTrue();
        }
    }
}