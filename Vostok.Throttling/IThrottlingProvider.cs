using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vostok.Throttling
{
    public interface IThrottlingProvider
    {
        [NotNull]
        IThrottlingMetrics Metrics { get; }

        Task<IThrottlingResult> ThrottleAsync(ThrottlingProperties properties, TimeSpan? deadline = null, ThrottlingPriority priority = ThrottlingPriority.Ordinary);        
        
        Task<IThrottlingResult> ThrottleAsync(string consumerId = null, TimeSpan? deadline = null, ThrottlingPriority priority = ThrottlingPriority.Ordinary);
    }
}
