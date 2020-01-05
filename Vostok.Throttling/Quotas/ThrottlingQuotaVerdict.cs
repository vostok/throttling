using JetBrains.Annotations;

namespace Vostok.Throttling.Quotas
{
    [PublicAPI]
    public struct ThrottlingQuotaVerdict
    {
        private ThrottlingQuotaVerdict(bool allowed, string rejectionReason)
        {
            Allowed = allowed;
            RejectionReason = rejectionReason;
        }

        public static ThrottlingQuotaVerdict Allow()
            => new ThrottlingQuotaVerdict(true, null);

        public static ThrottlingQuotaVerdict Reject([CanBeNull] string reason)
            => new ThrottlingQuotaVerdict(false, reason);

        public readonly bool Allowed;

        [CanBeNull]
        public readonly string RejectionReason;
    }
}
