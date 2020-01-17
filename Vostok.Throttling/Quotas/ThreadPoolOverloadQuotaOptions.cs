using JetBrains.Annotations;

namespace Vostok.Throttling.Quotas
{
    /// <summary>
    /// Configuration options of <see cref="ThreadPoolOverloadQuota"/>.
    /// </summary>
    [PublicAPI]
    public class ThreadPoolOverloadQuotaOptions
    {
        /// <summary>
        /// <para>Minimum duration in seconds for thread pool exhaustion in order to start rejecting requests.</para>
        /// <para>See <see cref="ThreadPoolOverloadQuota"/> for more details.</para>
        /// </summary>
        public int AllowedSecondsInExhaustion { get; set; } = 15;
    }
}