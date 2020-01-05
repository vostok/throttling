using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Config
{
    /// <summary>
    /// <para>Represents entire configuration of <see cref="ThrottlingProvider"/>.</para>
    /// <para>Use <see cref="ThrottlingConfigurationBuilder"/> to construct instances of <see cref="ThrottlingConfiguration"/>.</para>
    /// </summary>
    [PublicAPI]
    public class ThrottlingConfiguration
    {
        public ThrottlingConfiguration(
            [NotNull] Func<ThrottlingEssentials> essentials,
            [NotNull] IReadOnlyDictionary<string, Func<PropertyQuotaOptions>> propertyQuotas,
            [NotNull] IReadOnlyList<IThrottlingQuota> customQuotas)
        {
            Essentials = essentials ?? throw new ArgumentNullException(nameof(essentials));
            PropertyQuotas = propertyQuotas ?? throw new ArgumentNullException(nameof(propertyQuotas));
            CustomQuotas = customQuotas ?? throw new ArgumentNullException(nameof(customQuotas));
        }

        /// <summary>
        /// <inheritdoc cref="ThrottlingEssentials"/>
        /// <para>This delegate is called once in <see cref="ThrottlingEssentials.RefreshPeriod"/> to update settings.</para>
        /// </summary>
        [NotNull]
        public Func<ThrottlingEssentials> Essentials { get; }

        /// <summary>
        /// <para>A collection of option provider delegates for <see cref="PropertyQuota"/>s, keyed by property name.</para>
        /// <para>These delegates are called once in <see cref="ThrottlingEssentials.RefreshPeriod"/> to update settings.</para>
        /// <para>May be empty.</para>
        /// <para>See <see cref="PropertyQuotaOptions"/> for more details.</para>
        /// </summary>
        [NotNull]
        public IReadOnlyDictionary<string, Func<PropertyQuotaOptions>> PropertyQuotas { get; }

        /// <summary>
        /// <para>A list of custom user-provided qouta implementations.</para>
        /// <para>May be empty.</para>
        /// </summary>
        [NotNull]
        public IReadOnlyList<IThrottlingQuota> CustomQuotas { get; }

        /// <summary>
        /// <para>An optional delegate that allows to customize the calculation of the number of available CPU cores.</para>
        /// <para>It affects handling of <see cref="ThrottlingEssentials.CapacityLimitPerCore"/> property.</para>
        /// </summary>
        [CanBeNull]
        public Func<int> NumberOfCoresProvider { get; set; }

        /// <summary>
        /// An optional callback that, if specified, will be called on any internal error occuring in <see cref="ThrottlingProvider"/>.
        /// </summary>
        [CanBeNull]
        public Action<Exception> ErrorCallback { get; set; }
    }
}
