using System.Collections.Generic;

namespace Vostok.Throttling
{
    internal class ThrottlingMetrics : IThrottlingMetrics
    {
        public int CapacityLimit { get; set; }

        public int RemainingCapacity { get; set; }

        public int ConsumedCapacity { get; set; }

        public int QueueSize { get; set; }

        public int QueueLimit { get; set; }

        public Dictionary<string, int> ConsumedById { get; set; } = new Dictionary<string, int>();

        public Dictionary<string, Dictionary<string, int>> ConsumedByProperties { get; set; } = new Dictionary<string, Dictionary<string, int>>();

        public Dictionary<ThrottlingPriority, int> ConsumedByPriority { get; set; } = new Dictionary<ThrottlingPriority, int>();
    }
}
