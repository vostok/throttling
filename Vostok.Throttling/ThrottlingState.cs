using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Threading;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling
{
    internal class ThrottlingState : IThrottlingQuotaContext
    {
        [NotNull]
        public readonly LifoSemaphore Semaphore;

        [NotNull]
        public readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AtomicInt>> Counters;

        [NotNull]
        public volatile IReadOnlyList<IThrottlingQuota> Quotas;

        public volatile bool IsActual;

        public volatile bool Enabled;

        public volatile int CapacityLimit;

        public volatile int QueueLimit;

        public TimeSpan RefreshPeriod;

        public ThrottlingState()
        {
            Semaphore = new LifoSemaphore(0);
            Quotas = Array.Empty<IThrottlingQuota>();
            Counters = new ConcurrentDictionary<string, ConcurrentDictionary<string, AtomicInt>>(StringComparer.OrdinalIgnoreCase);
            RefreshPeriod = TimeSpan.FromSeconds(5);
        }

        public int GetConsumption(string property, string value)
        {
            if (!Counters.TryGetValue(property, out var valueCounters))
                return 0;

            if (!valueCounters.TryGetValue(value, out var counter))
                return 0;

            return counter.Value;
        }

        public AtomicInt ObtainCounter(string property, string value)
            => Counters
                .GetOrAdd(property, _ => new ConcurrentDictionary<string, AtomicInt>(StringComparer.OrdinalIgnoreCase))
                .GetOrAdd(value, _ => new AtomicInt(0));

        int IThrottlingQuotaContext.CapacityLimit => CapacityLimit;
    }
}
