using System;
using System.Threading.Tasks;
using Vostok.Throttling.Config;

namespace Vostok.Throttling
{
    internal class ThrottlingStateProvider : IThrottlingStateProvider
    {
        private readonly ThrottlingConfiguration configuration;
        private readonly ThrottlingState state;
        private readonly IThrottlingStateActualizer actualizer;
        private readonly object lockObject = new object();

        private volatile bool actualizedOnce;

        public ThrottlingStateProvider(ThrottlingConfiguration configuration)
            : this(configuration, new ThrottlingStateActualizer(configuration))
        {
        }

        public ThrottlingStateProvider(ThrottlingConfiguration configuration, IThrottlingStateActualizer actualizer)
        {
            this.configuration = configuration;
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
                        try
                        {
                            actualizer.Actualize(state);
                            actualizedOnce = true;
                        }
                        catch (Exception error)
                        {
                            if (!actualizedOnce || configuration.ErrorCallback == null)
                                throw;

                            configuration.ErrorCallback.Invoke(error);
                        }

                        ScheduleNextActualization();
                    }
                }
            }

            return state;
        }

        private void ScheduleNextActualization()
        {
            var refreshPeriod = state.RefreshPeriod;
            if (refreshPeriod >= TimeSpan.Zero)
            {
                Task.Delay(refreshPeriod).ContinueWith(_ => state.IsActual = false);
            }
        }
    }
}