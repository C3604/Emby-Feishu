using EmbyFeishu.Models;

namespace EmbyFeishu.Messaging
{
    /// <summary>
    /// 通知消息格式化接口。将统一事件模型转换为可直接序列化发送的飞书请求体。
    /// </summary>
    public interface INotificationFormatter
    {
        /// <summary>
        /// 构造飞书请求体（文本或卡片），由客户端负责序列化。
        /// </summary>
        object BuildBody(NotificationEvent evt, PluginOptions options);
    }
}
