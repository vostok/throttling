using System;

namespace Vostok.Throttling.Results
{
    internal class FailedThrottlingResult : IThrottlingResult
    {
        public FailedThrottlingResult(ThrottlingStatus status, TimeSpan elapsed)
        {
            Status = status;
            WaitTime = elapsed;
        }

        public void Dispose()
        {
        }

        public ThrottlingStatus Status { get; }

        public TimeSpan WaitTime { get; }
    }
}
