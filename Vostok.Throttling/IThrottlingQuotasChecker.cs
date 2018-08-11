using JetBrains.Annotations;

namespace Vostok.Throttling
{
    internal interface IThrottlingQuotasChecker
    {
        ThrottlingStatus Check([NotNull] ThrottlingState state, ThrottlingProperties properties, ThrottlingPriority priority);
    }
}