using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu.Configuration.Groups
{
    /// <summary>
    /// 第八组：高级与诊断 — 限流、去重、聚合、诊断、测试。
    /// </summary>
    public class AdvancedAndDiagnosticsGroup : EditableOptionsBase
    {
        public override string EditorTitle => "高级与诊断";

        // 限流
        [DisplayName("每分钟最大通知数")]
        [Description("超过后普通通知被限流或聚合，范围 1～240")]
        [IsAdvanced]
        [MinValue(1)]
        [MaxValue(240)]
        public int MaximumNotificationsPerMinute { get; set; } = 30;

        [DisplayName("安全事件豁免限流")]
        [IsAdvanced]
        public bool SecurityEventsBypassRateLimit { get; set; } = true;

        [DisplayName("限流时聚合")]
        [IsAdvanced]
        public bool AggregateWhenRateLimited { get; set; } = true;

        // 测试
        [DisplayName("发送测试通知")]
        [Description("勾选后点击保存，向飞书发送一条测试消息。发送后自动取消勾选")]
        public bool SendTestNotification { get; set; } = false;

        [DisplayName("上次测试结果")]
        [Description("显示最近一次测试推送的结果。只读，由保存操作自动更新。")]
        public string LastTestResult { get; set; } = "";
    }
}
