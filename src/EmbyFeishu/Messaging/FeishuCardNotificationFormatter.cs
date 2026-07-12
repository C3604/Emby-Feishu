using System.Collections.Generic;
using EmbyFeishu.Models;

namespace EmbyFeishu.Messaging
{
    /// <summary>
    /// 飞书交互卡片格式化器。使用结构化对象（字典/列表）构造卡片，由 IJsonSerializer 序列化，
    /// 不做任何 JSON 字符串拼接。Header 颜色按 Severity 映射。
    /// </summary>
    public class FeishuCardNotificationFormatter : INotificationFormatter
    {
        public object BuildBody(NotificationEvent evt, PluginOptions options)
        {
            var msg = MessageComposer.Compose(evt, options);
            var elements = new List<object>();

            // 摘要
            if (!string.IsNullOrWhiteSpace(msg.Summary))
            {
                elements.Add(new Dictionary<string, object>
                {
                    ["tag"] = "div",
                    ["text"] = new Dictionary<string, object>
                    {
                        ["tag"] = "lark_md",
                        ["content"] = msg.Summary
                    }
                });
            }

            // 关键字段（双列）
            var fieldElems = new List<object>();
            foreach (var field in msg.VisibleFields)
            {
                if (string.IsNullOrWhiteSpace(field.Value))
                    continue;

                fieldElems.Add(new Dictionary<string, object>
                {
                    ["is_short"] = true,
                    ["text"] = new Dictionary<string, object>
                    {
                        ["tag"] = "lark_md",
                        ["content"] = "**" + field.Label + "**\n" + field.Value
                    }
                });
            }

            if (fieldElems.Count > 0)
            {
                elements.Add(new Dictionary<string, object>
                {
                    ["tag"] = "div",
                    ["fields"] = fieldElems
                });
            }

            // 分隔线 + 页脚
            var footerParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(msg.ServerText))
                footerParts.Add(msg.ServerText);
            if (!string.IsNullOrWhiteSpace(msg.TimeText))
                footerParts.Add(msg.TimeText);

            if (footerParts.Count > 0)
            {
                elements.Add(new Dictionary<string, object> { ["tag"] = "hr" });
                elements.Add(new Dictionary<string, object>
                {
                    ["tag"] = "note",
                    ["elements"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["tag"] = "lark_md",
                            ["content"] = string.Join("  ·  ", footerParts)
                        }
                    }
                });
            }

            var card = new Dictionary<string, object>
            {
                ["config"] = new Dictionary<string, object> { ["wide_screen_mode"] = true },
                ["header"] = new Dictionary<string, object>
                {
                    ["template"] = SeverityColor(evt.Severity),
                    ["title"] = new Dictionary<string, object>
                    {
                        ["tag"] = "plain_text",
                        ["content"] = msg.Header
                    }
                },
                ["elements"] = elements
            };

            return new Dictionary<string, object>
            {
                ["msg_type"] = "interactive",
                ["card"] = card
            };
        }

        /// <summary>严重程度到飞书卡片主题色</summary>
        public static string SeverityColor(NotificationSeverity severity)
        {
            switch (severity)
            {
                case NotificationSeverity.Success: return "green";
                case NotificationSeverity.Warning: return "orange";
                case NotificationSeverity.Error: return "red";
                case NotificationSeverity.Security: return "red";
                default: return "blue";
            }
        }
    }
}
