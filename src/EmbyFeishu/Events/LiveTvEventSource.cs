using System;
using EmbyFeishu.Models;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Events;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// Live TV 事件源（可选）。当服务器未启用 Live TV 或无法注入 ILiveTvManager 时不会构造，
    /// 不影响其他事件源与插件加载。
    /// </summary>
    public class LiveTvEventSource : IEventSource
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly NotificationContext _ctx;
        private bool _started;

        public string Name => "LiveTv";

        public LiveTvEventSource(ILiveTvManager liveTvManager, NotificationContext ctx)
        {
            _liveTvManager = liveTvManager;
            _ctx = ctx;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _liveTvManager.RecordingStarted += OnRecordingStarted;
            _liveTvManager.RecordingEnded += OnRecordingEnded;
            _liveTvManager.TimerCreated += OnTimerCreated;
            _liveTvManager.TimerUpdated += OnTimerUpdated;
            _liveTvManager.TimerCancelled += OnTimerCancelled;
            _liveTvManager.SeriesTimerCreated += OnSeriesTimerCreated;
            _liveTvManager.SeriesTimerUpdated += OnSeriesTimerUpdated;
            _liveTvManager.SeriesTimerCancelled += OnSeriesTimerCancelled;
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _liveTvManager.RecordingStarted -= OnRecordingStarted;
            _liveTvManager.RecordingEnded -= OnRecordingEnded;
            _liveTvManager.TimerCreated -= OnTimerCreated;
            _liveTvManager.TimerUpdated -= OnTimerUpdated;
            _liveTvManager.TimerCancelled -= OnTimerCancelled;
            _liveTvManager.SeriesTimerCreated -= OnSeriesTimerCreated;
            _liveTvManager.SeriesTimerUpdated -= OnSeriesTimerUpdated;
            _liveTvManager.SeriesTimerCancelled -= OnSeriesTimerCancelled;
        }

        public void Dispose() => Stop();

        private void OnRecordingStarted(object sender, GenericEventArgs<ActiveRecordingInfo> e)
        {
            var rec = e?.Argument;
            var name = rec?.Program?.Name ?? rec?.Timer?.Name ?? rec?.Channel?.Name;
            Publish(NotificationEventType.RecordingStarted, "开始录制", "🔴", name, o => o.NotifyRecordingStarted);
        }

        private void OnRecordingEnded(object sender, GenericEventArgs<ActiveRecordingInfo> e)
        {
            var rec = e?.Argument;
            var name = rec?.Program?.Name ?? rec?.Timer?.Name ?? rec?.Channel?.Name;
            Publish(NotificationEventType.RecordingEnded, "结束录制", "⏺️", name, o => o.NotifyRecordingEnded);
        }

        private void OnTimerCreated(object sender, GenericEventArgs<TimerEventInfo> e)
            => Publish(NotificationEventType.TimerCreated, "创建录制定时", "⏲️", e?.Argument?.Timer?.Name, o => o.NotifyTimerCreated);

        private void OnTimerUpdated(object sender, GenericEventArgs<TimerEventInfo> e)
            => Publish(NotificationEventType.TimerUpdated, "更新录制定时", "⏲️", e?.Argument?.Timer?.Name, o => o.NotifyTimerUpdated);

        private void OnTimerCancelled(object sender, GenericEventArgs<TimerEventInfo> e)
            => Publish(NotificationEventType.TimerCancelled, "取消录制定时", "⏲️", e?.Argument?.Timer?.Name, o => o.NotifyTimerCancelled);

        private void OnSeriesTimerCreated(object sender, GenericEventArgs<SeriesTimerEventInfo> e)
            => Publish(NotificationEventType.SeriesTimerCreated, "创建连续录制", "📆", e?.Argument?.SeriesTimer?.Name, o => o.NotifySeriesTimerCreated);

        private void OnSeriesTimerUpdated(object sender, GenericEventArgs<SeriesTimerEventInfo> e)
            => Publish(NotificationEventType.SeriesTimerUpdated, "更新连续录制", "📆", e?.Argument?.SeriesTimer?.Name, o => o.NotifySeriesTimerUpdated);

        private void OnSeriesTimerCancelled(object sender, GenericEventArgs<SeriesTimerEventInfo> e)
            => Publish(NotificationEventType.SeriesTimerCancelled, "取消连续录制", "📆", e?.Argument?.SeriesTimer?.Name, o => o.NotifySeriesTimerCancelled);

        private void Publish(NotificationEventType type, string title, string emoji, string name, Func<PluginOptions, bool> enabled)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !options.EnableLiveTvNotifications || !enabled(options)) return;

                var evt = new NotificationEvent
                {
                    EventType = type,
                    Category = NotificationCategory.LiveTv,
                    Severity = NotificationSeverity.Information,
                    Emoji = emoji,
                    Title = title
                };
                evt.AddField("节目", name, MessageDetailLevel.Simple);
                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理 Live TV 事件异常: {0}", ex.Message); }
        }
    }
}
