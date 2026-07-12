using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu.Configuration.Groups
{
    /// <summary>
    /// 第四组：播放通知 — 所有播放相关开关与参数。
    /// </summary>
    public class PlaybackNotificationGroup : EditableOptionsBase
    {
        public override string EditorTitle => "播放通知";

        [DisplayName("通知播放开始")]
        public bool NotifyPlaybackStarted { get; set; } = true;

        [DisplayName("通知播放停止")]
        public bool NotifyPlaybackStopped { get; set; } = true;

        [DisplayName("通知播放暂停")]
        [IsAdvanced]
        public bool NotifyPlaybackPaused { get; set; } = false;

        [DisplayName("通知播放恢复")]
        [IsAdvanced]
        public bool NotifyPlaybackResumed { get; set; } = false;

        [DisplayName("通知播放完成")]
        [Description("播放到片尾（PlayedToCompletion）时单独推送\"播放完成\"")]
        public bool NotifyPlaybackCompleted { get; set; } = true;

        [DisplayName("通知中途放弃")]
        [Description("未播放完成即停止时推送\"放弃播放\"")]
        [IsAdvanced]
        public bool NotifyPlaybackAbandoned { get; set; } = false;

        [DisplayName("通知播放方式变化")]
        [Description("直放/直接串流/转码状态发生变化时通知")]
        [IsAdvanced]
        public bool NotifyPlaybackMethodChanged { get; set; } = false;

        [DisplayName("通知播放进度里程碑")]
        [IsAdvanced]
        public bool NotifyPlaybackMilestones { get; set; } = false;

        [DisplayName("里程碑阈值（%）")]
        [Description("逗号分隔的百分比，如 25,50,75")]
        [IsAdvanced]
        [VisibleCondition(nameof(NotifyPlaybackMilestones), SimpleCondition.IsTrue)]
        public string PlaybackMilestones { get; set; } = "25,50,75";

        [DisplayName("最短播放秒数")]
        [Description("播放时长不足此秒数时不发送停止/放弃通知，范围 0～600")]
        [IsAdvanced]
        [MinValue(0)]
        [MaxValue(600)]
        public int MinimumStopSeconds { get; set; } = 5;

        [DisplayName("播放完成阈值（%）")]
        [Description("进度达到此百分比视为播放完成，范围 50～100")]
        [IsAdvanced]
        [MinValue(50)]
        [MaxValue(100)]
        public int CompletionThresholdPercent { get; set; } = 90;

        [DisplayName("仅通知视频播放")]
        [Description("开启后只有视频类型的播放会触发通知，忽略音频等")]
        public bool OnlyVideo { get; set; } = true;
    }
}
