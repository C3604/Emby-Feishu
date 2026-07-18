namespace EmbyFeishu.Configuration
{
    /// <summary>
    /// 配置同步器：在 PluginOptions 的扁平字段与分组对象之间双向同步。
    /// SyncToGroups、SyncFromGroups、ConfigMigrator 均委托此类，消除重复代码。
    /// 新增配置字段时只需在此文件中添加一行，无需多处修改。
    /// </summary>
    internal static class ConfigSynchronizer
    {
        /// <summary>
        /// 扁平字段 → 分组对象。用于旧配置迁移和从扁平字段恢复分组。
        /// </summary>
        public static void CopyToGroups(PluginOptions o)
        {
            o.EnsureGroups();

            // 飞书连接
            o.FeishuConnection.Enabled = o.Enabled;
            o.FeishuConnection.WebhookUrl = o.WebhookUrl ?? "";
            o.FeishuConnection.RequestTimeoutSeconds = o.RequestTimeoutSeconds;

            // 机器人安全
            o.BotSecurity.EnableCustomKeyword = o.EnableCustomKeyword;
            o.BotSecurity.CustomKeyword = o.CustomKeyword ?? "";
            o.BotSecurity.EnableSignatureVerification = o.EnableSignatureVerification;
            o.BotSecurity.SignatureSecretStatus = !string.IsNullOrEmpty(o.SignatureSecret) ? "已配置" : "未配置";

            // 消息显示
            o.MessageDisplay.MessageFormat = o.MessageFormat;
            o.MessageDisplay.MessageDetailLevel = o.MessageDetailLevel;
            o.MessageDisplay.ShowServerName = o.ShowServerName;
            o.MessageDisplay.ShowEventTime = o.ShowEventTime;
            o.MessageDisplay.IpAddressDisplayMode = o.IpAddressDisplayMode;
            o.MessageDisplay.DeviceIdDisplayMode = o.DeviceIdDisplayMode;
            o.MessageDisplay.FallbackToTextOnCardFailure = o.FallbackToTextOnCardFailure;
            o.MessageDisplay.ShowSensitiveTechnicalDetails = o.ShowSensitiveTechnicalDetails;
            o.MessageDisplay.IncludeUserName = o.IncludeUserName;
            o.MessageDisplay.IncludeMediaTitle = o.IncludeMediaTitle;
            o.MessageDisplay.IncludeMediaType = o.IncludeMediaType;
            o.MessageDisplay.IncludeSeriesEpisode = o.IncludeSeriesEpisode;
            o.MessageDisplay.IncludeClientName = o.IncludeClientName;
            o.MessageDisplay.IncludeDeviceName = o.IncludeDeviceName;
            o.MessageDisplay.IncludePlaybackPosition = o.IncludePlaybackPosition;
            o.MessageDisplay.IncludePlayedToCompletion = o.IncludePlayedToCompletion;

            // 播放通知
            o.PlaybackNotification.NotifyPlaybackStarted = o.NotifyPlaybackStarted;
            o.PlaybackNotification.NotifyPlaybackStopped = o.NotifyPlaybackStopped;
            o.PlaybackNotification.NotifyPlaybackPaused = o.NotifyPlaybackPaused;
            o.PlaybackNotification.NotifyPlaybackResumed = o.NotifyPlaybackResumed;
            o.PlaybackNotification.NotifyPlaybackCompleted = o.NotifyPlaybackCompleted;
            o.PlaybackNotification.NotifyPlaybackAbandoned = o.NotifyPlaybackAbandoned;
            o.PlaybackNotification.NotifyPlaybackMethodChanged = o.NotifyPlaybackMethodChanged;
            o.PlaybackNotification.NotifyPlaybackMilestones = o.NotifyPlaybackMilestones;
            o.PlaybackNotification.PlaybackMilestones = o.PlaybackMilestones ?? "25,50,75";
            o.PlaybackNotification.MinimumStopSeconds = o.MinimumStopSeconds;
            o.PlaybackNotification.CompletionThresholdPercent = o.CompletionThresholdPercent;
            o.PlaybackNotification.OnlyVideo = o.OnlyVideo;

            // 登录与用户
            o.LoginAndUser.NotifyAuthenticationSucceeded = o.NotifyAuthenticationSucceeded;
            o.LoginAndUser.NotifyAuthenticationFailed = o.NotifyAuthenticationFailed;
            o.LoginAndUser.NotifyUserLockedOut = o.NotifyUserLockedOut;
            o.LoginAndUser.NotifySessionStarted = o.NotifySessionStarted;
            o.LoginAndUser.NotifySessionEnded = o.NotifySessionEnded;
            o.LoginAndUser.NotifyRemoteControlDisconnected = o.NotifyRemoteControlDisconnected;
            o.LoginAndUser.NotifyPartyJoined = o.NotifyPartyJoined;
            o.LoginAndUser.NotifyPartyLeft = o.NotifyPartyLeft;
            o.LoginAndUser.NotifyUserPasswordChanged = o.NotifyUserPasswordChanged;
            o.LoginAndUser.NotifyUserCreated = o.NotifyUserCreated;
            o.LoginAndUser.NotifyUserDeleted = o.NotifyUserDeleted;
            o.LoginAndUser.NotifyUserUpdated = o.NotifyUserUpdated;
            o.LoginAndUser.NotifyUserPolicyUpdated = o.NotifyUserPolicyUpdated;
            o.LoginAndUser.NotifyUserConfigurationUpdated = o.NotifyUserConfigurationUpdated;
            o.LoginAndUser.UserFilterMode = o.UserFilterMode;
            o.LoginAndUser.UserNames = o.UserNames ?? "";

            // 媒体库与用户行为
            o.LibraryAndUserBehavior.NotifyNewMovies = o.NotifyNewMovies;
            o.LibraryAndUserBehavior.NotifyNewEpisodes = o.NotifyNewEpisodes;
            o.LibraryAndUserBehavior.NotifyNewMusic = o.NotifyNewMusic;
            o.LibraryAndUserBehavior.NotifyOtherNewItems = o.NotifyOtherNewItems;
            o.LibraryAndUserBehavior.NotifyItemsRemoved = o.NotifyItemsRemoved;
            o.LibraryAndUserBehavior.NotifyItemsUpdated = o.NotifyItemsUpdated;
            o.LibraryAndUserBehavior.EnableLibraryAggregation = o.EnableLibraryAggregation;
            o.LibraryAndUserBehavior.LibraryAggregationWindowSeconds = o.LibraryAggregationWindowSeconds;
            o.LibraryAndUserBehavior.MaximumIndividualLibraryMessages = o.MaximumIndividualLibraryMessages;
            o.LibraryAndUserBehavior.NotifyFavoriteAdded = o.NotifyFavoriteAdded;
            o.LibraryAndUserBehavior.NotifyFavoriteRemoved = o.NotifyFavoriteRemoved;
            o.LibraryAndUserBehavior.NotifyMarkedPlayed = o.NotifyMarkedPlayed;
            o.LibraryAndUserBehavior.NotifyMarkedUnplayed = o.NotifyMarkedUnplayed;
            o.LibraryAndUserBehavior.NotifyUserRatingChanged = o.NotifyUserRatingChanged;

            // 任务、Live TV 与服务器
            o.TaskAndLiveTvAndServer.NotifyTaskFailed = o.NotifyTaskFailed;
            o.TaskAndLiveTvAndServer.NotifyTaskCompleted = o.NotifyTaskCompleted;
            o.TaskAndLiveTvAndServer.NotifyTaskCancelled = o.NotifyTaskCancelled;
            o.TaskAndLiveTvAndServer.NotifyLibraryScanStarted = o.NotifyLibraryScanStarted;
            o.TaskAndLiveTvAndServer.NotifyLibraryScanCompleted = o.NotifyLibraryScanCompleted;
            o.TaskAndLiveTvAndServer.NotifyMetadataRefreshCompleted = o.NotifyMetadataRefreshCompleted;
            o.TaskAndLiveTvAndServer.NotifyBackupCompleted = o.NotifyBackupCompleted;
            o.TaskAndLiveTvAndServer.EnableLiveTvNotifications = o.EnableLiveTvNotifications;
            o.TaskAndLiveTvAndServer.NotifyRecordingStarted = o.NotifyRecordingStarted;
            o.TaskAndLiveTvAndServer.NotifyRecordingEnded = o.NotifyRecordingEnded;
            o.TaskAndLiveTvAndServer.NotifyTimerCreated = o.NotifyTimerCreated;
            o.TaskAndLiveTvAndServer.NotifyTimerUpdated = o.NotifyTimerUpdated;
            o.TaskAndLiveTvAndServer.NotifyTimerCancelled = o.NotifyTimerCancelled;
            o.TaskAndLiveTvAndServer.NotifySeriesTimerCreated = o.NotifySeriesTimerCreated;
            o.TaskAndLiveTvAndServer.NotifySeriesTimerUpdated = o.NotifySeriesTimerUpdated;
            o.TaskAndLiveTvAndServer.NotifySeriesTimerCancelled = o.NotifySeriesTimerCancelled;
            o.TaskAndLiveTvAndServer.NotifyServerStarted = o.NotifyServerStarted;
            o.TaskAndLiveTvAndServer.NotifyServerStopping = o.NotifyServerStopping;
            o.TaskAndLiveTvAndServer.NotifyUpdateAvailable = o.NotifyUpdateAvailable;
            o.TaskAndLiveTvAndServer.NotifyApplicationUpdated = o.NotifyApplicationUpdated;
            o.TaskAndLiveTvAndServer.NotifyRestartRequired = o.NotifyRestartRequired;
            o.TaskAndLiveTvAndServer.NotifyMaintenanceModeEntered = o.NotifyMaintenanceModeEntered;
            o.TaskAndLiveTvAndServer.NotifyMaintenanceModeExited = o.NotifyMaintenanceModeExited;

            // 高级与诊断
            o.AdvancedAndDiagnostics.MaximumNotificationsPerMinute = o.MaximumNotificationsPerMinute;
            o.AdvancedAndDiagnostics.SecurityEventsBypassRateLimit = o.SecurityEventsBypassRateLimit;
            o.AdvancedAndDiagnostics.AggregateWhenRateLimited = o.AggregateWhenRateLimited;
            o.AdvancedAndDiagnostics.MaxRetryCount = o.MaxRetryCount;
            o.AdvancedAndDiagnostics.ApplyPreset = o.ApplyPreset;
            o.AdvancedAndDiagnostics.DiagnosticInfo = o.DiagnosticInfo ?? "插件尚未启动";
            o.AdvancedAndDiagnostics.WebhookHealthStatus = o.WebhookHealthStatus ?? "未知";
            o.AdvancedAndDiagnostics.SendTestNotification = o.SendTestNotification;
            o.AdvancedAndDiagnostics.LastTestResult = o.LastTestResult ?? "";
        }

        /// <summary>
        /// 分组对象 → 扁平字段。用于保存时确保 JSON 序列化能写出旧格式。
        /// </summary>
        public static void CopyFromGroups(PluginOptions o)
        {
            o.EnsureGroups();

            // 飞书连接
            o.Enabled = o.FeishuConnection.Enabled;
            o.WebhookUrl = o.FeishuConnection.WebhookUrl ?? "";
            o.RequestTimeoutSeconds = o.FeishuConnection.RequestTimeoutSeconds;

            // 机器人安全（SignatureSecret 不在分组中，由 Validate 特殊处理）
            o.EnableCustomKeyword = o.BotSecurity.EnableCustomKeyword;
            o.CustomKeyword = o.BotSecurity.CustomKeyword ?? "";
            o.EnableSignatureVerification = o.BotSecurity.EnableSignatureVerification;

            // 消息显示
            o.MessageFormat = o.MessageDisplay.MessageFormat;
            o.MessageDetailLevel = o.MessageDisplay.MessageDetailLevel;
            o.ShowServerName = o.MessageDisplay.ShowServerName;
            o.ShowEventTime = o.MessageDisplay.ShowEventTime;
            o.IpAddressDisplayMode = o.MessageDisplay.IpAddressDisplayMode;
            o.DeviceIdDisplayMode = o.MessageDisplay.DeviceIdDisplayMode;
            o.FallbackToTextOnCardFailure = o.MessageDisplay.FallbackToTextOnCardFailure;
            o.ShowSensitiveTechnicalDetails = o.MessageDisplay.ShowSensitiveTechnicalDetails;
            o.IncludeUserName = o.MessageDisplay.IncludeUserName;
            o.IncludeMediaTitle = o.MessageDisplay.IncludeMediaTitle;
            o.IncludeMediaType = o.MessageDisplay.IncludeMediaType;
            o.IncludeSeriesEpisode = o.MessageDisplay.IncludeSeriesEpisode;
            o.IncludeClientName = o.MessageDisplay.IncludeClientName;
            o.IncludeDeviceName = o.MessageDisplay.IncludeDeviceName;
            o.IncludePlaybackPosition = o.MessageDisplay.IncludePlaybackPosition;
            o.IncludePlayedToCompletion = o.MessageDisplay.IncludePlayedToCompletion;

            // 播放通知
            o.NotifyPlaybackStarted = o.PlaybackNotification.NotifyPlaybackStarted;
            o.NotifyPlaybackStopped = o.PlaybackNotification.NotifyPlaybackStopped;
            o.NotifyPlaybackPaused = o.PlaybackNotification.NotifyPlaybackPaused;
            o.NotifyPlaybackResumed = o.PlaybackNotification.NotifyPlaybackResumed;
            o.NotifyPlaybackCompleted = o.PlaybackNotification.NotifyPlaybackCompleted;
            o.NotifyPlaybackAbandoned = o.PlaybackNotification.NotifyPlaybackAbandoned;
            o.NotifyPlaybackMethodChanged = o.PlaybackNotification.NotifyPlaybackMethodChanged;
            o.NotifyPlaybackMilestones = o.PlaybackNotification.NotifyPlaybackMilestones;
            o.PlaybackMilestones = o.PlaybackNotification.PlaybackMilestones ?? "25,50,75";
            o.MinimumStopSeconds = o.PlaybackNotification.MinimumStopSeconds;
            o.CompletionThresholdPercent = o.PlaybackNotification.CompletionThresholdPercent;
            o.OnlyVideo = o.PlaybackNotification.OnlyVideo;

            // 登录与用户
            o.NotifyAuthenticationSucceeded = o.LoginAndUser.NotifyAuthenticationSucceeded;
            o.NotifyAuthenticationFailed = o.LoginAndUser.NotifyAuthenticationFailed;
            o.NotifyUserLockedOut = o.LoginAndUser.NotifyUserLockedOut;
            o.NotifySessionStarted = o.LoginAndUser.NotifySessionStarted;
            o.NotifySessionEnded = o.LoginAndUser.NotifySessionEnded;
            o.NotifyRemoteControlDisconnected = o.LoginAndUser.NotifyRemoteControlDisconnected;
            o.NotifyPartyJoined = o.LoginAndUser.NotifyPartyJoined;
            o.NotifyPartyLeft = o.LoginAndUser.NotifyPartyLeft;
            o.NotifyUserPasswordChanged = o.LoginAndUser.NotifyUserPasswordChanged;
            o.NotifyUserCreated = o.LoginAndUser.NotifyUserCreated;
            o.NotifyUserDeleted = o.LoginAndUser.NotifyUserDeleted;
            o.NotifyUserUpdated = o.LoginAndUser.NotifyUserUpdated;
            o.NotifyUserPolicyUpdated = o.LoginAndUser.NotifyUserPolicyUpdated;
            o.NotifyUserConfigurationUpdated = o.LoginAndUser.NotifyUserConfigurationUpdated;
            o.UserFilterMode = o.LoginAndUser.UserFilterMode;
            o.UserNames = o.LoginAndUser.UserNames ?? "";

            // 媒体库与用户行为
            o.NotifyNewMovies = o.LibraryAndUserBehavior.NotifyNewMovies;
            o.NotifyNewEpisodes = o.LibraryAndUserBehavior.NotifyNewEpisodes;
            o.NotifyNewMusic = o.LibraryAndUserBehavior.NotifyNewMusic;
            o.NotifyOtherNewItems = o.LibraryAndUserBehavior.NotifyOtherNewItems;
            o.NotifyItemsRemoved = o.LibraryAndUserBehavior.NotifyItemsRemoved;
            o.NotifyItemsUpdated = o.LibraryAndUserBehavior.NotifyItemsUpdated;
            o.EnableLibraryAggregation = o.LibraryAndUserBehavior.EnableLibraryAggregation;
            o.LibraryAggregationWindowSeconds = o.LibraryAndUserBehavior.LibraryAggregationWindowSeconds;
            o.MaximumIndividualLibraryMessages = o.LibraryAndUserBehavior.MaximumIndividualLibraryMessages;
            o.NotifyFavoriteAdded = o.LibraryAndUserBehavior.NotifyFavoriteAdded;
            o.NotifyFavoriteRemoved = o.LibraryAndUserBehavior.NotifyFavoriteRemoved;
            o.NotifyMarkedPlayed = o.LibraryAndUserBehavior.NotifyMarkedPlayed;
            o.NotifyMarkedUnplayed = o.LibraryAndUserBehavior.NotifyMarkedUnplayed;
            o.NotifyUserRatingChanged = o.LibraryAndUserBehavior.NotifyUserRatingChanged;

            // 任务、Live TV 与服务器
            o.NotifyTaskFailed = o.TaskAndLiveTvAndServer.NotifyTaskFailed;
            o.NotifyTaskCompleted = o.TaskAndLiveTvAndServer.NotifyTaskCompleted;
            o.NotifyTaskCancelled = o.TaskAndLiveTvAndServer.NotifyTaskCancelled;
            o.NotifyLibraryScanStarted = o.TaskAndLiveTvAndServer.NotifyLibraryScanStarted;
            o.NotifyLibraryScanCompleted = o.TaskAndLiveTvAndServer.NotifyLibraryScanCompleted;
            o.NotifyMetadataRefreshCompleted = o.TaskAndLiveTvAndServer.NotifyMetadataRefreshCompleted;
            o.NotifyBackupCompleted = o.TaskAndLiveTvAndServer.NotifyBackupCompleted;
            o.EnableLiveTvNotifications = o.TaskAndLiveTvAndServer.EnableLiveTvNotifications;
            o.NotifyRecordingStarted = o.TaskAndLiveTvAndServer.NotifyRecordingStarted;
            o.NotifyRecordingEnded = o.TaskAndLiveTvAndServer.NotifyRecordingEnded;
            o.NotifyTimerCreated = o.TaskAndLiveTvAndServer.NotifyTimerCreated;
            o.NotifyTimerUpdated = o.TaskAndLiveTvAndServer.NotifyTimerUpdated;
            o.NotifyTimerCancelled = o.TaskAndLiveTvAndServer.NotifyTimerCancelled;
            o.NotifySeriesTimerCreated = o.TaskAndLiveTvAndServer.NotifySeriesTimerCreated;
            o.NotifySeriesTimerUpdated = o.TaskAndLiveTvAndServer.NotifySeriesTimerUpdated;
            o.NotifySeriesTimerCancelled = o.TaskAndLiveTvAndServer.NotifySeriesTimerCancelled;
            o.NotifyServerStarted = o.TaskAndLiveTvAndServer.NotifyServerStarted;
            o.NotifyServerStopping = o.TaskAndLiveTvAndServer.NotifyServerStopping;
            o.NotifyUpdateAvailable = o.TaskAndLiveTvAndServer.NotifyUpdateAvailable;
            o.NotifyApplicationUpdated = o.TaskAndLiveTvAndServer.NotifyApplicationUpdated;
            o.NotifyRestartRequired = o.TaskAndLiveTvAndServer.NotifyRestartRequired;
            o.NotifyMaintenanceModeEntered = o.TaskAndLiveTvAndServer.NotifyMaintenanceModeEntered;
            o.NotifyMaintenanceModeExited = o.TaskAndLiveTvAndServer.NotifyMaintenanceModeExited;

            // 高级与诊断
            o.MaximumNotificationsPerMinute = o.AdvancedAndDiagnostics.MaximumNotificationsPerMinute;
            o.SecurityEventsBypassRateLimit = o.AdvancedAndDiagnostics.SecurityEventsBypassRateLimit;
            o.AggregateWhenRateLimited = o.AdvancedAndDiagnostics.AggregateWhenRateLimited;
            o.MaxRetryCount = o.AdvancedAndDiagnostics.MaxRetryCount;
            o.ApplyPreset = o.AdvancedAndDiagnostics.ApplyPreset;
            o.DiagnosticInfo = o.AdvancedAndDiagnostics.DiagnosticInfo ?? "插件尚未启动";
            o.WebhookHealthStatus = o.AdvancedAndDiagnostics.WebhookHealthStatus ?? "未知";
            o.SendTestNotification = o.AdvancedAndDiagnostics.SendTestNotification;
            o.LastTestResult = o.AdvancedAndDiagnostics.LastTestResult ?? "";
        }
    }
}
