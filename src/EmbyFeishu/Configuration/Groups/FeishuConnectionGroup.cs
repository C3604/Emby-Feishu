using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu.Configuration.Groups
{
    /// <summary>
    /// 第一组：飞书连接 — 插件总开关、Webhook、超时。
    /// </summary>
    public class FeishuConnectionGroup : EditableOptionsBase
    {
        public override string EditorTitle => "飞书连接";

        public override string EditorDescription
            => "飞书自定义机器人使用指南：https://open.feishu.cn/document/client-docs/bot-v3/add-custom-bot\n\n"
             + "快速开始：1) 在飞书群中添加自定义机器人 → 2) 复制 Webhook 地址粘贴到下方 → 3) 启用插件并保存 → 4) 在「高级与诊断」中发送测试通知验证";

        [DisplayName("启用插件")]
        [Description("插件总开关。关闭后不会发送任何通知，但事件监听仍在运行，重新开启即时生效")]
        public bool Enabled { get; set; } = false;

        [DisplayName("飞书 Webhook 地址")]
        [Description("飞书群机器人的 Webhook 地址，以 https:// 开头。在飞书群设置 → 群机器人 → 自定义机器人中获取")]
        public string WebhookUrl { get; set; } = "";

        [DisplayName("请求超时（秒）")]
        [Description("飞书 Webhook 请求超时时间。网络较慢时可适当增大，范围 3～60")]
        [IsAdvanced]
        [MinValue(3)]
        [MaxValue(60)]
        public int RequestTimeoutSeconds { get; set; } = 10;
    }
}
