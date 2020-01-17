using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Throttling.Config;

namespace Vostok.Throttling
{
    /// <summary>
    /// <para>An instance of <see cref="IThrottlingEvent"/> provides detailed information about a single <see cref="IThrottlingProvider.ThrottleAsync"/> operation.</para>
    /// <para>Subscribe to the observable implemented by <see cref="IThrottlingProvider"/> to receive such events.</para>
    /// <para>Snapshot consistency of numeric properties (limits and consumption) is not guaranteed.</para>
    /// <para>Events are always produced at the start of the throttling operation, however long it may be.</para>
    /// </summary>
    [PublicAPI]
    public interface IThrottlingEvent
    {
        /// <summary>
        /// See <see cref="ThrottlingEssentials.CapacityLimit"/>.
        /// </summary>
        int CapacityLimit { get; }

        /// <summary>
        /// Current capacity consumed by all in-flight requests in total.
        /// </summary>
        int CapacityConsumed { get; }

        /// <summary>
        /// See <see cref="ThrottlingEssentials.QueueLimit"/>.
        /// </summary>
        int QueueLimit { get; }

        /// <summary>
        /// Current waiting queue size.
        /// </summary>
        int QueueSize { get; }

        /// <summary>
        /// Key-value properties passed to the <see cref="IThrottlingProvider.ThrottleAsync"/> call.
        /// </summary>
        [NotNull]
        IReadOnlyDictionary<string, string> Properties { get; }

        /// <summary>
        /// <para>Current consumption of capacity broken down by the values from <see cref="Properties"/>.</para>
        /// <para>Keys in this dictionary are the same as keys in <see cref="Properties"/>.</para>
        /// <para>The value for each property is the current count of in-flight requests whose corresponding values match the value from <see cref="Properties"/>.</para>
        /// </summary>
        [NotNull]
        IReadOnlyDictionary<string, int> PropertyConsumption { get; }
    }
}