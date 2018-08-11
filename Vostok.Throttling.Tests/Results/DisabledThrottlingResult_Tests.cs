using System;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Throttling.Results;

namespace Vostok.Throttling.Tests.Results
{
    [TestFixture]
    internal class DisabledThrottlingResult_Tests
    {
        private DisabledThrottlingResult result;

        [SetUp]
        public void TestSetup()
        {
            result = new DisabledThrottlingResult();
        }

        [Test]
        public void Status_property_should_return_passed_value()
        {
            result.Status.Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void WaitTime_property_should_return_zero()
        {
            result.WaitTime.Should().Be(TimeSpan.Zero);
        }
    }
}