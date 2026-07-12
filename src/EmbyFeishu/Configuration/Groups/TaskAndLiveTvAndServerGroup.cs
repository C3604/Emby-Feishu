using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu.Configuration.Groups
{
    /// <summary>
    /// 第七组：任务、Live TV 与服务器 — 任务完成/失败、媒体库扫描、Live TV、服务器状态。
    /// </summary>
    public class TaskAndLiveTvAndServerGroup : EditableOptionsBase
    {
        public override string EditorTitle => "任务、Live TV 与服务器";

        // 任务
        [DisplayName("通知任务失败")]
        public bool NotifyTaskFailed { get; set; } = true;

        [DisplayName("通知任务完成")]
        [IsAdvanced]
        public bool NotifyTaskCompleted { get; set; } = false;

        [DisplayName("通知任务取消")]
        [IsAdvanced]
        public bool NotifyTaskCancelled { get; set; } = false;

        [DisplayName("通知媒体库扫描开始")]
        [IsAdvanced]
        public bool NotifyLibraryScanStarted { get; set; } = false;

        [DisplayName("通知媒体库扫描完成")]
        public bool NotifyLibraryScanCompleted { get; set; } = true;

        [DisplayName("通知元数据刷新完成")]
        [IsAdvanced]
        public bool NotifyMetadataRefreshCompleted { get; set; } = false;

        [DisplayName("通知备份完成")]
        [IsAdvanced]
        public bool NotifyBackupCompleted { get; set; } = false;

        // Live TV
        [DisplayName("启用 Live TV 通知")]
        [Description("服务器未启用 Live TV 时本组不生效")]
        [IsAdvanced]
        public bool EnableLiveTvNotifications { get; set; } = false;

        [DisplayName("通知开始录制")]
        [IsAdvanced]
        [VisibleCondition(nameof(EnableLiveTvNotifications), SimpleCondition.IsTrue)]
        public bool NotifyRecordingStarted { get; set; } = false;

        [DisplayName("通知结束录制")]
        [IsAdvanced]
        [VisibleCondition(nameof(EnableLiveTvNotifications), SimpleCondition.IsTrue)]
        public bool NotifyRecordingEnded { get; set; } = false;

        [DisplayName("通知创建定时")]
        [IsAdvanced]
        [VisibleCondition(nameof(EnableLiveTvNotifications), SimpleCondition.IsTrue)]
        public bool NotifyTimerCreated { get; set; } = false;

        [DisplayName("通知更新定时")]
        [IsAdvanced]
        [VisibleCondition(nameof(EnableLiveTvNotifications), SimpleCondition.IsTrue)]
        public bool NotifyTimerUpdated { get; set; } = false;

        [DisplayName("通知取消定时")]
        [IsAdvanced]
        [VisibleCondition(nameof(EnableLiveTvNotifications), SimpleCondition.IsTrue)]
        public bool NotifyTimerCancelled { get; set; } = false;

        [DisplayName("通知创建连续定时")]
        [IsAdvanced]
        [VisibleCondition(nameof(EnableLiveTvNotifications), SimpleCondition.IsTrue)]
        public bool NotifySeriesTimerCreated { get; set; } = false;

        [DisplayName("通知更新连续定时")]
        [IsAdvanced]
        [VisibleCondition(nameof(EnableLiveTvNotifications), SimpleCondition.IsTrue)]
        public bool NotifySeriesTimerUpdated { get; set; } = false;

        [DisplayName("通知取消连续定时")]
        [IsAdvanced]
        [VisibleCondition(nameof(EnableLiveTvNotifications), SimpleCondition.IsTrue)]
        public bool NotifySeriesTimerCancelled { get; set; } = false;

        // 服务器状态
        [DisplayName("通知服务器启动")]
        public bool NotifyServerStarted { get; set; } = true;

        [DisplayName("通知服务器停止")]
        [IsAdvanced]
        public bool NotifyServerStopping { get; set; } = false;

        [DisplayName("通知有可用更新")]
        public bool NotifyUpdateAvailable { get; set; } = true;

        [DisplayName("通知已应用更新")]
        public bool NotifyApplicationUpdated { get; set; } = true;

        [DisplayName("通知需要重启")]
        public bool NotifyRestartRequired { get; set; } = true;

        [DisplayName("通知进入维护模式")]
        [IsAdvanced]
        public bool NotifyMaintenanceModeEntered { get; set; } = false;

        [DisplayName("通知退出维护模式")]
        [IsAdvanced]
        public bool NotifyMaintenanceModeExited { get; set; } = false;
    }
}
