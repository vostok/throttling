using System;

namespace Vostok.Throttling.Results
{
    internal class DisabledResult : IThrottlingResult
    {
        public ThrottlingStatus Status => ThrottlingStatus.Passed;

        public TimeSpan WaitTime => TimeSpan.Zero;

        public string RejectionReason => null;

        public void Dispose()
        {
        }
    }
}