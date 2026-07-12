using System;
using EmbyFeishu.Configuration;
using EmbyFeishu.Infrastructure;
using EmbyFeishu.Messaging;
using EmbyFeishu.Models;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 播放事件源：开始/暂停/恢复/停止/完成/放弃/里程碑/播放方式变化。
    /// 保持既有播放通知行为不变（默认 Custom 详细程度 + 旧字段开关）。
    /// </summary>
    public class PlaybackEventSource : IEventSource
    {
        private readonly ISessionManager _sessionManager;
        private readonly NotificationContext _ctx;
        private readonly PlaybackStateTracker _stateTracker;
        private bool _started;

        public string Name => "Playback";

        public PlaybackEventSource(ISessionManager sessionManager, NotificationContext ctx, PlaybackStateTracker stateTracker)
        {
            _sessionManager = sessionManager;
            _ctx = ctx;
            _stateTracker = stateTracker;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _sessionManager.PlaybackStart += OnPlaybackStart;
            _sessionManager.PlaybackProgress += OnPlaybackProgress;
            _sessionManager.PlaybackStopped += OnPlaybackStopped;
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _sessionManager.PlaybackStart -= OnPlaybackStart;
            _sessionManager.PlaybackProgress -= OnPlaybackProgress;
            _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        }

        public void Dispose() => Stop();

        // ================= 事件处理 =================

        private void OnPlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || e == null) return;
                if (!PassesFilters(e, options)) return;

                var key = BuildSessionKey(e);
                _stateTracker.OnPlaybackStarted(key);
                // 建立播放方式基线，首次不通知
                _stateTracker.CheckPlayMethodChanged(key, GetPlayMethodRaw(e));

                if (!options.NotifyPlaybackStarted) return;

                var evt = BuildPlaybackEvent(e, options, NotificationEventType.PlaybackStarted, "开始播放", "▶️", NotificationSeverity.Information);
                _ctx.Publish(evt);
            }
            catch (Exception ex)
            {
                _ctx.Logger.Error("[EmbyFeishu] 处理播放开始事件异常: {0}", ex.Message);
            }
        }

        private void OnPlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || e == null) return;
                if (!PassesFilters(e, options)) return;

                var key = BuildSessionKey(e);

                // 暂停 / 恢复
                if (options.NotifyPlaybackPaused || options.NotifyPlaybackResumed)
                {
                    var stateChange = _stateTracker.OnPlaybackProgress(key, e.IsPaused);
                    if (stateChange == PlaybackEventType.Paused && options.NotifyPlaybackPaused)
                    {
                        _ctx.Publish(BuildPlaybackEvent(e, options, NotificationEventType.PlaybackPaused, "暂停播放", "⏸️", NotificationSeverity.Information));
                    }
                    else if (stateChange == PlaybackEventType.Resumed && options.NotifyPlaybackResumed)
                    {
                        _ctx.Publish(BuildPlaybackEvent(e, options, NotificationEventType.PlaybackResumed, "恢复播放", "▶️", NotificationSeverity.Information));
                    }
                }

                // 播放方式变化
                if (options.NotifyPlaybackMethodChanged)
                {
                    var raw = GetPlayMethodRaw(e);
                    if (_stateTracker.CheckPlayMethodChanged(key, raw))
                    {
                        var evt = BuildPlaybackEvent(e, options, NotificationEventType.PlaybackMethodChanged, "播放方式变化", "🔀", NotificationSeverity.Information);
                        evt.AddField("当前播放方式", PlayMethodDisplay(raw), MessageDetailLevel.Simple);
                        _ctx.Publish(evt);
                    }
                }

                // 进度里程碑
                if (options.NotifyPlaybackMilestones && !e.IsPaused)
                {
                    var percent = ComputePercent(e.PlaybackPositionTicks, e.MediaInfo?.RunTimeTicks);
                    if (percent.HasValue)
                    {
                        var milestones = ConfigValidator.ParseMilestones(options.PlaybackMilestones);
                        var reached = _stateTracker.CheckMilestone(key, percent.Value, milestones);
                        if (reached.HasValue)
                        {
                            var evt = BuildPlaybackEvent(e, options, NotificationEventType.PlaybackMilestone,
                                "播放进度 " + reached.Value + "%", "📊", NotificationSeverity.Information);
                            evt.AddField("进度", reached.Value + "%", MessageDetailLevel.Simple);
                            _ctx.Publish(evt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ctx.Logger.Error("[EmbyFeishu] 处理播放进度事件异常: {0}", ex.Message);
            }
        }

        private void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || e == null) return;
                if (!PassesFilters(e, options))
                {
                    _stateTracker.OnPlaybackStopped(BuildSessionKey(e));
                    return;
                }

                var key = BuildSessionKey(e);

                // 最短播放时长过滤
                if (options.MinimumStopSeconds > 0)
                {
                    var pos = e.PlaybackPositionTicks;
                    if (pos.HasValue && pos.Value > 0)
                    {
                        var seconds = TimeSpan.FromTicks(pos.Value).TotalSeconds;
                        if (seconds < options.MinimumStopSeconds)
                        {
                            _stateTracker.OnPlaybackStopped(key);
                            _ctx.Logger.Debug("[EmbyFeishu] 播放不足 {0} 秒，跳过停止通知", options.MinimumStopSeconds);
                            return;
                        }
                    }
                }

                var percent = ComputePercent(e.PlaybackPositionTicks, e.MediaInfo?.RunTimeTicks);
                var completed = e.PlayedToCompletion
                    || (percent.HasValue && percent.Value >= options.CompletionThresholdPercent);

                NotificationEvent evt = null;

                if (completed)
                {
                    if (options.NotifyPlaybackCompleted)
                    {
                        _stateTracker.MarkCompleted(key);
                        evt = BuildPlaybackEvent(e, options, NotificationEventType.PlaybackCompleted, "播放完成", "✅", NotificationSeverity.Success);
                        evt.AddField("完成进度", (percent ?? 100) + "%", MessageDetailLevel.Standard);
                    }
                }
                else
                {
                    var abandoned = percent.HasValue && percent.Value < 25;
                    if (abandoned && options.NotifyPlaybackAbandoned)
                    {
                        evt = BuildPlaybackEvent(e, options, NotificationEventType.PlaybackAbandoned, "放弃播放", "⏹️", NotificationSeverity.Information);
                    }
                    else if (options.NotifyPlaybackStopped)
                    {
                        evt = BuildPlaybackEvent(e, options, NotificationEventType.PlaybackStopped, "停止播放", "⏹️", NotificationSeverity.Information);
                    }
                }

                if (evt != null)
                {
                    // 播放位置字段（自定义模式沿用旧开关）
                    AddPositionField(evt, e);
                    if (evt.EventType == NotificationEventType.PlaybackStopped)
                    {
                        evt.AddField("播放完成", e.PlayedToCompletion ? "是" : "否", MessageDetailLevel.Detailed, false, CustomFieldKeys.PlayedToCompletion);
                    }
                    _ctx.Publish(evt);
                }

                _stateTracker.OnPlaybackStopped(key);
            }
            catch (Exception ex)
            {
                _ctx.Logger.Error("[EmbyFeishu] 处理播放停止事件异常: {0}", ex.Message);
            }
        }

        // ================= 构建 =================

        private NotificationEvent BuildPlaybackEvent(
            PlaybackProgressEventArgs e, PluginOptions options,
            NotificationEventType type, string title, string emoji, NotificationSeverity severity)
        {
            var mediaInfo = e.MediaInfo;
            var evt = new NotificationEvent
            {
                EventType = type,
                Category = NotificationCategory.Playback,
                Severity = severity,
                Emoji = emoji,
                Title = title,
                UserName = GetUserName(e),
                ClientName = e.ClientName ?? e.Session?.Client,
                DeviceName = e.DeviceName ?? e.Session?.DeviceName,
                ItemId = mediaInfo?.Id,
                ItemName = mediaInfo?.Name,
                ItemType = mediaInfo?.Type
            };

            // 用户
            evt.AddField("用户", evt.UserName, MessageDetailLevel.Simple, false, CustomFieldKeys.UserName);

            // 媒体标题（是否含季集号由详细程度/自定义开关决定）
            var includeSeries = options.MessageDetailLevel == MessageDetailLevel.Custom
                ? options.IncludeSeriesEpisode
                : true;
            var title2 = MediaTitleFormatter.Format(
                mediaInfo?.Name,
                mediaInfo?.SeriesName,
                includeSeries ? mediaInfo?.ParentIndexNumber : null,
                includeSeries ? mediaInfo?.IndexNumber : null,
                includeSeries && !string.IsNullOrWhiteSpace(mediaInfo?.SeriesName) ? mediaInfo?.Name : null);
            evt.AddField("媒体", title2, MessageDetailLevel.Simple, false, CustomFieldKeys.MediaTitle);

            // 类型
            evt.AddField("类型", mediaInfo?.MediaType, MessageDetailLevel.Standard, false, CustomFieldKeys.MediaType);

            // 播放方式（标准起显示；自定义模式无对应旧开关，故不显示，保持旧外观）
            evt.AddField("播放方式", PlayMethodDisplay(GetPlayMethodRaw(e)), MessageDetailLevel.Standard);

            // 客户端 / 设备
            evt.AddField("客户端", evt.ClientName, MessageDetailLevel.Standard, false, CustomFieldKeys.ClientName);
            evt.AddField("设备", evt.DeviceName, MessageDetailLevel.Standard, false, CustomFieldKeys.DeviceName);

            // 详细技术字段
            if (mediaInfo?.RunTimeTicks != null)
                evt.AddField("媒体时长", TimeFormatter.FormatTicks(mediaInfo.RunTimeTicks), MessageDetailLevel.Detailed);
            AddYearField(evt, e);
            AddTechnicalFields(evt, e, options);

            return evt;
        }

        private void AddPositionField(NotificationEvent evt, PlaybackStopEventArgs e)
        {
            var pos = TimeFormatter.FormatTicks(e.PlaybackPositionTicks);
            var total = TimeFormatter.FormatTicks(e.MediaInfo?.RunTimeTicks);
            if (pos != null || total != null)
            {
                var value = (pos ?? "00:00") + " / " + (total ?? "未知");
                evt.AddField("播放位置", value, MessageDetailLevel.Detailed, false, CustomFieldKeys.PlaybackPosition);
            }
        }

        private void AddYearField(NotificationEvent evt, PlaybackProgressEventArgs e)
        {
            try
            {
                var year = e.Item?.ProductionYear;
                if (year.HasValue && year.Value > 0)
                    evt.AddField("年份", year.Value.ToString(), MessageDetailLevel.Detailed);
            }
            catch { /* 忽略 */ }
        }

        private void AddTechnicalFields(NotificationEvent evt, PlaybackProgressEventArgs e, PluginOptions options)
        {
            try
            {
                var ti = e.Session?.TranscodingInfo;
                if (ti != null)
                {
                    evt.AddField("视频编码", ti.VideoCodec, MessageDetailLevel.Detailed, true);
                    evt.AddField("音频编码", ti.AudioCodec, MessageDetailLevel.Detailed, true);
                    if (ti.AudioChannels.HasValue)
                        evt.AddField("声道", ti.AudioChannels.Value + " ch", MessageDetailLevel.Detailed, true);
                    if (ti.Width.HasValue && ti.Height.HasValue)
                        evt.AddField("分辨率", ti.Width.Value + "x" + ti.Height.Value, MessageDetailLevel.Detailed, true);
                }

                var appVer = e.Session?.ApplicationVersion;
                evt.AddField("客户端版本", appVer, MessageDetailLevel.Detailed, true);

                var ip = e.Session?.RemoteEndPoint?.ToString();
                var maskedIp = _ctx.Sanitizer.SanitizeIpAddress(ip, options.IpAddressDisplayMode);
                evt.AddField("IP", maskedIp, MessageDetailLevel.Detailed, true);
            }
            catch { /* 技术字段获取失败不影响主通知 */ }
        }

        // ================= 辅助 =================

        private bool PassesFilters(PlaybackProgressEventArgs e, PluginOptions options)
        {
            if (options.OnlyVideo && !IsVideoItem(e))
                return false;

            var userName = GetUserName(e);
            return UserFilter.ShouldNotify(userName, options.UserFilterMode, options.UserNames);
        }

        private static bool IsVideoItem(PlaybackProgressEventArgs e)
        {
            var mediaType = e.MediaInfo?.MediaType;
            if (string.IsNullOrWhiteSpace(mediaType))
                return false;
            return string.Equals(mediaType, "Video", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetUserName(PlaybackProgressEventArgs e)
        {
            var session = e.Session;
            if (session != null && !string.IsNullOrWhiteSpace(session.UserName))
                return session.UserName;
            return null;
        }

        private static string BuildSessionKey(PlaybackProgressEventArgs e)
        {
            var deviceName = e.DeviceName ?? e.Session?.DeviceName;
            return PlaybackStateTracker.GetSessionKey(e.PlaySessionId, e.Session?.Id, e.MediaInfo?.Id, deviceName);
        }

        private static string GetPlayMethodRaw(PlaybackProgressEventArgs e)
        {
            var pm = e.Session?.PlayState?.PlayMethod;
            return pm?.ToString();
        }

        private static string PlayMethodDisplay(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            switch (raw)
            {
                case "DirectPlay": return "直接播放";
                case "DirectStream": return "直接串流";
                case "Transcode": return "转码";
                default: return raw;
            }
        }

        private static int? ComputePercent(long? positionTicks, long? runtimeTicks)
        {
            if (!positionTicks.HasValue || !runtimeTicks.HasValue || runtimeTicks.Value <= 0)
                return null;
            var pct = (int)(positionTicks.Value * 100.0 / runtimeTicks.Value);
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            return pct;
        }
    }
}
