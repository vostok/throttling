using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace Vostok.Throttling.Tests
{
    [TestFixture]
    internal class FuzzyLifoSemaphore_Tests
    {
        private FuzzyLifoSemaphore semaphore;

        [SetUp]
        public void TestSetup()
        {
            semaphore = new FuzzyLifoSemaphore(3);
        }

        [Test]
        public void WaitAsync_should_return_completed_tasks_immediately_until_count_goes_negative()
        {
            semaphore.WaitAsync().IsCompleted.Should().BeTrue();
            semaphore.WaitAsync().IsCompleted.Should().BeTrue();
            semaphore.WaitAsync().IsCompleted.Should().BeTrue();
        }

        [Test]
        public void WaitAsync_should_return_waiter_tasks_after_count_goes_negative()
        {
            semaphore.WaitAsync();
            semaphore.WaitAsync();
            semaphore.WaitAsync();

            semaphore.WaitAsync().IsCompleted.Should().BeFalse();
            semaphore.WaitAsync().IsCompleted.Should().BeFalse();
        }

        [Test]
        public void CurrentCount_property_should_initially_return_initial_count()
        {
            semaphore.CurrentCount.Should().Be(3);
        }

        [Test]
        public void CurrentCount_property_should_decrease_after_wait_calls()
        {
            for (int i = 1; i <= 3; i++)
            {
                semaphore.WaitAsync();

                semaphore.CurrentCount.Should().Be(3 - i);
            }
        }

        [Test]
        public void CurrentCount_property_should_return_zero_when_there_is_a_queue_of_waiters()
        {
            for (int i = 1; i <= 5; i++)
            {
                semaphore.WaitAsync();
            }

            semaphore.CurrentCount.Should().Be(0);
        }

        [Test]
        public void CurrentQueue_property_should_initially_return_zero()
        {
            semaphore.CurrentQueue.Should().Be(0);
        }

        [Test]
        public void CurrentQueue_property_should_remain_zero_until_there_is_first_queued_waiter()
        {
            for (int i = 0; i < 3; i++)
            {
                semaphore.WaitAsync();
                semaphore.CurrentQueue.Should().Be(0);
            }
        }

        [Test]
        public void CurrentQueue_property_should_return_count_of_queued_waiters_if_there_are_any()
        {
            for (int i = 0; i < 3; i++)
            {
                semaphore.WaitAsync();
            }

            for (int i = 1; i <= 3; i++)
            {
                semaphore.WaitAsync();
                semaphore.CurrentQueue.Should().Be(i);
            }
        }

        [Test]
        public void Release_should_increase_current_count_by_one()
        {
            for (int i = 1; i <= 3; i++)
            {
                semaphore.Release();
                semaphore.CurrentCount.Should().Be(3 + i);
            }
        }

        [Test]
        public void Release_should_unblock_pending_waiter()
        {
            for (int i = 0; i < 3; i++)
                semaphore.WaitAsync();

            var waiter = semaphore.WaitAsync();

            semaphore.Release();

            waiter.ShouldCompleteIn(1.Seconds());
        }

        [Test]
        public void Release_should_unblock_waiters_in_lifo_order()
        {
            for (int i = 0; i < 3; i++)
                semaphore.WaitAsync();

            var waiter1 = semaphore.WaitAsync();
            var waiter2 = semaphore.WaitAsync();
            var waiter3 = semaphore.WaitAsync();

            semaphore.Release();

            waiter3.ShouldCompleteIn(1.Seconds());
            waiter2.IsCompleted.Should().BeFalse();
            waiter1.IsCompleted.Should().BeFalse();

            semaphore.Release();

            waiter2.ShouldCompleteIn(1.Seconds());
            waiter1.IsCompleted.Should().BeFalse();

            semaphore.Release();

            waiter1.ShouldCompleteIn(1.Seconds());
        }

        [Test]
        public void Release_with_count_should_increase_current_count_by_given_amount()
        {
            semaphore.Release(3);
            semaphore.CurrentCount.Should().Be(6);
        }

        [Test]
        public void Release_with_count_should_unblock_pending_waiters_in_lifo_order()
        {
            for (int i = 0; i < 3; i++)
                semaphore.WaitAsync();

            var waiter1 = semaphore.WaitAsync();
            var waiter2 = semaphore.WaitAsync();
            var waiter3 = semaphore.WaitAsync();

            semaphore.Release(2);

            waiter3.ShouldCompleteIn(1.Seconds());
            waiter2.ShouldCompleteIn(1.Seconds());
            waiter1.IsCompleted.Should().BeFalse();

            semaphore.CurrentCount.Should().Be(0);
            semaphore.CurrentQueue.Should().Be(1);

            semaphore.Release(2);

            waiter1.ShouldCompleteIn(1.Seconds());

            semaphore.CurrentCount.Should().Be(1);
            semaphore.CurrentQueue.Should().Be(0);
        }
    }
}