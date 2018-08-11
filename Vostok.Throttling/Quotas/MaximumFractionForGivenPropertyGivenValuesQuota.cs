using System;
using System.Collections.Generic;

namespace Vostok.Throttling.Quotas
{
    public class MaximumFractionForGivenPropertyGivenValuesQuota : IThrottlingPropertiesQuota
    {
        private readonly string key;
        private readonly MaximumFractionForGivenConsumersQuota fractionQuota;

        public MaximumFractionForGivenPropertyGivenValuesQuota(string key, IDictionary<string, double> maximumFractions)
        {
            fractionQuota = new MaximumFractionForGivenConsumersQuota(maximumFractions);

            this.key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public bool Allows(IReadOnlyDictionary<string, ConsumedByValue> consumed, int totalCapacity)
        {
            if (!consumed.TryGetValue(key, out var pair))
                return true;

            return fractionQuota.Allows(pair.Value, pair.Consumed, totalCapacity);
        }
    }
}