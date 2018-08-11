using FluentAssertions;
using NUnit.Framework;
using Vostok.Commons.Threading;
using Vostok.Throttling.Results;

namespace Vostok.Throttling.Tests.Results
{
    [TestFixture]
    internal class PassedThrottlingResult_Tests
    {
        private LockFreeLifoSemaphore semaphore;
        private AtomicInt consumerCounter;
        private AtomicInt priorityCounter;

        [SetUp]
        public void TestSetup()
        {
            semaphore = new LockFreeLifoSemaphore(1);
            consumerCounter = new AtomicInt(3);
            priorityCounter = new AtomicInt(4);
        }

        [Test]
        public void Status_property_should_return_passed_value()
        {
            CreateResult().Status.Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void Dispose_should_release_semaphore_capacity()
        {
            CreateResult().Dispose();

            semaphore.CurrentCount.Should().Be(2);
        }

        [Test]
        public void Dispose_should_decrement_consumer_counter()
        {
            CreateResult().Dispose();

            consumerCounter.Value.Should().Be(2);
        }

        [Test]
        public void Dispose_should_decrement_priority_counter()
        {
            CreateResult().Dispose();

            priorityCounter.Value.Should().Be(3);
        }

        [Test]
        public void Should_protect_semaphore_from_double_dispose()
        {
            var result = CreateResult();

            result.Dispose();
            result.Dispose();

            semaphore.CurrentCount.Should().Be(2);
        }

        [Test]
        public void Should_protect_consumer_counter_from_double_dispose()
        {
            var result = CreateResult();

            result.Dispose();
            result.Dispose();

            consumerCounter.Value.Should().Be(2);
        }

        [Test]
        public void Should_Work_If_NoCounters()
        {
            var result = new PassedThrottlingResult(semaphore, new AtomicInt[0], 1.Seconds()); 

            result.Dispose();
        }

        [Test]
        public void Should_Work_If_NullCounters()
        {
            var result = new PassedThrottlingResult(semaphore, null, 1.Seconds()); 

            result.Dispose();
        }

        [Test]
        public void Should_protect_priority_counter_from_double_dispose()
        {
            var result = CreateResult();

            result.Dispose();
            result.Dispose();

            priorityCounter.Value.Should().Be(3);
        }

        private PassedThrottlingResult CreateResult()
        {
            return new PassedThrottlingResult(semaphore, new[] {consumerCounter, priorityCounter}, 1.Seconds());
        } 
    }
}