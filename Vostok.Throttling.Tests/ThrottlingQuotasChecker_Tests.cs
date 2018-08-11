using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Commons.Threading;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests
{
    [TestFixture]
    internal class ThrottlingQuotasChecker_Tests
    {
        private string consumerId;
        private ThrottlingPriority priority;
        private ThrottlingState state;
        private ThrottlingQuotasChecker checker;

        [SetUp]
        public void TestSetup()
        {
            consumerId = Guid.NewGuid().ToString();
            priority = ThrottlingPriority.Sheddable;

            state = new ThrottlingState
            {
                CapacityLimit = 10,
                ExternalQuotas = new[]
                {
                    Substitute.For<IThrottlingExternalQuota>(),
                    Substitute.For<IThrottlingExternalQuota>()
                },
                ConsumerQuotas = new[]
                {
                    Substitute.For<IThrottlingConsumerQuota>(),
                    Substitute.For<IThrottlingConsumerQuota>()
                },
                PriorityQuotas = new[]
                {
                    Substitute.For<IThrottlingPriorityQuota>(),
                    Substitute.For<IThrottlingPriorityQuota>()
                },
                PropertiesQuotas = new[]
                {
                    Substitute.For<IThrottlingPropertiesQuota>(),
                    Substitute.For<IThrottlingPropertiesQuota>()
                }
            };

            foreach (var quota in state.PropertiesQuotas)
                quota.Allows(null, 0).ReturnsForAnyArgs(true);

            foreach (var quota in state.ExternalQuotas)
                quota.Allows().Returns(true);

            foreach (var quota in state.ConsumerQuotas)
                quota.Allows(null, 0, 0).ReturnsForAnyArgs(true);

            foreach (var quota in state.PriorityQuotas)
                quota.Allows(ThrottlingPriority.Ordinary, 0, 0).ReturnsForAnyArgs(true);

            checker = new ThrottlingQuotasChecker();
        }

        [Test]
        public void Should_return_passed_result_when_all_quotas_allow_to_pass()
        {
            Check().Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void Should_check_all_external_quotas()
        {
            Check();

            foreach (var quota in state.ExternalQuotas)
            {
                quota.Received(1).Allows();
            }
        }

        [Test]
        public void Should_handle_null_external_quotas_array()
        {
            state.ExternalQuotas = null;

            Check().Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void Should_return_rejected_status_when_any_of_external_quotas_does_not_allow_to_pass()
        {
            state.ExternalQuotas[1].Allows().Returns(false);

            Check().Should().Be(ThrottlingStatus.RejectedDueToExternalQuota);
        }

        [Test]
        public void Should_check_all_consumer_quotas_with_correct_parameters_when_there_is_no_consumer_counter_yet()
        {
            Check();

            foreach (var quota in state.ConsumerQuotas)
            {
                quota.Received(1).Allows(consumerId, 1, 10);
            }
        }

        [Test]
        public void Should_check_all_consumer_quotas_with_correct_parameters_when_there_is_a_consumer_counter()
        {
            state.GetPropertyCounter(KnownProperties.ConsumerIdKey)[consumerId] = new AtomicInt(5);

            Check();

            foreach (var quota in state.ConsumerQuotas)
            {
                quota.Received(1).Allows(consumerId, 6, 10);
            }
        }

        [Test]
        public void Should_handle_null_consumer_quotas_array()
        {
            state.ConsumerQuotas = null;

            Check().Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void Should_handle_null_property_quotas_array()
        {
            state.PropertiesQuotas = null;

            Check().Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void Should_return_rejected_status_when_any_of_consumer_quotas_does_not_allow_to_pass()
        {
            state.ConsumerQuotas[1].Allows(null, 0, 0).ReturnsForAnyArgs(false);

            Check().Should().Be(ThrottlingStatus.RejectedDueToConsumerQuota);
        }

        [Test]
        public void Should_check_all_priority_quotas()
        {
            state.PriorityCounters[priority].Value = 5;

            Check();

            foreach (var quota in state.PriorityQuotas)
            {
                quota.Received(1).Allows(priority, 6, 10);
            }
        }

        [Test]
        public void Should_handle_null_priority_quotas_array()
        {
            state.PriorityQuotas = null;

            Check().Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void Should_return_rejected_status_when_any_of_priority_quotas_does_not_allow_to_pass()
        {
            state.PriorityQuotas[1].Allows(priority, 0, 0).ReturnsForAnyArgs(false);

            Check().Should().Be(ThrottlingStatus.RejectedDueToPriorityQuota);
        }

        [Test]
        public void Should_pass_if_null_properties()
        {
            state.PropertiesQuotas[0].Allows(null,  0).ReturnsForAnyArgs(false);

            checker.Check(state, new ThrottlingProperties(), priority).Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void Should_pass_if_empty_properties()
        {
            state.PropertiesQuotas[0].Allows(null, 0).ReturnsForAnyArgs(false);
            checker.Check(state, new ThrottlingProperties(new Property[0]), priority).Should().Be(ThrottlingStatus.Passed);
        }

        [Test]
        public void Should_pass_if_no_properties_and_consumer_quotas()
        {
            state.PropertiesQuotas = null;
            state.ConsumerQuotas = null;

            checker.Check(state, new ThrottlingProperties(new Property("a", "b")), priority).Should().Be(ThrottlingStatus.Passed);
        }


        [Test]
        public void Should_return_rejected_status_when_any_of_propertyquotas_does_not_allow_to_pass()
        {
            var consumed = new Dictionary<string, ConsumedByValue>() {{"a", new ConsumedByValue("b", 6)}};
            SetCounter("a", 5, consumed);

            state.PropertiesQuotas[1].Allows(null, 0).ReturnsForAnyArgs(false);

            checker.Check(state, ThrottlingProperties(consumed), priority).Should().Be(ThrottlingStatus.RejectedDueToConsumerQuota);

            state.PropertiesQuotas[0].Received().Allows(consumed.MatchEquivalentTo(), state.CapacityLimit);
            state.PropertiesQuotas[1].Received().Allows(consumed.MatchEquivalentTo(), state.CapacityLimit);
        }

        [Test]
        public void Should_return_pass_correct_counters_to_propertyquotas()
        {
            var consumed = new Dictionary<string, ConsumedByValue>()
            {
                { "a", new ConsumedByValue("b", 6) },
                { "c", new ConsumedByValue("d", 51) },
            };
            SetCounter("a", 5, consumed);
            SetCounter("c", 50, consumed);

            checker.Check(state, ThrottlingProperties(consumed), priority).Should().Be(ThrottlingStatus.Passed);

            state.PropertiesQuotas[0].Received().Allows(consumed.MatchEquivalentTo(), state.CapacityLimit);
            state.PropertiesQuotas[1].Received().Allows(consumed.MatchEquivalentTo(), state.CapacityLimit);
        }

        [Test]
        public void Should_pass_last_duplicate_property_to_quotas()
        {
            var consumed = new Dictionary<string, ConsumedByValue>()
            {
                { "a", new ConsumedByValue("b", 6) },
            };
            SetCounter("a", 5, consumed);

            checker.Check(state, new ThrottlingProperties(new Property("a", "b_old"), new Property("a", "b")), priority).Should().Be(ThrottlingStatus.Passed);

            state.PropertiesQuotas[0].Received().Allows(consumed.MatchEquivalentTo(), state.CapacityLimit);
            state.PropertiesQuotas[1].Received().Allows(consumed.MatchEquivalentTo(), state.CapacityLimit);
        }

        private static ThrottlingProperties ThrottlingProperties(Dictionary<string, ConsumedByValue> d)
        {
            List< Property > properties = new List<Property>();
            foreach (var kvp in d)
                properties.Add(new Property(kvp.Key, kvp.Value.Value));
            return new ThrottlingProperties(properties.ToArray());
        }

        private void SetCounter(string key, int counter, Dictionary<string, ConsumedByValue> consumed)
        {
            state.GetPropertyCounter(key)[consumed[key].Value] = new AtomicInt(counter);
        }

        private ThrottlingStatus Check()
        {
            return checker.Check(state, new ThrottlingProperties(KnownProperties.ConsumerId(consumerId)), priority);
        }
    }
}