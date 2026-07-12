using System;
using EmbyFeishu.Models;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Events;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 登录与会话事件源。
    /// 安全约束：绝不读取/记录/推送 AuthenticationRequest.Password 与 AuthenticationResult.AccessToken。
    /// </summary>
    public class SessionEventSource : IEventSource
    {
        private readonly ISessionManager _sessionManager;
        private readonly NotificationContext _ctx;
        private bool _started;

        public string Name => "Session";

        public SessionEventSource(ISessionManager sessionManager, NotificationContext ctx)
        {
            _sessionManager = sessionManager;
            _ctx = ctx;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _sessionManager.AuthenticationSucceeded += OnAuthSucceeded;
            _sessionManager.AuthenticationFailed += OnAuthFailed;
            _sessionManager.SessionStarted += OnSessionStarted;
            _sessionManager.SessionEnded += OnSessionEnded;
            _sessionManager.RemoteControlDisconnected += OnRemoteControlDisconnected;
            _sessionManager.AddedToParty += OnAddedToParty;
            _sessionManager.RemovedFromParty += OnRemovedFromParty;
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _sessionManager.AuthenticationSucceeded -= OnAuthSucceeded;
            _sessionManager.AuthenticationFailed -= OnAuthFailed;
            _sessionManager.SessionStarted -= OnSessionStarted;
            _sessionManager.SessionEnded -= OnSessionEnded;
            _sessionManager.RemoteControlDisconnected -= OnRemoteControlDisconnected;
            _sessionManager.AddedToParty -= OnAddedToParty;
            _sessionManager.RemovedFromParty -= OnRemovedFromParty;
        }

        public void Dispose() => Stop();

        private void OnAuthSucceeded(object sender, GenericEventArgs<AuthenticationResult> e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !options.NotifyAuthenticationSucceeded) return;

                var result = e?.Argument;
                if (result == null) return;

                // 只取用户名与会话信息，严禁触碰 AccessToken
                var evt = new NotificationEvent
                {
                    EventType = NotificationEventType.AuthenticationSucceeded,
                    Category = NotificationCategory.Authentication,
                    Severity = NotificationSeverity.Information,
                    Emoji = "🔓",
                    Title = "登录成功",
                    UserName = result.User?.Name
                };
                evt.AddField("用户", result.User?.Name, MessageDetailLevel.Simple);
                var si = result.SessionInfo;
                if (si != null)
                {
                    evt.AddField("客户端", si.Client);
                    evt.AddField("设备", si.DeviceName);
                    evt.AddField("IP", _ctx.Sanitizer.SanitizeIpAddress(si.RemoteEndPoint?.ToString(), options.IpAddressDisplayMode), MessageDetailLevel.Detailed);
                }
                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理登录成功事件异常: {0}", ex.Message); }
        }

        private void OnAuthFailed(object sender, GenericEventArgs<AuthenticationRequest> e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !options.NotifyAuthenticationFailed) return;

                var req = e?.Argument;
                if (req == null) return;

                // 严禁访问 req.Password
                var maskedIp = _ctx.Sanitizer.SanitizeIpAddress(req.RemoteAddress?.ToString(), options.IpAddressDisplayMode);
                var maskedDevice = _ctx.Sanitizer.SanitizeDeviceId(req.DeviceId, options.DeviceIdDisplayMode);

                var evt = new NotificationEvent
                {
                    EventType = NotificationEventType.AuthenticationFailed,
                    Category = NotificationCategory.Authentication,
                    Severity = NotificationSeverity.Security,
                    Emoji = "🚨",
                    Title = "Emby 登录失败",
                    UserName = req.Username,
                    // 短时间内同一用户+设备+IP 的连续失败去重，避免爆破产生通知风暴
                    DeduplicationKey = "authfail|" + (req.Username ?? "") + "|" + (req.DeviceId ?? "") + "|" + (req.RemoteAddress?.ToString() ?? ""),
                    DedupWindowSeconds = 30
                };
                evt.AddField("用户", req.Username, MessageDetailLevel.Simple);
                evt.AddField("客户端", req.App);
                evt.AddField("客户端版本", req.AppVersion, MessageDetailLevel.Detailed, true);
                evt.AddField("设备", req.DeviceName);
                evt.AddField("设备ID", maskedDevice, MessageDetailLevel.Detailed);
                evt.AddField("协议", req.Protocol, MessageDetailLevel.Detailed);
                evt.AddField("远程地址", maskedIp, MessageDetailLevel.Simple);
                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理登录失败事件异常: {0}", ex.Message); }
        }

        private void OnSessionStarted(object sender, SessionEventArgs e)
        {
            PublishSessionEvent(e, NotificationEventType.SessionStarted, "会话开始", "🟢",
                o => o.NotifySessionStarted);
        }

        private void OnSessionEnded(object sender, SessionEventArgs e)
        {
            PublishSessionEvent(e, NotificationEventType.SessionEnded, "会话结束", "⚪",
                o => o.NotifySessionEnded);
        }

        private void OnRemoteControlDisconnected(object sender, SessionEventArgs e)
        {
            PublishSessionEvent(e, NotificationEventType.RemoteControlDisconnected, "远程控制断开", "🔌",
                o => o.NotifyRemoteControlDisconnected);
        }

        private void PublishSessionEvent(SessionEventArgs e, NotificationEventType type, string title, string emoji, Func<PluginOptions, bool> enabled)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !enabled(options)) return;

                var si = e?.SessionInfo;
                if (si == null) return;

                var evt = new NotificationEvent
                {
                    EventType = type,
                    Category = NotificationCategory.Session,
                    Severity = NotificationSeverity.Information,
                    Emoji = emoji,
                    Title = title,
                    UserName = si.UserName,
                    ClientName = si.Client,
                    DeviceName = si.DeviceName
                };
                evt.AddField("用户", si.UserName, MessageDetailLevel.Simple);
                evt.AddField("客户端", si.Client);
                evt.AddField("设备", si.DeviceName);
                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理会话事件异常: {0}", ex.Message); }
        }

        private void OnAddedToParty(object sender, PartyEventArgs e)
        {
            PublishPartyEvent(NotificationEventType.PartyJoined, "加入同步播放", "👥",
                o => o.NotifyPartyJoined);
        }

        private void OnRemovedFromParty(object sender, PartyEventArgs e)
        {
            PublishPartyEvent(NotificationEventType.PartyLeft, "离开同步播放", "👋",
                o => o.NotifyPartyLeft);
        }

        private void PublishPartyEvent(NotificationEventType type, string title, string emoji, Func<PluginOptions, bool> enabled)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !enabled(options)) return;

                var evt = new NotificationEvent
                {
                    EventType = type,
                    Category = NotificationCategory.Session,
                    Severity = NotificationSeverity.Information,
                    Emoji = emoji,
                    Title = title,
                    Summary = "同步播放成员发生变化"
                };
                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理同步播放事件异常: {0}", ex.Message); }
        }
    }
}
