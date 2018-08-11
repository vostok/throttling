using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Commons.Threading;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling
{
    internal class ThrottlingState
    {
        [NotNull]
        public readonly FuzzyLifoSemaphore Semaphore = new FuzzyLifoSemaphore(0);

        [NotNull]
        public readonly Dictionary<ThrottlingPriority, AtomicInt> PriorityCounters = Enum
            .GetValues(typeof (ThrottlingPriority))
            .Cast<ThrottlingPriority>()
            .ToDictionary(_ => _, _ => new AtomicInt(0));

        [NotNull]
        public readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AtomicInt>> PropertyCounters = new ConcurrentDictionary<string, ConcurrentDictionary<string, AtomicInt>>();

        public volatile bool IsActual;

        public volatile bool Enabled;

        public volatile int CapacityLimit;

        public volatile int QueueLimit;

        [CanBeNull]
        public volatile IThrottlingExternalQuota[] ExternalQuotas;

        [CanBeNull]
        public volatile IThrottlingConsumerQuota[] ConsumerQuotas;

        [CanBeNull]
        public volatile IThrottlingPropertiesQuota[] PropertiesQuotas;

        [CanBeNull]
        public volatile IThrottlingPriorityQuota[] PriorityQuotas;

        public ConcurrentDictionary<string, AtomicInt> GetPropertyCounter(string key)
        {
            return PropertyCounters.GetOrAdd(key, _ => new ConcurrentDictionary<string, AtomicInt>());
        }
    }
}