using System;
using System.Threading.Tasks;

namespace Vostok.Throttling
{
    internal class ThrottlingStateProvider : IThrottlingStateProvider
    {
        public ThrottlingStateProvider(ThrottlingConfig config)
            : this(config, new ThrottlingStateActualizer(config))
        {
        }

        public ThrottlingStateProvider(ThrottlingConfig config, IThrottlingStateActualizer actualizer)
        {
            this.config = config;
            this.actualizer = actualizer;

            state = new ThrottlingState();
        }

        public ThrottlingState ObtainState()
        {
            if (!state.IsActual)
            {
                lock (lockObject)
                {
                    if (!state.IsActual)
                    {
                        actualizer.Actualize(state);

                        ScheduleNextActualization();
                    }
                }
            }

            return state;
        }

        // (iloktionov): В случае полностью забитого тредпула эта штука не отработает вовремя, но этим можно пожертвовать.
        // (iloktionov): Идеальное соблюдение периода обновления лимитов не является приоритетной задачей.
        private void ScheduleNextActualization()
        {
            var refreshPeriod = config.RefreshPeriod;
            if (refreshPeriod >= TimeSpan.Zero)
            {
                Task.Delay(refreshPeriod).ContinueWith(_ => state.IsActual = false);
            }
        }

        private readonly ThrottlingConfig config;
        private readonly ThrottlingState state;
        private readonly IThrottlingStateActualizer actualizer;
        private readonly object lockObject = new object();
    }
}
