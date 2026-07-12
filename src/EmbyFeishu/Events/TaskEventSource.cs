using System;
using EmbyFeishu.Models;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Tasks;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 计划任务事件源。任务分类优先使用稳定 Key/Category，名称仅作降级判断。
    /// 错误摘要经脱敏，绝不发送完整堆栈、绝对路径或敏感配置。不发送实时进度。
    /// </summary>
    public class TaskEventSource : IEventSource
    {
        private readonly ITaskManager _taskManager;
        private readonly NotificationContext _ctx;
        private bool _started;

        public string Name => "ScheduledTask";

        public TaskEventSource(ITaskManager taskManager, NotificationContext ctx)
        {
            _taskManager = taskManager;
            _ctx = ctx;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _taskManager.TaskExecuting += OnTaskExecuting;
            _taskManager.TaskCompleted += OnTaskCompleted;
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _taskManager.TaskExecuting -= OnTaskExecuting;
            _taskManager.TaskCompleted -= OnTaskCompleted;
        }

        public void Dispose() => Stop();

        private void OnTaskExecuting(object sender, GenericEventArgs<IScheduledTaskWorker> e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !options.NotifyLibraryScanStarted) return;

                var worker = e?.Argument;
                if (worker == null) return;

                if (!IsLibraryScan(null, worker.Name, worker.Category)) return;

                var evt = new NotificationEvent
                {
                    EventType = NotificationEventType.LibraryScanStarted,
                    Category = NotificationCategory.ScheduledTask,
                    Severity = NotificationSeverity.Information,
                    Emoji = "🔍",
                    Title = "媒体库扫描开始",
                    Summary = "服务器开始扫描媒体库"
                };
                evt.AddField("任务", worker.Name, MessageDetailLevel.Simple);
                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理任务开始事件异常: {0}", ex.Message); }
        }

        private void OnTaskCompleted(object sender, TaskCompletionEventArgs e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null) return;

                var result = e?.Result;
                if (result == null) return;

                var key = result.Key;
                var name = result.Name ?? e.Task?.Name;
                var category = e.Task?.Category;
                var status = result.Status;

                var isScan = IsLibraryScan(key, name, category);
                var isMetadata = IsMetadataRefresh(key, name);
                var isBackup = IsBackup(key, name);

                NotificationEventType type;
                string title, emoji;
                NotificationSeverity severity;
                bool enabled;

                if (status == TaskCompletionStatus.Failed || status == TaskCompletionStatus.Aborted)
                {
                    type = NotificationEventType.TaskFailed;
                    title = "Emby 计划任务失败";
                    emoji = "❌";
                    severity = NotificationSeverity.Error;
                    enabled = options.NotifyTaskFailed;
                }
                else if (status == TaskCompletionStatus.Cancelled)
                {
                    type = NotificationEventType.TaskCancelled;
                    title = "计划任务已取消";
                    emoji = "🚫";
                    severity = NotificationSeverity.Warning;
                    enabled = options.NotifyTaskCancelled;
                }
                else // Completed
                {
                    if (isScan)
                    {
                        type = NotificationEventType.LibraryScanCompleted; title = "媒体库扫描完成"; emoji = "✅";
                        severity = NotificationSeverity.Success; enabled = options.NotifyLibraryScanCompleted;
                    }
                    else if (isMetadata)
                    {
                        type = NotificationEventType.MetadataRefreshCompleted; title = "元数据刷新完成"; emoji = "🔄";
                        severity = NotificationSeverity.Success; enabled = options.NotifyMetadataRefreshCompleted;
                    }
                    else if (isBackup)
                    {
                        type = NotificationEventType.BackupCompleted; title = "备份完成"; emoji = "💾";
                        severity = NotificationSeverity.Success; enabled = options.NotifyBackupCompleted;
                    }
                    else
                    {
                        type = NotificationEventType.TaskCompleted; title = "计划任务完成"; emoji = "✅";
                        severity = NotificationSeverity.Information; enabled = options.NotifyTaskCompleted;
                    }
                }

                if (!enabled) return;

                var evt = new NotificationEvent
                {
                    EventType = type,
                    Category = NotificationCategory.ScheduledTask,
                    Severity = severity,
                    Emoji = emoji,
                    Title = title
                };
                evt.AddField("任务", name, MessageDetailLevel.Simple);
                evt.AddField("分类", category);
                evt.AddField("状态", StatusText(status), MessageDetailLevel.Simple);
                evt.AddField("开始时间", Infrastructure.TimeFormatter.FormatDateTime(result.StartTimeUtc.LocalDateTime), MessageDetailLevel.Detailed);
                evt.AddField("结束时间", Infrastructure.TimeFormatter.FormatDateTime(result.EndTimeUtc.LocalDateTime), MessageDetailLevel.Detailed);
                evt.AddField("耗时", FormatDuration(result.EndTimeUtc - result.StartTimeUtc));

                if (type == NotificationEventType.TaskFailed && !string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    // 仅使用简短 ErrorMessage（非 LongErrorMessage 堆栈），并脱敏路径
                    var safe = _ctx.Sanitizer.SanitizeException(result.ErrorMessage, null);
                    evt.AddField("错误摘要", Truncate(safe, 300), MessageDetailLevel.Simple);
                }

                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理任务完成事件异常: {0}", ex.Message); }
        }

        private static bool IsLibraryScan(string key, string name, string category)
        {
            if (Contains(key, "RefreshLibrary") || Contains(key, "Scan")) return true;
            if (Contains(name, "扫描") || Contains(name, "Scan Media") || Contains(name, "Scan Library")) return true;
            return false;
        }

        private static bool IsMetadataRefresh(string key, string name)
        {
            if (Contains(key, "RefreshLibrary")) return false; // 扫描优先
            return Contains(key, "Metadata") || Contains(key, "Refresh") || Contains(name, "元数据");
        }

        private static bool IsBackup(string key, string name)
        {
            return Contains(key, "Backup") || Contains(name, "备份") || Contains(name, "Backup");
        }

        private static bool Contains(string haystack, string needle)
        {
            return !string.IsNullOrEmpty(haystack) && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string StatusText(TaskCompletionStatus status)
        {
            switch (status)
            {
                case TaskCompletionStatus.Completed: return "完成";
                case TaskCompletionStatus.Failed: return "失败";
                case TaskCompletionStatus.Cancelled: return "已取消";
                case TaskCompletionStatus.Aborted: return "已中止";
                default: return status.ToString();
            }
        }

        private static string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalSeconds < 0) return null;
            if (ts.TotalHours >= 1)
                return string.Format("{0}小时{1}分{2}秒", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            if (ts.TotalMinutes >= 1)
                return string.Format("{0}分{1}秒", ts.Minutes, ts.Seconds);
            return ts.Seconds + "秒";
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max) + "…";
        }
    }
}
