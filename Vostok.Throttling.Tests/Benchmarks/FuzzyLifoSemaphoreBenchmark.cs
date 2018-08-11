using System.Threading;
using BenchmarkDotNet.Attributes;

namespace Vostok.Throttling.Tests.Benchmarks
{
    public class FuzzyLifoSemaphoreBenchmark
    {
        private readonly SemaphoreSlim slimSemaphore;
        private readonly FuzzyLifoSemaphore fuzzySemaphore;

        public FuzzyLifoSemaphoreBenchmark()
        {
            slimSemaphore = new SemaphoreSlim(50, 50);
            fuzzySemaphore = new FuzzyLifoSemaphore(50);
        }

        [Benchmark]
        public void Slim()
        {
            slimSemaphore.WaitAsync().GetAwaiter().GetResult();
            slimSemaphore.Release();
        }

        [Benchmark]
        public void Fuzzy()
        {
            fuzzySemaphore.WaitAsync().GetAwaiter().GetResult();
            fuzzySemaphore.Release();
        }
    }
}