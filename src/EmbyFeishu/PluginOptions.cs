using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu
{
    /// <summary>
    /// 插件配置选项，由 Emby Simple UI 自动生成设置页面
    /// </summary>
    public class PluginOptions : EditableOptionsBase
    {
        public override string EditorTitle => "Emby 飞书通知设置";

        public override string EditorDescription => "配置飞书 Webhook 地址和通知选项。保存后立即生效。";

        // ===== 基本设置 =====

        [DisplayName("启用插件")]
        [Description("插件总开关，关闭后不会发送任何通知")]
        public bool Enabled { get; set; } = false;

        [DisplayName("飞书 Webhook 地址")]
        [Description("飞书群机器人的 Webhook 地址，以 https:// 开头")]
        public string WebhookUrl { get; set; } = "";

        [DisplayName("仅通知视频播放")]
        [Description("开启后只有视频类型的播放会触发通知，忽略音频等")]
        public bool OnlyVideo { get; set; } = true;

        // ===== 测试推送 =====

        [DisplayName("发送测试通知")]
        [Description("勾选后点击保存，将向飞书发送一条测试消息以验证配置是否正确。发送后自动取消勾选。")]
        public bool SendTestNotification { get; set; } = false;

        [DisplayName("上次测试结果")]
        [Description("显示最近一次测试推送的结果")]
        public string LastTestResult { get; set; } = "";

        // ===== 事件开关 =====

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

        // ===== 高级设置 =====

        [DisplayName("请求超时（秒）")]
        [Description("飞书 Webhook 请求超时时间，范围 3～60")]
        [IsAdvanced]
        [MinValue(3)]
        [MaxValue(60)]
        public int RequestTimeoutSeconds { get; set; } = 10;

        [DisplayName("最短播放秒数")]
        [Description("播放时长不足此秒数时不发送停止通知，范围 0～600")]
        [IsAdvanced]
        [MinValue(0)]
        [MaxValue(600)]
        public int MinimumStopSeconds { get; set; } = 5;

        [DisplayName("用户过滤模式")]
        [Description("All=所有用户, IncludeOnly=仅通知指定用户, Exclude=排除指定用户")]
        [IsAdvanced]
        public Models.UserFilterMode UserFilterMode { get; set; } = Models.UserFilterMode.All;

        [DisplayName("用户名列表")]
        [Description("逗号、分号或换行分隔的用户名。仅在过滤模式为 IncludeOnly 或 Exclude 时生效")]
        [IsAdvanced]
        [EditMultiline(3)]
        public string UserNames { get; set; } = "";

        // ===== 消息字段开关 =====

        [DisplayName("显示用户名")]
        [IsAdvanced]
        public bool IncludeUserName { get; set; } = true;

        [DisplayName("显示媒体标题")]
        [IsAdvanced]
        public bool IncludeMediaTitle { get; set; } = true;

        [DisplayName("显示媒体类型")]
        [IsAdvanced]
        public bool IncludeMediaType { get; set; } = false;

        [DisplayName("显示剧集信息")]
        [IsAdvanced]
        public bool IncludeSeriesEpisode { get; set; } = true;

        [DisplayName("显示客户端名称")]
        [IsAdvanced]
        public bool IncludeClientName { get; set; } = true;

        [DisplayName("显示设备名称")]
        [IsAdvanced]
        public bool IncludeDeviceName { get; set; } = true;

        [DisplayName("显示播放位置")]
        [IsAdvanced]
        public bool IncludePlaybackPosition { get; set; } = false;

        [DisplayName("显示是否播放完成")]
        [IsAdvanced]
        public bool IncludePlayedToCompletion { get; set; } = true;

        /// <summary>
        /// 配置校验，由 Emby Simple UI 框架在保存时调用
        /// </summary>
        protected override void Validate(ValidationContext context)
        {
            base.Validate(context);

            if (SendTestNotification)
            {
                if (string.IsNullOrWhiteSpace(WebhookUrl))
                {
                    context.AddValidationError("发送测试通知前，请先填写飞书 Webhook 地址。");
                    SendTestNotification = false;
                    return;
                }

                var urlErrors = Configuration.ConfigValidator.ValidateWebhookUrl(WebhookUrl);
                foreach (var msg in urlErrors)
                {
                    context.AddValidationError(msg);
                }

                if (context.HasErrors)
                {
                    SendTestNotification = false;
                    return;
                }
            }

            var configErrors = Configuration.ConfigValidator.Validate(this);
            foreach (var msg in configErrors)
            {
                context.AddValidationError(msg);
            }

            UserNames = Configuration.ConfigValidator.NormalizeUserNames(UserNames);

            // 非飞书/Lark 域名仅作提示，不阻断保存（允许自定义中转地址）
            if (!context.HasErrors
                && !string.IsNullOrWhiteSpace(WebhookUrl)
                && !Configuration.ConfigValidator.IsLikelyFeishuDomain(WebhookUrl))
            {
                Plugin.Instance?.LogWarning(
                    "Webhook 域名不是飞书(feishu.cn)或 Lark(larksuite.com)，若为自定义中转地址可忽略此提示。");
            }
        }
    }
}
