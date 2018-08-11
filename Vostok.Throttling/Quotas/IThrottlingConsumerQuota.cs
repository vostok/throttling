namespace Vostok.Throttling.Quotas
{
    public interface IThrottlingConsumerQuota
    {
        bool Allows(string consumerId, int consumed, int totalCapacity);
    }
}