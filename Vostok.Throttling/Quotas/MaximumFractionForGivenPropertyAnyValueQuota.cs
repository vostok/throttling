using System;
using System.Collections.Generic;

namespace Vostok.Throttling.Quotas
{
    public class MaximumFractionForGivenPropertyAnyValueQuota : IThrottlingPropertiesQuota
    {
        private readonly string key;
        private readonly MaximumFractionForAnyConsumerQuota fractionQuota;

        public MaximumFractionForGivenPropertyAnyValueQuota(string key, double maximumFraction)
        {
            fractionQuota = new MaximumFractionForAnyConsumerQuota(maximumFraction);

            this.key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public bool Allows(IReadOnlyDictionary<string, ConsumedByValue> consumed, int totalCapacity)
        {
            if (!consumed.TryGetValue(key, out var pair))
                return true;

            return fractionQuota.Allows(null, pair.Consumed, totalCapacity);
        }
    }
}