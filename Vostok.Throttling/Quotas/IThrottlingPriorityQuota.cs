namespace Vostok.Throttling.Quotas
{
    public interface IThrottlingPriorityQuota
    {
        bool Allows(ThrottlingPriority priority, int consumed, int totalCapacity);
    }
}