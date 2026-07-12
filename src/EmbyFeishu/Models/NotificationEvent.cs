using System;
using System.Collections.Generic;

namespace EmbyFeishu.Models
{
    /// <summary>
    /// 单个展示字段。Value 必须已经过脱敏处理。
    /// MinLevel 表示该字段在哪个详细程度起显示。
    /// </summary>
    public class NotificationField
    {
        public string Label { get; set; }
        public string Value { get; set; }

        /// <summary>该字段起始生效的详细程度（Simple 最低）。默认 Standard。</summary>
        public MessageDetailLevel MinLevel { get; set; } = MessageDetailLevel.Standard;

        /// <summary>是否属于技术细节字段（仅 Detailed 且开启敏感技术细节时显示）</summary>
        public bool IsTechnical { get; set; }

        /// <summary>
        /// 自定义模式下对应的旧字段开关键（仅播放事件使用）。
        /// 见 EmbyFeishu.Messaging.CustomFieldKeys。
        /// </summary>
        public string CustomKey { get; set; }

        public NotificationField() { }

        public NotificationField(string label, string value, MessageDetailLevel minLevel = MessageDetailLevel.Standard, bool isTechnical = false, string customKey = null)
        {
            Label = label;
            Value = value;
            MinLevel = minLevel;
            IsTechnical = isTechnical;
            CustomKey = customKey;
        }
    }

    /// <summary>
    /// 统一的内部通知事件模型。所有 Emby 事件都会先转换为该不可变数据快照后再入队，
    /// 事件回调线程绝不把 Emby 的 Session/User/BaseItem 原始对象放入队列。
    /// </summary>
    public class NotificationEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString("N");
        public NotificationEventType EventType { get; set; }
        public NotificationCategory Category { get; set; }
        public NotificationSeverity Severity { get; set; } = NotificationSeverity.Information;
        public DateTime OccurredAt { get; set; } = DateTime.Now;

        /// <summary>标题（含图标由格式化器组合）</summary>
        public string Title { get; set; }

        /// <summary>图标 Emoji</summary>
        public string Emoji { get; set; }

        /// <summary>简短摘要，Simple 模式下可作为唯一正文</summary>
        public string Summary { get; set; }

        public string UserName { get; set; }
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public string ClientName { get; set; }
        public string DeviceName { get; set; }
        public string ServerName { get; set; }

        /// <summary>去重键，由事件源或策略计算</summary>
        public string DeduplicationKey { get; set; }

        /// <summary>去重时间窗口（秒）。0 表示不去重。</summary>
        public int DedupWindowSeconds { get; set; }

        /// <summary>展示字段（已脱敏）</summary>
        public List<NotificationField> Fields { get; set; } = new List<NotificationField>();

        /// <summary>
        /// 便捷方法：追加一个非空字段。value 为空则忽略（保证“空字段直接省略”）。
        /// </summary>
        public NotificationEvent AddField(string label, string value, MessageDetailLevel minLevel = MessageDetailLevel.Standard, bool isTechnical = false, string customKey = null)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Fields.Add(new NotificationField(label, value, minLevel, isTechnical, customKey));
            }
            return this;
        }
    }
}
