using System;
using JetBrains.Annotations;

namespace Vostok.Throttling.Config
{
    /// <summary>
    /// General configuration options of <see cref="ThrottlingProvider"/> that are not related to quotas.
    /// </summary>
    [PublicAPI]
    public class ThrottlingEssentials
    {
        /// <summary>
        /// If set to <c>false</c>, disables throttling mechanisms entirely.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Same as <see cref="CapacityLimit"/>, but expressed as a per-core multiplier.
        /// </summary>
        public int? CapacityLimitPerCore { get; set; } = 24;

        /// <summary>
        /// <para>Total capacity limit, maximum number of parallel in-flight requests.</para>
        /// <para>Can also be expressed as a per-core multiplier with <see cref="CapacityLimitPerCore"/>.</para>
        /// <para>Takes precedence over <see cref="CapacityLimitPerCore"/>.</para>
        /// </summary>
        public int? CapacityLimit { get; set; }

        /// <summary>
        /// Maximum size of the waiting queue that forms when <see cref="CapacityLimit"/> is reached.
        /// </summary>
        public int QueueLimit { get; set; } = 500;

        /// <summary>
        /// Period of synchronization between configuration options and internal state.
        /// </summary>
        public TimeSpan RefreshPeriod { get; set; } = TimeSpan.FromSeconds(5);
    }
}
