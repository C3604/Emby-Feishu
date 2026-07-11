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
    /// 后台通知调度器，线程安全，可停止
    /// </summary>
    public class NotificationDispatcher : INotificationDispatcher
    {
        private const int MaxQueueSize = 200;
        private readonly ConcurrentQueue<PlaybackNotificationEvent> _queue = new ConcurrentQueue<PlaybackNotificationEvent>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly IFeishuWebhookClient _webhookClient;
        private readonly INotificationFormatter _formatter;
        private readonly ILogger _logger;
        private Task _processingTask;
        private volatile int _queueCount;
        private volatile bool _disposed;

        public NotificationDispatcher(
            IFeishuWebhookClient webhookClient,
            INotificationFormatter formatter,
            ILogger logger)
        {
            _webhookClient = webhookClient;
            _formatter = formatter;
            _logger = logger;
        }

        public void Enqueue(PlaybackNotificationEvent evt)
        {
            if (_disposed) return;

            if (_queueCount >= MaxQueueSize)
            {
                _queue.TryDequeue(out _);
                Interlocked.Decrement(ref _queueCount);
                _logger.Warn("[EmbyFeishu] 通知队列已满（{0}），丢弃最旧消息", MaxQueueSize);
            }

            _queue.Enqueue(evt);
            Interlocked.Increment(ref _queueCount);
            _signal.Release();
            _logger.Debug("[EmbyFeishu] 通知已入队: {0} - {1}", evt.EventType, evt.ItemName ?? "未知");
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
                try
                {
                    _processingTask.Wait(TimeSpan.FromSeconds(5));
                }
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
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // 关闭竞态：信号量已释放，安全退出
                    break;
                }

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

        private async Task SendNotificationAsync(PlaybackNotificationEvent evt, CancellationToken token)
        {
            var options = Plugin.Instance?.GetPluginOptions();
            if (options == null || !options.Enabled) return;

            var text = _formatter.Format(evt, options);
            var timeoutMs = options.RequestTimeoutSeconds * 1000;

            var result = await _webhookClient.SendTextAsync(options.WebhookUrl, text, timeoutMs, token).ConfigureAwait(false);

            if (result.Success)
            {
                _logger.Info("[EmbyFeishu] 通知发送成功: {0} - {1}", evt.EventType, evt.UserName ?? "未知");
                return;
            }

            _logger.Warn("[EmbyFeishu] 通知发送失败: {0}", result.ErrorMessage ?? "未知错误");

            if (result.ShouldRetry && !token.IsCancellationRequested)
            {
                _logger.Info("[EmbyFeishu] 等待 1 秒后重试...");
                try
                {
                    await Task.Delay(1000, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                var retryResult = await _webhookClient.SendTextAsync(options.WebhookUrl, text, timeoutMs, token).ConfigureAwait(false);
                if (retryResult.Success)
                {
                    _logger.Info("[EmbyFeishu] 重试成功: {0}", evt.EventType);
                }
                else
                {
                    _logger.Warn("[EmbyFeishu] 重试仍然失败: {0}", retryResult.ErrorMessage ?? "未知错误");
                }
            }
        }
    }
}
