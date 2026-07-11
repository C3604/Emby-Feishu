using System;
using System.Threading;
using System.Threading.Tasks;
using EmbyFeishu.Events;
using EmbyFeishu.Feishu;
using EmbyFeishu.Infrastructure;
using EmbyFeishu.Messaging;
using EmbyFeishu.Models;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace EmbyFeishu
{
    /// <summary>
    /// 插件入口点，负责事件订阅和生命周期管理
    /// </summary>
    public class EntryPoint : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly PlaybackStateTracker _stateTracker;
        private readonly NotificationDispatcher _dispatcher;
        private readonly FeishuTextNotificationFormatter _formatter;
        private Timer _cleanupTimer;

        public EntryPoint(
            ISessionManager sessionManager,
            ILogManager logManager,
            IHttpClient httpClient,
            IJsonSerializer jsonSerializer)
        {
            _sessionManager = sessionManager;
            _logger = logManager.GetLogger("EmbyFeishu");
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _stateTracker = new PlaybackStateTracker();
            _formatter = new FeishuTextNotificationFormatter();

            var webhookClient = new FeishuWebhookClient(_httpClient, _jsonSerializer, _logger);
            _dispatcher = new NotificationDispatcher(webhookClient, _formatter, _logger);
        }

        /// <summary>
        /// 插件启动时由 Emby 调用
        /// </summary>
        public void Run()
        {
            _logger.Info("[EmbyFeishu] 插件入口点启动");

            _sessionManager.PlaybackStart += OnPlaybackStart;
            _sessionManager.PlaybackProgress += OnPlaybackProgress;
            _sessionManager.PlaybackStopped += OnPlaybackStopped;

            _logger.Info("[EmbyFeishu] 事件订阅完成");

            _dispatcher.Start();

            _cleanupTimer = new Timer(_ =>
            {
                try { _stateTracker.CleanupStale(); }
                catch (Exception ex) { _logger.Debug("[EmbyFeishu] 清理过期会话异常: {0}", ex.Message); }
            }, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            var plugin = Plugin.Instance;
            if (plugin != null)
            {
                var options = plugin.GetPluginOptions();
                _logger.Info("[EmbyFeishu] 插件状态: 启用={0}", options?.Enabled ?? false);
            }
        }

        /// <summary>
        /// 插件卸载时由 Emby 调用
        /// </summary>
        public void Dispose()
        {
            _logger.Info("[EmbyFeishu] 正在释放插件资源...");

            _sessionManager.PlaybackStart -= OnPlaybackStart;
            _sessionManager.PlaybackProgress -= OnPlaybackProgress;
            _sessionManager.PlaybackStopped -= OnPlaybackStopped;

            _cleanupTimer?.Dispose();
            _dispatcher?.Dispose();

            _logger.Info("[EmbyFeishu] 插件资源已释放");
        }

        private void OnPlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            try
            {
                var options = GetOptionsIfEnabled();
                if (options == null) return;

                if (e == null) return;

                if (options.OnlyVideo && !IsVideoItem(e))
                    return;

                var userName = GetUserName(e);
                if (!UserFilter.ShouldNotify(userName, options.UserFilterMode, options.UserNames))
                    return;

                var evt = BuildEvent(e, PlaybackEventType.Started);
                var key = PlaybackStateTracker.GetSessionKey(evt.PlaySessionId, evt.SessionId, evt.ItemId, evt.DeviceName);
                _stateTracker.OnPlaybackStarted(key);

                if (!options.NotifyPlaybackStarted)
                    return;

                _dispatcher.Enqueue(evt);
            }
            catch (Exception ex)
            {
                _logger.Error("[EmbyFeishu] 处理播放开始事件异常: {0}", ex.Message);
            }
        }

        private void OnPlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            try
            {
                var options = GetOptionsIfEnabled();
                if (options == null) return;

                if (!options.NotifyPlaybackPaused && !options.NotifyPlaybackResumed)
                    return;

                if (e == null) return;

                if (options.OnlyVideo && !IsVideoItem(e))
                    return;

                var userName = GetUserName(e);
                if (!UserFilter.ShouldNotify(userName, options.UserFilterMode, options.UserNames))
                    return;

                var isPaused = e.IsPaused;
                var key = PlaybackStateTracker.GetSessionKey(e.PlaySessionId, e.Session?.Id, GetItemId(e), e.DeviceName);
                var stateChange = _stateTracker.OnPlaybackProgress(key, isPaused);

                if (stateChange == null)
                    return;

                if (stateChange == PlaybackEventType.Paused && !options.NotifyPlaybackPaused)
                    return;
                if (stateChange == PlaybackEventType.Resumed && !options.NotifyPlaybackResumed)
                    return;

                var evt = BuildEvent(e, stateChange.Value);
                _dispatcher.Enqueue(evt);
            }
            catch (Exception ex)
            {
                _logger.Error("[EmbyFeishu] 处理播放进度事件异常: {0}", ex.Message);
            }
        }

        private void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            try
            {
                var options = GetOptionsIfEnabled();
                if (options == null) return;

                if (e == null) return;

                if (options.OnlyVideo && !IsVideoItem(e))
                    return;

                var userName = GetUserName(e);
                if (!UserFilter.ShouldNotify(userName, options.UserFilterMode, options.UserNames))
                    return;

                var key = PlaybackStateTracker.GetSessionKey(e.PlaySessionId, e.Session?.Id, GetItemId(e), e.DeviceName);
                _stateTracker.OnPlaybackStopped(key);

                if (!options.NotifyPlaybackStopped)
                    return;

                if (options.MinimumStopSeconds > 0)
                {
                    var positionTicks = e.PlaybackPositionTicks;
                    if (positionTicks.HasValue && positionTicks.Value > 0)
                    {
                        var positionSeconds = TimeSpan.FromTicks(positionTicks.Value).TotalSeconds;
                        if (positionSeconds < options.MinimumStopSeconds)
                        {
                            _logger.Debug("[EmbyFeishu] 播放不足 {0} 秒，跳过停止通知", options.MinimumStopSeconds);
                            return;
                        }
                    }
                }

                var evt = BuildEvent(e, PlaybackEventType.Stopped);
                if (e.PlayedToCompletion)
                {
                    evt.PlayedToCompletion = true;
                }
                else
                {
                    evt.PlayedToCompletion = false;
                }

                _dispatcher.Enqueue(evt);
            }
            catch (Exception ex)
            {
                _logger.Error("[EmbyFeishu] 处理播放停止事件异常: {0}", ex.Message);
            }
        }

        private PluginOptions GetOptionsIfEnabled()
        {
            var plugin = Plugin.Instance;
            if (plugin == null) return null;

            var options = plugin.GetPluginOptions();
            if (options == null || !options.Enabled) return null;

            if (string.IsNullOrWhiteSpace(options.WebhookUrl)) return null;

            return options;
        }

        private PlaybackNotificationEvent BuildEvent(PlaybackProgressEventArgs e, PlaybackEventType eventType)
        {
            var item = e.MediaInfo;
            var session = e.Session;

            var evt = new PlaybackNotificationEvent
            {
                EventType = eventType,
                OccurredAt = DateTime.Now,
                PlaySessionId = e.PlaySessionId,
                SessionId = session?.Id,
                UserName = GetUserName(e),
                ClientName = e.ClientName ?? session?.Client,
                DeviceName = e.DeviceName ?? session?.DeviceName,
                PlaybackPositionTicks = e.PlaybackPositionTicks
            };

            if (item != null)
            {
                evt.ItemId = item.Id;
                evt.ItemName = item.Name;
                evt.ItemType = item.Type;
                evt.MediaType = item.MediaType;
                evt.RuntimeTicks = item.RunTimeTicks;
                FillSeriesInfo(evt, item);
            }

            return evt;
        }

        private void FillSeriesInfo(PlaybackNotificationEvent evt, BaseItemDto item)
        {
            if (item == null) return;

            evt.SeriesName = item.SeriesName;
            evt.SeasonNumber = item.ParentIndexNumber;
            evt.EpisodeNumber = item.IndexNumber;

            if (string.IsNullOrWhiteSpace(evt.EpisodeName) && !string.IsNullOrWhiteSpace(item.SeriesName))
            {
                evt.EpisodeName = item.Name;
            }
        }

        private string GetUserName(PlaybackProgressEventArgs e)
        {
            var session = e.Session;
            if (session != null)
            {
                var userName = session.UserName;
                if (!string.IsNullOrWhiteSpace(userName))
                    return userName;
            }

            return null;
        }

        private string GetItemId(PlaybackProgressEventArgs e)
        {
            return e.MediaInfo?.Id;
        }

        private bool IsVideoItem(PlaybackProgressEventArgs e)
        {
            var mediaType = e.MediaInfo?.MediaType;
            if (string.IsNullOrWhiteSpace(mediaType))
                return false;
            return string.Equals(mediaType, "Video", StringComparison.OrdinalIgnoreCase);
        }
    }
}
