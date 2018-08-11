namespace Vostok.Throttling
{
    public enum ThrottlingStatus
    {
        Passed,
        RejectedDueToDeadline,
        RejectedDueToFullQueue,
        RejectedDueToExternalQuota,
        RejectedDueToConsumerQuota,
        RejectedDueToPriorityQuota
    }
}