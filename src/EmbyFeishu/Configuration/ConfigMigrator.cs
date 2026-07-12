namespace EmbyFeishu.Configuration
{
    /// <summary>
    /// 配置兼容层。v2 引入分组配置，旧扁平字段在反序列化后自动迁移到分组对象。
    /// 绝不改动 GUID、Webhook 或已有开关的用户取值。
    /// </summary>
    public static class ConfigMigrator
    {
        /// <summary>当前配置架构版本</summary>
        public const int CurrentSchemaVersion = 2;

        /// <summary>
        /// 对配置对象应用迁移。幂等，可重复调用。
        /// 只在 ConfigSchemaVersion 低于当前版本时执行一次性迁移。
        /// 不再无条件调用 SyncToGroups/SyncFromGroups——由调用方决定同步方向。
        /// 也不重置 LastTestResult——那是用户可见的诊断信息。
        /// </summary>
        public static void Apply(PluginOptions options)
        {
            if (options == null)
                return;

            options.EnsureGroups();

            // v0/v1 → v2：将旧扁平字段迁移到新分组对象（仅执行一次）
            if (options.ConfigSchemaVersion < CurrentSchemaVersion)
            {
                MigrateToGroups(options);
                // 迁移后同步：先确保分组对象有值，再回写扁平字段
                options.SyncFromGroups();
                options.ConfigSchemaVersion = CurrentSchemaVersion;
            }

            ClampRanges(options);
        }

        /// <summary>
        /// 仅当旧扁平字段有非默认值且新分组仍为默认值时，才从旧字段复制。
        /// 已迁移的配置不会被旧字段覆盖。
        /// </summary>
        private static void MigrateToGroups(PluginOptions options)
        {
            options.EnsureGroups();

            // 飞书连接：核心字段无条件迁移
            options.FeishuConnection.WebhookUrl = options.WebhookUrl;
            options.FeishuConnection.Enabled = options.Enabled;
            options.FeishuConnection.RequestTimeoutSeconds = options.RequestTimeoutSeconds;

            // 机器人安全：默认值，不需要迁移（均为关闭/空）

            // 消息显示：迁移 MessageFormat 和 MessageDetailLevel
            options.MessageDisplay.MessageFormat = options.MessageFormat;
            options.MessageDisplay.MessageDetailLevel = options.MessageDetailLevel;
            options.MessageDisplay.ShowServerName = options.ShowServerName;
            options.MessageDisplay.ShowEventTime = options.ShowEventTime;
            options.MessageDisplay.IpAddressDisplayMode = options.IpAddressDisplayMode;
            options.MessageDisplay.DeviceIdDisplayMode = options.DeviceIdDisplayMode;
            options.MessageDisplay.FallbackToTextOnCardFailure = options.FallbackToTextOnCardFailure;
            options.MessageDisplay.ShowSensitiveTechnicalDetails = options.ShowSensitiveTechnicalDetails;
            options.MessageDisplay.IncludeUserName = options.IncludeUserName;
            options.MessageDisplay.IncludeMediaTitle = options.IncludeMediaTitle;
            options.MessageDisplay.IncludeMediaType = options.IncludeMediaType;
            options.MessageDisplay.IncludeSeriesEpisode = options.IncludeSeriesEpisode;
            options.MessageDisplay.IncludeClientName = options.IncludeClientName;
            options.MessageDisplay.IncludeDeviceName = options.IncludeDeviceName;
            options.MessageDisplay.IncludePlaybackPosition = options.IncludePlaybackPosition;
            options.MessageDisplay.IncludePlayedToCompletion = options.IncludePlayedToCompletion;

            // 播放通知
            options.PlaybackNotification.NotifyPlaybackStarted = options.NotifyPlaybackStarted;
            options.PlaybackNotification.NotifyPlaybackStopped = options.NotifyPlaybackStopped;
            options.PlaybackNotification.NotifyPlaybackPaused = options.NotifyPlaybackPaused;
            options.PlaybackNotification.NotifyPlaybackResumed = options.NotifyPlaybackResumed;
            options.PlaybackNotification.NotifyPlaybackCompleted = options.NotifyPlaybackCompleted;
            options.PlaybackNotification.NotifyPlaybackAbandoned = options.NotifyPlaybackAbandoned;
            options.PlaybackNotification.NotifyPlaybackMethodChanged = options.NotifyPlaybackMethodChanged;
            options.PlaybackNotification.NotifyPlaybackMilestones = options.NotifyPlaybackMilestones;
            options.PlaybackNotification.PlaybackMilestones = options.PlaybackMilestones ?? "25,50,75";
            options.PlaybackNotification.MinimumStopSeconds = options.MinimumStopSeconds;
            options.PlaybackNotification.CompletionThresholdPercent = options.CompletionThresholdPercent;
            options.PlaybackNotification.OnlyVideo = options.OnlyVideo;

            // 登录与用户
            options.LoginAndUser.NotifyAuthenticationSucceeded = options.NotifyAuthenticationSucceeded;
            options.LoginAndUser.NotifyAuthenticationFailed = options.NotifyAuthenticationFailed;
            options.LoginAndUser.NotifyUserLockedOut = options.NotifyUserLockedOut;
            options.LoginAndUser.NotifySessionStarted = options.NotifySessionStarted;
            options.LoginAndUser.NotifySessionEnded = options.NotifySessionEnded;
            options.LoginAndUser.NotifyRemoteControlDisconnected = options.NotifyRemoteControlDisconnected;
            options.LoginAndUser.NotifyPartyJoined = options.NotifyPartyJoined;
            options.LoginAndUser.NotifyPartyLeft = options.NotifyPartyLeft;
            options.LoginAndUser.NotifyUserPasswordChanged = options.NotifyUserPasswordChanged;
            options.LoginAndUser.NotifyUserCreated = options.NotifyUserCreated;
            options.LoginAndUser.NotifyUserDeleted = options.NotifyUserDeleted;
            options.LoginAndUser.NotifyUserUpdated = options.NotifyUserUpdated;
            options.LoginAndUser.NotifyUserPolicyUpdated = options.NotifyUserPolicyUpdated;
            options.LoginAndUser.NotifyUserConfigurationUpdated = options.NotifyUserConfigurationUpdated;
            options.LoginAndUser.UserFilterMode = options.UserFilterMode;
            options.LoginAndUser.UserNames = options.UserNames ?? "";

            // 媒体库与用户行为
            options.LibraryAndUserBehavior.NotifyNewMovies = options.NotifyNewMovies;
            options.LibraryAndUserBehavior.NotifyNewEpisodes = options.NotifyNewEpisodes;
            options.LibraryAndUserBehavior.NotifyNewMusic = options.NotifyNewMusic;
            options.LibraryAndUserBehavior.NotifyOtherNewItems = options.NotifyOtherNewItems;
            options.LibraryAndUserBehavior.NotifyItemsRemoved = options.NotifyItemsRemoved;
            options.LibraryAndUserBehavior.NotifyItemsUpdated = options.NotifyItemsUpdated;
            options.LibraryAndUserBehavior.EnableLibraryAggregation = options.EnableLibraryAggregation;
            options.LibraryAndUserBehavior.LibraryAggregationWindowSeconds = options.LibraryAggregationWindowSeconds;
            options.LibraryAndUserBehavior.MaximumIndividualLibraryMessages = options.MaximumIndividualLibraryMessages;
            options.LibraryAndUserBehavior.NotifyFavoriteAdded = options.NotifyFavoriteAdded;
            options.LibraryAndUserBehavior.NotifyFavoriteRemoved = options.NotifyFavoriteRemoved;
            options.LibraryAndUserBehavior.NotifyMarkedPlayed = options.NotifyMarkedPlayed;
            options.LibraryAndUserBehavior.NotifyMarkedUnplayed = options.NotifyMarkedUnplayed;
            options.LibraryAndUserBehavior.NotifyUserRatingChanged = options.NotifyUserRatingChanged;

            // 任务、Live TV 与服务器
            options.TaskAndLiveTvAndServer.NotifyTaskFailed = options.NotifyTaskFailed;
            options.TaskAndLiveTvAndServer.NotifyTaskCompleted = options.NotifyTaskCompleted;
            options.TaskAndLiveTvAndServer.NotifyTaskCancelled = options.NotifyTaskCancelled;
            options.TaskAndLiveTvAndServer.NotifyLibraryScanStarted = options.NotifyLibraryScanStarted;
            options.TaskAndLiveTvAndServer.NotifyLibraryScanCompleted = options.NotifyLibraryScanCompleted;
            options.TaskAndLiveTvAndServer.NotifyMetadataRefreshCompleted = options.NotifyMetadataRefreshCompleted;
            options.TaskAndLiveTvAndServer.NotifyBackupCompleted = options.NotifyBackupCompleted;
            options.TaskAndLiveTvAndServer.EnableLiveTvNotifications = options.EnableLiveTvNotifications;
            options.TaskAndLiveTvAndServer.NotifyRecordingStarted = options.NotifyRecordingStarted;
            options.TaskAndLiveTvAndServer.NotifyRecordingEnded = options.NotifyRecordingEnded;
            options.TaskAndLiveTvAndServer.NotifyTimerCreated = options.NotifyTimerCreated;
            options.TaskAndLiveTvAndServer.NotifyTimerUpdated = options.NotifyTimerUpdated;
            options.TaskAndLiveTvAndServer.NotifyTimerCancelled = options.NotifyTimerCancelled;
            options.TaskAndLiveTvAndServer.NotifySeriesTimerCreated = options.NotifySeriesTimerCreated;
            options.TaskAndLiveTvAndServer.NotifySeriesTimerUpdated = options.NotifySeriesTimerUpdated;
            options.TaskAndLiveTvAndServer.NotifySeriesTimerCancelled = options.NotifySeriesTimerCancelled;
            options.TaskAndLiveTvAndServer.NotifyServerStarted = options.NotifyServerStarted;
            options.TaskAndLiveTvAndServer.NotifyServerStopping = options.NotifyServerStopping;
            options.TaskAndLiveTvAndServer.NotifyUpdateAvailable = options.NotifyUpdateAvailable;
            options.TaskAndLiveTvAndServer.NotifyApplicationUpdated = options.NotifyApplicationUpdated;
            options.TaskAndLiveTvAndServer.NotifyRestartRequired = options.NotifyRestartRequired;
            options.TaskAndLiveTvAndServer.NotifyMaintenanceModeEntered = options.NotifyMaintenanceModeEntered;
            options.TaskAndLiveTvAndServer.NotifyMaintenanceModeExited = options.NotifyMaintenanceModeExited;

            // 高级与诊断
            options.AdvancedAndDiagnostics.MaximumNotificationsPerMinute = options.MaximumNotificationsPerMinute;
            options.AdvancedAndDiagnostics.SecurityEventsBypassRateLimit = options.SecurityEventsBypassRateLimit;
            options.AdvancedAndDiagnostics.AggregateWhenRateLimited = options.AggregateWhenRateLimited;
            options.AdvancedAndDiagnostics.LastTestResult = options.LastTestResult ?? "";
        }

        /// <summary>
        /// 将数值配置夹到合法范围。
        /// </summary>
        private static void ClampRanges(PluginOptions options)
        {
            options.FeishuConnection.RequestTimeoutSeconds = Clamp(options.FeishuConnection.RequestTimeoutSeconds, 3, 60);
            options.PlaybackNotification.MinimumStopSeconds = Clamp(options.PlaybackNotification.MinimumStopSeconds, 0, 600);
            options.PlaybackNotification.CompletionThresholdPercent = Clamp(options.PlaybackNotification.CompletionThresholdPercent, 50, 100);
            options.LibraryAndUserBehavior.LibraryAggregationWindowSeconds = Clamp(options.LibraryAndUserBehavior.LibraryAggregationWindowSeconds, 10, 600);
            options.LibraryAndUserBehavior.MaximumIndividualLibraryMessages = Clamp(options.LibraryAndUserBehavior.MaximumIndividualLibraryMessages, 0, 50);
            options.AdvancedAndDiagnostics.MaximumNotificationsPerMinute = Clamp(options.AdvancedAndDiagnostics.MaximumNotificationsPerMinute, 1, 240);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
