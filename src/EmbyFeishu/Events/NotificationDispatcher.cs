using System;
using System.Collections.Concurrent;
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
        private readonly ILogger _logger;
        private Task _processingTask;
        private volatile int _queueCount;
        private volatile bool _disposed;

        public NotificationDispatcher(
            IFeishuWebhookClient webhookClient,
            INotificationFormatter textFormatter,
            INotificationFormatter cardFormatter,
            ILogger logger)
        {
            _webhookClient = webhookClient;
            _textFormatter = textFormatter;
            _cardFormatter = cardFormatter;
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

            var timeoutMs = options.RequestTimeoutSeconds * 1000;
            var useCard = options.MessageFormat == MessageFormat.FeishuCard;

            // 主格式发送（含一次瞬时错误重试）
            var primaryFormatter = useCard ? _cardFormatter : _textFormatter;
            var result = await SendWithRetryAsync(evt, primaryFormatter, options.WebhookUrl, timeoutMs, token).ConfigureAwait(false);

            if (result.Success)
            {
                _logger.Info("[EmbyFeishu] 通知发送成功: {0} - {1}", evt.EventType, evt.UserName ?? evt.ItemName ?? "");
                return;
            }

            // 卡片失败且允许回退：改用文本发送一次（保证同一业务事件最多成功一次）
            if (useCard && options.FallbackToTextOnCardFailure && !token.IsCancellationRequested)
            {
                _logger.Warn("[EmbyFeishu] 卡片发送失败，回退为文本: {0}", result.ErrorMessage ?? "未知错误");
                var textResult = await SendWithRetryAsync(evt, _textFormatter, options.WebhookUrl, timeoutMs, token).ConfigureAwait(false);
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
        /// </summary>
        private async Task<WebhookSendResult> SendWithRetryAsync(
            NotificationEvent evt, INotificationFormatter formatter, string url, int timeoutMs, CancellationToken token)
        {
            object body;
            try
            {
                body = formatter.BuildBody(evt, Plugin.Instance.GetPluginOptions());
            }
            catch (Exception ex)
            {
                _logger.Error("[EmbyFeishu] 构造消息体失败: {0}", ex.Message);
                return WebhookSendResult.Fail("构造消息体失败", false);
            }

            var result = await _webhookClient.SendAsync(url, body, timeoutMs, token).ConfigureAwait(false);
            if (result.Success || !result.ShouldRetry || token.IsCancellationRequested)
                return result;

            try { await Task.Delay(1000, token).ConfigureAwait(false); }
            catch (OperationCanceledException) { return result; }

            return await _webhookClient.SendAsync(url, body, timeoutMs, token).ConfigureAwait(false);
        }
    }
}
