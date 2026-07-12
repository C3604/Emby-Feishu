using System.Threading;
using System.Threading.Tasks;

namespace EmbyFeishu.Feishu
{
    /// <summary>
    /// 飞书 Webhook 客户端接口
    /// </summary>
    public interface IFeishuWebhookClient
    {
        /// <summary>
        /// 发送任意结构化请求体（文本或卡片）到飞书 Webhook。
        /// body 由格式化器构造，客户端负责序列化与发送，不做字符串拼接。
        /// </summary>
        Task<WebhookSendResult> SendAsync(string webhookUrl, object body, int timeoutMs, CancellationToken cancellationToken);

        /// <summary>
        /// 发送纯文本消息（便捷封装）
        /// </summary>
        Task<WebhookSendResult> SendTextAsync(string webhookUrl, string text, int timeoutMs, CancellationToken cancellationToken);
    }
}
