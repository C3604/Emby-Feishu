using System;

namespace EmbyFeishu.Models
{
    /// <summary>
    /// 播放通知事件数据模型（不可变，脱离 Emby 对象）
    /// </summary>
    public class PlaybackNotificationEvent
    {
        public PlaybackEventType EventType { get; set; }
        public DateTime OccurredAt { get; set; }
        public string PlaySessionId { get; set; }
        public string SessionId { get; set; }
        public string UserName { get; set; }
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public string MediaType { get; set; }
        public string SeriesName { get; set; }
        public int? SeasonNumber { get; set; }
        public int? EpisodeNumber { get; set; }
        public string EpisodeName { get; set; }
        public string ClientName { get; set; }
        public string DeviceName { get; set; }
        public long? PlaybackPositionTicks { get; set; }
        public long? RuntimeTicks { get; set; }
        public bool? PlayedToCompletion { get; set; }
    }
}
