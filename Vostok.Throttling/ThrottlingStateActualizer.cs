using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vostok.Commons.Threading;
using Vostok.Throttling.Config;
using Vostok.Throttling.Quotas;

#pragma warning disable 4014

namespace Vostok.Throttling
{
    internal class ThrottlingStateActualizer : IThrottlingStateActualizer
    {
        private readonly ThrottlingConfiguration configuration;

        public ThrottlingStateActualizer(ThrottlingConfiguration configuration)
            => this.configuration = configuration;

        public void Actualize(ThrottlingState state)
        {
            var oldCapacityLimit = state.CapacityLimit;

            UpdateEssentials(state);
            UpdateQuotas(state);
            CleanupCounters(state);
            AdjustSemaphore(state, oldCapacityLimit);

            state.IsActual = true;
        }

        private static void CleanupCounters(ThrottlingState state)
        {
            foreach (var countersMap in state.Counters.Select(pair => pair.Value))
            foreach (var pair in countersMap.Where(pair => pair.Value.Value == 0))
            {
                countersMap.TryRemove(pair.Key, out _);
            }
        }

        private static void AdjustSemaphore(ThrottlingState state, int oldLimit)
        {
            var difference = state.CapacityLimit - oldLimit;
            if (difference == 0)
                return;

            if (difference > 0)
            {
                state.Semaphore.Release(difference);
            }
            else
            {
                DrainSemaphore(state.Semaphore, -difference);
            }
        }

        private static async Task DrainSemaphore(LifoSemaphore semaphore, int amount)
        {
            for (var i = 0; i < amount; i++)
                await semaphore.WaitAsync().ConfigureAwait(false);
        }

        private void UpdateEssentials(ThrottlingState state)
        {
            var newEssentials = configuration.Essentials();

            state.Enabled = newEssentials.Enabled;
            state.CapacityLimit = ComputeCapacityLimit(newEssentials);
            state.QueueLimit = Math.Max(0, newEssentials.QueueLimit);
            state.RefreshPeriod = newEssentials.RefreshPeriod;
        }

        private int ComputeCapacityLimit(ThrottlingEssentials essentials)
        {
            if (essentials.CapacityLimit.HasValue)
                return Math.Max(1, essentials.CapacityLimit.Value);

            if (essentials.CapacityLimitPerCore.HasValue)
            {
                var numberOfCores = configuration.NumberOfCoresProvider?.Invoke() ?? Environment.ProcessorCount;
                return Math.Max(1, essentials.CapacityLimitPerCore.Value * numberOfCores);
            }

            return int.MaxValue;
        }

        private void UpdateQuotas(ThrottlingState state)
        {
            var newQuotas = new List<IThrottlingQuota>();

            newQuotas.AddRange(configuration.CustomQuotas);
            newQuotas.AddRange(BuildPropertyQuotas());

            state.Quotas = newQuotas;
        }

        private IEnumerable<IThrottlingQuota> BuildPropertyQuotas()
        {
            foreach (var pair in configuration.PropertyQuotas)
                yield return new PropertyQuota(pair.Key, pair.Value());
        }
    }
}