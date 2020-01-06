using System.Collections.Generic;

// ReSharper disable NotNullMemberIsNotInitialized

namespace Vostok.Throttling
{
    internal class ThrottlingEvent : IThrottlingEvent
    {
        public int CapacityLimit { get; set; }
        
        public int CapacityConsumed { get; set; }
        
        public int QueueLimit { get; set; }
        
        public int QueueSize { get; set; }

        public IReadOnlyDictionary<string, string> Properties { get; set; }

        public IReadOnlyDictionary<string, int> PropertyConsumption { get; set; }
    }
}
