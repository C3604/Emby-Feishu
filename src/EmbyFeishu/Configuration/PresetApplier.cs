using EmbyFeishu.Models;

namespace EmbyFeishu.Configuration
{
    /// <summary>
    /// 将通知预设模板应用到配置选项。预设仅改变事件开关，不改变连接、安全、格式等设置。
    /// </summary>
    public static class PresetApplier
    {
        public static void Apply(PluginOptions options, NotificationPreset preset)
        {
            if (preset == NotificationPreset.None)
                return;

            options.EnsureGroups();

            switch (preset)
            {
                case NotificationPreset.Conservative:
                    ApplyConservative(options);
                    break;
                case NotificationPreset.Standard:
                    ApplyStandard(options);
                    break;
                case NotificationPreset.Full:
                    ApplyFull(options);
                    break;
                case NotificationPreset.PlaybackOnly:
                    ApplyPlaybackOnly(options);
                    break;
            }

            options.SyncFromGroups();
        }

        private static void ApplyConservative(PluginOptions o)
        {
            // 播放：全关
            SetAllPlayback(o, false);

            // 安全事件
            o.LoginAndUser.NotifyAuthenticationSucceeded = false;
            o.LoginAndUser.NotifyAuthenticationFailed = true;
            o.LoginAndUser.NotifyUserLockedOut = true;
            o.LoginAndUser.NotifySessionStarted = false;
            o.LoginAndUser.NotifySessionEnded = false;
            o.LoginAndUser.NotifyRemoteControlDisconnected = false;
            o.LoginAndUser.NotifyPartyJoined = false;
            o.LoginAndUser.NotifyPartyLeft = false;
            o.LoginAndUser.NotifyUserPasswordChanged = true;
            o.LoginAndUser.NotifyUserCreated = false;
            o.LoginAndUser.NotifyUserDeleted = true;
            o.LoginAndUser.NotifyUserUpdated = false;
            o.LoginAndUser.NotifyUserPolicyUpdated = false;
            o.LoginAndUser.NotifyUserConfigurationUpdated = false;

            // 媒体库：关
            SetAllLibrary(o, false);
            SetAllUserBehavior(o, false);

            // 任务：仅失败
            o.TaskAndLiveTvAndServer.NotifyTaskFailed = true;
            o.TaskAndLiveTvAndServer.NotifyTaskCompleted = false;
            o.TaskAndLiveTvAndServer.NotifyTaskCancelled = false;
            o.TaskAndLiveTvAndServer.NotifyLibraryScanStarted = false;
            o.TaskAndLiveTvAndServer.NotifyLibraryScanCompleted = false;
            o.TaskAndLiveTvAndServer.NotifyMetadataRefreshCompleted = false;
            o.TaskAndLiveTvAndServer.NotifyBackupCompleted = false;

            // Live TV：关
            o.TaskAndLiveTvAndServer.EnableLiveTvNotifications = false;

            // 服务器：仅关键
            o.TaskAndLiveTvAndServer.NotifyServerStarted = true;
            o.TaskAndLiveTvAndServer.NotifyServerStopping = true;
            o.TaskAndLiveTvAndServer.NotifyUpdateAvailable = true;
            o.TaskAndLiveTvAndServer.NotifyApplicationUpdated = true;
            o.TaskAndLiveTvAndServer.NotifyRestartRequired = true;
            o.TaskAndLiveTvAndServer.NotifyMaintenanceModeEntered = false;
            o.TaskAndLiveTvAndServer.NotifyMaintenanceModeExited = false;
        }

        private static void ApplyStandard(PluginOptions o)
        {
            // 播放：开始+停止+完成
            o.PlaybackNotification.NotifyPlaybackStarted = true;
            o.PlaybackNotification.NotifyPlaybackStopped = true;
            o.PlaybackNotification.NotifyPlaybackPaused = false;
            o.PlaybackNotification.NotifyPlaybackResumed = false;
            o.PlaybackNotification.NotifyPlaybackCompleted = true;
            o.PlaybackNotification.NotifyPlaybackAbandoned = false;
            o.PlaybackNotification.NotifyPlaybackMethodChanged = false;
            o.PlaybackNotification.NotifyPlaybackMilestones = false;

            // 安全
            o.LoginAndUser.NotifyAuthenticationSucceeded = false;
            o.LoginAndUser.NotifyAuthenticationFailed = true;
            o.LoginAndUser.NotifyUserLockedOut = true;
            o.LoginAndUser.NotifySessionStarted = false;
            o.LoginAndUser.NotifySessionEnded = false;
            o.LoginAndUser.NotifyRemoteControlDisconnected = false;
            o.LoginAndUser.NotifyPartyJoined = false;
            o.LoginAndUser.NotifyPartyLeft = false;
            o.LoginAndUser.NotifyUserPasswordChanged = true;
            o.LoginAndUser.NotifyUserCreated = false;
            o.LoginAndUser.NotifyUserDeleted = false;
            o.LoginAndUser.NotifyUserUpdated = false;
            o.LoginAndUser.NotifyUserPolicyUpdated = false;
            o.LoginAndUser.NotifyUserConfigurationUpdated = false;

            // 媒体库：新增电影+剧集
            o.LibraryAndUserBehavior.NotifyNewMovies = true;
            o.LibraryAndUserBehavior.NotifyNewEpisodes = true;
            o.LibraryAndUserBehavior.NotifyNewMusic = false;
            o.LibraryAndUserBehavior.NotifyOtherNewItems = false;
            o.LibraryAndUserBehavior.NotifyItemsRemoved = false;
            o.LibraryAndUserBehavior.NotifyItemsUpdated = false;
            SetAllUserBehavior(o, false);

            // 任务
            o.TaskAndLiveTvAndServer.NotifyTaskFailed = true;
            o.TaskAndLiveTvAndServer.NotifyTaskCompleted = false;
            o.TaskAndLiveTvAndServer.NotifyTaskCancelled = false;
            o.TaskAndLiveTvAndServer.NotifyLibraryScanStarted = false;
            o.TaskAndLiveTvAndServer.NotifyLibraryScanCompleted = true;
            o.TaskAndLiveTvAndServer.NotifyMetadataRefreshCompleted = false;
            o.TaskAndLiveTvAndServer.NotifyBackupCompleted = false;

            // Live TV：关
            o.TaskAndLiveTvAndServer.EnableLiveTvNotifications = false;

            // 服务器
            o.TaskAndLiveTvAndServer.NotifyServerStarted = true;
            o.TaskAndLiveTvAndServer.NotifyServerStopping = false;
            o.TaskAndLiveTvAndServer.NotifyUpdateAvailable = true;
            o.TaskAndLiveTvAndServer.NotifyApplicationUpdated = true;
            o.TaskAndLiveTvAndServer.NotifyRestartRequired = true;
            o.TaskAndLiveTvAndServer.NotifyMaintenanceModeEntered = false;
            o.TaskAndLiveTvAndServer.NotifyMaintenanceModeExited = false;
        }

        private static void ApplyFull(PluginOptions o)
        {
            SetAllPlayback(o, true);
            o.PlaybackNotification.NotifyPlaybackMilestones = true;

            o.LoginAndUser.NotifyAuthenticationSucceeded = true;
            o.LoginAndUser.NotifyAuthenticationFailed = true;
            o.LoginAndUser.NotifyUserLockedOut = true;
            o.LoginAndUser.NotifySessionStarted = true;
            o.LoginAndUser.NotifySessionEnded = true;
            o.LoginAndUser.NotifyRemoteControlDisconnected = true;
            o.LoginAndUser.NotifyPartyJoined = true;
            o.LoginAndUser.NotifyPartyLeft = true;
            o.LoginAndUser.NotifyUserPasswordChanged = true;
            o.LoginAndUser.NotifyUserCreated = true;
            o.LoginAndUser.NotifyUserDeleted = true;
            o.LoginAndUser.NotifyUserUpdated = true;
            o.LoginAndUser.NotifyUserPolicyUpdated = true;
            o.LoginAndUser.NotifyUserConfigurationUpdated = true;

            SetAllLibrary(o, true);
            SetAllUserBehavior(o, true);

            o.TaskAndLiveTvAndServer.NotifyTaskFailed = true;
            o.TaskAndLiveTvAndServer.NotifyTaskCompleted = true;
            o.TaskAndLiveTvAndServer.NotifyTaskCancelled = true;
            o.TaskAndLiveTvAndServer.NotifyLibraryScanStarted = true;
            o.TaskAndLiveTvAndServer.NotifyLibraryScanCompleted = true;
            o.TaskAndLiveTvAndServer.NotifyMetadataRefreshCompleted = true;
            o.TaskAndLiveTvAndServer.NotifyBackupCompleted = true;
            o.TaskAndLiveTvAndServer.EnableLiveTvNotifications = true;
            o.TaskAndLiveTvAndServer.NotifyRecordingStarted = true;
            o.TaskAndLiveTvAndServer.NotifyRecordingEnded = true;
            o.TaskAndLiveTvAndServer.NotifyTimerCreated = true;
            o.TaskAndLiveTvAndServer.NotifyTimerUpdated = true;
            o.TaskAndLiveTvAndServer.NotifyTimerCancelled = true;
            o.TaskAndLiveTvAndServer.NotifySeriesTimerCreated = true;
            o.TaskAndLiveTvAndServer.NotifySeriesTimerUpdated = true;
            o.TaskAndLiveTvAndServer.NotifySeriesTimerCancelled = true;

            o.TaskAndLiveTvAndServer.NotifyServerStarted = true;
            o.TaskAndLiveTvAndServer.NotifyServerStopping = true;
            o.TaskAndLiveTvAndServer.NotifyUpdateAvailable = true;
            o.TaskAndLiveTvAndServer.NotifyApplicationUpdated = true;
            o.TaskAndLiveTvAndServer.NotifyRestartRequired = true;
            o.TaskAndLiveTvAndServer.NotifyMaintenanceModeEntered = true;
            o.TaskAndLiveTvAndServer.NotifyMaintenanceModeExited = true;
        }

        private static void ApplyPlaybackOnly(PluginOptions o)
        {
            // 播放全开
            SetAllPlayback(o, true);
            o.PlaybackNotification.NotifyPlaybackMilestones = false;

            // 其余全关
            o.LoginAndUser.NotifyAuthenticationSucceeded = false;
            o.LoginAndUser.NotifyAuthenticationFailed = false;
            o.LoginAndUser.NotifyUserLockedOut = false;
            o.LoginAndUser.NotifySessionStarted = false;
            o.LoginAndUser.NotifySessionEnded = false;
            o.LoginAndUser.NotifyRemoteControlDisconnected = false;
            o.LoginAndUser.NotifyPartyJoined = false;
            o.LoginAndUser.NotifyPartyLeft = false;
            o.LoginAndUser.NotifyUserPasswordChanged = false;
            o.LoginAndUser.NotifyUserCreated = false;
            o.LoginAndUser.NotifyUserDeleted = false;
            o.LoginAndUser.NotifyUserUpdated = false;
            o.LoginAndUser.NotifyUserPolicyUpdated = false;
            o.LoginAndUser.NotifyUserConfigurationUpdated = false;

            SetAllLibrary(o, false);
            SetAllUserBehavior(o, false);

            o.TaskAndLiveTvAndServer.NotifyTaskFailed = false;
            o.TaskAndLiveTvAndServer.NotifyTaskCompleted = false;
            o.TaskAndLiveTvAndServer.NotifyTaskCancelled = false;
            o.TaskAndLiveTvAndServer.NotifyLibraryScanStarted = false;
            o.TaskAndLiveTvAndServer.NotifyLibraryScanCompleted = false;
            o.TaskAndLiveTvAndServer.NotifyMetadataRefreshCompleted = false;
            o.TaskAndLiveTvAndServer.NotifyBackupCompleted = false;
            o.TaskAndLiveTvAndServer.EnableLiveTvNotifications = false;
            o.TaskAndLiveTvAndServer.NotifyServerStarted = false;
            o.TaskAndLiveTvAndServer.NotifyServerStopping = false;
            o.TaskAndLiveTvAndServer.NotifyUpdateAvailable = false;
            o.TaskAndLiveTvAndServer.NotifyApplicationUpdated = false;
            o.TaskAndLiveTvAndServer.NotifyRestartRequired = false;
            o.TaskAndLiveTvAndServer.NotifyMaintenanceModeEntered = false;
            o.TaskAndLiveTvAndServer.NotifyMaintenanceModeExited = false;
        }

        private static void SetAllPlayback(PluginOptions o, bool value)
        {
            o.PlaybackNotification.NotifyPlaybackStarted = value;
            o.PlaybackNotification.NotifyPlaybackStopped = value;
            o.PlaybackNotification.NotifyPlaybackPaused = value;
            o.PlaybackNotification.NotifyPlaybackResumed = value;
            o.PlaybackNotification.NotifyPlaybackCompleted = value;
            o.PlaybackNotification.NotifyPlaybackAbandoned = value;
            o.PlaybackNotification.NotifyPlaybackMethodChanged = value;
        }

        private static void SetAllLibrary(PluginOptions o, bool value)
        {
            o.LibraryAndUserBehavior.NotifyNewMovies = value;
            o.LibraryAndUserBehavior.NotifyNewEpisodes = value;
            o.LibraryAndUserBehavior.NotifyNewMusic = value;
            o.LibraryAndUserBehavior.NotifyOtherNewItems = value;
            o.LibraryAndUserBehavior.NotifyItemsRemoved = value;
            o.LibraryAndUserBehavior.NotifyItemsUpdated = value;
        }

        private static void SetAllUserBehavior(PluginOptions o, bool value)
        {
            o.LibraryAndUserBehavior.NotifyFavoriteAdded = value;
            o.LibraryAndUserBehavior.NotifyFavoriteRemoved = value;
            o.LibraryAndUserBehavior.NotifyMarkedPlayed = value;
            o.LibraryAndUserBehavior.NotifyMarkedUnplayed = value;
            o.LibraryAndUserBehavior.NotifyUserRatingChanged = value;
        }
    }
}
