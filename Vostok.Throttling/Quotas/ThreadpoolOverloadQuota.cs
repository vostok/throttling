using System;
using System.Threading;
using Vostok.Commons.Helpers.Conversions;

#pragma warning disable 420

namespace Vostok.Throttling.Quotas
{
    public class ThreadpoolOverloadQuota : IThrottlingExternalQuota
    {
        static ThreadpoolOverloadQuota()
        {
            cache = new Tuple<DateTime, int>(DateTime.UtcNow, 0);
        }

        public ThreadpoolOverloadQuota(int allowedSecondsInExhaustion = 5)
        {
            this.allowedSecondsInExhaustion = allowedSecondsInExhaustion;
        }

        public bool Allows()
        {
            var currentTime = DateTime.UtcNow;

            var oldCache = cache;

            if (currentTime - oldCache.Item1 >= 1.Seconds())
            {
                var newSecondsInExhaustion = IsThreadPoolExhausted() ? oldCache.Item2 + 1 : 0;

                var newCache = Tuple.Create(currentTime, newSecondsInExhaustion);

                Interlocked.CompareExchange(ref cache, newCache, oldCache);
            }
            var secondsInExhaustion = cache.Item2;

            return secondsInExhaustion < allowedSecondsInExhaustion;
        }
        
        private bool IsThreadPoolExhausted()
        {
            ThreadPool.GetAvailableThreads(out var availableWorkers, out var availableIocp);
            ThreadPool.GetMinThreads(out var minWorkers, out var minIocp);
            ThreadPool.GetMaxThreads(out var maxWorkers, out var maxIocp);

            var usedWorkers = maxWorkers - availableWorkers;
            var usedIocp = maxIocp - availableIocp;
            
            return usedWorkers >= minWorkers || usedIocp >= minIocp;
        }

        private readonly int allowedSecondsInExhaustion;

        private static volatile Tuple<DateTime, int> cache;
    }
}