using System;
using System.Collections.Generic;

namespace EmbyFeishu.Models
{
    /// <summary>
    /// 播放会话状态，用于暂停/恢复去重、里程碑与播放方式变化判断。
    /// </summary>
    public class PlaybackSessionState
    {
        /// <summary>当前是否暂停</summary>
        public bool IsPaused { get; set; }

        /// <summary>会话创建时间，用于过期清理</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>最后更新时间</summary>
        public DateTime LastUpdatedAt { get; set; }

        /// <summary>已发送过的里程碑阈值</summary>
        public HashSet<int> MilestonesSent { get; set; } = new HashSet<int>();

        /// <summary>上次观测到的播放方式（DirectPlay/DirectStream/Transcode）。null 表示尚未建立基线。</summary>
        public string LastPlayMethod { get; set; }

        /// <summary>是否已针对本次播放发送过“播放完成”（用于与普通停止互斥）</summary>
        public bool CompletedNotified { get; set; }
    }
}
