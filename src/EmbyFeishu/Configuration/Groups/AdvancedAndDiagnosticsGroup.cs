using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using EmbyFeishu.Models;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu.Configuration.Groups
{
    /// <summary>
    /// 第八组：高级与诊断 — 预设、限流、去重、聚合、诊断、测试。
    /// </summary>
    public class AdvancedAndDiagnosticsGroup : EditableOptionsBase
    {
        public override string EditorTitle => "高级与诊断";

        // 配置预设
        [DisplayName("快速配置预设")]
        [Description("选择预设后保存，将自动设置所有事件开关。None=不应用；Conservative=仅安全与关键事件；Standard=播放+安全+服务器（推荐）；Full=全部 52 种事件；PlaybackOnly=仅播放。应用后自动重置为 None")]
        public NotificationPreset ApplyPreset { get; set; } = NotificationPreset.None;

        // 限流
        [DisplayName("每分钟最大通知数")]
        [Description("超过此数量后，普通通知将被限流或聚合为汇总消息。安全事件（登录失败等）可豁免。范围 1～240")]
        [IsAdvanced]
        [MinValue(1)]
        [MaxValue(240)]
        public int MaximumNotificationsPerMinute { get; set; } = 30;

        [DisplayName("安全事件豁免限流")]
        [Description("开启后，登录失败、用户锁定等安全事件不受每分钟上限约束，确保安全告警及时送达")]
        [IsAdvanced]
        public bool SecurityEventsBypassRateLimit { get; set; } = true;

        [DisplayName("限流时聚合")]
        [Description("开启后，被限流抑制的通知数量将定期汇总为一条提示消息")]
        [IsAdvanced]
        public bool AggregateWhenRateLimited { get; set; } = true;

        // 重试
        [DisplayName("最大重试次数")]
        [Description("飞书发送失败时的重试次数（仅对 429/5xx/网络错误重试）。使用指数退避策略（1s → 2s → 4s）。范围 0～3")]
        [IsAdvanced]
        [MinValue(0)]
        [MaxValue(3)]
        public int MaxRetryCount { get; set; } = 1;

        // 测试
        [DisplayName("发送测试通知")]
        [Description("勾选后点击保存，向飞书发送一条测试消息（会同时验证关键词和签名设置）。发送后自动取消勾选")]
        public bool SendTestNotification { get; set; } = false;

        [DisplayName("上次测试结果")]
        [Description("显示最近一次测试推送的结果（只读，由保存操作自动更新）")]
        public string LastTestResult { get; set; } = "";

        // 诊断
        [DisplayName("运行诊断信息")]
        [Description("插件运行期间的实时统计（只读，每次打开配置页刷新）")]
        [IsAdvanced]
        public string DiagnosticInfo { get; set; } = "插件尚未启动";

        [DisplayName("Webhook 连接状态")]
        [Description("基于最近发送结果的 Webhook 健康状态（只读）")]
        [IsAdvanced]
        public string WebhookHealthStatus { get; set; } = "未知";
    }
}
