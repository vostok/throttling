using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests.Quotas
{
    [TestFixture]
    internal class MaximumFractionForGivenPropertyAnyValueQuota_Tests
    {
        private const string key = "k1";
        private MaximumFractionForGivenPropertyAnyValueQuota quota;

        [SetUp]
        public void TestSetup()
        {
            quota = new MaximumFractionForGivenPropertyAnyValueQuota(key, 0.5);
        }

        [Test]
        public void Ctor_should_fail_when_given_negative_fraction()
        {
            Action action = () => quota = new MaximumFractionForGivenPropertyAnyValueQuota(key, -0.01);

            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Ctor_should_fail_when_given_null_key()
        {
            Action action = () => quota = new MaximumFractionForGivenPropertyAnyValueQuota(null, 0.5);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Ctor_should_fail_when_given_larger_than_one_fraction()
        {
            Action action = () => quota = new MaximumFractionForGivenPropertyAnyValueQuota(key, 1.01);

            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Allow_should_return_true_when_maximum_fraction_is_not_exceeded()
        {
            Allows(new[] {new Property(key, "any")}, new[] {10}, 100).Should().BeTrue();
        }

        [Test]
        public void Allow_should_return_true_when_empty_pairs()
        {
            Allows(new Property[0], new int[0], 100).Should().BeTrue();
        }

        [Test]
        public void Allow_should_use_correct_counter()
        {
            Allows(
                    new[] {new Property("k_100", "100"), new Property("k_200", "200"), new Property(key, "any")},
                    new[] {80, 99, 49},
                    100)
                .Should()
                .BeTrue();
        }

        [Test]
        public void Allow_should_return_false_when_maximum_fraction_is_exceeded()
        {
            Allows(new[] {new Property(key, "any")}, new[] {51}, 100).Should().BeFalse();
        }

        [Test]
        public void Allow_should_return_true_when_maximum_fraction_is_reached_exactly()
        {
            Allows(new[] {new Property(key, "any")}, new[] {50}, 100).Should().BeTrue();
        }

        private bool Allows(Property[] properties, int[] consumed, int totalCapacity)
        {
            Dictionary<string, ConsumedByValue> consumedDict = new Dictionary<string, ConsumedByValue>();
            for (int i = 0; i < properties.Length; i++)
                consumedDict.Add(properties[i].Key, new ConsumedByValue(properties[i].Value, consumed[i]));
            return quota.Allows(consumedDict, totalCapacity);
        }
    }
}