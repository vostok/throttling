using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Throttling.Quotas
{
    public interface IThrottlingPropertiesQuota
    {
        bool Allows([NotNull] IReadOnlyDictionary<string, ConsumedByValue> consumed, int totalCapacity);
    }
}