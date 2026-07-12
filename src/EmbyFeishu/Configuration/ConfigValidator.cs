using System;
using System.Collections.Generic;
using System.Linq;

namespace EmbyFeishu.Configuration
{
    /// <summary>
    /// 配置校验器
    /// </summary>
    public static class ConfigValidator
    {
        /// <summary>
        /// 单独校验 Webhook URL 格式，返回错误列表
        /// </summary>
        public static List<string> ValidateWebhookUrl(string webhookUrl)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(webhookUrl))
                return errors;

            if (!Uri.TryCreate(webhookUrl.Trim(), UriKind.Absolute, out var uri))
            {
                errors.Add("Webhook 地址格式不正确，请输入完整的 URL。");
            }
            else if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Webhook 地址必须使用 HTTPS 协议。");
            }

            return errors;
        }

        /// <summary>
        /// 校验配置选项，返回错误信息列表（空表示通过）
        /// </summary>
        public static List<string> Validate(PluginOptions options)
        {
            var errors = new List<string>();
            options.EnsureGroups();

            var conn = options.FeishuConnection;

            if (conn.Enabled && string.IsNullOrWhiteSpace(conn.WebhookUrl))
            {
                errors.Add("启用插件时，飞书 Webhook 地址不能为空。");
            }

            if (!string.IsNullOrWhiteSpace(conn.WebhookUrl))
            {
                if (!Uri.TryCreate(conn.WebhookUrl.Trim(), UriKind.Absolute, out var uri))
                {
                    errors.Add("Webhook 地址格式不正确，请输入完整的 URL。");
                }
                else if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Webhook 地址必须使用 HTTPS 协议。");
                }
            }

            if (conn.RequestTimeoutSeconds < 3 || conn.RequestTimeoutSeconds > 60)
                errors.Add("请求超时时间必须在 3～60 秒之间。");

            var pb = options.PlaybackNotification;
            if (pb.MinimumStopSeconds < 0 || pb.MinimumStopSeconds > 600)
                errors.Add("最短播放秒数必须在 0～600 之间。");
            if (pb.CompletionThresholdPercent < 50 || pb.CompletionThresholdPercent > 100)
                errors.Add("播放完成阈值必须在 50～100 之间。");

            var lib = options.LibraryAndUserBehavior;
            if (lib.LibraryAggregationWindowSeconds < 10 || lib.LibraryAggregationWindowSeconds > 600)
                errors.Add("媒体库聚合窗口必须在 10～600 秒之间。");

            var adv = options.AdvancedAndDiagnostics;
            if (adv.MaximumNotificationsPerMinute < 1 || adv.MaximumNotificationsPerMinute > 240)
                errors.Add("每分钟最大通知数必须在 1～240 之间。");

            return errors;
        }

        /// <summary>
        /// 解析播放进度里程碑百分比列表，返回升序去重、落在 1～99 的整数。
        /// </summary>
        public static List<int> ParseMilestones(string raw)
        {
            var result = new List<int>();
            if (string.IsNullOrWhiteSpace(raw))
                return result;

            var parts = raw.Split(new[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out var v) && v >= 1 && v <= 99)
                {
                    if (!result.Contains(v))
                        result.Add(v);
                }
            }

            result.Sort();
            return result;
        }

        /// <summary>
        /// 判断 Webhook 域名是否像飞书或 Lark。返回 false 时仅作提示，不阻断保存。
        /// </summary>
        public static bool IsLikelyFeishuDomain(string webhookUrl)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                return false;

            if (!Uri.TryCreate(webhookUrl.Trim(), UriKind.Absolute, out var uri))
                return false;

            var host = uri.Host.ToLowerInvariant();
            return host.Contains("feishu.cn") || host.Contains("larksuite.com");
        }

        /// <summary>
        /// 规范化用户名列表：去空格、去空项、去重（忽略大小写）
        /// </summary>
        public static string NormalizeUserNames(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            var names = raw.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .Where(n => n.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return string.Join(", ", names);
        }

        /// <summary>
        /// 校验自定义关键词格式。返回 null 表示通过，否则为错误信息。
        /// </summary>
        public static string ValidateCustomKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return null;

            var trimmed = keyword.Trim();
            if (trimmed.Length == 0)
                return "自定义关键词不能全部由空格组成。";

            if (trimmed.Contains("\n") || trimmed.Contains("\r") || trimmed.Contains("\t"))
                return "自定义关键词不能包含换行、回车或制表符。";

            foreach (var c in trimmed)
            {
                if (char.IsControl(c))
                    return "自定义关键词不能包含控制字符。";
            }

            return null;
        }
    }
}
