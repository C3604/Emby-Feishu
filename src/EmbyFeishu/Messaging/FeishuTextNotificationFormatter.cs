using System.Text;
using EmbyFeishu.Infrastructure;
using EmbyFeishu.Models;

namespace EmbyFeishu.Messaging
{
    /// <summary>
    /// 飞书文本消息格式化器
    /// </summary>
    public class FeishuTextNotificationFormatter : INotificationFormatter
    {
        public string Format(PlaybackNotificationEvent evt, PluginOptions options)
        {
            var sb = new StringBuilder();

            sb.AppendLine(GetEventEmoji(evt.EventType) + " " + GetEventTitle(evt.EventType));
            sb.AppendLine();

            if (options.IncludeUserName && !string.IsNullOrWhiteSpace(evt.UserName))
            {
                sb.AppendLine("用户：" + evt.UserName);
            }

            if (options.IncludeMediaTitle)
            {
                var title = MediaTitleFormatter.Format(
                    evt.ItemName, evt.SeriesName,
                    options.IncludeSeriesEpisode ? evt.SeasonNumber : null,
                    options.IncludeSeriesEpisode ? evt.EpisodeNumber : null,
                    options.IncludeSeriesEpisode ? evt.EpisodeName : null);
                sb.AppendLine("媒体：" + title);
            }

            if (options.IncludeMediaType && !string.IsNullOrWhiteSpace(evt.MediaType))
            {
                sb.AppendLine("类型：" + evt.MediaType);
            }

            if (options.IncludePlaybackPosition && evt.EventType == PlaybackEventType.Stopped)
            {
                var pos = TimeFormatter.FormatTicks(evt.PlaybackPositionTicks);
                var total = TimeFormatter.FormatTicks(evt.RuntimeTicks);
                if (pos != null || total != null)
                {
                    sb.AppendLine("播放位置：" + (pos ?? "00:00") + " / " + (total ?? "未知"));
                }
            }

            if (options.IncludePlayedToCompletion && evt.EventType == PlaybackEventType.Stopped && evt.PlayedToCompletion.HasValue)
            {
                sb.AppendLine("播放完成：" + (evt.PlayedToCompletion.Value ? "是" : "否"));
            }

            if (options.IncludeClientName && !string.IsNullOrWhiteSpace(evt.ClientName))
            {
                sb.AppendLine("客户端：" + evt.ClientName);
            }

            if (options.IncludeDeviceName && !string.IsNullOrWhiteSpace(evt.DeviceName))
            {
                sb.AppendLine("设备：" + evt.DeviceName);
            }

            sb.AppendLine("时间：" + TimeFormatter.FormatDateTime(evt.OccurredAt));

            return sb.ToString().TrimEnd();
        }

        private static string GetEventEmoji(PlaybackEventType type)
        {
            switch (type)
            {
                case PlaybackEventType.Started: return "▶️";
                case PlaybackEventType.Stopped: return "⏹️";
                case PlaybackEventType.Paused: return "⏸️";
                case PlaybackEventType.Resumed: return "▶️";
                default: return "📢";
            }
        }

        private static string GetEventTitle(PlaybackEventType type)
        {
            switch (type)
            {
                case PlaybackEventType.Started: return "开始播放";
                case PlaybackEventType.Stopped: return "停止播放";
                case PlaybackEventType.Paused: return "暂停播放";
                case PlaybackEventType.Resumed: return "恢复播放";
                default: return "播放事件";
            }
        }
    }
}
