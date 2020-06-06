using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Collections;
using Vostok.Commons.Helpers.Observable;
using Vostok.Commons.Threading;
using Vostok.Throttling.Config;
using Vostok.Throttling.Results;

namespace Vostok.Throttling
{
    /// <inheritdoc cref="IThrottlingProvider"/>
    [PublicAPI]
    public class ThrottlingProvider : IThrottlingProvider
    {
        private static readonly Stopwatch Watch = Stopwatch.StartNew();
        private static readonly IThrottlingResult DisabledResult = new DisabledResult();
        private static readonly IThrottlingResult FullQueueResult = new FailedResult(ThrottlingStatus.RejectedDueToQueue, TimeSpan.Zero, "Wait queue is full.");
        private static readonly IReadOnlyDictionary<string, string> EmptyProperties = new Dictionary<string, string>();
        private static readonly IReadOnlyDictionary<string, int> EmptyConsumption = new Dictionary<string, int>();

        private readonly IThrottlingStateProvider stateProvider;
        private readonly BroadcastObservable<IThrottlingEvent> eventsObservable;
        private readonly BroadcastObservable<IThrottlingResult> resultsObservable;
        private readonly Action<Exception> errorCallback;

        public ThrottlingProvider([NotNull] ThrottlingConfiguration configuration)
            : this(new ThrottlingStateProvider(configuration ?? throw new ArgumentNullException(nameof(configuration))), configuration.ErrorCallback)
        {
        }

        internal ThrottlingProvider(IThrottlingStateProvider stateProvider, Action<Exception> errorCallback)
        {
            this.stateProvider = stateProvider;
            this.errorCallback = errorCallback;

            eventsObservable = new BroadcastObservable<IThrottlingEvent>();
            resultsObservable = new BroadcastObservable<IThrottlingResult>();
        }

        public ThrottlingInfo CurrentInfo => CaptureCurrentInfo(stateProvider.ObtainState());

        public async Task<IThrottlingResult> ThrottleAsync(IReadOnlyDictionary<string, string> properties, TimeSpan? deadline)
        {
            properties = properties ?? EmptyProperties;

            var state = stateProvider.ObtainState();

            PublishEventIfNeeded(state, properties);

            var result = await ThrottleInternalAsync(state, properties, deadline).ConfigureAwait(false);

            PublishResultIfNeeded(state, result);

            return result;
        }

        public IDisposable Subscribe(IObserver<IThrottlingEvent> observer) =>
            eventsObservable.Subscribe(observer);

        public IDisposable Subscribe(IObserver<IThrottlingResult> observer) =>
            resultsObservable.Subscribe(observer);

        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IThrottlingResult CheckEnabled(ThrottlingState state)
            => state.Enabled ? null : DisabledResult;

        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IThrottlingResult CheckQueueLimit(ThrottlingState state)
        {
            var queueSize = state.Semaphore.CurrentQueue;
            var queueLimit = state.QueueLimit;

            if (queueLimit > queueSize)
                return null;

            if (queueLimit == 0 && state.Semaphore.CurrentCount > 0)
                return null;

            return FullQueueResult;
        }

        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IThrottlingResult CheckQuotas(ThrottlingState state, IReadOnlyDictionary<string, string> properties, TimeSpan? waitTime)
        {
            foreach (var quota in state.Quotas)
            {
                var verdict = quota.Check(properties, state);
                if (!verdict.Allowed)
                    return new FailedResult(ThrottlingStatus.RejectedDueToQuota, waitTime ?? TimeSpan.Zero, verdict.RejectionReason);
            }

            return null;
        }

        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AtomicInt[] BuildCounters(ThrottlingState state, IReadOnlyDictionary<string, string> properties)
        {
            var counters = new AtomicInt[properties.Count];
            var countersIndex = 0;

            foreach (var pair in properties)
                counters[countersIndex++] = state.ObtainCounter(pair.Key, pair.Value);

            return counters;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementCounters(AtomicInt[] counters)
        {
            foreach (var counter in counters)
                counter.Increment();
        }

        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IThrottlingResult TryAcquireImmediately(ThrottlingState state, AtomicInt[] counters, out Task waitTask)
        {
            waitTask = state.Semaphore.WaitAsync();

            if (!waitTask.IsCompleted)
                return null;

            IncrementCounters(counters);

            return new PassedResult(state.Semaphore, counters, TimeSpan.Zero);
        }

        private static async Task<IThrottlingResult> ThrottleWithWaitingAsync(
            ThrottlingState state,
            AtomicInt[] counters,
            Task waitTask,
            IReadOnlyDictionary<string, string> properties,
            TimeSpan? deadline)
        {
            var waitStartTime = Watch.Elapsed;

            await waitTask.ConfigureAwait(false);

            var waitTime = Watch.Elapsed - waitStartTime;
            if (waitTime >= deadline)
            {
                state.Semaphore.Release();
                return new FailedResult(ThrottlingStatus.RejectedDueToDeadline, waitTime, $"Queue wait ({waitTime}) was longer than request deadline ({deadline}).");
            }

            var quotasRecheckResult = CheckQuotas(state, properties, waitTime);
            if (quotasRecheckResult != null)
            {
                state.Semaphore.Release();
                return quotasRecheckResult;
            }

            IncrementCounters(counters);

            return new PassedResult(state.Semaphore, counters, waitTime);
        }

        private static IReadOnlyDictionary<string, int> CaptureConsumption(ThrottlingState state, IReadOnlyDictionary<string, string> properties)
        {
            if (properties.Count == 0)
                return EmptyConsumption;

            var pairs = new List<KeyValuePair<string, int>>(properties.Count);

            foreach (var pair in properties)
                pairs.Add(new KeyValuePair<string, int>(pair.Key, state.GetConsumption(pair.Key, pair.Value)));

            return new ReadonlyListDictionary<string, int>(pairs, StringComparer.OrdinalIgnoreCase);
        }

        private Task<IThrottlingResult> ThrottleInternalAsync(ThrottlingState state, IReadOnlyDictionary<string, string> properties, TimeSpan? deadline)
        {
            var result = CheckEnabled(state);
            if (result != null)
                return Task.FromResult(result);

            if ((result = CheckQueueLimit(state)) != null)
                return Task.FromResult(result);

            if ((result = CheckQuotas(state, properties, null)) != null)
                return Task.FromResult(result);

            var counters = BuildCounters(state, properties);

            if ((result = TryAcquireImmediately(state, counters, out var waitTask)) != null)
                return Task.FromResult(result);

            return ThrottleWithWaitingAsync(state, counters, waitTask, properties, deadline);
        }

        private void PublishEventIfNeeded(ThrottlingState state, IReadOnlyDictionary<string, string> properties)
        {
            if (!eventsObservable.HasObservers || !state.Enabled)
                return;

            var evt = new ThrottlingEvent
            {
                Properties = properties,
                CapacityLimit = state.CapacityLimit,
                CapacityConsumed = Math.Max(0, state.CapacityLimit - state.Semaphore.CurrentCount),
                QueueLimit = state.QueueLimit,
                QueueSize = state.Semaphore.CurrentQueue,
                PropertyConsumption = CaptureConsumption(state, properties)
            };

            try
            {
                eventsObservable.Push(evt);
            }
            catch (Exception error)
            {
                errorCallback?.Invoke(error);
            }
        }
      
        private void PublishResultIfNeeded(ThrottlingState state, IThrottlingResult result)
        {
            if (!resultsObservable.HasObservers || !state.Enabled)
                return;

            try
            {
                resultsObservable.Push(result);
            }
            catch (Exception error)
            {
                errorCallback?.Invoke(error);
            }
        }

        private static ThrottlingInfo CaptureCurrentInfo(ThrottlingState state)
            => new ThrottlingInfo
            {
                Enabled = state.Enabled,
                CapacityLimit = state.CapacityLimit,
                CapacityConsumed = Math.Max(0, state.CapacityLimit - state.Semaphore.CurrentCount),
                QueueLimit = state.QueueLimit,
                QueueSize = state.Semaphore.CurrentQueue,
                PerPropertyConsumption = state.Counters.ToDictionary(
                    pair => pair.Key, 
                    pair => pair.Value
                        .Select(p => (name: p.Key, count: p.Value.Value))
                        .OrderByDescending(p => p.count)
                        .ToDictionary(p => p.name, p => p.count))
            };
    }
}