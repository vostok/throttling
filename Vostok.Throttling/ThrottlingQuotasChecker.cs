using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling
{
    internal class ThrottlingQuotasChecker : IThrottlingQuotasChecker
    {
        public ThrottlingStatus Check(ThrottlingState state, ThrottlingProperties properties,
            ThrottlingPriority priority)
        {
            if (!CheckExternalQuotas(state))
                return ThrottlingStatus.RejectedDueToExternalQuota;

            if (!CheckPriorityQuotas(state, priority))
                return ThrottlingStatus.RejectedDueToPriorityQuota;

            if (!CheckConsumerQuotas(state, properties))
                return ThrottlingStatus.RejectedDueToConsumerQuota;

            if (!CheckPropertyQuotas(state, properties))
                return ThrottlingStatus.RejectedDueToConsumerQuota;

            return ThrottlingStatus.Passed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckExternalQuotas(ThrottlingState state)
        {
            return state.ExternalQuotas == null || state.ExternalQuotas.All(quota => quota.Allows());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckPriorityQuotas(ThrottlingState state, ThrottlingPriority priority)
        {
            if (state.PriorityQuotas == null)
                return true;

            int consumed = state.PriorityCounters[priority].Value;

            foreach (var quota in state.PriorityQuotas)
            {
                if (!quota.Allows(priority, consumed + 1, state.CapacityLimit))
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckPropertyQuotas(ThrottlingState state, ThrottlingProperties properties)
        {
            if (properties.Properties == null || properties.Properties.Length == 0 ||
                state.PropertiesQuotas == null)
                return true;

            var consumed = GetConsumed(properties.Properties, state);

            foreach (var quota in state.PropertiesQuotas)
            {
                if (!quota.Allows(consumed, state.CapacityLimit))
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckConsumerQuotas(ThrottlingState state, ThrottlingProperties properties)
        {
            if (properties.Properties == null || properties.Properties.Length == 0 || state.ConsumerQuotas == null)
                return true;

            var consumerProperty = properties[KnownProperties.ConsumerIdKey];
            if (consumerProperty == null)
                return true;

            var consumed = GetIncrementedCount(consumerProperty.Value, state);
            var consumerId = consumerProperty.Value.Value;

            foreach (var quota in state.ConsumerQuotas)
            {
                if (!quota.Allows(consumerId, consumed, state.CapacityLimit))
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<string, ConsumedByValue> GetConsumed(Property[] properties, ThrottlingState state)
        {
            var consumed = new Dictionary<string, ConsumedByValue>(properties.Length);

            for (var index = 0; index < properties.Length; index++)
            {
                var property = properties[index];
                consumed[property.Key] = new ConsumedByValue(property.Value, GetIncrementedCount(property, state));
            }

            return consumed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIncrementedCount(Property property, ThrottlingState state)
        {
            var propertyCounter = state.GetPropertyCounter(property.Key);
            var currentValue = propertyCounter.TryGetValue(property.Value, out var consumerCounter)
                ? consumerCounter.Value
                : 0;
            return currentValue + 1;
        }
    }
}