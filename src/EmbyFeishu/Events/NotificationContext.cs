using System;
using EmbyFeishu.Infrastructure;
using EmbyFeishu.Models;
using MediaBrowser.Model.Logging;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 事件源共享上下文。集中提供配置读取、脱敏、服务器名、日志，
    /// 并统一 Publish 入口（先过策略去重/限流，再入队后台调度）。
    /// </summary>
    public class NotificationContext
    {
        public ILogger Logger { get; }
        public ISensitiveDataSanitizer Sanitizer { get; }
        public INotificationPolicy Policy { get; }
        public INotificationDispatcher Dispatcher { get; }
        public Func<PluginOptions> GetOptions { get; }
        public Func<string> GetServerName { get; }

        public NotificationContext(
            ILogger logger,
            ISensitiveDataSanitizer sanitizer,
            INotificationPolicy policy,
            INotificationDispatcher dispatcher,
            Func<PluginOptions> getOptions,
            Func<string> getServerName)
        {
            Logger = logger;
            Sanitizer = sanitizer;
            Policy = policy;
            Dispatcher = dispatcher;
            GetOptions = getOptions;
            GetServerName = getServerName;
        }

        /// <summary>当前是否已配置且启用</summary>
        public PluginOptions GetEnabledOptions()
        {
            var options = GetOptions();
            if (options == null || !options.Enabled)
                return null;
            if (string.IsNullOrWhiteSpace(options.WebhookUrl))
                return null;
            return options;
        }

        /// <summary>
        /// 统一发布入口。补全服务器名，走策略判定后入队。绝不在此做网络请求。
        /// </summary>
        public void Publish(NotificationEvent evt)
        {
            if (evt == null)
                return;

            try
            {
                var options = GetEnabledOptions();
                if (options == null)
                    return;

                if (string.IsNullOrWhiteSpace(evt.ServerName))
                    evt.ServerName = GetServerName();

                var decision = Policy.Evaluate(evt, options);
                if (decision != PolicyDecision.Send)
                {
                    Logger.Debug("[EmbyFeishu] 事件被策略抑制({0}): {1}", decision, evt.EventType);
                    return;
                }

                Dispatcher.Enqueue(evt);
            }
            catch (Exception ex)
            {
                Logger.Error("[EmbyFeishu] 发布事件异常: {0}", ex.Message);
            }
        }
    }
}
