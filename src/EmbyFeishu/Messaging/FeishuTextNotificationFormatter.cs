using System.Text;
using EmbyFeishu.Feishu;
using EmbyFeishu.Models;

namespace EmbyFeishu.Messaging
{
    /// <summary>
    /// 飞书文本消息格式化器。基于 MessageComposer 的筛选结果渲染纯文本。
    /// </summary>
    public class FeishuTextNotificationFormatter : INotificationFormatter
    {
        public object BuildBody(NotificationEvent evt, PluginOptions options)
        {
            var text = BuildText(evt, options);
            return new FeishuWebhookRequest
            {
                msg_type = "text",
                content = new FeishuTextContent { text = text }
            };
        }

        /// <summary>
        /// 渲染文本正文（供测试与卡片回退复用）。
        /// </summary>
        public static string BuildText(NotificationEvent evt, PluginOptions options)
        {
            var msg = MessageComposer.Compose(evt, options);
            var sb = new StringBuilder();

            sb.AppendLine(msg.Header);
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(msg.Summary))
            {
                sb.AppendLine(msg.Summary);
            }

            foreach (var field in msg.VisibleFields)
            {
                // 空字段在 AddField/Compose 阶段已被过滤，这里再兜底一次
                if (!string.IsNullOrWhiteSpace(field.Value))
                {
                    sb.AppendLine(field.Label + "：" + field.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(msg.ServerText))
            {
                sb.AppendLine("服务器：" + msg.ServerText);
            }

            if (!string.IsNullOrWhiteSpace(msg.TimeText))
            {
                sb.AppendLine("时间：" + msg.TimeText);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
