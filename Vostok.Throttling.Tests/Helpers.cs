using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Vostok.Throttling.Tests
{
    internal static class Helpers
    {
        private static bool Check<T>(T actual, T expected)
        {
            try
            {
                actual.ShouldBeEquivalentTo(expected);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static T MatchEquivalentTo<T>(this T expected)
        {
            return Arg.Is<T>(actual => Check(actual, expected));
        }

        public static void CompletesInTimeout(Action action, TimeSpan timeout)
        {
            var task = Task.Run(action);
            var timeoutTask = Task.Delay(timeout);
            var result = Task.WhenAny(task, timeoutTask);
            if (result == timeoutTask)
            {
                Assert.Fail("Timeout exceeded");
            }
        }
        
        
        public static void ShouldCompleteIn(this Task task, TimeSpan timeout)
        {
            task.Wait(timeout).Should().BeTrue();
        }

        public static void SetupThreadPool(int multiplier)
        {
            const int maximumThreads = 32767;
            var minimumThreads = Math.Min(Environment.ProcessorCount * multiplier, maximumThreads);
            
            ThreadPool.SetMaxThreads(maximumThreads, maximumThreads);

            ThreadPool.SetMinThreads(minimumThreads, minimumThreads);
            
        }
    }
}