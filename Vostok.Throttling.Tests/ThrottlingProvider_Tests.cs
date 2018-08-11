using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Commons.Threading;
using Vostok.Throttling.Results;

namespace Vostok.Throttling.Tests
{
    [TestFixture]
    internal class ThrottlingProvider_Tests
    {
        private const int Capacity = 5;
        private const int QueueLimit = 10;

        private ThrottlingState state;
        private ThrottlingProvider provider;

        private IThrottlingStateProvider stateProvider;
        private IThrottlingQuotasChecker quotasChecker;
        private readonly ThrottlingProperties emptyProps = new ThrottlingProperties();

        [SetUp]
        public void TestSetup()
        {
            state = new ThrottlingState
            {
                Enabled = true,
                CapacityLimit = Capacity,
                QueueLimit = QueueLimit,
            };

            state.Semaphore.Release(Capacity);

            stateProvider = Substitute.For<IThrottlingStateProvider>();
            stateProvider.ObtainState().Returns(state);

            quotasChecker = Substitute.For<IThrottlingQuotasChecker>();
            quotasChecker.Check(null, emptyProps, ThrottlingPriority.Ordinary).ReturnsForAnyArgs(ThrottlingStatus.Passed);

            provider = new ThrottlingProvider(stateProvider, quotasChecker);
        }

        [Test]
        public void Metrics_property_should_return_empty_metrics_when_throttling_is_disabled()
        {
            state.Enabled = false;

            var metrics = provider.Metrics;

            metrics.CapacityLimit.Should().Be(0);
            metrics.QueueLimit.Should().Be(0);
            metrics.QueueSize.Should().Be(0);
            metrics.RemainingCapacity.Should().Be(0);
            metrics.ConsumedCapacity.Should().Be(0);
            metrics.ConsumedById.Should().BeEmpty();
        }

        [Test]
        public void Metrics_property_should_return_correct_max_capacity_value()
        {
            provider.Metrics.CapacityLimit.Should().Be(Capacity);
        }

        [Test]
        public void Metrics_property_should_return_correct_remaining_capacity_value()
        {
            state.Semaphore.WaitAsync().GetAwaiter().GetResult();
            state.Semaphore.WaitAsync().GetAwaiter().GetResult();

            provider.Metrics.RemainingCapacity.Should().Be(Capacity - 2);
        }

        [Test]
        public void Metrics_property_should_return_correct_consumed_capacity_value()
        {
            state.Semaphore.WaitAsync().GetAwaiter().GetResult();
            state.Semaphore.WaitAsync().GetAwaiter().GetResult();

            provider.Metrics.ConsumedCapacity.Should().Be(2);
        }

        [Test]
        public void Metrics_property_should_return_correct_queue_limit_value()
        {
            provider.Metrics.QueueLimit.Should().Be(QueueLimit);
        }

        [Test]
        public void Metrics_property_should_return_correct_queue_size_value()
        {
            DrainSemaphoreAsync(Capacity + 2);

            provider.Metrics.QueueSize.Should().Be(2);
        }

        [Test]
        public void Metrics_property_should_return_correct_consumer_counters()
        {
            state.GetPropertyCounter(KnownProperties.ConsumerIdKey)["foo"] = new AtomicInt(5);
            state.GetPropertyCounter(KnownProperties.ConsumerIdKey)["bar"] = new AtomicInt(6);

            provider.Metrics.ConsumedById.Should().HaveCount(2);
            provider.Metrics.ConsumedById.Should().Contain("foo", 5);
            provider.Metrics.ConsumedById.Should().Contain("bar", 6);
        }

        [Test]
        public void Metrics_property_should_return_correct_property_counters()
        {
            state.GetPropertyCounter("k1")["foo"] = new AtomicInt(5);
            state.GetPropertyCounter("k2")["bar"] = new AtomicInt(6);
            state.GetPropertyCounter("k2")["x"] = new AtomicInt(10);

            provider.Metrics.ConsumedByProperties.Keys.Should().BeEquivalentTo("k1", "k2");
            provider.Metrics.ConsumedByProperties["k1"].Should().Contain("foo", 5);
            provider.Metrics.ConsumedByProperties["k1"].Count.Should().Be(1);

            provider.Metrics.ConsumedByProperties["k2"].Should().Contain("bar", 6);
            provider.Metrics.ConsumedByProperties["k2"].Should().Contain("x", 10);
            provider.Metrics.ConsumedByProperties["k2"].Count.Should().Be(2);
        }

        [Test]
        public void Metrics_property_should_not_include_zero_consumer_counters_into_returned_dictionary()
        {
            state.GetPropertyCounter(KnownProperties.ConsumerIdKey)["foo"] = new AtomicInt(5);
            state.GetPropertyCounter(KnownProperties.ConsumerIdKey)["bar"] = new AtomicInt(0);
            state.GetPropertyCounter(KnownProperties.ConsumerIdKey)["baz"] = new AtomicInt(0);

            provider.Metrics.ConsumedById.Should().HaveCount(1);
            provider.Metrics.ConsumedById.Should().Contain("foo", 5);
        }

        [Test]
        public void Metrics_property_should_return_correct_priority_counters()
        {
            state.PriorityCounters[ThrottlingPriority.Ordinary] = new AtomicInt(5);
            state.PriorityCounters[ThrottlingPriority.Sheddable] = new AtomicInt(6);

            provider.Metrics.ConsumedByPriority.Should().Contain(ThrottlingPriority.Critical, 0);
            provider.Metrics.ConsumedByPriority.Should().Contain(ThrottlingPriority.Ordinary, 5);
            provider.Metrics.ConsumedByPriority.Should().Contain(ThrottlingPriority.Sheddable, 6);
        }

        [Test]
        public void ThrottleAsync_should_return_disabled_result_when_throttling_is_disabled()
        {
            state.Enabled = false;

            Throttle().Should().BeOfType<DisabledThrottlingResult>();
        }

        [Test]
        public void ThrottleAsync_should_succeed_when_all_restrictions_are_met()
        {
            state.Enabled = false;

            Throttle().Status.Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void ThrottleAsync_should_reject_without_side_effects_when_queue_limit_is_reached()
        {
            DrainSemaphoreAsync(Capacity + QueueLimit);

            Throttle().Status.Should().Be(ThrottlingStatus.RejectedDueToFullQueue);

            VerifyThereAreNoSideEffects(0, QueueLimit);
        }

        [Test]
        public void ThrottleAsync_should_not_reject_when_queue_limit_is_not_reached_yet()
        {
            DrainSemaphoreAsync(Capacity + QueueLimit - 1);

            var throttlingTask = ThrottleAsync();

            throttlingTask.IsCompleted.Should().BeFalse();
        }

        [Test]
        public void ThrottleAsync_should_reject_without_side_effects_when_queue_limit_is_zero_and_semaphore_is_full()
        {
            state.QueueLimit = 0;

            DrainSemaphore();

            Throttle().Status.Should().Be(ThrottlingStatus.RejectedDueToFullQueue);

            VerifyThereAreNoSideEffects(0);
        }

        [Test]
        public void ThrottleAsync_should_not_reject_when_queue_limit_is_zero_and_semaphore_still_has_spare_capacity()
        {
            state.QueueLimit = 0;

            DrainSemaphore(Capacity - 1);

            Throttle().Status.Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void ThrottleAsync_should_not_reject_without_side_effects_when_queue_size_is_not_zero_and_request_priority_is_sheddable()
        {
            using (var result = Throttle(priority: ThrottlingPriority.Sheddable))
            {
                result.Status.Should().Be(ThrottlingStatus.Passed);
            }

            VerifyThereAreNoSideEffects();
        }

        [Test]
        public void ThrottleAsync_should_not_reject_without_side_effects_when_queue_size_is_not_zero_and_request_priority_is_ordinary()
        {
            DrainSemaphoreAsync(Capacity + 1);

            var throttlingTask = ThrottleAsync();

            throttlingTask.IsCompleted.Should().BeFalse();

            VerifyThereAreNoSideEffects(0, 2);
        }

        [Test]
        public void ThrottleAsync_should_not_reject_without_side_effects_when_queue_size_is_not_zero_and_request_priority_is_critical()
        {
            DrainSemaphoreAsync(Capacity + 1);

            var throttlingTask = ThrottleAsync(priority: ThrottlingPriority.Critical);

            throttlingTask.IsCompleted.Should().BeFalse();

            VerifyThereAreNoSideEffects(0, 2);
        }

        [Test]
        public void ThrottleAsync_should_register_a_consumer_counter_if_provided_with_consumer_id()
        {
            Throttle("foo");

            state.GetPropertyCounter(KnownProperties.ConsumerIdKey).Should().ContainKey("foo");
        }

        [Test]
        public void ThrottleAsync_should_check_quotas_when_there_is_a_consumer_id()
        {
            Throttle("foo", priority: ThrottlingPriority.Critical);

            quotasChecker.Received(1).Check(state, Match(KnownProperties.ConsumerId("foo")), ThrottlingPriority.Critical);
        }

        [Test]
        public void ThrottleAsync_should_check_quotas_when_there_properties()
        {
            var properties = new ThrottlingProperties(new Property("a", "b"));
            provider.ThrottleAsync(properties, priority:ThrottlingPriority.Critical).GetAwaiter().GetResult();

            quotasChecker.Received(1).Check(state, properties, ThrottlingPriority.Critical);
        }

        [Test]
        public void ThrottleAsync_should_check_quotas_when_there_null_properties()
        {

            var properties = emptyProps;
            provider.ThrottleAsync(properties, priority:ThrottlingPriority.Critical).GetAwaiter().GetResult();

            quotasChecker.Received(1).Check(state, properties, ThrottlingPriority.Critical);
        }

        [Test]
        public void ThrottleAsync_should_check_quotas_when_there_empty_properties()
        {
            var properties = new ThrottlingProperties(new Property[0]);
            provider.ThrottleAsync(properties, priority:ThrottlingPriority.Critical).GetAwaiter().GetResult();

            quotasChecker.Received(1).Check(state, properties, ThrottlingPriority.Critical);
        }

        [Test]
        public void ThrottleAsync_should_check_quotas_when_there_is_no_consumer_id()
        {
            Throttle(priority: ThrottlingPriority.Sheddable);

            quotasChecker.Received(1).Check(state, emptyProps, ThrottlingPriority.Sheddable);
        }

        [Test]
        public void ThrottleAsync_should_reject_when_quotas_check_fails()
        {
            quotasChecker.Check(null, emptyProps, ThrottlingPriority.Ordinary).ReturnsForAnyArgs(ThrottlingStatus.RejectedDueToConsumerQuota);

            Throttle("foo").Status.Should().Be(ThrottlingStatus.RejectedDueToConsumerQuota);
        }

        [Test]
        public void ThrottleAsync_should_not_reject_when_quota_allows_a_consumer_to_consume_that_much()
        {
            quotasChecker.Check(null, emptyProps, ThrottlingPriority.Ordinary).ReturnsForAnyArgs(ThrottlingStatus.Passed);

            Throttle("foo").Status.Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void ThrottleAsync_should_increment_consumer_counter_in_case_of_immediate_success()
        {
            Throttle("foo").Status.Should().Be(ThrottlingStatus.Passed);


            state.GetPropertyCounter(KnownProperties.ConsumerIdKey)["foo"].Value.Should().Be(1);
        }

        [TestCase(ThrottlingPriority.Critical)]
        [TestCase(ThrottlingPriority.Ordinary)]
        [TestCase(ThrottlingPriority.Sheddable)]
        public void ThrottleAsync_should_increment_priority_counter_in_case_of_immediate_success(ThrottlingPriority priority)
        {
            Throttle("foo", priority: priority).Status.Should().Be(ThrottlingStatus.Passed);

            state.PriorityCounters[priority].Value.Should().Be(1);
        }

        [Test]
        public void ThrottleAsync_should_eat_capacity_unit_in_case_of_immediate_success()
        {
            Throttle("foo").Status.Should().Be(ThrottlingStatus.Passed);

            state.Semaphore.CurrentCount.Should().Be(Capacity - 1);
        }

        [Test]
        public void ThrottleAsync_should_correctly_reclaim_resources_after_disposing_result_of_immediate_success()
        {
            Throttle("foo").Dispose();

            VerifyThereAreNoSideEffects();
        }

        [Test]
        public void ThrottleAsync_should_wait_for_available_capacity_if_semaphore_is_depleted()
        {
            DrainSemaphore();

            ThrottleAsync().IsCompleted.Should().BeFalse();
        }

        [Test]
        public void ThrottleAsync_should_complete_when_there_is_available_capacity()
        {
            DrainSemaphore();

            var task = ThrottleAsync();

            state.Semaphore.Release();

            task.Wait(1.Seconds()).Should().BeTrue();
        }

        [Test]
        public void ThrottleAsync_should_reject_and_reclaim_resources_if_wait_time_exceeded_given_deadline()
        {
            DrainSemaphore();

            var task = ThrottleAsync("foo", 1.Seconds().Negate());

            state.Semaphore.Release();

            task.Result.Status.Should().Be(ThrottlingStatus.RejectedDueToDeadline);

            VerifyThereAreNoSideEffects(1);
        }

        [Test]
        public void ThrottleAsync_should_not_reject_if_wait_time_is_not_greater_than_deadline()
        {
            DrainSemaphore();

            var task = ThrottleAsync("foo", 1.Hours());

            state.Semaphore.Release();

            task.Result.Status.Should().Be(ThrottlingStatus.Passed);
        }


        [Test]
        public void ThrottleAsync_should_reject_if_quotas_recheck_after_waiting_fails()
        {
            quotasChecker.Check(state, Match(KnownProperties.ConsumerId("foo")), ThrottlingPriority.Ordinary).Returns(ThrottlingStatus.Passed, ThrottlingStatus.RejectedDueToExternalQuota);

            DrainSemaphore();

            var task = ThrottleAsync("foo");

            state.Semaphore.Release();

            task.Result.Status.Should().Be(ThrottlingStatus.RejectedDueToExternalQuota);

            VerifyThereAreNoSideEffects(1);
        }

        [Test]
        public void ThrottleAsync_should_increment_consumer_counter_after_successful_wait()
        {
            DrainSemaphore();

            var task = ThrottleAsync("foo");

            state.Semaphore.Release();

            task.Wait();

            state.GetPropertyCounter(KnownProperties.ConsumerIdKey)["foo"].Value.Should().Be(1);
        }

        [Test]
        public void ThrottleAsync_should_increment_all_property_counters_after_successful_wait()
        {
            DrainSemaphore();
            var properties = new ThrottlingProperties(new Property("k1", "1"), new Property("k2", "2"));

            var task = (Task)provider.ThrottleAsync(properties, priority: ThrottlingPriority.Critical);

            state.Semaphore.Release();

            task.Wait();

            state.GetPropertyCounter("k1")["1"].Value.Should().Be(1);
            state.GetPropertyCounter("k2")["2"].Value.Should().Be(1);
        }

        [Test]
        public void ThrottleAsync_should_reclaim_resources_upon_disposal_of_result_returned_after_successful_wait()
        {
            DrainSemaphore();

            var task = ThrottleAsync("foo");

            state.Semaphore.Release();

            using (task.Result) { }

            VerifyThereAreNoSideEffects(1);
        }

        [Test]
        public void ThrottleAsync_should_provide_LIFO_ordering_for_waiting_requests()
        {
            DrainSemaphore();

            var task1 = ThrottleAsync();
            var task2 = ThrottleAsync();
            var task3 = ThrottleAsync();

            state.Semaphore.Release();

            task3.Wait(1.Seconds()).Should().BeTrue();
            task2.IsCompleted.Should().BeFalse();
            task1.IsCompleted.Should().BeFalse();

            state.Semaphore.Release();

            task2.Wait(1.Seconds()).Should().BeTrue();
            task1.IsCompleted.Should().BeFalse();

            state.Semaphore.Release();

            task1.Wait(1.Seconds()).Should().BeTrue();
        }

        private IThrottlingResult Throttle(string consumerId = null, TimeSpan? deadline = null, ThrottlingPriority priority = ThrottlingPriority.Ordinary)
        {
            return provider.ThrottleAsync(consumerId, deadline, priority).GetAwaiter().GetResult();
        }

        private Task<IThrottlingResult> ThrottleAsync(string consumerId = null, TimeSpan? deadline = null, ThrottlingPriority priority = ThrottlingPriority.Ordinary)
        {
            return provider.ThrottleAsync(consumerId, deadline, priority);
        }

        private void DrainSemaphore(int amount = Capacity)
        {
            for (int i = 0; i < amount; i++)
            {
                state.Semaphore.WaitAsync().GetAwaiter().GetResult();
            }
        }

        private void DrainSemaphoreAsync(int amount = Capacity)
        {
            for (int i = 0; i < amount; i++)
            {
                state.Semaphore.WaitAsync();
            }
        }

        private void VerifyThereAreNoSideEffects(int expectedCapacity = Capacity, int expectedQueueSize = 0)
        {
            state.Semaphore.CurrentCount.Should().Be(expectedCapacity);
            state.Semaphore.CurrentQueue.Should().Be(expectedQueueSize);
            provider.Metrics.ConsumedById.Should().BeEmpty();

            foreach (var counter in state.PropertyCounters)
            {
                foreach (var counterValue in counter.Value)
                {
                    counterValue.Value.Value.Should().Be(0);
                }
            }
            foreach (var counter in state.PriorityCounters.Values)
            {
                counter.Value.Should().Be(0);
            }
        }

        static ThrottlingProperties Match(params Property[] expected)
        {
            return new ThrottlingProperties(expected).MatchEquivalentTo();
        }
    }
}