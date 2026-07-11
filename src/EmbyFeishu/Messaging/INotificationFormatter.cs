using EmbyFeishu.Models;

namespace EmbyFeishu.Messaging
{
    /// <summary>
    /// 通知消息格式化接口
    /// </summary>
    public interface INotificationFormatter
    {
        /// <summary>
        /// 将播放事件格式化为通知文本
        /// </summary>
        string Format(PlaybackNotificationEvent evt, PluginOptions options);
    }
}
