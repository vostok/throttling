using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Config
{
    [PublicAPI]
    public class ThrottlingConfigurationBuilder
    {
        private static readonly ThrottlingEssentials DefaultEssentials = new ThrottlingEssentials();

        private Func<ThrottlingEssentials> essentialsProvider;
        private Func<int> numberOfCoresProvider;
        private Dictionary<string, Func<PropertyQuotaOptions>> propertyQuotas;
        private List<IThrottlingQuota> customQuotas;

        public ThrottlingConfigurationBuilder()
        {
            essentialsProvider = () => DefaultEssentials;
            propertyQuotas = new Dictionary<string, Func<PropertyQuotaOptions>>(StringComparer.OrdinalIgnoreCase);
            customQuotas = new List<IThrottlingQuota>();
        }

        [NotNull]
        public ThrottlingConfiguration Build()
            => new ThrottlingConfiguration(essentialsProvider, propertyQuotas, customQuotas) {NumberOfCoresProvider = numberOfCoresProvider};

        [NotNull]
        public ThrottlingConfigurationBuilder SetEssentials([NotNull] ThrottlingEssentials essentials)
        {
            if (essentials == null)
                throw new ArgumentNullException(nameof(essentials));

            return SetEssentials(() => essentials);
        }

        [NotNull]
        public ThrottlingConfigurationBuilder SetEssentials([NotNull] Func<ThrottlingEssentials> essentials)
        {
            essentialsProvider = essentials ?? throw new ArgumentNullException(nameof(essentials));
            return this;
        }

        [NotNull]
        public ThrottlingConfigurationBuilder SetPropertyQuota([NotNull] string propertyName, [NotNull] PropertyQuotaOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return SetPropertyQuota(propertyName, () => options);
        }

        [NotNull]
        public ThrottlingConfigurationBuilder SetPropertyQuota([NotNull] string propertyName, [NotNull] Func<PropertyQuotaOptions> options)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            propertyQuotas[propertyName] = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        [NotNull]
        public ThrottlingConfigurationBuilder SetConsumerQuota([NotNull] PropertyQuotaOptions options)
            => SetPropertyQuota(WellKnownThrottlingProperties.Consumer, options);

        [NotNull]
        public ThrottlingConfigurationBuilder SetConsumerQuota([NotNull] Func<PropertyQuotaOptions> options)
            => SetPropertyQuota(WellKnownThrottlingProperties.Consumer, options);

        [NotNull]
        public ThrottlingConfigurationBuilder SetPriorityQuota([NotNull] PropertyQuotaOptions options)
            => SetPropertyQuota(WellKnownThrottlingProperties.Priority, options);

        [NotNull]
        public ThrottlingConfigurationBuilder SetPriorityQuota([NotNull] Func<PropertyQuotaOptions> options)
            => SetPropertyQuota(WellKnownThrottlingProperties.Priority, options);

        [NotNull]
        public ThrottlingConfigurationBuilder SetMethodQuota([NotNull] PropertyQuotaOptions options)
            => SetPropertyQuota(WellKnownThrottlingProperties.Method, options);

        [NotNull]
        public ThrottlingConfigurationBuilder SetMethodQuota([NotNull] Func<PropertyQuotaOptions> options)
            => SetPropertyQuota(WellKnownThrottlingProperties.Method, options);

        [NotNull]
        public ThrottlingConfigurationBuilder SetUrlQuota([NotNull] PropertyQuotaOptions options)
            => SetPropertyQuota(WellKnownThrottlingProperties.Url, options);

        [NotNull]
        public ThrottlingConfigurationBuilder SetUrlQuota([NotNull] Func<PropertyQuotaOptions> options)
            => SetPropertyQuota(WellKnownThrottlingProperties.Url, options);

        [NotNull]
        public ThrottlingConfigurationBuilder AddCustomQuota([NotNull] IThrottlingQuota quota)
        {
            customQuotas.Add(quota ?? throw new ArgumentNullException(nameof(quota)));
            return this;
        }

        [NotNull]
        public ThrottlingConfigurationBuilder SetNumberOfCores(Func<int> numberOfCores)
        {
            numberOfCoresProvider = numberOfCores ?? throw new ArgumentNullException(nameof(numberOfCores));
            return this;
        }

        [NotNull]
        public ThrottlingConfigurationBuilder SetNumberOfCores(int numberOfCores)
        {
            numberOfCoresProvider = () => numberOfCores;
            return this;
        }
    }
}
