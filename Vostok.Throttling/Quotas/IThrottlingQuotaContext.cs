using JetBrains.Annotations;
using Vostok.Throttling.Config;

namespace Vostok.Throttling.Quotas
{
    [PublicAPI]
    public interface IThrottlingQuotaContext
    {
        /// <summary>
        /// Returns current capacity limit. See <see cref="ThrottlingEssentials.CapacityLimit"/> for more details.
        /// </summary>
        int CapacityLimit { get; }

        /// <summary>
        /// Returns current capacity consumption by request with given <paramref name="property"/> having given <paramref name="value"/>.
        /// </summary>
        int GetConsumption([NotNull] string property, [NotNull] string value);
    }
}
