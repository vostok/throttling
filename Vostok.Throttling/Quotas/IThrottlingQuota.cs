using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Throttling.Quotas
{
    /// <summary>
    /// <para>Throttling quotas are primarily used to divide total capacity (parallelism limit) between different consumers according to request properties.</para>
    /// <para>They can also be used to reject requests when some kind of external/system resource is exhausted (CPU, memory, thread pool).</para>
    /// <para>See <see cref="PropertyQuota"/> for an example of per-property quota.</para>
    /// <para>See <see cref="ThreadPoolOverloadQuota"/> for an example of external resource quota.</para>
    /// </summary>
    [PublicAPI]
    public interface IThrottlingQuota
    {
        /// <summary>
        /// Determines whether a request with given <paramref name="properties"/> can pass.
        /// </summary>
        ThrottlingQuotaVerdict Check([NotNull] IReadOnlyDictionary<string, string> properties, [NotNull] IThrottlingQuotaContext context);
    }
}