using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Throttling
{
    public interface IThrottlingMetrics
    {
        int CapacityLimit { get; }

        int RemainingCapacity { get; }

        int ConsumedCapacity { get; }

        int QueueSize { get; }

        int QueueLimit { get; }

        [NotNull]
        Dictionary<string, int> ConsumedById { get; }

        [NotNull]
        Dictionary<ThrottlingPriority, int> ConsumedByPriority { get; }

        [NotNull]
        Dictionary<string, Dictionary<string, int>> ConsumedByProperties { get; }
    }
}