using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Vostok.Throttling.Quotas
{
    [PublicAPI]
    public class PropertyQuota : IThrottlingQuota
    {
        private readonly double globalLimit;
        private readonly HashSet<string> blacklist;
        private readonly HashSet<string> whitelist;
        private readonly Dictionary<string, double> individualLimits;

        public PropertyQuota([NotNull] string property, [NotNull] PropertyQuotaOptions options)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            
            blacklist = new HashSet<string>(options.Blacklist ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            whitelist = new HashSet<string>(options.Whitelist ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            globalLimit = options.GlobalLimit ?? double.MaxValue;
            individualLimits = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (options.IndividualLimits != null)
                foreach (var pair in options.IndividualLimits)
                    individualLimits[pair.Key] = pair.Value;
        }

        public string Property { get; }

        public ThrottlingQuotaVerdict Check(IReadOnlyDictionary<string, string> properties, IThrottlingQuotaContext context)
        {
            if (!properties.TryGetValue(Property, out var value))
                return ThrottlingQuotaVerdict.Allow();

            if (blacklist.Contains(value))
                return ThrottlingQuotaVerdict.Reject($"'{Property}' = '{value}' is blacklisted.");

            if (whitelist.Contains(value))
                return ThrottlingQuotaVerdict.Allow();

            var consumption = context.GetConsumption(Property, value) + 1;
            var utilization = (double)consumption / Math.Max(1, context.CapacityLimit);
            var limit = individualLimits.TryGetValue(value, out var individualLimit) ? individualLimit : globalLimit;

            return utilization <= limit
                ? ThrottlingQuotaVerdict.Allow()
                : ThrottlingQuotaVerdict.Reject($"Capacity utilization for '{Property}' = '{value}' ({utilization:F2}) would exceed the configured limit ({limit:F2}).");
        }
    }
}
