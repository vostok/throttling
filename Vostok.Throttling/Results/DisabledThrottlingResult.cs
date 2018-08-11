using System;

namespace Vostok.Throttling.Results
{
    internal class DisabledThrottlingResult : IThrottlingResult
    {
        public void Dispose()
        {
        }

        public ThrottlingStatus Status => ThrottlingStatus.Passed;

        public TimeSpan WaitTime => TimeSpan.Zero;
    }
}