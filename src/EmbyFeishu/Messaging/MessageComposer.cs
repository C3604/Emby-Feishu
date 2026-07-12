using System.Collections.Generic;
using EmbyFeishu.Infrastructure;
using EmbyFeishu.Models;

namespace EmbyFeishu.Messaging
{
    /// <summary>自定义模式下播放事件的字段开关键</summary>
    public static class CustomFieldKeys
    {
        public const string UserName = "UserName";
        public const string MediaTitle = "MediaTitle";
        public const string MediaType = "MediaType";
        public const string ClientName = "ClientName";
        public const string DeviceName = "DeviceName";
        public const string PlaybackPosition = "PlaybackPosition";
        public const string PlayedToCompletion = "PlayedToCompletion";
    }

    /// <summary>组合后的消息（供文本与卡片格式化器共用）</summary>
    public class ComposedMessage
    {
        public string Header { get; set; }
        public string Summary { get; set; }
        public List<NotificationField> VisibleFields { get; set; } = new List<NotificationField>();
        public string TimeText { get; set; }
        public string ServerText { get; set; }
    }

    /// <summary>
    /// 根据事件、配置和详细程度筛选出应展示的字段。
    /// 文本与卡片格式化器都基于它，保证两种格式内容一致。
    /// </summary>
    public static class MessageComposer
    {
        public static ComposedMessage Compose(NotificationEvent evt, PluginOptions options)
        {
            var msg = new ComposedMessage
            {
                Header = (string.IsNullOrEmpty(evt.Emoji) ? "" : evt.Emoji + " ") + (evt.Title ?? "通知"),
                Summary = evt.Summary
            };

            var level = options.MessageDetailLevel;

            foreach (var field in evt.Fields)
            {
                if (ShouldShow(field, evt, options, level))
                {
                    msg.VisibleFields.Add(field);
                }
            }

            if (options.ShowEventTime)
            {
                msg.TimeText = TimeFormatter.FormatDateTime(evt.OccurredAt);
            }

            if (options.ShowServerName && !string.IsNullOrWhiteSpace(evt.ServerName))
            {
                msg.ServerText = evt.ServerName;
            }

            return msg;
        }

        private static bool ShouldShow(NotificationField field, NotificationEvent evt, PluginOptions options, MessageDetailLevel level)
        {
            // 技术敏感字段仅在 Detailed 且开启开关时展示
            if (field.IsTechnical)
            {
                return level == MessageDetailLevel.Detailed && options.ShowSensitiveTechnicalDetails;
            }

            switch (level)
            {
                case MessageDetailLevel.Simple:
                    return field.MinLevel == MessageDetailLevel.Simple;

                case MessageDetailLevel.Standard:
                    return field.MinLevel == MessageDetailLevel.Simple || field.MinLevel == MessageDetailLevel.Standard;

                case MessageDetailLevel.Detailed:
                    return true;

                case MessageDetailLevel.Custom:
                    if (evt.Category == NotificationCategory.Playback)
                        return IsCustomFieldEnabled(field.CustomKey, options);
                    // 非播放事件的自定义模式等同标准
                    return field.MinLevel == MessageDetailLevel.Simple || field.MinLevel == MessageDetailLevel.Standard;

                default:
                    return true;
            }
        }

        private static bool IsCustomFieldEnabled(string customKey, PluginOptions options)
        {
            switch (customKey)
            {
                case CustomFieldKeys.UserName: return options.IncludeUserName;
                case CustomFieldKeys.MediaTitle: return options.IncludeMediaTitle;
                case CustomFieldKeys.MediaType: return options.IncludeMediaType;
                case CustomFieldKeys.ClientName: return options.IncludeClientName;
                case CustomFieldKeys.DeviceName: return options.IncludeDeviceName;
                case CustomFieldKeys.PlaybackPosition: return options.IncludePlaybackPosition;
                case CustomFieldKeys.PlayedToCompletion: return options.IncludePlayedToCompletion;
                default: return false;
            }
        }
    }
}
