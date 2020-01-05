using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests.Quotas
{
    [TestFixture]
    internal class PropertyQuota_Tests
    {
        private string property;
        private string propertyValue;
        private List<string> blacklist;
        private List<string> whitelist;
        private double? globalLimit;
        private Dictionary<string, double> individualLimits;
        private Dictionary<string, string> requestProperties;
        private int totalCapacity;
        private int consumption;
        private IThrottlingQuotaContext context;

        [SetUp]
        public void TestSetup()
        {
            property = Guid.NewGuid().ToString().Substring(0, 8);
            propertyValue = "value" + Guid.NewGuid().ToString().Substring(0, 8);
            blacklist = new List<string>();
            whitelist = new List<string>();
            globalLimit = 0.75;
            individualLimits = new Dictionary<string, double>();
            requestProperties = new Dictionary<string, string>
            {
                [property] = propertyValue
            };
            totalCapacity = 100;
            consumption = 10;

            context = Substitute.For<IThrottlingQuotaContext>();
            context.CapacityLimit.Returns(_ => totalCapacity);
            context.GetConsumption(property, requestProperties[property]).Returns(_ => consumption);
        }

        [Test]
        public void Should_allow_when_request_properties_do_not_contain_quoted_property()
        {
            consumption = totalCapacity;
            blacklist.Add(propertyValue);
            requestProperties.Clear();

            ShouldAllow();
        }

        [Test]
        public void Should_allow_when_no_limits_are_configured()
        {
            consumption = totalCapacity;
            globalLimit = null;

            ShouldAllow();
        }

        [Test]
        public void Should_reject_blacklisted_values()
        {
            blacklist.Add(propertyValue);

            ShouldReject();
        }

        [Test]
        public void Blacklist_should_be_case_insensitive()
        {
            blacklist.Add(propertyValue.ToUpper());

            ShouldReject();
        }

        [Test]
        public void Blacklist_should_take_precedence_over_whitelist()
        {
            blacklist.Add(propertyValue);
            whitelist.Add(propertyValue);

            ShouldReject();
        }

        [Test]
        public void Should_allow_whitelisted_values()
        {
            consumption = totalCapacity;

            whitelist.Add(propertyValue);

            ShouldAllow();
        }

        [Test]
        public void Whitelist_should_be_case_insensitive()
        {
            consumption = totalCapacity;

            whitelist.Add(propertyValue.ToUpper());

            ShouldAllow();
        }

        [Test]
        public void Should_allow_by_global_limit()
        {
            consumption = 74;

            ShouldAllow();
        }

        [Test]
        public void Should_reject_by_global_limit()
        {
            consumption = 75;

            ShouldReject();
        }

        [Test]
        public void Should_allow_by_individual_limit()
        {
            consumption = 90;

            individualLimits[propertyValue] = 1;

            ShouldAllow();
        }

        [Test]
        public void Should_reject_by_individual_limit()
        {
            consumption = 10;

            individualLimits[propertyValue] = 0.05;

            ShouldReject();
        }

        [Test]
        public void Individual_limits_should_be_case_insensitive()
        {
            consumption = 10;

            individualLimits[propertyValue.ToUpper()] = 0.05;

            ShouldReject();
        }

        private void ShouldAllow()
            => CreateQuota().Check(requestProperties, context).Allowed.Should().BeTrue();

        private void ShouldReject()
        {
            var verdict = CreateQuota().Check(requestProperties, context);

            verdict.Allowed.Should().BeFalse();

            Console.Out.WriteLine(verdict.RejectionReason);
        }

        private PropertyQuota CreateQuota()
            => new PropertyQuota(property, new PropertyQuotaOptions
            {
                Blacklist = blacklist,
                Whitelist = whitelist,
                GlobalLimit = globalLimit,
                IndividualLimits = individualLimits
            });
    }
}
