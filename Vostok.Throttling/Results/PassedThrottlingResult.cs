using System;
using System.Threading;
using Vostok.Commons.Threading;

namespace Vostok.Throttling.Results
{
    internal class PassedThrottlingResult : IThrottlingResult
    {
        public PassedThrottlingResult(LockFreeLifoSemaphore semaphore, AtomicInt[] counters, TimeSpan elapsed)
        {
            this.semaphore = semaphore;
            this.counters = counters;

            WaitTime = elapsed;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref semaphore, null)?.Release();

            var currentCounters = Interlocked.Exchange(ref counters, null);
            if (currentCounters == null)
                return;

            foreach (var counter in currentCounters)
            {
                counter.Decrement();
            }
        }

        public ThrottlingStatus Status => ThrottlingStatus.Passed;

        public TimeSpan WaitTime { get; }

        private LockFreeLifoSemaphore semaphore;
        private AtomicInt[] counters;
    }
}