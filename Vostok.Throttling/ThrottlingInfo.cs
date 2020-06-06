using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Throttling.Config;

namespace Vostok.Throttling
{
    /// <summary>
    /// <see cref="ThrottlingInfo"/> is a snapshot of the internal state of <see cref="ThrottlingProvider"/>.
    /// </summary>
    [PublicAPI]
    public class ThrottlingInfo
    {
        /// <summary>
        /// See <see cref="ThrottlingEssentials.Enabled"/>.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// See <see cref="ThrottlingEssentials.CapacityLimit"/>.
        /// </summary>
        public int CapacityLimit { get; set; }

        /// <summary>
        /// Current capacity consumed by all in-flight requests in total.
        /// </summary>
        public int CapacityConsumed { get; set; }

        /// <summary>
        /// See <see cref="ThrottlingEssentials.QueueLimit"/>.
        /// </summary>
        public int QueueLimit { get; set; }

        /// <summary>
        /// Current waiting queue size.
        /// </summary>
        public int QueueSize { get; set; }

        /// <summary>
        /// Current per-property --> per-value capacity consumption.
        /// </summary>
        public Dictionary<string, Dictionary<string, int>> PerPropertyConsumption { get; set; }
    }
}
