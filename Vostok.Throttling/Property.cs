using System;
using JetBrains.Annotations;

namespace Vostok.Throttling
{
    public struct Property
    {
        [NotNull]
        public readonly string Key;

        [NotNull]
        public readonly string Value;

        public Property(string key, string value)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}