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

            if (options.Enabled && string.IsNullOrWhiteSpace(options.WebhookUrl))
            {
                errors.Add("启用插件时，飞书 Webhook 地址不能为空。");
            }

            if (!string.IsNullOrWhiteSpace(options.WebhookUrl))
            {
                if (!Uri.TryCreate(options.WebhookUrl.Trim(), UriKind.Absolute, out var uri))
                {
                    errors.Add("Webhook 地址格式不正确，请输入完整的 URL。");
                }
                else if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Webhook 地址必须使用 HTTPS 协议。");
                }
                // 域名是否为飞书/Lark 只作为非阻断性提示（见 IsLikelyFeishuDomain），不锁死自定义中转域名
            }

            if (options.RequestTimeoutSeconds < 3 || options.RequestTimeoutSeconds > 60)
            {
                errors.Add("请求超时时间必须在 3～60 秒之间。");
            }

            if (options.MinimumStopSeconds < 0 || options.MinimumStopSeconds > 600)
            {
                errors.Add("最短播放秒数必须在 0～600 之间。");
            }

            return errors;
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
    }
}
