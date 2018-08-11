using System;
using Vostok.Commons.Threading;

namespace Vostok.Throttling.Quotas
{
    public class DropRequestsQuota : IThrottlingExternalQuota
    {
        public DropRequestsQuota(double dropProbability)
        {
            if (dropProbability < 0.0 || dropProbability > 1.0)
                throw new ArgumentOutOfRangeException(nameof(dropProbability), "Drop probability must be in [0; 1] range.");

            this.dropProbability = dropProbability;
        }

        public bool Allows()
        {
            return dropProbability <= 0.0 || ThreadSafeRandom.NextDouble() > dropProbability;
        }

        private readonly double dropProbability;
    }
}