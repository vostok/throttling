using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Throttling.Quotas;

namespace Vostok.Throttling.Tests.Benchmarks
{
    [Ignore("")]
    [MemoryDiagnoser]
    public class ThrottlingProviderBenchmarks
    {
        private ThrottlingProvider providerForOld;
        private ThrottlingProvider providerForNew;

        [GlobalSetup]
        [SetUp]
        public void Setup()
        {
            providerForOld = new ThrottlingProvider(CreateConfig(null));
            providerForNew = new ThrottlingProvider(CreateConfig(new [] {new MaximumFractionForGivenPropertyAnyValueQuota("k1", 0.9), new MaximumFractionForGivenPropertyAnyValueQuota("k2", 0.9), }));
            using (providerForNew
                .ThrottleAsync(new ThrottlingProperties(new Property("k1", "v1"), new Property("k2", "v2")))
                .GetAwaiter().GetResult())
            {
            }
        }

        private static ThrottlingConfig CreateConfig(IThrottlingPropertiesQuota[] propertiesQuotas)
        {
            var config = new ThrottlingConfig();
            config.CapacityLimit = () => 100;
            config.Enabled = () => true;
            config.PriorityQuotas = () => new[] {new MaximumOrdinaryFractionQuota(0.99),};
            config.ExternalQuotas = () => new[] {new DropRequestsQuota(0.0)};
            config.ConsumerQuotas = () => new[] {new ConsumersWhitelistQuota(new HashSet<string> {"client"}),};
            config.PropertiesQuotas = () => propertiesQuotas;
            config.RefreshPeriod = -1.Ticks();
            return config;
        }

        [Benchmark]
        public void Test_OldMethod()
        {
            using (providerForOld.ThrottleAsync("client").GetAwaiter().GetResult())
            {
            }
        }

        [Benchmark]
        public void Test_NewMethod()
        {
            using (providerForNew
                .ThrottleAsync(new ThrottlingProperties(new Property("k1", "v1"), new Property("k2", "v2")))
                .GetAwaiter().GetResult())
            {
            }
        }

        [Test]
        public void TestRunbenchmarks()
        {
            BenchmarkRunnerCore.Run(BenchmarkConverter.TypeToBenchmarks(GetType()),
                job => new InProcessToolchain(false));
        }
        /* current:

         Method |     Mean |     Error |    StdDev |  Gen 0 | Allocated |
--------------- |---------:|----------:|----------:|-------:|----------:|
 Test_OldMethod | 378.1 ns | 11.947 ns | 11.175 ns | 0.0024 |     232 B |
 Test_NewMethod | 575.8 ns |  6.720 ns |  5.957 ns | 0.0057 |     496 B |
 */
        /*
        old benchmark (no properties):
         Method |     Mean |     Error |    StdDev |  Gen 0 | Allocated |
--------------- |---------:|----------:|----------:|-------:|----------:|
 Test_OldMethod | 255.0 ns | 0.7461 ns | 0.6979 ns | 0.0014 |     160 B |
 */
    }
}