using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Commons.Threading;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests
{
    [TestFixture]
    internal class ThrottlingStateActualizer_Tests
    {
        private ThrottlingConfig config;
        private ThrottlingState state;
        private ThrottlingStateActualizer actualizer;

        [SetUp]
        public void TestSetup()
        {
            config = new ThrottlingConfig
            {
                Enabled = () => true,
                QueueLimit = () => 100,
                CapacityLimit = () => 10,
                ExternalQuotas = () => new [] {Substitute.For<IThrottlingExternalQuota>()},
                ConsumerQuotas = () => new [] {Substitute.For<IThrottlingConsumerQuota>()},
                PriorityQuotas = () => new [] {Substitute.For<IThrottlingPriorityQuota>()},
                PropertiesQuotas = () => new [] {Substitute.For<IThrottlingPropertiesQuota>()}
            };

            state = new ThrottlingState();
            actualizer = new ThrottlingStateActualizer(config);
        }

        [Test]
        public void Should_make_the_state_actual()
        {
            actualizer.Actualize(state);

            state.IsActual.Should().BeTrue();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_update_enabled_flag(bool enabled)
        {
            config.Enabled = () => enabled;

            state.Enabled = !enabled;

            actualizer.Actualize(state);

            state.Enabled.Should().Be(enabled);
        }

        [Test]
        public void Should_update_queue_limit()
        {
            actualizer.Actualize(state);

            state.QueueLimit.Should().Be(100);
        }

        [Test]
        public void Should_update_capacity()
        {
            actualizer.Actualize(state);

            state.CapacityLimit.Should().Be(10);
        }

        [Test]
        public void Should_update_external_quotas()
        {
            actualizer.Actualize(state);

            state.ExternalQuotas.Should().NotBeEmpty();
        }

        [Test]
        public void Should_update_consumer_quotas()
        {
            actualizer.Actualize(state);

            state.ConsumerQuotas.Should().NotBeEmpty();
        }

        [Test]
        public void Should_update_priority_quotas()
        {
            actualizer.Actualize(state);

            state.PriorityQuotas.Should().NotBeEmpty();
        }

        [Test]
        public void Should_not_allow_negative_queue_limit_sizes()
        {
            config.QueueLimit = () => -1;

            actualizer.Actualize(state);

            state.QueueLimit.Should().Be(0);
        }

        [Test]
        public void Should_not_allow_negative_capacity_limits()
        {
            config.CapacityLimit = () => -1;

            actualizer.Actualize(state);

            state.CapacityLimit.Should().Be(1);
        }

        [Test]
        public void Should_not_allow_zero_capacity_limits()
        {
            config.CapacityLimit = () => 0;

            actualizer.Actualize(state);

            state.CapacityLimit.Should().Be(1);
        }

        [Test]
        public void Should_remove_property_counters_with_zero_values()
        {
            state.GetPropertyCounter("k1")["a"] = new AtomicInt(0);
            state.GetPropertyCounter("k1")["b"] = new AtomicInt(1);
            state.GetPropertyCounter("k1")["c"] = new AtomicInt(0);
            state.GetPropertyCounter("k1")["d"] = new AtomicInt(2);
            state.GetPropertyCounter("k1")["e"] = new AtomicInt(3);
            state.GetPropertyCounter("k2")["x"] = new AtomicInt(3);
            state.GetPropertyCounter("k3")["y"] = new AtomicInt(0);

            actualizer.Actualize(state);

            state.PropertyCounters["k1"].Keys.Should().BeEquivalentTo("b", "d", "e");
            state.PropertyCounters["k2"].Keys.Should().BeEquivalentTo("x");
            state.PropertyCounters["k3"].Keys.Should().BeEmpty();
        }

        [Test]
        public void Should_adjust_semaphore_when_increasing_capacity()
        {
            state.CapacityLimit = 5;
            state.Semaphore.Release(2);

            config.CapacityLimit = () => 15;

            actualizer.Actualize(state);

            state.Semaphore.CurrentCount.Should().Be(12);
        }

        [Test]
        public void Should_adjust_semaphore_when_decreasing_capacity()
        {
            state.CapacityLimit = 15;
            state.Semaphore.Release(12);

            config.CapacityLimit = () => 5;

            actualizer.Actualize(state);

            state.Semaphore.CurrentCount.Should().Be(2);
        }

        [Test]
        public void Should_be_able_to_drain_contended_semaphore()
        {
            state.CapacityLimit = 15;

            config.CapacityLimit = () => 5;

            actualizer.Actualize(state);

            for (int i = 0; i < 12; i++)
            {
                state.Semaphore.Release();

                System.Threading.Thread.Sleep(1);
            }

            for (int i = 0; i < 20; i++)
            {
                if (state.Semaphore.CurrentCount == 2)
                    return;

                System.Threading.Thread.Sleep(50);
            }

            Assert.Fail();
        }

        [Test]
        public void Should_not_block_while_draining_contended_semaphore()
        {
            Helpers.CompletesInTimeout(() =>
                {
                    state.CapacityLimit = 15;

                    config.CapacityLimit = () => 5;

                    actualizer.Actualize(state);
                }, 5.Seconds());
        }
    }
}