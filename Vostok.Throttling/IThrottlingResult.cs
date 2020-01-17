using System;
using JetBrains.Annotations;

namespace Vostok.Throttling
{
    /// <summary>
    /// Represents the result of an attempt to pass throttling mechanisms.
    /// </summary>
    [PublicAPI]
    public interface IThrottlingResult : IDisposable
    {
        /// <summary>
        /// See <see cref="ThrottlingStatus"/>.
        /// </summary>
        ThrottlingStatus Status { get; }

        /// <summary>
        /// Total time spent waiting in the queue.
        /// </summary>
        TimeSpan WaitTime { get; }

        /// <summary>
        /// Details of the reason for request rejection. Only present when <see cref="Status"/> is not equal to <see cref="ThrottlingStatus.Passed"/>.
        /// </summary>
        [CanBeNull]
        string RejectionReason { get; }
    }
}