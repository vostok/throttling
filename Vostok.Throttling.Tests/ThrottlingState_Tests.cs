using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Throttling.Tests
{
    [TestFixture]
    internal class ThrottlingState_Tests
    {
        private ThrottlingState state;

        [SetUp]
        public void TestSetup()
            => state = new ThrottlingState();

        [Test]
        public void Should_initially_have_a_semaphore_with_zero_count()
            => state.Semaphore.CurrentCount.Should().Be(0);

        [Test]
        public void Should_initially_have_empty_property_counters_dictionary()
            => state.Counters.Should().BeEmpty();

        [Test]
        public void Should_initially_have_empty_quotas_list()
            => state.Quotas.Should().BeEmpty();

        [Test]
        public void Should_initially_be_inactual()
            => state.IsActual.Should().BeFalse();

        [Test]
        public void Should_initially_have_zero_capacity()
            => state.CapacityLimit.Should().Be(0);

        [Test]
        public void Should_add_new_property_counters_if_requested()
        {
            var counter = state.ObtainCounter("k1", "v1");

            counter.Value.Should().Be(0);
            counter.Should().BeSameAs(state.Counters["k1"]["v1"]);
        }

        [Test]
        public void GetConsumption_should_return_zero_when_there_is_no_such_property()
            => state.GetConsumption("k1", "v1").Should().Be(0);

        [Test]
        public void GetConsumption_should_return_zero_when_there_is_no_such_value()
        {
            state.ObtainCounter("k1", "v2").Increment();

            state.GetConsumption("k1", "v1").Should().Be(0);
        }

        [Test]
        public void GetConsumption_should_return_value_of_stored_counter()
        {
            state.ObtainCounter("k1", "v1").Increment();
            state.ObtainCounter("k1", "v1").Increment();

            state.GetConsumption("k1", "v1").Should().Be(2);
        }

        [Test]
        public void GetConsumption_should_be_case_insensitive()
        {
            state.ObtainCounter("k1", "v1").Increment();
            state.ObtainCounter("k1", "v1").Increment();

            state.GetConsumption("K1", "V1").Should().Be(2);
        }
    }
}
