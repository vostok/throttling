using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Collections;

namespace Vostok.Throttling
{
    [PublicAPI]
    public class ThrottlingPropertiesBuilder
    {
        private const string UnknownValue = "unknown";

        private readonly List<KeyValuePair<string, string>> properties;

        public ThrottlingPropertiesBuilder()
            => properties = new List<KeyValuePair<string, string>>(4);

        [NotNull]
        public IReadOnlyDictionary<string, string> Build()
            => new ReadonlyListDictionary<string, string>(properties, StringComparer.OrdinalIgnoreCase);

        [NotNull]
        public ThrottlingPropertiesBuilder AddConsumer([CanBeNull] string consumer)
            => AddProperty(WellKnownThrottlingProperties.Consumer, consumer);

        [NotNull]
        public ThrottlingPropertiesBuilder AddPriority([CanBeNull] string priority)
            => AddProperty(WellKnownThrottlingProperties.Priority, priority);

        [NotNull]
        public ThrottlingPropertiesBuilder AddMethod([CanBeNull] string method)
            => AddProperty(WellKnownThrottlingProperties.Method, method);

        [NotNull]
        public ThrottlingPropertiesBuilder AddUrl([CanBeNull] string url)
            => AddProperty(WellKnownThrottlingProperties.Url, url);

        [NotNull]
        public ThrottlingPropertiesBuilder AddProperty([NotNull] string key, [CanBeNull] string value)
        {
            properties.Add(new KeyValuePair<string, string>(key ?? throw new ArgumentNullException(nameof(key)), value ?? UnknownValue));
            return this;
        }
    }
}