namespace EmbyFeishu.Feishu
{
    /// <summary>
    /// 飞书 Webhook 请求体。可为 text 或 interactive 类型。
    /// timestamp 和 sign 在发送时由安全装饰器动态加入（不保存在请求对象中），
    /// 因此每次发送均重新生成。
    /// </summary>
    public class FeishuWebhookRequest
    {
        public string msg_type { get; set; }
        public FeishuTextContent content { get; set; }
        /// <summary>仅 msg_type=interactive 时使用</summary>
        public object card { get; set; }
    }

    /// <summary>
    /// 飞书文本消息内容
    /// </summary>
    public class FeishuTextContent
    {
        public string text { get; set; }
    }
}
