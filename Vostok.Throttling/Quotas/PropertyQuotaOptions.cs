using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Throttling.Quotas
{
    /// <summary>
    /// Configuration options of <see cref="PropertyQuota"/>.
    /// </summary>
    [PublicAPI]
    public class PropertyQuotaOptions
    {
        /// <summary>
        /// <para>An optional list of values that should be unconditionally rejected by the quota.</para>
        /// <para>Takes precedence over <see cref="Whitelist"/>, <see cref="GlobalLimit"/> and <see cref="IndividualLimits"/>.</para>
        /// </summary>
        [CanBeNull]
        public IReadOnlyList<string> Blacklist { get; set; }

        /// <summary>
        /// <para>An optional list of values that should be unconditionally accepted by the quota.</para>
        /// <para>Takes precedence over <see cref="GlobalLimit"/> and <see cref="IndividualLimits"/>.</para>
        /// </summary>
        [CanBeNull]
        public IReadOnlyList<string> Whitelist { get; set; }

        /// <summary>
        /// <para>A maximum fraction of total capacity any single value can occupy.</para>
        /// <para>If not null, should have a value in [0; 1] range.</para>
        /// </summary>
        [CanBeNull]
        public double? GlobalLimit { get; set; } = 0.75;

        /// <summary>
        /// <para>Per-value overrides of the <see cref="GlobalLimit"/>.</para>
        /// <para>Takes precedence over <see cref="GlobalLimit"/> for specified values.</para>
        /// </summary>
        [CanBeNull]
        public IReadOnlyDictionary<string, double> IndividualLimits { get; set; }
    }
}