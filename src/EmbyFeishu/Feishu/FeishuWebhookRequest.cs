namespace EmbyFeishu.Feishu
{
    /// <summary>
    /// 飞书 Webhook 请求体
    /// </summary>
    public class FeishuWebhookRequest
    {
        public string msg_type { get; set; }
        public FeishuTextContent content { get; set; }
    }

    /// <summary>
    /// 飞书文本消息内容
    /// </summary>
    public class FeishuTextContent
    {
        public string text { get; set; }
    }
}
