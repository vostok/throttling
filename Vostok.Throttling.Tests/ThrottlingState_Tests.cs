using System;
using System.Linq;
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
        {
            state = new ThrottlingState();
        }

        [Test]
        public void Should_initially_have_a_semaphore_with_zero_count()
        {
            state.Semaphore.CurrentCount.Should().Be(0);
        }

        [Test]
        public void Should_initially_have_empty_property_counters_dictionary()
        {
            state.PropertyCounters.Should().BeEmpty();
        }

        [Test]
        public void Should_initially_have_a_priority_counters_dictionary_with_all_the_values()
        {
            foreach (var priority in Enum.GetValues(typeof(ThrottlingPriority)).Cast<ThrottlingPriority>())
            {
                state.PriorityCounters[priority].Value.Should().Be(0);
            }
        }

        [Test]
        public void Should_initially_be_inactual()
        {
            state.IsActual.Should().BeFalse();
        }

        [Test]
        public void Should_add_new_property_counters_if_requested()
        {
            var d = state.GetPropertyCounter("k1");
            state.PropertyCounters.Keys.Should().BeEquivalentTo(new[] {"k1"});
            state.PropertyCounters["k1"].Should().BeEmpty();
            state.PropertyCounters["k1"].Should().BeSameAs(d);

            state.GetPropertyCounter("k1").Should().BeSameAs(d);

            state.GetPropertyCounter("k2");
            state.PropertyCounters.Keys.Should().BeEquivalentTo(new[] {"k1", "k2"});
        }

        [Test]
        public void Should_initially_have_zero_capacity()
        {
            state.CapacityLimit.Should().Be(0);
        }
    }
}