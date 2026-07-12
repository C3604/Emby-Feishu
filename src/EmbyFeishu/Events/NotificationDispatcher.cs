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
    /// 支持按配置选择文本或卡片格式，卡片失败时按配置回退文本一次。
    /// 在构建飞书请求时统一注入关键词和签名。
    /// </summary>
    public class NotificationDispatcher : INotificationDispatcher
    {
        private const int MaxQueueSize = 200;
        private readonly ConcurrentQueue<NotificationEvent> _queue = new ConcurrentQueue<NotificationEvent>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly IFeishuWebhookClient _webhookClient;
        private readonly INotificationFormatter _textFormatter;
        private readonly INotificationFormatter _cardFormatter;
        private readonly IFeishuMessageSecurityDecorator _securityDecorator;
        private readonly ILogger _logger;
        private Task _processingTask;
        private volatile int _queueCount;
        private volatile bool _disposed;

        public NotificationDispatcher(
            IFeishuWebhookClient webhookClient,
            INotificationFormatter textFormatter,
            INotificationFormatter cardFormatter,
            IFeishuMessageSecurityDecorator securityDecorator,
            ILogger logger)
        {
            _webhookClient = webhookClient;
            _textFormatter = textFormatter;
            _cardFormatter = cardFormatter;
            _securityDecorator = securityDecorator;
            _logger = logger;
        }

        public void Enqueue(NotificationEvent evt)
        {
            if (_disposed || evt == null) return;

            if (_queueCount >= MaxQueueSize)
            {
                _queue.TryDequeue(out _);
                Interlocked.Decrement(ref _queueCount);
                _logger.Warn("[EmbyFeishu] 通知队列已满（{0}），丢弃最旧消息", MaxQueueSize);
            }

            _queue.Enqueue(evt);
            Interlocked.Increment(ref _queueCount);
            _signal.Release();
            _logger.Debug("[EmbyFeishu] 通知已入队: {0} - {1}", evt.EventType, evt.ItemName ?? evt.UserName ?? "");
        }

        public void Start()
        {
            _processingTask = Task.Run(() => ProcessLoop());
            _logger.Info("[EmbyFeishu] 通知调度器已启动");
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

                while (_queue.TryDequeue(out var evt))
                {
                    Interlocked.Decrement(ref _queueCount);
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

            // 主格式发送（含一次瞬时错误重试）
            var primaryFormatter = useCard ? _cardFormatter : _textFormatter;
            var result = await SendWithRetryAsync(evt, primaryFormatter, options, webhookUrl, timeoutMs, token).ConfigureAwait(false);

            if (result.Success)
            {
                _logger.Info("[EmbyFeishu] 通知发送成功: {0} - {1}", evt.EventType, evt.UserName ?? evt.ItemName ?? "");
                return;
            }

            // 卡片失败且允许回退：改用文本发送一次
            if (useCard && options.FallbackToTextOnCardFailure && !token.IsCancellationRequested)
            {
                _logger.Warn("[EmbyFeishu] 卡片发送失败，回退为文本: {0}", result.ErrorMessage ?? "未知错误");
                var textResult = await SendWithRetryAsync(evt, _textFormatter, options, webhookUrl, timeoutMs, token).ConfigureAwait(false);
                if (textResult.Success)
                {
                    _logger.Info("[EmbyFeishu] 文本回退发送成功: {0}", evt.EventType);
                    return;
                }
                _logger.Warn("[EmbyFeishu] 文本回退仍失败: {0}", textResult.ErrorMessage ?? "未知错误");
                return;
            }

            _logger.Warn("[EmbyFeishu] 通知发送失败: {0}", result.ErrorMessage ?? "未知错误");
        }

        /// <summary>
        /// 用指定格式化器发送，瞬时错误最多重试一次。
        /// 每次尝试都重新应用安全装饰（签名重新生成）。
        /// </summary>
        private async Task<WebhookSendResult> SendWithRetryAsync(
            NotificationEvent evt, INotificationFormatter formatter, PluginOptions options, string url, int timeoutMs, CancellationToken token)
        {
            return await TrySendAsync(evt, formatter, options, url, timeoutMs, token, isRetry: false).ConfigureAwait(false);
        }

        private async Task<WebhookSendResult> TrySendAsync(
            NotificationEvent evt, INotificationFormatter formatter, PluginOptions options,
            string url, int timeoutMs, CancellationToken token, bool isRetry)
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

            // 将格式化器返回的 FeishuWebhookRequest 转换为字典以便安全装饰
            var requestBody = ConvertToDictionary(body, options);
            // 应用签名+时间戳
            requestBody = (Dictionary<string, object>)_securityDecorator.DecorateRequest(requestBody, options);

            var result = await _webhookClient.SendAsync(url, requestBody, timeoutMs, token).ConfigureAwait(false);
            if (result.Success || !result.ShouldRetry || token.IsCancellationRequested)
                return result;

            // 重试时重新生成签名
            if (!isRetry)
            {
                try { await Task.Delay(1000, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return result; }

                return await TrySendAsync(evt, formatter, options, url, timeoutMs, token, isRetry: true).ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        /// 将格式化器输出的对象转为字典，同时应用文本/卡片关键词装饰。
        /// 确保 FeishuWebhookRequest 和字典格式都能处理。
        /// </summary>
        private Dictionary<string, object> ConvertToDictionary(object body, PluginOptions options)
        {
            if (body is Dictionary<string, object> dict)
            {
                // 如果是卡片格式（msg_type=interactive），先装饰卡片内容再装饰请求
                if (dict.TryGetValue("msg_type", out var mt) && mt is string msgType && msgType == "interactive")
                {
                    dict = (Dictionary<string, object>)_securityDecorator.DecorateCard(dict, options);
                }
                else if (dict.TryGetValue("msg_type", out var mt2) && mt2 is string msgType2 && msgType2 == "text")
                {
                    // 文本格式：装饰 text 内容
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
                // 文本格式的 FeishuWebhookRequest
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

            // 未知类型，尝试作为通用对象序列化
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
