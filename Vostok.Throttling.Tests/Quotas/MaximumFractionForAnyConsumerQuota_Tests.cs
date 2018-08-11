using System;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests.Quotas
{
    [TestFixture]
    internal class MaximumFractionForAnyConsumerQuota_Tests
    {
        private MaximumFractionForAnyConsumerQuota quota;

        [SetUp]
        public void TestSetup()
        {
            quota = new MaximumFractionForAnyConsumerQuota(0.5);
        }

        [Test]
        public void Ctor_should_fail_when_given_negative_fraction()
        {
            Action action = () => quota = new MaximumFractionForAnyConsumerQuota(-0.01);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Ctor_should_fail_when_given_larger_than_one_fraction()
        {
            Action action = () => quota = new MaximumFractionForAnyConsumerQuota(1.01);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Allow_should_return_true_when_maximum_fraction_is_not_exceeded()
        {
            quota.Allows("consumer", 10, 100).Should().BeTrue();
        }

        [Test]
        public void Allow_should_return_false_when_maximum_fraction_is_exceeded()
        {
            quota.Allows("consumer", 51, 100).Should().BeFalse();
        }

        [Test]
        public void Allow_should_return_true_when_maximum_fraction_is_reached_exactly()
        {
            quota.Allows("consumer", 50, 100).Should().BeTrue();
        }
    }
}