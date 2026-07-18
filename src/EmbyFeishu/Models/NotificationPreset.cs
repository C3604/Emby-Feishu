namespace EmbyFeishu.Models
{
    /// <summary>
    /// 通知配置预设模板
    /// </summary>
    public enum NotificationPreset
    {
        /// <summary>不应用预设（默认）</summary>
        None,
        /// <summary>谨慎模式：仅安全与关键事件</summary>
        Conservative,
        /// <summary>标准模式：播放 + 安全 + 服务器</summary>
        Standard,
        /// <summary>全量模式：所有 52 种事件</summary>
        Full,
        /// <summary>仅播放模式：只监控播放相关事件</summary>
        PlaybackOnly
    }
}
