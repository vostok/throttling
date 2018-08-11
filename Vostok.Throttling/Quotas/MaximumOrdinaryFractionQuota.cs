using System;

namespace Vostok.Throttling.Quotas
{
    public class MaximumOrdinaryFractionQuota : IThrottlingPriorityQuota
    {
        public MaximumOrdinaryFractionQuota(double maximumFraction)
        {
            if (maximumFraction < 0.0 || maximumFraction > 1.0)
                throw new ArgumentOutOfRangeException(nameof(maximumFraction), "Maximum fraction must be in [0; 1] range.");

            this.maximumFraction = maximumFraction;
        }

        public bool Allows(ThrottlingPriority priority, int consumed, int totalCapacity)
        {
            if (priority != ThrottlingPriority.Ordinary)
                return true;

            return (double) consumed / totalCapacity <= maximumFraction;
        }

        private readonly double maximumFraction;
    }
}