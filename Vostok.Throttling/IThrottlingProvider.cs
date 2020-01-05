using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vostok.Throttling
{
    /// <summary>
    /// <para>Provides request throttling based on parallelism limiting.</para>
    /// <para>Does not support rate limiting.</para>
    /// <para>Supports quoting and queueing.</para>
    /// </summary>
    [PublicAPI]
    public interface IThrottlingProvider : IObservable<IThrottlingEvent>
    {
        /// <summary>
        /// <para>Attempts to determine whether a request with given <paramref name="properties"/> and <paramref name="deadline"/> can be executed with configured parallelism limits.</para>
        /// <para>May wait asynchronously if the request is placed to a queue by the implementation.</para>
        /// <para>Check returned result's <see cref="IThrottlingResult.Status"/> to see whether to continue with the request.</para>
        /// <para>Always dispose of successful <see cref="IThrottlingResult"/>s after executing the request.</para>
        /// <para>See <see cref="WellKnownThrottlingProperties"/> for the keys of some well-known request properties.</para>
        /// </summary>
        [ItemNotNull]
        Task<IThrottlingResult> ThrottleAsync([CanBeNull] IReadOnlyDictionary<string, string> properties, [CanBeNull] TimeSpan? deadline);
    }
}
