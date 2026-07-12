using System;
using System.Collections.Generic;
using System.Threading;
using EmbyFeishu.Events;
using EmbyFeishu.Feishu;
using EmbyFeishu.Infrastructure;
using EmbyFeishu.Messaging;
using EmbyFeishu.Models;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;

namespace EmbyFeishu
{
    /// <summary>
    /// 插件入口点，负责组装各事件源、后台队列与生命周期管理。
    /// 事件回调只提取数据并入队，绝不在回调线程做网络请求。
    /// </summary>
    public class EntryPoint : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ITaskManager _taskManager;
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        private readonly PlaybackStateTracker _stateTracker;
        private readonly NotificationPolicy _policy;
        private readonly NotificationDispatcher _dispatcher;
        private readonly LibraryAggregator _aggregator;
        private readonly FeishuWebhookClient _webhookClient;
        private readonly NotificationContext _context;
        private readonly List<IEventSource> _sources = new List<IEventSource>();
        private UserDataEventSource _userDataSource;

        private Timer _cleanupTimer;

        public EntryPoint(
            ISessionManager sessionManager,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IUserDataManager userDataManager,
            ITaskManager taskManager,
            IServerApplicationHost appHost,
            ILogManager logManager,
            IHttpClient httpClient,
            IJsonSerializer jsonSerializer)
        {
            _sessionManager = sessionManager;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
            _taskManager = taskManager;
            _appHost = appHost;
            _logger = logManager.GetLogger("EmbyFeishu");
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;

            _stateTracker = new PlaybackStateTracker();
            _policy = new NotificationPolicy();
            _webhookClient = new FeishuWebhookClient(_httpClient, _jsonSerializer, _logger);
            _dispatcher = new NotificationDispatcher(
                _webhookClient,
                new FeishuTextNotificationFormatter(),
                new FeishuCardNotificationFormatter(),
                _logger);

            _context = new NotificationContext(
                _logger,
                new SensitiveDataSanitizer(),
                _policy,
                _dispatcher,
                () => Plugin.Instance?.GetPluginOptions(),
                GetServerName);

            _aggregator = new LibraryAggregator(
                evt => _context.Publish(evt),
                () => Plugin.Instance?.GetPluginOptions(),
                GetServerName,
                _logger);
        }

        public void Run()
        {
            _logger.Info("[EmbyFeishu] 插件入口点启动");

            // 后台队列必须先启动（关键路径，失败记 Error）
            try
            {
                _dispatcher.Start();
            }
            catch (Exception ex)
            {
                _logger.Error("[EmbyFeishu] 通知调度器启动失败: {0}", ex.Message);
                return;
            }

            // 组装事件源
            _sources.Add(new PlaybackEventSource(_sessionManager, _context, _stateTracker));
            _sources.Add(new SessionEventSource(_sessionManager, _context));
            _sources.Add(new UserEventSource(_userManager, _context));
            _sources.Add(new LibraryEventSource(_libraryManager, _context, _aggregator));
            _userDataSource = new UserDataEventSource(_userDataManager, _context);
            _sources.Add(_userDataSource);
            _sources.Add(new TaskEventSource(_taskManager, _context));
            _sources.Add(new ServerEventSource(_appHost, _context));

            // Live TV 可选：无法注入时跳过，不影响其他功能
            TryAddLiveTv();

            // 逐个启动，单个可选源失败不影响其他
            foreach (var src in _sources)
            {
                try
                {
                    src.Start();
                    _logger.Info("[EmbyFeishu] 事件源已启动: {0}", src.Name);
                }
                catch (Exception ex)
                {
                    _logger.Error("[EmbyFeishu] 事件源 {0} 启动失败: {1}", src.Name, ex.Message);
                }
            }

            // 定时清理
            _cleanupTimer = new Timer(_ => SafeCleanup(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            // 服务器启动通知（在初始化、订阅、队列就绪之后发送）
            PublishServerStarted();

            var options = Plugin.Instance?.GetPluginOptions();
            _logger.Info("[EmbyFeishu] 插件状态: 启用={0}", options?.Enabled ?? false);
        }

        private void TryAddLiveTv()
        {
            try
            {
                var liveTv = _appHost.Resolve<ILiveTvManager>();
                if (liveTv != null)
                {
                    _sources.Add(new LiveTvEventSource(liveTv, _context));
                    _logger.Info("[EmbyFeishu] 已加载 Live TV 事件源");
                }
                else
                {
                    _logger.Info("[EmbyFeishu] 未检测到 Live TV，跳过该事件源");
                }
            }
            catch (Exception ex)
            {
                _logger.Info("[EmbyFeishu] Live TV 不可用，跳过该事件源: {0}", ex.Message);
            }
        }

        private void PublishServerStarted()
        {
            try
            {
                var options = _context.GetEnabledOptions();
                if (options == null || !options.NotifyServerStarted) return;

                var evt = new NotificationEvent
                {
                    EventType = NotificationEventType.ServerStarted,
                    Category = NotificationCategory.Server,
                    Severity = NotificationSeverity.Success,
                    Emoji = "🚀",
                    Title = "Emby Server 已启动",
                    ServerName = GetServerName()
                };
                try { evt.AddField("版本", _appHost.ApplicationVersion?.ToString(), MessageDetailLevel.Simple); } catch { }
                try { evt.AddField("系统", _appHost.OperatingSystemDisplayName, MessageDetailLevel.Detailed); } catch { }
                _context.Publish(evt);
            }
            catch (Exception ex) { _logger.Error("[EmbyFeishu] 发送服务器启动通知异常: {0}", ex.Message); }
        }

        private void SafeCleanup()
        {
            try { _stateTracker.CleanupStale(); } catch (Exception ex) { _logger.Debug("[EmbyFeishu] 清理会话异常: {0}", ex.Message); }
            try { _userDataSource?.CleanupStale(); } catch (Exception ex) { _logger.Debug("[EmbyFeishu] 清理用户数据缓存异常: {0}", ex.Message); }
            try { _policy.CleanupStale(); } catch (Exception ex) { _logger.Debug("[EmbyFeishu] 清理策略缓存异常: {0}", ex.Message); }

            // 汇总被限流抑制的通知数量
            try
            {
                var suppressed = _policy.DrainSuppressedCount();
                if (suppressed > 0)
                {
                    var options = _context.GetEnabledOptions();
                    if (options != null && options.AggregateWhenRateLimited)
                    {
                        var evt = new NotificationEvent
                        {
                            EventType = NotificationEventType.ServerStarted, // 复用服务器分类的通用形态
                            Category = NotificationCategory.Server,
                            Severity = NotificationSeverity.Warning,
                            Emoji = "⏳",
                            Title = "通知已限流",
                            Summary = string.Format("过去一段时间有 {0} 条通知因超过频率上限被抑制", suppressed),
                            ServerName = GetServerName()
                        };
                        _dispatcher.Enqueue(evt);
                    }
                }
            }
            catch (Exception ex) { _logger.Debug("[EmbyFeishu] 限流汇总异常: {0}", ex.Message); }
        }

        private string GetServerName()
        {
            try { return _appHost.FriendlyName; }
            catch { return "Emby Server"; }
        }

        public void Dispose()
        {
            _logger.Info("[EmbyFeishu] 正在释放插件资源...");

            // 尽力而为的服务器停止通知（不长时间阻塞；强杀/断电无法保证）
            TryPublishServerStopping();

            foreach (var src in _sources)
            {
                try { src.Stop(); src.Dispose(); }
                catch (Exception ex) { _logger.Debug("[EmbyFeishu] 停止事件源 {0} 异常: {1}", src.Name, ex.Message); }
            }
            _sources.Clear();

            _cleanupTimer?.Dispose();
            _aggregator?.Dispose();
            _dispatcher?.Dispose();

            _logger.Info("[EmbyFeishu] 插件资源已释放");
        }

        private void TryPublishServerStopping()
        {
            try
            {
                var options = _context.GetEnabledOptions();
                if (options == null || !options.NotifyServerStopping) return;

                var text = string.Format("🛑 Emby Server 正在停止\n\n服务器：{0}\n时间：{1}",
                    GetServerName(), TimeFormatter.FormatDateTime(DateTime.Now));

                // 直接短超时发送，最多等待 2 秒，避免拖慢关闭
                var task = _webhookClient.SendTextAsync(options.WebhookUrl, text, 2000, CancellationToken.None);
                task.Wait(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                _logger.Debug("[EmbyFeishu] 发送服务器停止通知失败（可忽略）: {0}", ex.Message);
            }
        }
    }
}
