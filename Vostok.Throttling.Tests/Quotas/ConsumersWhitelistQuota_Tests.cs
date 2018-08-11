using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests.Quotas
{
    [TestFixture]
    internal class ConsumersWhitelistQuota_Tests
    {
        private ConsumersWhitelistQuota quota;

        [SetUp]
        public void TestSetup()
        {
            quota = new ConsumersWhitelistQuota(new HashSet<string> {"foo", "bar"});
        }

        [Test]
        public void Should_allow_whitelisted_consumers()
        {
            quota.Allows("foo", 10, 10).Should().BeTrue();
            quota.Allows("bar", 10, 10).Should().BeTrue();
        }


        [Test]
        public void Should_disallow_unknown_consumers()
        {
            quota.Allows("baz", 1, 10).Should().BeFalse();
        }
    }
}