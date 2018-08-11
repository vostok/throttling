using System.Threading;
using BenchmarkDotNet.Attributes;

namespace Vostok.Throttling.Tests.Benchmarks
{
    public class FuzzyLifoSemaphoreBenchmark
    {
        private readonly SemaphoreSlim slimSemaphore;
        private readonly LockFreeLifoSemaphore lockFreeSemaphore;

        public FuzzyLifoSemaphoreBenchmark()
        {
            slimSemaphore = new SemaphoreSlim(50, 50);
            lockFreeSemaphore = new LockFreeLifoSemaphore(50);
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
            lockFreeSemaphore.WaitAsync().GetAwaiter().GetResult();
            lockFreeSemaphore.Release();
        }
    }
}