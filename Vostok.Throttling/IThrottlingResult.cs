using System;

namespace Vostok.Throttling
{
    public interface IThrottlingResult : IDisposable
    {
        ThrottlingStatus Status { get; }

        TimeSpan WaitTime { get; }
    }
}