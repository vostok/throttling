using System;
using System.Linq;
using System.Threading;
using Vostok.Commons.Threading;

namespace Vostok.Throttling.Results
{
    internal class PassedResult : IThrottlingResult
    {
        private LifoSemaphore semaphore;
        private AtomicInt[] counters;

        public PassedResult(LifoSemaphore semaphore, AtomicInt[] counters, TimeSpan waitTime)
        {
            this.semaphore = semaphore;
            this.counters = counters;

            WaitTime = waitTime;
        }

        public ThrottlingStatus Status => ThrottlingStatus.Passed;

        public string RejectionReason => null;

        public TimeSpan WaitTime { get; }

        public void Dispose()
        {
            foreach (var counter in Interlocked.Exchange(ref counters, null) ?? Enumerable.Empty<AtomicInt>())
                counter.Decrement();

            Interlocked.Exchange(ref semaphore, null)?.Release();
        }
    }
}