using System;
using JetBrains.Annotations;

namespace Vostok.Throttling.Results
{
    internal class FailedResult : IThrottlingResult
    {
        public FailedResult(ThrottlingStatus status, TimeSpan waitTime, [CanBeNull] string rejectionReason)
        {
            Status = status;
            WaitTime = waitTime;
            RejectionReason = rejectionReason;
        }

        public ThrottlingStatus Status { get; }
        
        public TimeSpan WaitTime { get; }

        public string RejectionReason { get; }

        public void Dispose()
        {
        }
    }
}
