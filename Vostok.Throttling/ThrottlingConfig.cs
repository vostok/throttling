using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Conversions;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling
{
    public class ThrottlingConfig
    {
        [NotNull]
        public Func<bool> Enabled { get; set; } = () => true;

        [NotNull]
        public Func<int> CapacityLimit { get; set; } = () => 16 * Environment.ProcessorCount;

        [NotNull]
        public Func<int> QueueLimit { get; set; } = () => 500;

        [NotNull]
        public Func<IEnumerable<IThrottlingExternalQuota>> ExternalQuotas { get; set; } = () => null;

        [NotNull]
        public Func<IEnumerable<IThrottlingConsumerQuota>> ConsumerQuotas { get; set; } = () => null;

        [NotNull]
        public Func<IEnumerable<IThrottlingPriorityQuota>> PriorityQuotas { get; set; } = () => null;

        [NotNull]
        public Func<IEnumerable<IThrottlingPropertiesQuota>> PropertiesQuotas { get; set; } = () => null;

        public TimeSpan RefreshPeriod { get; set; } = 5.Seconds();
    }
}