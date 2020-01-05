using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Throttling.Config;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests.Config
{
    [TestFixture]
    internal class ThrottlingConfigurationBuilder_Tests
    {
        private ThrottlingConfigurationBuilder builder;

        [SetUp]
        public void TestSetup()
            => builder = new ThrottlingConfigurationBuilder();

        [Test]
        public void Should_return_a_valid_configuration_without_any_setup()
        {
            var config = builder.Build();

            config.Essentials().Enabled.Should().BeTrue();
            config.PropertyQuotas.Should().BeEmpty();
            config.CustomQuotas.Should().BeEmpty();
            config.NumberOfCoresProvider.Should().BeNull();
        }

        [Test]
        public void Should_allow_to_set_essentials()
        {
            builder.SetEssentials(new ThrottlingEssentials {QueueLimit = 100500});

            builder.Build().Essentials().QueueLimit.Should().Be(100500);
        }

        [Test]
        public void Should_allow_to_add_custom_quotas()
        {
            var quota1 = Substitute.For<IThrottlingQuota>();
            var quota2 = Substitute.For<IThrottlingQuota>();

            builder.AddCustomQuota(quota1);
            builder.AddCustomQuota(quota2);

            builder.Build().CustomQuotas.Should().Equal(quota1, quota2);
        }

        [Test]
        public void Should_use_case_insensitive_keys_for_property_quotas()
        {
            var options1 = new PropertyQuotaOptions();
            var options2 = new PropertyQuotaOptions();
            var options3 = new PropertyQuotaOptions();

            builder.SetPropertyQuota("prop1", options1);
            builder.SetPropertyQuota("prop2", options2);
            builder.SetPropertyQuota("PROP2", options3);

            var config = builder.Build();

            config.PropertyQuotas.Should().HaveCount(2);
            config.PropertyQuotas["prop1"]().Should().BeSameAs(options1);
            config.PropertyQuotas["prop2"]().Should().BeSameAs(options3);
        }
    }
}
