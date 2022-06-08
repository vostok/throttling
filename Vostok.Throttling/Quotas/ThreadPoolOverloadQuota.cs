using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Commons.Threading;

namespace Vostok.Throttling.Quotas
{
    /// <summary>
    /// <para><see cref="ThreadPoolOverloadQuota"/> rejects all incoming requests if it observes built-in .NET thread pool to be exhausted continually for some duration.</para>
    /// <para>Thread pool is considered exhausted when it has at least as many busy threads as its min threads value (both worker and IOCP).</para>
    /// </summary>
    [PublicAPI]
    public class ThreadPoolOverloadQuota : IThrottlingQuota
    {
        private static volatile Tuple<DateTime, int, string> cache = Tuple.Create(DateTime.MinValue, 0, string.Empty);

        private readonly Func<ThreadPoolOverloadQuotaOptions> options;

        public ThreadPoolOverloadQuota([NotNull] ThreadPoolOverloadQuotaOptions options)
            : this(() => options)
        {
        }

        public ThreadPoolOverloadQuota([NotNull] Func<ThreadPoolOverloadQuotaOptions> options)
            => this.options = options ?? throw new ArgumentNullException(nameof(options));

        public ThrottlingQuotaVerdict Check(IReadOnlyDictionary<string, string> properties, IThrottlingQuotaContext context)
        {
            var currentTime = DateTime.UtcNow;

            var oldCache = cache;

            if ((currentTime - oldCache.Item1).TotalSeconds >= 1)
            {
                var state = ThreadPoolUtility.GetPoolState();
                var usedWorkerThreadsExhausted = state.UsedWorkerThreads >= state.MinWorkerThreads;
                var usedIocpThreadsExhausted = state.UsedIocpThreads >= state.MinIocpThreads;
                var exhausted = usedWorkerThreadsExhausted || usedIocpThreadsExhausted;
                var newSecondsInExhaustion = exhausted ? oldCache.Item2 + 1 : 0;
                var rejectReason = exhausted
                    ? GetRejectReason(usedWorkerThreadsExhausted, usedIocpThreadsExhausted, newSecondsInExhaustion, state)
                    : string.Empty;
                var newCache = Tuple.Create(currentTime, newSecondsInExhaustion, rejectReason);

                Interlocked.CompareExchange(ref cache, newCache, oldCache);
            }

            var exhaustedDuration = cache.Item2;
            var allowedDuration = options().AllowedSecondsInExhaustion;
            var reason = cache.Item3;

            return exhaustedDuration < allowedDuration
                ? ThrottlingQuotaVerdict.Allow()
                : ThrottlingQuotaVerdict.Reject(reason);
        }
        
        [NotNull]
        private static string GetRejectReason(bool usedWorkerThreadsExhausted, bool usedIocpThreadsExhausted, int secondsInExhaustion, ThreadPoolState state)
        {
            var result = new StringBuilder($"Thread pool has been exhausted for at least {secondsInExhaustion} seconds.");

            if (usedWorkerThreadsExhausted)
                result.Append($" UsedWorkerThreads exhausted (UsedWorkerThreads: {state.UsedWorkerThreads} >= MinWorkerThreads: {state.MinWorkerThreads}).");

            if (usedIocpThreadsExhausted)
                result.Append($" UsedIocpThreads exhausted (UsedIocpThreads: {state.UsedIocpThreads} >= MinIocpThreads: {state.MinIocpThreads}).");
            
            return result.ToString();
        }
    }
}