using System;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;

namespace Vostok.Throttling.Tests
{
    [TestFixture]
    internal class ThrottlingStateProvider_Tests
    {
        private ThrottlingConfig config;
        private ThrottlingState state;
        private IThrottlingStateActualizer actualizer;
        private ThrottlingStateProvider provider;

        [SetUp]
        public void TestSetup()
        {
            config = new ThrottlingConfig
            {
                RefreshPeriod = TimeSpan.Zero,
            };

            actualizer = Substitute.For<IThrottlingStateActualizer>();
            provider = new ThrottlingStateProvider(config, actualizer);

            state = provider.ObtainState();
            
            actualizer.ClearReceivedCalls();
        }

        [Test]
        public void Should_always_return_the_same_state()
        {
            provider.ObtainState().Should().BeSameAs(state);
        }

        [Test]
        public void Should_actualize_inactual_state()
        {
            state.IsActual = false;

            provider.ObtainState();

            actualizer.Received(1).Actualize(state);
        }

        [Test]
        public void Should_not_actualize_actual_state()
        {
            state.IsActual = true;

            provider.ObtainState();

            actualizer.ReceivedCalls().Should().BeEmpty();
        }

        [Test]
        public void Should_make_state_inactual_after_refresh_period()
        {
            config.RefreshPeriod = 200.Milliseconds();

            state.IsActual = false;

            provider.ObtainState();

            state.IsActual = true;

            for (int i = 0; i < 50; i++)
            {
                if (!state.IsActual)
                    Assert.Pass();

                Thread.Sleep(10.Milliseconds());
            }

            Assert.Fail("State must become inactual in 500ms, which it clearly didn't do.");
        }
    }
}