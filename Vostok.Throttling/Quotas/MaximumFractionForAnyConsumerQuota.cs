using System;

namespace Vostok.Throttling.Quotas
{
    public class MaximumFractionForAnyConsumerQuota : IThrottlingConsumerQuota
    {
        public MaximumFractionForAnyConsumerQuota(double maximumFraction)
        {
            if (maximumFraction < 0.0 || maximumFraction > 1.0)
                throw new ArgumentOutOfRangeException(nameof(maximumFraction), "Maximum fraction must be in [0; 1] range.");

            this.maximumFraction = maximumFraction;
        }

        public bool Allows(string consumerId, int consumed, int totalCapacity)
        {
            return (double) consumed / totalCapacity <= maximumFraction;
        }

        private readonly double maximumFraction;
    }
}
