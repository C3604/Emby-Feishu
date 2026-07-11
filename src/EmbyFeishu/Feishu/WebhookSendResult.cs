namespace EmbyFeishu.Feishu
{
    /// <summary>
    /// Webhook 发送结果
    /// </summary>
    public class WebhookSendResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public bool ShouldRetry { get; set; }

        public static WebhookSendResult Ok()
        {
            return new WebhookSendResult { Success = true };
        }

        public static WebhookSendResult Fail(string message, bool shouldRetry = false)
        {
            return new WebhookSendResult { Success = false, ErrorMessage = message, ShouldRetry = shouldRetry };
        }
    }
}
