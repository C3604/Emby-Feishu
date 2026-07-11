namespace EmbyFeishu.Feishu
{
    /// <summary>
    /// 飞书 Webhook 响应体
    /// </summary>
    public class FeishuWebhookResponse
    {
        public int code { get; set; }
        public string msg { get; set; }

        /// <summary>
        /// 飞书返回 code=0 为成功
        /// </summary>
        public bool IsSuccess => code == 0;
    }
}
