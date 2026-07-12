using System.Threading;
using EmbyFeishu.Models;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 通知策略实现：先去重，再限流。安全事件（Severity=Security）可按配置豁免限流，
    /// 但仍受去重约束，避免同一攻击产生无限通知。
    /// </summary>
    public class NotificationPolicy : INotificationPolicy
    {
        private readonly DeduplicationCache _dedup = new DeduplicationCache();
        private readonly SlidingWindowRateLimiter _rateLimiter = new SlidingWindowRateLimiter();
        private int _suppressedCount;

        public PolicyDecision Evaluate(NotificationEvent evt, PluginOptions options)
        {
            if (evt == null)
                return PolicyDecision.SuppressedDuplicate;

            // 1) 去重
            if (!string.IsNullOrEmpty(evt.DeduplicationKey) && evt.DedupWindowSeconds > 0)
            {
                if (!_dedup.TryMark(evt.DeduplicationKey, evt.DedupWindowSeconds))
                    return PolicyDecision.SuppressedDuplicate;
            }

            // 2) 限流
            var isSecurity = evt.Severity == NotificationSeverity.Security;
            if (isSecurity && options.SecurityEventsBypassRateLimit)
            {
                return PolicyDecision.Send;
            }

            if (!_rateLimiter.Allow(options.MaximumNotificationsPerMinute))
            {
                if (options.AggregateWhenRateLimited)
                {
                    Interlocked.Increment(ref _suppressedCount);
                }
                return PolicyDecision.SuppressedRateLimit;
            }

            return PolicyDecision.Send;
        }

        public int DrainSuppressedCount()
        {
            return Interlocked.Exchange(ref _suppressedCount, 0);
        }

        public void CleanupStale()
        {
            _dedup.CleanupStale();
        }
    }
}
