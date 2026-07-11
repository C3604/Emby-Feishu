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
        /// 发送文本消息到飞书 Webhook
        /// </summary>
        Task<WebhookSendResult> SendTextAsync(string webhookUrl, string text, int timeoutMs, CancellationToken cancellationToken);
    }
}
