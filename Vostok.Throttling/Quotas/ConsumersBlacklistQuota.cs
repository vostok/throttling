using System;
using System.Collections.Generic;

namespace Vostok.Throttling.Quotas
{
    public class ConsumersBlacklistQuota : IThrottlingConsumerQuota
    {
        public ConsumersBlacklistQuota(ISet<string> blacklist)
        {
            if (blacklist == null)
                throw new ArgumentNullException(nameof(blacklist));

            this.blacklist = blacklist;
        }

        public bool Allows(string consumerId, int consumed, int totalCapacity)
        {
            return !blacklist.Contains(consumerId);
        }

        private readonly ISet<string> blacklist;
    }
}