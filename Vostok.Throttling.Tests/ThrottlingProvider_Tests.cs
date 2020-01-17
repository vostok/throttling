using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Throttling.Quotas;
using Vostok.Throttling.Results;

#pragma warning disable 4014

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
        private IThrottlingQuota quota1;
        private IThrottlingQuota quota2;

        private Dictionary<string, string> properties;
        private List<IThrottlingEvent> events;
        private List<IThrottlingResult> results;
        private Action<Exception> errorCallback;

        [SetUp]
        public void TestSetup()
        {
            state = new ThrottlingState
            {
                Enabled = true,
                CapacityLimit = Capacity,
                QueueLimit = QueueLimit
            };

            state.Semaphore.Release(Capacity);

            stateProvider = Substitute.For<IThrottlingStateProvider>();
            stateProvider.ObtainState().Returns(state);

            quota1 = Substitute.For<IThrottlingQuota>();
            quota1.Check(Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<IThrottlingQuotaContext>()).Returns(ThrottlingQuotaVerdict.Allow());

            quota2 = Substitute.For<IThrottlingQuota>();
            quota2.Check(Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<IThrottlingQuotaContext>()).Returns(ThrottlingQuotaVerdict.Allow());

            state.Quotas = new[] {quota1, quota2};

            errorCallback = Substitute.For<Action<Exception>>();
            provider = new ThrottlingProvider(stateProvider, errorCallback);

            properties = new Dictionary<string, string> {["foo"] = "bar"};
            events = new List<IThrottlingEvent>();
            results = new List<IThrottlingResult>();

            provider.Subscribe(new TestObserver<IThrottlingEvent>(evt => events.Add(evt)));
            provider.Subscribe(new TestObserver<IThrottlingResult>(res => results.Add(res)));
        }

        [Test]
        public void ThrottleAsync_should_return_disabled_result_when_throttling_is_disabled()
        {
            state.Enabled = false;

            Throttle().Should().BeOfType<DisabledResult>().Which.Status.Should().Be(ThrottlingStatus.Passed);

            events.Should().BeEmpty();
        }

        [Test]
        public void ThrottleAsync_should_succeed_when_all_restrictions_are_met()
        {
            Throttle().Status.Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void ThrottleAsync_should_reject_without_side_effects_when_queue_limit_is_reached()
        {
            DrainSemaphoreAsync(Capacity + QueueLimit);

            Throttle().Status.Should().Be(ThrottlingStatus.RejectedDueToQueue);

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

            Throttle().Status.Should().Be(ThrottlingStatus.RejectedDueToQueue);

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
        public void ThrottleAsync_should_not_reject_without_side_effects_when_queue_size_is_not_zero()
        {
            DrainSemaphoreAsync(Capacity + 1);

            var throttlingTask = ThrottleAsync();

            throttlingTask.IsCompleted.Should().BeFalse();

            VerifyThereAreNoSideEffects(0, 2);
        }

        [Test]
        public void ThrottleAsync_should_register_a_consumption_counter_if_provided_with_a_property()
        {
            Throttle();

            state.ObtainCounter("foo", "bar").Value.Should().Be(1);
        }

        [Test]
        public void ThrottleAsync_should_check_quotas_before_waiting()
        {
            Throttle();

            quota1.Received(1).Check(properties, state);
            quota2.Received(1).Check(properties, state);
        }

        [Test]
        public void ThrottleAsync_should_reject_when_quotas_check_fails()
        {
            quota2.Check(properties, state).Returns(ThrottlingQuotaVerdict.Reject("for reasons"));

            Throttle().Status.Should().Be(ThrottlingStatus.RejectedDueToQuota);
        }

        [Test]
        public void ThrottleAsync_should_eat_capacity_unit_in_case_of_immediate_success()
        {
            Throttle().Status.Should().Be(ThrottlingStatus.Passed);

            state.Semaphore.CurrentCount.Should().Be(Capacity - 1);
        }

        [Test]
        public void ThrottleAsync_should_correctly_reclaim_resources_after_disposing_result_of_immediate_success()
        {
            Throttle().Dispose();

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

            var task = ThrottleAsync(1.Seconds().Negate());

            state.Semaphore.Release();

            task.Result.Status.Should().Be(ThrottlingStatus.RejectedDueToDeadline);

            VerifyThereAreNoSideEffects(1);
        }

        [Test]
        public void ThrottleAsync_should_not_reject_if_wait_time_is_not_greater_than_deadline()
        {
            DrainSemaphore();

            var task = ThrottleAsync(1.Hours());

            state.Semaphore.Release();

            task.Result.Status.Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void ThrottleAsync_should_reject_if_quotas_recheck_after_waiting_fails()
        {
            quota2.Check(properties, state).Returns(ThrottlingQuotaVerdict.Allow(), ThrottlingQuotaVerdict.Reject("for reasons"));

            DrainSemaphore();

            var task = ThrottleAsync();

            state.Semaphore.Release();

            task.Result.Status.Should().Be(ThrottlingStatus.RejectedDueToQuota);

            VerifyThereAreNoSideEffects(1);

            quota2.Received(2).Check(properties, state);
        }

        [Test]
        public void ThrottleAsync_should_not_increment_consumption_counter_before_successful_wait()
        {
            DrainSemaphore();

            ThrottleAsync();

            state.ObtainCounter("foo", "bar").Value.Should().Be(0);
        }

        [Test]
        public void ThrottleAsync_should_increment_consumption_counter_after_successful_wait()
        {
            DrainSemaphore();

            var task = ThrottleAsync();

            state.Semaphore.Release();

            task.Wait();

            state.ObtainCounter("foo", "bar").Value.Should().Be(1);
        }

        [Test]
        public void ThrottleAsync_should_reclaim_resources_upon_disposal_of_result_returned_after_successful_wait()
        {
            DrainSemaphore();

            var task = ThrottleAsync();

            state.Semaphore.Release();

            using (task.Result)
            {
            }

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

        [Test]
        public void ThrottleAsync_should_produce_an_event_before_each_throttling_operation()
        {
            for (var i = 0; i < Capacity; i++)
            {
                events.Clear();

                Throttle();

                var evt = events.Should().ContainSingle().Which;

                evt.CapacityLimit.Should().Be(Capacity);
                evt.CapacityConsumed.Should().Be(i);
                evt.QueueLimit.Should().Be(QueueLimit);
                evt.QueueSize.Should().Be(0);
                evt.Properties.Should().BeSameAs(properties);
            }

            for (var i = 0; i < QueueLimit; i++)
            {
                events.Clear();

                ThrottleAsync();

                var evt = events.Should().ContainSingle().Which;

                evt.CapacityLimit.Should().Be(Capacity);
                evt.CapacityConsumed.Should().Be(Capacity);
                evt.QueueLimit.Should().Be(QueueLimit);
                evt.QueueSize.Should().Be(i);
                evt.Properties.Should().BeSameAs(properties);
                evt.PropertyConsumption["foo"].Should().Be(Capacity);
            }

            for (var i = 0; i < 10; i++)
            {
                events.Clear();

                ThrottleAsync();

                var evt = events.Should().ContainSingle().Which;

                evt.CapacityLimit.Should().Be(Capacity);
                evt.CapacityConsumed.Should().Be(Capacity);
                evt.QueueLimit.Should().Be(QueueLimit);
                evt.QueueSize.Should().Be(QueueLimit);
                evt.Properties.Should().BeSameAs(properties);
                evt.PropertyConsumption["foo"].Should().Be(Capacity);
            }
        }

        [Test]
        public void ThrottleAsync_should_produce_a_result_after_each_throttling_operation()
        {
            for (var i = 0; i < Capacity; i++)
            {
                results.Clear();

                Throttle();

                var result = results.Should().ContainSingle().Which;

                result.Status.Should().Be(ThrottlingStatus.Passed);
                result.WaitTime.Should().Be(TimeSpan.Zero);
            }

            for (var i = 0; i < QueueLimit; i++)
            {
                results.Clear();

                ThrottleAsync();

                results.Should().BeEmpty();
            }

            for (var i = 0; i < 10; i++)
            {
                results.Clear();

                ThrottleAsync();

                var result = results.Should().ContainSingle().Which;

                result.Status.Should().Be(ThrottlingStatus.RejectedDueToQueue);
                result.WaitTime.Should().Be(TimeSpan.Zero);
            }
        }

        [Test]
        public void ThrottleAsync_should_handle_observer_exceptions_with_error_callback()
        {
            var error = new Exception();

            provider.Subscribe(new TestObserver<IThrottlingEvent>(_ => throw error));
            provider.Subscribe(new TestObserver<IThrottlingResult>(_ => throw error));

            Throttle();

            errorCallback.Received(2).Invoke(error);
        }

        private IThrottlingResult Throttle(TimeSpan? deadline = null)
            => ThrottleAsync(deadline).GetAwaiter().GetResult();

        private async Task<IThrottlingResult> ThrottleAsync(TimeSpan? deadline = null)
        {
            var result = await provider.ThrottleAsync(properties, deadline);

            Console.Out.WriteLine($"Status = {result.Status}");
            Console.Out.WriteLine($"Wait time = {result.WaitTime}");
            Console.Out.WriteLine($"Rejection reason = {result.RejectionReason}");

            return result;
        }

        private void DrainSemaphore(int amount = Capacity)
        {
            for (var i = 0; i < amount; i++)
                state.Semaphore.WaitAsync().GetAwaiter().GetResult();
        }

        private void DrainSemaphoreAsync(int amount = Capacity)
        {
            for (var i = 0; i < amount; i++)
                state.Semaphore.WaitAsync();
        }

        private void VerifyThereAreNoSideEffects(int expectedCapacity = Capacity, int expectedQueueSize = 0)
        {
            state.Semaphore.CurrentCount.Should().Be(expectedCapacity);
            state.Semaphore.CurrentQueue.Should().Be(expectedQueueSize);

            foreach (var counter in state.Counters)
            foreach (var counterValue in counter.Value)
            {
                counterValue.Value.Value.Should().Be(0);
            }
        }

        private class TestObserver<T> : IObserver<T>
        {
            private readonly Action<T> action;

            public TestObserver(Action<T> action)
                => this.action = action;

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(T value)
                => action(value);
        }
    }
}