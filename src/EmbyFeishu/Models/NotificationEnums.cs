namespace EmbyFeishu.Models
{
    /// <summary>
    /// 通知事件分类
    /// </summary>
    public enum NotificationCategory
    {
        Playback,
        Authentication,
        Session,
        Library,
        UserActivity,
        UserManagement,
        ScheduledTask,
        LiveTv,
        Server
    }

    /// <summary>
    /// 通知严重程度，用于卡片配色和限流豁免判断
    /// </summary>
    public enum NotificationSeverity
    {
        Information,
        Success,
        Warning,
        Error,
        Security
    }

    /// <summary>
    /// 统一的内部事件类型（跨所有分类）
    /// </summary>
    public enum NotificationEventType
    {
        // 播放
        PlaybackStarted,
        PlaybackPaused,
        PlaybackResumed,
        PlaybackStopped,
        PlaybackCompleted,
        PlaybackAbandoned,
        PlaybackMethodChanged,
        PlaybackMilestone,

        // 认证与会话
        AuthenticationSucceeded,
        AuthenticationFailed,
        SessionStarted,
        SessionEnded,
        RemoteControlDisconnected,
        PartyJoined,
        PartyLeft,

        // 用户管理
        UserLockedOut,
        UserCreated,
        UserDeleted,
        UserUpdated,
        UserPasswordChanged,
        UserPolicyUpdated,
        UserConfigurationUpdated,

        // 媒体库
        ItemAdded,
        ItemUpdated,
        ItemRemoved,
        LibraryAggregated,

        // 用户行为（用户媒体数据）
        FavoriteAdded,
        FavoriteRemoved,
        MarkedPlayed,
        MarkedUnplayed,
        UserRatingChanged,

        // 计划任务
        TaskCompleted,
        TaskFailed,
        TaskCancelled,
        LibraryScanStarted,
        LibraryScanCompleted,
        MetadataRefreshCompleted,
        BackupCompleted,

        // Live TV
        RecordingStarted,
        RecordingEnded,
        TimerCreated,
        TimerUpdated,
        TimerCancelled,
        SeriesTimerCreated,
        SeriesTimerUpdated,
        SeriesTimerCancelled,

        // 服务器
        ServerStarted,
        ServerStopping,
        UpdateAvailable,
        ApplicationUpdated,
        RestartRequired,
        MaintenanceModeEntered,
        MaintenanceModeExited
    }

    /// <summary>
    /// 飞书消息格式
    /// </summary>
    public enum MessageFormat
    {
        /// <summary>纯文本（默认，保持升级前外观）</summary>
        Text,
        /// <summary>飞书交互卡片</summary>
        FeishuCard
    }

    /// <summary>
    /// 消息详细程度
    /// </summary>
    public enum MessageDetailLevel
    {
        /// <summary>仅标题与最重要的一个字段</summary>
        Simple,
        /// <summary>标准字段</summary>
        Standard,
        /// <summary>包含技术细节</summary>
        Detailed,
        /// <summary>自定义：播放事件沿用旧的字段开关，其余按标准</summary>
        Custom
    }

    /// <summary>
    /// IP 地址显示方式
    /// </summary>
    public enum IpAddressDisplayMode
    {
        Hidden,
        Masked,
        Full
    }

    /// <summary>
    /// 设备 ID 显示方式
    /// </summary>
    public enum DeviceIdDisplayMode
    {
        Hidden,
        Masked,
        Full
    }
}
