using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu.Configuration.Groups
{
    /// <summary>
    /// 第二组：机器人安全校验 — 自定义关键词和签名校验。
    /// </summary>
    public class BotSecurityGroup : EditableOptionsBase
    {
        public override string EditorTitle => "机器人安全校验";

        public override string EditorDescription
            => "自定义关键词和签名校验需要与飞书群机器人安全设置保持一致。插件中的设置不会自动修改飞书机器人配置。";

        [DisplayName("启用自定义关键词")]
        [Description("开启后，插件发送的每一条消息都会携带该关键词。请确保它与飞书机器人安全设置中的自定义关键词完全一致。")]
        public bool EnableCustomKeyword { get; set; } = false;

        [DisplayName("自定义关键词")]
        [Description("与飞书群机器人安全设置中的关键词一致。开启后每条消息都会携带。")]
        [VisibleCondition(nameof(EnableCustomKeyword), SimpleCondition.IsTrue)]
        public string CustomKeyword { get; set; } = "";

        [DisplayName("启用签名校验")]
        [Description("飞书机器人开启\"签名校验\"后需填写下方密钥。")]
        public bool EnableSignatureVerification { get; set; } = false;

        [DisplayName("签名密钥")]
        [Description("飞书机器人\"签名校验\"页面提供的密钥（非 Webhook Token）。输入新内容并保存则更新密钥，留空保持原值。")]
        [VisibleCondition(nameof(EnableSignatureVerification), SimpleCondition.IsTrue)]
        [IsPassword]
        [IsAdvanced]
        public string SignatureSecretInput { get; set; } = "";

        /// <summary>已配置的签名密钥状态（只读信息）。</summary>
        [DisplayName("签名密钥状态")]
        [Description("仅显示是否已配置，不会在界面中暴露真实密钥。")]
        public string SignatureSecretStatus { get; set; } = "未配置";
    }
}
