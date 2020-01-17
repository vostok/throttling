using JetBrains.Annotations;

namespace Vostok.Throttling.Quotas
{
    [PublicAPI]
    public struct ThrottlingQuotaVerdict
    {
        public readonly bool Allowed;

        [CanBeNull]
        public readonly string RejectionReason;

        private ThrottlingQuotaVerdict(bool allowed, string rejectionReason)
        {
            Allowed = allowed;
            RejectionReason = rejectionReason;
        }

        public static ThrottlingQuotaVerdict Allow()
            => new ThrottlingQuotaVerdict(true, null);

        public static ThrottlingQuotaVerdict Reject([CanBeNull] string reason)
            => new ThrottlingQuotaVerdict(false, reason);
    }
}