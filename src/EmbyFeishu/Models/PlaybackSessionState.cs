using System;

namespace EmbyFeishu.Models
{
    /// <summary>
    /// 播放会话状态，用于暂停/恢复去重
    /// </summary>
    public class PlaybackSessionState
    {
        /// <summary>当前是否暂停</summary>
        public bool IsPaused { get; set; }

        /// <summary>会话创建时间，用于过期清理</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>最后更新时间</summary>
        public DateTime LastUpdatedAt { get; set; }
    }
}
