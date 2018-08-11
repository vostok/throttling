using System;
using System.Collections.Generic;
using System.Linq;

namespace Vostok.Throttling.Quotas
{
    public class MaximumFractionForGivenConsumersQuota : IThrottlingConsumerQuota
    {
        public MaximumFractionForGivenConsumersQuota(IDictionary<string, double> maximumFractions)
        {
            if (maximumFractions == null)
                throw new ArgumentNullException(nameof(maximumFractions));

            foreach (var pair in maximumFractions)
            {
                if (pair.Value < 0.0 || pair.Value > 1.0)
                    throw new ArgumentOutOfRangeException(nameof(maximumFractions), "Maximum fraction must be in [0; 1] range.");
            }

            individialQuotas = maximumFractions.ToDictionary(pair => pair.Key, pair => new MaximumFractionForAnyConsumerQuota(pair.Value));
        }

        public bool Allows(string consumerId, int consumed, int totalCapacity)
        {
            MaximumFractionForAnyConsumerQuota individualQuota;

            return !individialQuotas.TryGetValue(consumerId, out individualQuota) || individualQuota.Allows(consumerId, consumed, totalCapacity);
        }

        private readonly Dictionary<string, MaximumFractionForAnyConsumerQuota> individialQuotas;
    }
}