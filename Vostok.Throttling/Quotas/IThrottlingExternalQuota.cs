namespace Vostok.Throttling.Quotas
{
    public interface IThrottlingExternalQuota
    {
        bool Allows();
    }
}