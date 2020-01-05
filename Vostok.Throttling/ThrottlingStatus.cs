using JetBrains.Annotations;

namespace Vostok.Throttling
{
    /// <summary>
    /// Represents the ultimate outcome of an attempt to pass throttling mechanisms.
    /// </summary>
    [PublicAPI]
    public enum ThrottlingStatus
    {
        /// <summary>
        /// Request can be executed: calling code should proceed and dispose <see cref="IThrottlingResult"/> afterwards.
        /// </summary>
        Passed,

        /// <summary>
        /// Request can't be executed: it's deadline had already expired when it was dequeued.
        /// </summary>
        RejectedDueToDeadline,

        /// <summary>
        /// Request can't be executed: it couldn't be placed in the queue due to overflow.
        /// </summary>
        RejectedDueToQueue,

        /// <summary>
        /// Request can't be executed: it was rejected by one of the configured quotas.
        /// </summary>
        RejectedDueToQuota
    }
}
