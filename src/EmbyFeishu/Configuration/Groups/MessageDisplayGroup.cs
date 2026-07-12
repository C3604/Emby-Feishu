using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using EmbyFeishu.Models;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu.Configuration.Groups
{
    /// <summary>
    /// 第三组：消息显示 — 文本/卡片模式、详细程度、显示选项。
    /// </summary>
    public class MessageDisplayGroup : EditableOptionsBase
    {
        public override string EditorTitle => "消息显示";

        [DisplayName("消息格式")]
        [Description("Text=纯文本（默认，与旧版一致）；FeishuCard=飞书交互卡片")]
        public MessageFormat MessageFormat { get; set; } = MessageFormat.Text;

        [DisplayName("详细程度")]
        [Description("Simple=极简；Standard=标准；Detailed=含技术细节；Custom=自定义（播放事件沿用下方字段开关）。默认 Custom 以保持旧版外观")]
        public MessageDetailLevel MessageDetailLevel { get; set; } = MessageDetailLevel.Custom;

        [DisplayName("显示服务器名称")]
        [IsAdvanced]
        public bool ShowServerName { get; set; } = true;

        [DisplayName("显示事件时间")]
        [IsAdvanced]
        public bool ShowEventTime { get; set; } = true;

        [DisplayName("IP 显示方式")]
        [Description("Hidden=隐藏；Masked=脱敏（默认）；Full=完整")]
        [IsAdvanced]
        public IpAddressDisplayMode IpAddressDisplayMode { get; set; } = IpAddressDisplayMode.Masked;

        [DisplayName("设备 ID 显示方式")]
        [Description("Hidden=隐藏；Masked=脱敏（默认）；Full=完整")]
        [IsAdvanced]
        public DeviceIdDisplayMode DeviceIdDisplayMode { get; set; } = DeviceIdDisplayMode.Masked;

        [DisplayName("卡片失败时回退文本")]
        [Description("卡片发送失败时尝试改用文本发送一次")]
        [IsAdvanced]
        public bool FallbackToTextOnCardFailure { get; set; } = true;

        // 以下为 Custom 模式下的 Include* 开关
        [DisplayName("[自定义] 显示用户名")]
        [IsAdvanced]
        [VisibleCondition(nameof(MessageDetailLevel), ValueCondition.IsEqual, MessageDetailLevel.Custom)]
        public bool IncludeUserName { get; set; } = true;

        [DisplayName("[自定义] 显示媒体标题")]
        [IsAdvanced]
        [VisibleCondition(nameof(MessageDetailLevel), ValueCondition.IsEqual, MessageDetailLevel.Custom)]
        public bool IncludeMediaTitle { get; set; } = true;

        [DisplayName("[自定义] 显示媒体类型")]
        [IsAdvanced]
        [VisibleCondition(nameof(MessageDetailLevel), ValueCondition.IsEqual, MessageDetailLevel.Custom)]
        public bool IncludeMediaType { get; set; } = false;

        [DisplayName("[自定义] 显示剧集信息")]
        [IsAdvanced]
        [VisibleCondition(nameof(MessageDetailLevel), ValueCondition.IsEqual, MessageDetailLevel.Custom)]
        public bool IncludeSeriesEpisode { get; set; } = true;

        [DisplayName("[自定义] 显示客户端名称")]
        [IsAdvanced]
        [VisibleCondition(nameof(MessageDetailLevel), ValueCondition.IsEqual, MessageDetailLevel.Custom)]
        public bool IncludeClientName { get; set; } = true;

        [DisplayName("[自定义] 显示设备名称")]
        [IsAdvanced]
        [VisibleCondition(nameof(MessageDetailLevel), ValueCondition.IsEqual, MessageDetailLevel.Custom)]
        public bool IncludeDeviceName { get; set; } = true;

        [DisplayName("[自定义] 显示播放位置")]
        [IsAdvanced]
        [VisibleCondition(nameof(MessageDetailLevel), ValueCondition.IsEqual, MessageDetailLevel.Custom)]
        public bool IncludePlaybackPosition { get; set; } = false;

        [DisplayName("[自定义] 显示是否播放完成")]
        [IsAdvanced]
        [VisibleCondition(nameof(MessageDetailLevel), ValueCondition.IsEqual, MessageDetailLevel.Custom)]
        public bool IncludePlayedToCompletion { get; set; } = true;

        [DisplayName("显示敏感技术细节")]
        [Description("开启后在 Detailed 模式展示编码、分辨率等技术字段")]
        [IsAdvanced]
        public bool ShowSensitiveTechnicalDetails { get; set; } = false;
    }
}
