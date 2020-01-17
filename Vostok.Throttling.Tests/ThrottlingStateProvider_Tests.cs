using System;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Throttling.Config;

namespace Vostok.Throttling.Tests
{
    [TestFixture]
    internal class ThrottlingStateProvider_Tests
    {
        private ThrottlingConfiguration configuration;
        private ThrottlingState state;
        private ThrottlingEssentials essentials;
        private IThrottlingStateActualizer actualizer;
        private ThrottlingStateProvider provider;

        [SetUp]
        public void TestSetup()
        {
            essentials = new ThrottlingEssentials {RefreshPeriod = TimeSpan.Zero};

            configuration = new ThrottlingConfigurationBuilder()
                .SetEssentials(() => essentials)
                .Build();

            actualizer = Substitute.For<IThrottlingStateActualizer>();
            provider = new ThrottlingStateProvider(configuration, actualizer);

            state = provider.ObtainState();

            actualizer.ClearReceivedCalls();
        }

        [Test]
        public void Should_always_return_the_same_state()
        {
            for (var i = 0; i < 100; i++)
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
            state.IsActual = false;
            state.RefreshPeriod = 200.Milliseconds();

            provider.ObtainState();

            state.IsActual = true;

            for (var i = 0; i < 100; i++)
            {
                if (!state.IsActual)
                    Assert.Pass();

                Thread.Sleep(10.Milliseconds());
            }

            Assert.Fail("State must become inactual in 1 second, which it clearly didn't do.");
        }

        [Test]
        public void Should_rethrow_exceptions_on_first_actualization()
        {
            configuration.ErrorCallback = _ => {};

            provider = new ThrottlingStateProvider(configuration, actualizer);

            actualizer.When(a => a.Actualize(Arg.Any<ThrottlingState>())).Throw(new Exception("lalala"));

            Action action = () => provider.ObtainState();

            action.Should().Throw<Exception>();
        }

        [Test]
        public void Should_rethrow_exceptions_when_there_is_no_error_callback()
        {
            state.IsActual = false;

            actualizer.When(a => a.Actualize(Arg.Any<ThrottlingState>())).Throw(new Exception("lalala"));

            Action action = () => provider.ObtainState();

            action.Should().Throw<Exception>();
        }

        [Test]
        public void Should_report_exceptions_in_regular_actualizations_to_error_callback()
        {
            var error = new Exception("lalala");

            configuration.ErrorCallback = Substitute.For<Action<Exception>>();

            state.IsActual = false;

            actualizer.When(a => a.Actualize(Arg.Any<ThrottlingState>())).Throw(error);

            provider.ObtainState();

            configuration.ErrorCallback.Received(1).Invoke(error);
        }
    }
}