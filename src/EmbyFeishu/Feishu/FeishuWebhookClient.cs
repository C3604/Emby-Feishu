using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbyFeishu.Infrastructure;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
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

        public async Task<WebhookSendResult> SendTextAsync(string webhookUrl, string text, int timeoutMs, CancellationToken cancellationToken)
        {
            var maskedUrl = WebhookMasker.Mask(webhookUrl);

            try
            {
                var request = new FeishuWebhookRequest
                {
                    msg_type = "text",
                    content = new FeishuTextContent { text = text }
                };

                var jsonMemory = _jsonSerializer.SerializeToString(request);
                var jsonBody = jsonMemory.ToString();

                var httpRequest = new HttpRequestOptions
                {
                    Url = webhookUrl,
                    RequestContentType = "application/json",
                    RequestContent = jsonBody.AsMemory(),
                    TimeoutMs = timeoutMs,
                    CancellationToken = cancellationToken,
                    LogErrors = false,
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
            catch (Exception ex)
            {
                var isTimeout = ex.Message != null && ex.Message.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
                var isNetwork = ex is System.Net.WebException || ex is IOException;
                var shouldRetry = isTimeout || isNetwork;
                _logger.Warn("[EmbyFeishu] Webhook 请求失败: {0} ({1})", ex.Message, maskedUrl);
                return WebhookSendResult.Fail(ex.Message, shouldRetry);
            }
        }
    }
}
