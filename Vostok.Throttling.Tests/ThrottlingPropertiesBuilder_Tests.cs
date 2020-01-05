using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Throttling.Tests
{
    [TestFixture]
    internal class ThrottlingPropertiesBuilder_Tests
    {
        private ThrottlingPropertiesBuilder builder;

        [SetUp]
        public void TestSetup()
            => builder = new ThrottlingPropertiesBuilder();

        [Test]
        public void Should_correctly_add_all_well_known_properties()
        {
            var properties = builder
                .AddConsumer("nginx")
                .AddPriority("ordinary")
                .AddMethod("GET")
                .AddUrl("/users")
                .Build();

            properties.Should().HaveCount(4);
            properties[WellKnownThrottlingProperties.Consumer].Should().Be("nginx");
            properties[WellKnownThrottlingProperties.Priority].Should().Be("ordinary");
            properties[WellKnownThrottlingProperties.Method].Should().Be("GET");
            properties[WellKnownThrottlingProperties.Url].Should().Be("/users");
        }

        [Test]
        public void Should_produce_case_insensitive_properties()
        {
            builder.AddMethod("GET");

            builder.Build()[WellKnownThrottlingProperties.Method.ToUpper()].Should().Be("GET");
        }
    }
}
