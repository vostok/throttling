using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Vostok.Commons.Threading;
using Vostok.Throttling.Results;

namespace Vostok.Throttling
{
    // (iloktionov): Реализация троттлинга запросов, отвечающая ключевым требованиям к механизму:
    // (iloktionov): 1. "Горячий" рубильник вкл/выкл и тюнинг ёмкости.
    // (iloktionov): 2. Отсечение запросов с истёкшим бюджетом времени.
    // (iloktionov): 3. Ограничение на размер очереди из ожидающих запросов.
    // (iloktionov): 4. Ограничение на долю ёмкости для различных потребителей.
    // (iloktionov): 5. Доступность основных метрик (ёмкость, занятость, размер очереди).
    public class ThrottlingProvider : IThrottlingProvider
    {
        internal ThrottlingProvider(IThrottlingStateProvider stateProvider, IThrottlingQuotasChecker quotasChecker)
        {
            this.stateProvider = stateProvider;
            this.quotasChecker = quotasChecker;
        }

        public ThrottlingProvider(ThrottlingConfig config)
            : this(new ThrottlingStateProvider(config), new ThrottlingQuotasChecker())
        {
        }

        public IThrottlingMetrics Metrics
        {
            get
            {
                var state = stateProvider.ObtainState();
                if (!state.Enabled)
                    return new ThrottlingMetrics();

                var remaining = state.Semaphore.CurrentCount;
                var queueSize = state.Semaphore.CurrentQueue;
                var capacityLimit = state.CapacityLimit;

                // (iloktionov): Механизм сбора метрик допускает лёгкую неконсистентность (например, QueueSize > 0 при Consumed < Capacity).
                // (iloktionov): Это решение принято осознанно во избежание дополнительных блокировок.
                return new ThrottlingMetrics
                {
                    CapacityLimit = capacityLimit,
                    QueueSize = queueSize,
                    QueueLimit = state.QueueLimit,
                    RemainingCapacity = remaining,
                    ConsumedCapacity = capacityLimit - remaining,
                    ConsumedByPriority = state.PriorityCounters.ToDictionary(pair => pair.Key, pair => pair.Value.Value),

                    ConsumedByProperties = state.PropertyCounters.ToDictionary(
                        p => p.Key,
                        p => p.Value
                            .Where(p2 => p2.Value > 0)
                            .ToDictionary(p2 => p2.Key, p2 => p2.Value.Value)),

                    ConsumedById = state.PropertyCounters.TryGetValue(KnownProperties.ConsumerIdKey, out var consumedById)
                        ? consumedById
                            .Where(pair => pair.Value > 0)
                            .ToDictionary(pair => pair.Key, pair => pair.Value.Value)
                        : new Dictionary<string, int>()
                };
            }
        }

        public Task<IThrottlingResult> ThrottleAsync(string consumerId = null, TimeSpan? deadline = null,
            ThrottlingPriority priority = ThrottlingPriority.Ordinary)
        {
            var properties = consumerId == null
                ? new ThrottlingProperties()
                : new ThrottlingProperties(KnownProperties.ConsumerId(consumerId));

            return ThrottleInternalAsync(properties, deadline, priority);
        }

        public Task<IThrottlingResult> ThrottleAsync(ThrottlingProperties properties, TimeSpan? deadline = null,
            ThrottlingPriority priority = ThrottlingPriority.Ordinary)
        {
            return ThrottleInternalAsync(properties, deadline, priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task<IThrottlingResult> ThrottleInternalAsync(ThrottlingProperties properties, TimeSpan? deadline,
            ThrottlingPriority priority)
        {
            var result = null as IThrottlingResult;
            var state = stateProvider.ObtainState();

            if (!CheckEnabled(state, ref result))
                return Task.FromResult(result);

            if (!CheckQueueLimit(state, priority, ref result))
                return Task.FromResult(result);

            if (!CheckQuotas(state, properties, priority, ref result))
                return Task.FromResult(result);

            var counters = BuildCounters(properties.Properties, state, priority);

            if (TryAcquireImmediately(state, counters, ref result, out var waitTask))
                return Task.FromResult(result);

            return ThrottleWithWaitingAsync(state, counters, waitTask, properties, deadline, priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckEnabled(ThrottlingState state, ref IThrottlingResult result)
        {
            if (state.Enabled)
                return true;

            result = DisabledResult;

            return false;
        }

        // (iloktionov): Из-за отсутствия явной синхронизации лимит может быть незначительно превышен.
        // (iloktionov): Это осознанная жертва во избежание дополнительных блокировок.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckQueueLimit(ThrottlingState state, ThrottlingPriority priority,
            ref IThrottlingResult result)
        {
            var queueSize = state.Semaphore.CurrentQueue;
            var queueLimit = state.QueueLimit;

            if (priority == ThrottlingPriority.Sheddable && queueSize > 0)
            {
                result = RejectedByQueueResult;
                return false;
            }

            if (queueLimit > queueSize)
                return true;

            if (queueLimit == 0 && state.Semaphore.CurrentCount > 0)
                return true;

            result = RejectedByQueueResult;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckQuotas(ThrottlingState state, ThrottlingProperties properties, ThrottlingPriority priority,
            ref IThrottlingResult result)
        {
            var checkStatus = quotasChecker.Check(state, properties, priority);
            if (checkStatus == ThrottlingStatus.Passed)
                return true;

            result = new FailedThrottlingResult(checkStatus, TimeSpan.Zero);

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AtomicInt[] BuildCounters(Property[] properties, ThrottlingState state, ThrottlingPriority priority)
        {
            var propertiesCount = properties?.Length ?? 0;
            var counters = new AtomicInt[propertiesCount + 1]; //NOTE порядок счетчиков не важен!

            if (properties != null)
            {
                for (var index = 0; index < properties.Length; index++)
                {
                    var property = properties[index];
                    var dictionary = state.GetPropertyCounter(property.Key);
                    counters[index] = dictionary.GetOrAdd(property.Value, _ => new AtomicInt(0));
                }
            }

            counters[propertiesCount] = state.PriorityCounters[priority];
            return counters;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Increment(AtomicInt[] counters)
        {
            foreach (var counter in counters)
                counter.Increment();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryAcquireImmediately(
            ThrottlingState state,
            AtomicInt[] counters,
            ref IThrottlingResult result,
            out Task waitTask)
        {
            waitTask = state.Semaphore.WaitAsync();

            if (!waitTask.IsCompleted)
                return false;

            Increment(counters);

            result = new PassedThrottlingResult(state.Semaphore, counters, TimeSpan.Zero);

            return true;
        }

        private async Task<IThrottlingResult> ThrottleWithWaitingAsync(
            ThrottlingState state,
            AtomicInt[] counters,
            Task waitTask,
            ThrottlingProperties properties,
            TimeSpan? deadline,
            ThrottlingPriority priority)
        {
            var waitStartTime = Watch.Elapsed;

            await waitTask.ConfigureAwait(false);

            // (iloktionov): К этому моменту времени может пройти сильно больше, чем waitTime (таймаут не является "честным").
            // (iloktionov): Это решение продиктовано деградацией производительности при использовании таймаутов для семафора.
            // (iloktionov): Использование WaitAsync(timeout) под нагрузкой приводит к созданию множества таймеров, заваливающих приложение (проверено в нагрузочном тестировании Cauldron'а).
            var waitTime = Watch.Elapsed - waitStartTime;
            if (waitTime >= deadline)
            {
                state.Semaphore.Release();
                return new FailedThrottlingResult(ThrottlingStatus.RejectedDueToDeadline, waitTime);
            }

            // (iloktionov): После ожидания на семафоре необходимо перепроверить квоты, поскольку они не контролируют напрямую содержимое очереди.
            // (iloktionov): Без этого шага гарантии, даваемые квотами, могут быть нарушены (например, если вся очередь занята одним потребителем).
            var quotasRecheckStatus = quotasChecker.Check(state, properties, priority);
            if (quotasRecheckStatus != ThrottlingStatus.Passed)
            {
                state.Semaphore.Release();
                return new FailedThrottlingResult(quotasRecheckStatus, waitTime);
            }

            Increment(counters);

            return new PassedThrottlingResult(state.Semaphore, counters, waitTime);
        }

        private readonly IThrottlingStateProvider stateProvider;
        private readonly IThrottlingQuotasChecker quotasChecker;

        private static readonly IThrottlingResult RejectedByQueueResult =
            new FailedThrottlingResult(ThrottlingStatus.RejectedDueToFullQueue, TimeSpan.Zero);

        private static readonly IThrottlingResult DisabledResult = new DisabledThrottlingResult();
        private static readonly Stopwatch Watch = Stopwatch.StartNew();
    }
}