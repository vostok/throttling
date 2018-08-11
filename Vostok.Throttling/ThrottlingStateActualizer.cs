using System;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable 4014

namespace Vostok.Throttling
{
    internal class ThrottlingStateActualizer : IThrottlingStateActualizer
    {
        public ThrottlingStateActualizer(ThrottlingConfig config)
        {
            if (config.CapacityLimit == null)
                throw new ArgumentNullException(nameof(config.CapacityLimit));

            if (config.QueueLimit == null)
                throw new ArgumentNullException(nameof(config.QueueLimit));

            if (config.Enabled == null)
                throw new ArgumentNullException(nameof(config.Enabled));

            if (config.ExternalQuotas == null)
                throw new ArgumentNullException(nameof(config.ExternalQuotas));

            if (config.ConsumerQuotas == null)
                throw new ArgumentNullException(nameof(config.ConsumerQuotas));

            if (config.PropertiesQuotas == null)
                throw new ArgumentNullException(nameof(config.PropertiesQuotas));

            this.config = config;
        }

        public void Actualize(ThrottlingState state)
        {
            var oldCapacity = state.CapacityLimit;

            state.Enabled = config.Enabled();
            state.CapacityLimit = Math.Max(1, config.CapacityLimit());
            state.QueueLimit = Math.Max(0, config.QueueLimit());
            state.ExternalQuotas = config.ExternalQuotas()?.ToArray();
            state.ConsumerQuotas = config.ConsumerQuotas()?.ToArray();
            state.PriorityQuotas = config.PriorityQuotas()?.ToArray();
            state.PropertiesQuotas = config.PropertiesQuotas()?.ToArray();

            CleanupPropertyCounters(state);

            AdjustSemaphore(state, oldCapacity);

            state.IsActual = true;
        }

        // (iloktionov): Из-за состояния гонки этот механизм очистки вполне может выкинуть счётчик с ненулевым значением.
        // (iloktionov): Это приводит лишь к кратковременной (в течение выполнения запроса) аномалии в метриках, и не может приводить к отрицательным значениям.
        // (iloktionov): Гарантией этому служит вызов декремента на том же объекте счётчика, на котором был вызван инкремент.
        private static void CleanupPropertyCounters(ThrottlingState state)
        {
            foreach (var propertyCounters in state.PropertyCounters.Select(pair => pair.Value))
            foreach (var pair in propertyCounters.Where(pair => pair.Value == 0))
            {
                propertyCounters.TryRemove(pair.Key, out _);
            }
        }

        // (iloktionov): Если на лету меняется ёмкость троттлинга, необходимо тюнить семафор.
        // (iloktionov): Когда ёмкость увеличивается, достаточно мгновенно вернуть в него разность между новым и старым значением.
        // (iloktionov): Когда ёмкость уменьшается, следует "забрать" разность между старым и новым значением.
        // (iloktionov): Поскольку процесс "отбора" может затянуться из-за занятости семафора, его лучше выполнять асинхронно.
        private static void AdjustSemaphore(ThrottlingState state, int oldCapacity)
        {
            var difference = state.CapacityLimit - oldCapacity;
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

        private static async Task DrainSemaphore(FuzzyLifoSemaphore semaphore, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                await semaphore.WaitAsync();
            }
        }

        private readonly ThrottlingConfig config;
    }
}
