using System;
using System.Collections.Generic;

namespace Vostok.Throttling.Quotas
{
    public class ConsumersWhitelistQuota : IThrottlingConsumerQuota
    {
        public ConsumersWhitelistQuota(ISet<string> whitelist)
        {
            if (whitelist == null)
                throw new ArgumentNullException(nameof(whitelist));

            this.whitelist = whitelist;
        }

        public bool Allows(string consumerId, int consumed, int totalCapacity)
        {
            return whitelist.Contains(consumerId);
        }

        private readonly ISet<string> whitelist;
    }
}