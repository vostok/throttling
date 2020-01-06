using System;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Throttling.Config;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests
{
    [TestFixture]
    internal class ThrottlingStateActualizer_Tests
    {
        private ThrottlingConfigurationBuilder builder;
        private ThrottlingEssentials essentials;
        private ThrottlingState state;

        [SetUp]
        public void TestSetup()
        {
            essentials = new ThrottlingEssentials
            {
                Enabled = true,
                CapacityLimitPerCore = 10,
                QueueLimit = 123,
                RefreshPeriod = 1.Seconds()
            };

            builder = new ThrottlingConfigurationBuilder();
            builder.SetEssentials(() => essentials);

            state = new ThrottlingState();
        }

        [Test]
        public void Should_make_the_state_actual()
        {
            Actualize();

            state.IsActual.Should().BeTrue();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_update_enabled_flag(bool enabled)
        {
            essentials.Enabled = enabled;

            state.Enabled = !enabled;

            Actualize();

            state.Enabled.Should().Be(enabled);
        }

        [Test]
        public void Should_update_queue_limit()
        {
            Actualize();

            state.QueueLimit.Should().Be(essentials.QueueLimit);
        }

        [Test]
        public void Should_protect_from_negative_queue_limit()
        {
            essentials.QueueLimit = -1;

            Actualize();

            state.QueueLimit.Should().Be(0);
        }

        [Test]
        public void Should_update_refresh_period()
        {
            Actualize();

            state.RefreshPeriod.Should().Be(essentials.RefreshPeriod);
        }

        [Test]
        public void Should_update_capacity_per_core_with_custom_cores_count()
        {
            essentials.CapacityLimitPerCore = 12;

            builder.SetNumberOfCores(5);

            Actualize();

            state.CapacityLimit.Should().Be(60);
        }

        [Test]
        public void Should_update_capacity_per_core_with_default_cores_count()
        {
            essentials.CapacityLimitPerCore = 12;

            Actualize();

            state.CapacityLimit.Should().Be(essentials.CapacityLimitPerCore * Environment.ProcessorCount);
        }

        [Test]
        public void Should_update_capacity_from_absolute_value()
        {
            essentials.CapacityLimitPerCore = null;
            essentials.CapacityLimit = 122;

            Actualize();

            state.CapacityLimit.Should().Be(essentials.CapacityLimit);
        }


        [Test]
        public void Should_give_priority_to_absolute_value_of_capacity()
        {
            essentials.CapacityLimitPerCore = 1;
            essentials.CapacityLimit = 122;

            Actualize();

            state.CapacityLimit.Should().Be(essentials.CapacityLimit);
        }

        [Test]
        public void Should_set_infinite_capacity_by_default()
        {
            essentials.CapacityLimitPerCore = null;
            essentials.CapacityLimit = null;

            Actualize();

            state.CapacityLimit.Should().Be(int.MaxValue);
        }

        [Test]
        public void Should_protect_from_zero_capacity()
        {
            essentials.CapacityLimitPerCore = null;
            essentials.CapacityLimit = 0;

            Actualize();

            state.CapacityLimit.Should().Be(1);
        }

        [Test]
        public void Should_protect_from_negative_capacity()
        {
            essentials.CapacityLimitPerCore = null;
            essentials.CapacityLimit = -1;

            Actualize();

            state.CapacityLimit.Should().Be(1);
        }

        [Test]
        public void Should_protect_from_zero_capacity_per_core()
        {
            essentials.CapacityLimitPerCore = 0;

            Actualize();

            state.CapacityLimit.Should().Be(1);
        }

        [Test]
        public void Should_protect_from_negative_capacity_per_core()
        {
            essentials.CapacityLimitPerCore = -1;

            Actualize();

            state.CapacityLimit.Should().Be(1);
        }

        [Test]
        public void Should_produce_empty_quotas_if_none_are_configured()
        {
            Actualize();

            state.Quotas.Should().BeEmpty();
        }

        [Test]
        public void Should_include_custom_quotas()
        {
            var quota1 = Substitute.For<IThrottlingQuota>();
            var quota2 = Substitute.For<IThrottlingQuota>();

            builder.AddCustomQuota(quota1);
            builder.AddCustomQuota(quota2);

            Actualize();

            state.Quotas.Should().Equal(quota1, quota2);
        }

        [Test]
        public void Should_include_property_quotas()
        {
            var quota1 = Substitute.For<IThrottlingQuota>();
            var quota2 = Substitute.For<IThrottlingQuota>();

            builder.AddCustomQuota(quota1);
            builder.AddCustomQuota(quota2);
            builder.SetConsumerQuota(new PropertyQuotaOptions());
            builder.SetPriorityQuota(new PropertyQuotaOptions());

            Actualize();

            state.Quotas.Should().HaveCount(4);
            state.Quotas[0].Should().BeSameAs(quota1);
            state.Quotas[1].Should().BeSameAs(quota2);
            state.Quotas[2].Should().BeOfType<PropertyQuota>().Which.Property.Should().Be(WellKnownThrottlingProperties.Consumer);
            state.Quotas[3].Should().BeOfType<PropertyQuota>().Which.Property.Should().Be(WellKnownThrottlingProperties.Priority);
        }

        [Test]
        public void Should_remove_property_counters_with_zero_values()
        {
            state.ObtainCounter("k1", "a");
            state.ObtainCounter("k1", "b").Increment();
            state.ObtainCounter("k1", "c");
            state.ObtainCounter("k1", "d").Increment();
            state.ObtainCounter("k1", "e").Add(3);
            state.ObtainCounter("k2", "x").Add(4);
            state.ObtainCounter("k3", "y");

            Actualize();

            state.Counters["k1"].Keys.Should().BeEquivalentTo("b", "d", "e");
            state.Counters["k2"].Keys.Should().BeEquivalentTo("x");
            state.Counters["k3"].Keys.Should().BeEmpty();
        }

        [Test]
        public void Should_adjust_semaphore_when_increasing_capacity()
        {
            state.CapacityLimit = 5;
            state.Semaphore.Release(2);

            essentials.CapacityLimitPerCore = null;
            essentials.CapacityLimit = 15;

            Actualize();

            state.Semaphore.CurrentCount.Should().Be(12);
        }

        [Test]
        public void Should_adjust_semaphore_when_decreasing_capacity()
        {
            state.CapacityLimit = 15;
            state.Semaphore.Release(12);

            essentials.CapacityLimitPerCore = null;
            essentials.CapacityLimit = 5;

            Actualize();

            state.Semaphore.CurrentCount.Should().Be(2);
        }

        [Test]
        public void Should_be_able_to_drain_contended_semaphore()
        {
            state.CapacityLimit = 15;

            essentials.CapacityLimitPerCore = null;
            essentials.CapacityLimit = 5;

            Actualize();

            for (var i = 0; i < 12; i++)
            {
                state.Semaphore.Release();
                Thread.Sleep(1);
            }

            for (var i = 0; i < 20; i++)
            {
                if (state.Semaphore.CurrentCount == 2)
                    return;

                Thread.Sleep(50);
            }

            Assert.Fail();
        }

        [Test]
        [MaxTime(5000)]
        public void Should_not_block_while_draining_contended_semaphore()
        {
            state.CapacityLimit = 15;

            essentials.CapacityLimitPerCore = null;
            essentials.CapacityLimit = 5;

            Actualize();
        }

        private void Actualize()
            => new ThrottlingStateActualizer(builder.Build()).Actualize(state);
    }
}
