using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmbyFeishu.Feishu;
using EmbyFeishu.Messaging;
using EmbyFeishu.Models;
using MediaBrowser.Model.Logging;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 后台通知调度器，线程安全，可停止。承载统一事件模型 NotificationEvent。
    /// 支持优先级队列（安全事件优先）、指数退避重试、卡片回退文本。
    /// </summary>
    public class NotificationDispatcher : INotificationDispatcher
    {
        private const int DefaultMaxQueueSize = 200;
        private readonly ConcurrentQueue<NotificationEvent> _highPriorityQueue = new ConcurrentQueue<NotificationEvent>();
        private readonly ConcurrentQueue<NotificationEvent> _normalQueue = new ConcurrentQueue<NotificationEvent>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly IFeishuWebhookClient _webhookClient;
        private readonly INotificationFormatter _textFormatter;
        private readonly INotificationFormatter _cardFormatter;
        private readonly IFeishuMessageSecurityDecorator _securityDecorator;
        private readonly ILogger _logger;
        private readonly NotificationStatistics _statistics;
        private Task _processingTask;
        private volatile int _queueCount;
        private volatile bool _disposed;

        public NotificationStatistics Statistics => _statistics;

        public NotificationDispatcher(
            IFeishuWebhookClient webhookClient,
            INotificationFormatter textFormatter,
            INotificationFormatter cardFormatter,
            IFeishuMessageSecurityDecorator securityDecorator,
            ILogger logger)
            : this(webhookClient, textFormatter, cardFormatter, securityDecorator, logger, null)
        {
        }

        public NotificationDispatcher(
            IFeishuWebhookClient webhookClient,
            INotificationFormatter textFormatter,
            INotificationFormatter cardFormatter,
            IFeishuMessageSecurityDecorator securityDecorator,
            ILogger logger,
            NotificationStatistics statistics)
        {
            _webhookClient = webhookClient;
            _textFormatter = textFormatter;
            _cardFormatter = cardFormatter;
            _securityDecorator = securityDecorator;
            _logger = logger;
            _statistics = statistics ?? new NotificationStatistics();
        }

        public void Enqueue(NotificationEvent evt)
        {
            if (_disposed || evt == null) return;

            if (_queueCount >= DefaultMaxQueueSize)
            {
                if (_normalQueue.TryDequeue(out _))
                {
                    Interlocked.Decrement(ref _queueCount);
                }
                _statistics.RecordDropped();
                _logger.Warn("[EmbyFeishu] 通知队列已满（{0}），丢弃最旧消息", DefaultMaxQueueSize);
            }

            var isHighPriority = evt.Severity == NotificationSeverity.Security
                              || evt.Severity == NotificationSeverity.Error;

            if (isHighPriority)
                _highPriorityQueue.Enqueue(evt);
            else
                _normalQueue.Enqueue(evt);

            Interlocked.Increment(ref _queueCount);
            _statistics.UpdateQueueSize(_queueCount);
            _signal.Release();
            _logger.Debug("[EmbyFeishu] 通知已入队({0}): {1} - {2}",
                isHighPriority ? "高优先" : "普通",
                evt.EventType, evt.ItemName ?? evt.UserName ?? "");
        }

        public void Start()
        {
            _processingTask = Task.Run(() => ProcessLoop());
            _logger.Info("[EmbyFeishu] 通知调度器已启动（优先级队列 + 指数退避）");
        }

        public void Stop()
        {
            _cts.Cancel();
            _signal.Release();

            if (_processingTask != null)
            {
                try { _processingTask.Wait(TimeSpan.FromSeconds(5)); }
                catch (AggregateException) { }
            }

            _logger.Info("[EmbyFeishu] 通知调度器已停止");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _signal.Dispose();
            _cts.Dispose();
        }

        private async Task ProcessLoop()
        {
            var token = _cts.Token;

            while (!token.IsCancellationRequested && !_disposed)
            {
                try
                {
                    await _signal.WaitAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }

                if (token.IsCancellationRequested || _disposed) break;

                // 优先处理高优先级队列
                while (_highPriorityQueue.TryDequeue(out var highEvt))
                {
                    Interlocked.Decrement(ref _queueCount);
                    _statistics.UpdateQueueSize(_queueCount);
                    if (token.IsCancellationRequested || _disposed) break;

                    try
                    {
                        await SendNotificationAsync(highEvt, token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("[EmbyFeishu] 发送高优先通知时发生未预期异常: {0}", ex.Message);
                    }
                }

                // 再处理普通队列
                while (_normalQueue.TryDequeue(out var evt))
                {
                    Interlocked.Decrement(ref _queueCount);
                    _statistics.UpdateQueueSize(_queueCount);
                    if (token.IsCancellationRequested || _disposed) break;

                    try
                    {
                        await SendNotificationAsync(evt, token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("[EmbyFeishu] 发送通知时发生未预期异常: {0}", ex.Message);
                    }
                }
            }
        }

        private async Task SendNotificationAsync(NotificationEvent evt, CancellationToken token)
        {
            var options = Plugin.Instance?.GetPluginOptions();
            if (options == null || !options.Enabled) return;
            if (string.IsNullOrWhiteSpace(options.WebhookUrl)) return;

            var webhookUrl = options.WebhookUrl;
            var timeoutMs = options.RequestTimeoutSeconds * 1000;
            var useCard = options.MessageFormat == MessageFormat.FeishuCard;
            var maxRetries = Math.Max(0, Math.Min(options.MaxRetryCount, 3));

            // 主格式发送（含指数退避重试）
            var primaryFormatter = useCard ? _cardFormatter : _textFormatter;
            var result = await SendWithExponentialBackoffAsync(evt, primaryFormatter, options, webhookUrl, timeoutMs, maxRetries, token).ConfigureAwait(false);

            if (result.Success)
            {
                _statistics.RecordSent();
                _logger.Info("[EmbyFeishu] 通知发送成功: {0} - {1}", evt.EventType, evt.UserName ?? evt.ItemName ?? "");
                return;
            }

            // 卡片失败且允许回退：改用文本发送一次
            if (useCard && options.FallbackToTextOnCardFailure && !token.IsCancellationRequested)
            {
                _logger.Warn("[EmbyFeishu] 卡片发送失败，回退为文本: {0}", result.ErrorMessage ?? "未知错误");
                var textResult = await SendWithExponentialBackoffAsync(evt, _textFormatter, options, webhookUrl, timeoutMs, maxRetries, token).ConfigureAwait(false);
                if (textResult.Success)
                {
                    _statistics.RecordSent();
                    _logger.Info("[EmbyFeishu] 文本回退发送成功: {0}", evt.EventType);
                    return;
                }
                _statistics.RecordFailed();
                _logger.Warn("[EmbyFeishu] 文本回退仍失败: {0}", textResult.ErrorMessage ?? "未知错误");
                return;
            }

            _statistics.RecordFailed();
            _logger.Warn("[EmbyFeishu] 通知发送失败: {0}", result.ErrorMessage ?? "未知错误");
        }

        /// <summary>
        /// 用指数退避策略发送，延迟序列为 1s, 2s, 4s...
        /// 每次重试都重新构造消息体并重新生成签名。
        /// </summary>
        private async Task<WebhookSendResult> SendWithExponentialBackoffAsync(
            NotificationEvent evt, INotificationFormatter formatter, PluginOptions options,
            string url, int timeoutMs, int maxRetries, CancellationToken token)
        {
            WebhookSendResult lastResult = null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                if (token.IsCancellationRequested)
                    return lastResult ?? WebhookSendResult.Fail("已取消", false);

                // 重试前等待（指数退避）
                if (attempt > 0)
                {
                    _statistics.RecordRetry();
                    var delayMs = 1000 * (1 << (attempt - 1)); // 1s, 2s, 4s
                    _logger.Debug("[EmbyFeishu] 第 {0} 次重试，等待 {1}ms", attempt, delayMs);
                    try
                    {
                        await Task.Delay(delayMs, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return lastResult ?? WebhookSendResult.Fail("已取消", false);
                    }
                }

                lastResult = await TrySendOnceAsync(evt, formatter, options, url, timeoutMs, token).ConfigureAwait(false);

                if (lastResult.Success || !lastResult.ShouldRetry)
                    return lastResult;
            }

            return lastResult ?? WebhookSendResult.Fail("未知错误", false);
        }

        private async Task<WebhookSendResult> TrySendOnceAsync(
            NotificationEvent evt, INotificationFormatter formatter, PluginOptions options,
            string url, int timeoutMs, CancellationToken token)
        {
            object body;
            try
            {
                body = formatter.BuildBody(evt, options);
            }
            catch (Exception ex)
            {
                _logger.Error("[EmbyFeishu] 构造消息体失败: {0}", ex.Message);
                return WebhookSendResult.Fail("构造消息体失败", false);
            }

            var requestBody = ConvertToDictionary(body, options);
            requestBody = (Dictionary<string, object>)_securityDecorator.DecorateRequest(requestBody, options);

            return await _webhookClient.SendAsync(url, requestBody, timeoutMs, token).ConfigureAwait(false);
        }

        /// <summary>
        /// 将格式化器输出的对象转为字典，同时应用文本/卡片关键词装饰。
        /// </summary>
        private Dictionary<string, object> ConvertToDictionary(object body, PluginOptions options)
        {
            if (body is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("msg_type", out var mt) && mt is string msgType && msgType == "interactive")
                {
                    dict = (Dictionary<string, object>)_securityDecorator.DecorateCard(dict, options);
                }
                else if (dict.TryGetValue("msg_type", out var mt2) && mt2 is string msgType2 && msgType2 == "text")
                {
                    if (dict.TryGetValue("content", out var contentObj) && contentObj is Dictionary<string, object> contentDict)
                    {
                        if (contentDict.TryGetValue("text", out var textObj) && textObj is string text)
                        {
                            contentDict["text"] = _securityDecorator.DecorateText(text, options);
                        }
                    }
                }
                return dict;
            }

            if (body is FeishuWebhookRequest request)
            {
                var result = new Dictionary<string, object>();
                result["msg_type"] = request.msg_type ?? "text";

                if (request.msg_type == "interactive" && request.card != null)
                {
                    result["card"] = request.card;
                    result = (Dictionary<string, object>)_securityDecorator.DecorateCard(result, options);
                }
                else
                {
                    var text = request.content?.text ?? "";
                    text = _securityDecorator.DecorateText(text, options);
                    result["content"] = new Dictionary<string, object>
                    {
                        ["text"] = text
                    };
                }
                return result;
            }

            return new Dictionary<string, object>
            {
                ["msg_type"] = "text",
                ["content"] = new Dictionary<string, object>
                {
                    ["text"] = _securityDecorator.DecorateText(body?.ToString() ?? "", options)
                }
            };
        }
    }
}
