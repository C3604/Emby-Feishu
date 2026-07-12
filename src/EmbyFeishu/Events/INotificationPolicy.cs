using EmbyFeishu.Models;

namespace EmbyFeishu.Events
{
    /// <summary>策略判定结果</summary>
    public enum PolicyDecision
    {
        /// <summary>允许发送</summary>
        Send,
        /// <summary>因去重被抑制</summary>
        SuppressedDuplicate,
        /// <summary>因限流被抑制</summary>
        SuppressedRateLimit
    }

    /// <summary>
    /// 通知策略：去重、限流、聚合协调。
    /// </summary>
    public interface INotificationPolicy
    {
        /// <summary>评估事件是否应发送</summary>
        PolicyDecision Evaluate(NotificationEvent evt, PluginOptions options);

        /// <summary>取出并清零被限流抑制的计数（用于生成限流汇总）</summary>
        int DrainSuppressedCount();

        /// <summary>清理过期缓存</summary>
        void CleanupStale();
    }
}
