using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbyFeishu.Infrastructure;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;

namespace EmbyFeishu.Feishu
{
    /// <summary>
    /// 飞书 Webhook 客户端实现
    /// </summary>
    public class FeishuWebhookClient : IFeishuWebhookClient
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;

        public FeishuWebhookClient(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

        public Task<WebhookSendResult> SendTextAsync(string webhookUrl, string text, int timeoutMs, CancellationToken cancellationToken)
        {
            var request = new FeishuWebhookRequest
            {
                msg_type = "text",
                content = new FeishuTextContent { text = text }
            };
            return SendAsync(webhookUrl, request, timeoutMs, cancellationToken);
        }

        public async Task<WebhookSendResult> SendAsync(string webhookUrl, object body, int timeoutMs, CancellationToken cancellationToken)
        {
            var maskedUrl = WebhookMasker.Mask(webhookUrl);

            try
            {
                var jsonMemory = _jsonSerializer.SerializeToString(body);
                var jsonBody = jsonMemory.ToString();

                var httpRequest = new HttpRequestOptions
                {
                    Url = webhookUrl,
                    RequestContentType = "application/json",
                    RequestContent = jsonBody.AsMemory(),
                    TimeoutMs = timeoutMs,
                    CancellationToken = cancellationToken,
                    // 关闭 Emby HttpClient 自带的请求/响应/错误日志，避免完整 Webhook 地址被写入 Emby 日志
                    LogErrors = false,
                    LogRequest = false,
                    LogResponse = false,
                    BufferContent = false
                };

                using (var response = await _httpClient.Post(httpRequest).ConfigureAwait(false))
                {
                    if (response == null)
                    {
                        return WebhookSendResult.Fail("未收到响应", true);
                    }

                    using (var stream = response.Content)
                    {
                        if (stream == null)
                        {
                            return WebhookSendResult.Fail("响应内容为空", true);
                        }

                        string responseBody;
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            responseBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                        }

                        if (string.IsNullOrWhiteSpace(responseBody))
                        {
                            _logger.Debug("[EmbyFeishu] Webhook 响应为空，视为成功 ({0})", maskedUrl);
                            return WebhookSendResult.Ok();
                        }

                        try
                        {
                            var feishuResponse = _jsonSerializer.DeserializeFromString<FeishuWebhookResponse>(responseBody);
                            if (feishuResponse != null && feishuResponse.IsSuccess)
                            {
                                return WebhookSendResult.Ok();
                            }

                            var errorMsg = feishuResponse != null
                                ? string.Format("飞书业务错误: code={0}, msg={1}", feishuResponse.code, feishuResponse.msg ?? "")
                                : "飞书响应解析失败";
                            _logger.Warn("[EmbyFeishu] {0} ({1})", errorMsg, maskedUrl);
                            return WebhookSendResult.Fail(errorMsg, false);
                        }
                        catch
                        {
                            _logger.Debug("[EmbyFeishu] 无法解析飞书响应，但请求已完成 ({0})", maskedUrl);
                            return WebhookSendResult.Ok();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Debug("[EmbyFeishu] 请求已取消 ({0})", maskedUrl);
                return WebhookSendResult.Fail("请求已取消", false);
            }
            catch (HttpException httpEx)
            {
                // 使用 Emby 提供的结构化信息判断是否可重试，而非匹配异常文本
                var shouldRetry = httpEx.IsTimedOut || IsRetryableStatus(httpEx.StatusCode);
                var desc = httpEx.StatusCode.HasValue
                    ? string.Format("HTTP {0}", (int)httpEx.StatusCode.Value)
                    : (httpEx.IsTimedOut ? "请求超时" : "网络请求异常");
                var safe = WebhookMasker.Sanitize(httpEx.Message, webhookUrl);
                _logger.Warn("[EmbyFeishu] Webhook 请求失败: {0} ({1})", desc, maskedUrl);
                return WebhookSendResult.Fail(desc + "：" + safe, shouldRetry);
            }
            catch (Exception ex)
            {
                var isNetwork = ex is WebException || ex is IOException;
                var safe = WebhookMasker.Sanitize(ex.Message, webhookUrl);
                _logger.Warn("[EmbyFeishu] Webhook 请求失败: {0} ({1})", safe, maskedUrl);
                return WebhookSendResult.Fail(safe, isNetwork);
            }
        }

        /// <summary>
        /// 判断 HTTP 状态码是否值得重试：仅对 429 和 5xx 重试，4xx 视为配置/请求错误不重试。
        /// </summary>
        private static bool IsRetryableStatus(HttpStatusCode? code)
        {
            if (!code.HasValue)
                return true; // 状态码未知，通常是网络层问题，允许一次重试

            var c = (int)code.Value;
            if (c == 429)
                return true;
            return c >= 500 && c <= 599;
        }
    }
}
