using System;
using System.Collections.Generic;
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
        private static volatile Tuple<DateTime, int, ThreadPoolState> cache = Tuple.Create(DateTime.MinValue, 0, default(ThreadPoolState));

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
                var exhausted = state.UsedWorkerThreads >= state.MinWorkerThreads || state.UsedIocpThreads >= state.MinIocpThreads;
                var newSecondsInExhaustion = exhausted ? oldCache.Item2 + 1 : 0;
                var newCache = Tuple.Create(currentTime, newSecondsInExhaustion, state);

                Interlocked.CompareExchange(ref cache, newCache, oldCache);
            }

            var exhaustedDuration = cache.Item2;
            var allowedDuration = options().AllowedSecondsInExhaustion;
            var threadPoolState = cache.Item3;

            return exhaustedDuration < allowedDuration
                ? ThrottlingQuotaVerdict.Allow()
                : ThrottlingQuotaVerdict.Reject($"Thread pool has been exhausted for at least {exhaustedDuration.ToString()} seconds. ThreadPoolOverloadQuota statistic (UsedWorkerThreads:'{threadPoolState.UsedWorkerThreads.ToString()}' >= MinWorkerThreads: '{threadPoolState.MinWorkerThreads.ToString()}') or (UsedIocpThreads:'{threadPoolState.UsedIocpThreads.ToString()}' >= MinIocpThreads: '{threadPoolState.MinIocpThreads.ToString()}')");
        }
    }
}