using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu.Configuration.Groups
{
    /// <summary>
    /// 第一组：飞书连接 — 插件总开关、Webhook、超时与重试。
    /// </summary>
    public class FeishuConnectionGroup : EditableOptionsBase
    {
        public override string EditorTitle => "飞书连接";

        public override string EditorDescription
            => "飞书自定义机器人使用指南：https://open.feishu.cn/document/client-docs/bot-v3/add-custom-bot";

        [DisplayName("启用插件")]
        [Description("插件总开关，关闭后不会发送任何通知")]
        public bool Enabled { get; set; } = false;

        [DisplayName("飞书 Webhook 地址")]
        [Description("飞书群机器人的 Webhook 地址，以 https:// 开头")]
        public string WebhookUrl { get; set; } = "";

        [DisplayName("请求超时（秒）")]
        [Description("飞书 Webhook 请求超时时间，范围 3～60")]
        [IsAdvanced]
        [MinValue(3)]
        [MaxValue(60)]
        public int RequestTimeoutSeconds { get; set; } = 10;

        // 重试设置说明：当前为内部一次瞬时错误重试，暂无独立配置项
    }
}
