using System;
using EmbyFeishu.Models;
using MediaBrowser.Controller;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Updates;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 服务器状态事件源。需要重启/有可用更新仅在状态变为 true 时通知一次。
    /// 服务器启动/停止通知由 EntryPoint 在初始化完成/释放时触发（停止为尽力而为）。
    /// </summary>
    public class ServerEventSource : IEventSource
    {
        private readonly IServerApplicationHost _host;
        private readonly NotificationContext _ctx;
        private bool _started;

        public string Name => "Server";

        public ServerEventSource(IServerApplicationHost host, NotificationContext ctx)
        {
            _host = host;
            _ctx = ctx;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _host.ApplicationUpdated += OnApplicationUpdated;
            _host.HasPendingRestartChanged += OnHasPendingRestartChanged;
            _host.HasUpdateAvailableChanged += OnHasUpdateAvailableChanged;
            _host.EnterMaintenanceMode += OnEnterMaintenanceMode;
            _host.ExitMaintenanceMode += OnExitMaintenanceMode;
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _host.ApplicationUpdated -= OnApplicationUpdated;
            _host.HasPendingRestartChanged -= OnHasPendingRestartChanged;
            _host.HasUpdateAvailableChanged -= OnHasUpdateAvailableChanged;
            _host.EnterMaintenanceMode -= OnEnterMaintenanceMode;
            _host.ExitMaintenanceMode -= OnExitMaintenanceMode;
        }

        public void Dispose() => Stop();

        private void OnApplicationUpdated(object sender, GenericEventArgs<PackageVersionInfo> e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !options.NotifyApplicationUpdated) return;

                var pkg = e?.Argument;
                var evt = new NotificationEvent
                {
                    EventType = NotificationEventType.ApplicationUpdated,
                    Category = NotificationCategory.Server,
                    Severity = NotificationSeverity.Success,
                    Emoji = "⬆️",
                    Title = "Emby Server 已应用更新",
                    ServerName = _host.FriendlyName
                };
                evt.AddField("新版本", pkg?.versionStr, MessageDetailLevel.Simple);
                evt.AddField("组件", pkg?.name, MessageDetailLevel.Detailed);
                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理应用更新事件异常: {0}", ex.Message); }
        }

        private void OnHasPendingRestartChanged(object sender, EventArgs e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !options.NotifyRestartRequired) return;

                // 仅在状态变为“需要重启”时通知
                if (!_host.HasPendingRestart) return;

                var evt = new NotificationEvent
                {
                    EventType = NotificationEventType.RestartRequired,
                    Category = NotificationCategory.Server,
                    Severity = NotificationSeverity.Warning,
                    Emoji = "🔄",
                    Title = "Emby Server 需要重启",
                    Summary = "更新或配置等待应用",
                    ServerName = _host.FriendlyName
                };
                evt.AddField("当前版本", SafeVersion(), MessageDetailLevel.Standard);
                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理需要重启事件异常: {0}", ex.Message); }
        }

        private void OnHasUpdateAvailableChanged(object sender, EventArgs e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !options.NotifyUpdateAvailable) return;

                if (!_host.HasUpdateAvailable) return;

                var evt = new NotificationEvent
                {
                    EventType = NotificationEventType.UpdateAvailable,
                    Category = NotificationCategory.Server,
                    Severity = NotificationSeverity.Information,
                    Emoji = "🆕",
                    Title = "Emby Server 有可用更新",
                    ServerName = _host.FriendlyName
                };
                evt.AddField("当前版本", SafeVersion(), MessageDetailLevel.Simple);
                try { evt.AddField("可用版本", _host.AvailableVersion?.ToString(), MessageDetailLevel.Simple); } catch { }
                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理可用更新事件异常: {0}", ex.Message); }
        }

        private void OnEnterMaintenanceMode(object sender, EventArgs e)
            => PublishMaintenance(NotificationEventType.MaintenanceModeEntered, "进入维护模式", "🛠️", o => o.NotifyMaintenanceModeEntered);

        private void OnExitMaintenanceMode(object sender, EventArgs e)
            => PublishMaintenance(NotificationEventType.MaintenanceModeExited, "退出维护模式", "✅", o => o.NotifyMaintenanceModeExited);

        private void PublishMaintenance(NotificationEventType type, string title, string emoji, Func<PluginOptions, bool> enabled)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !enabled(options)) return;

                var evt = new NotificationEvent
                {
                    EventType = type,
                    Category = NotificationCategory.Server,
                    Severity = NotificationSeverity.Information,
                    Emoji = emoji,
                    Title = title,
                    ServerName = _host.FriendlyName
                };
                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理维护模式事件异常: {0}", ex.Message); }
        }

        private string SafeVersion()
        {
            try { return _host.ApplicationVersion?.ToString(); }
            catch { return null; }
        }
    }
}
