using System;
using EmbyFeishu.Models;

namespace EmbyFeishu.Infrastructure
{
    /// <summary>
    /// 敏感信息脱敏器实现。无状态，可安全复用。
    /// </summary>
    public class SensitiveDataSanitizer : ISensitiveDataSanitizer
    {
        public string SanitizeWebhook(string webhookUrl)
        {
            return WebhookMasker.Mask(webhookUrl);
        }

        public string SanitizeIpAddress(string ip, IpAddressDisplayMode mode)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return null;

            var trimmed = ip.Trim();

            switch (mode)
            {
                case IpAddressDisplayMode.Hidden:
                    return null;
                case IpAddressDisplayMode.Full:
                    return trimmed;
                default:
                    return MaskIp(trimmed);
            }
        }

        /// <summary>
        /// IPv4 保留前两段，其余用 * 替换；IPv6 保留前两组，其余用 * 替换。
        /// </summary>
        private static string MaskIp(string ip)
        {
            // 去掉可能的 IPv6 作用域或端口
            var cleaned = ip;
            var scopeIdx = cleaned.IndexOf('%');
            if (scopeIdx > 0) cleaned = cleaned.Substring(0, scopeIdx);

            if (cleaned.Contains(".") && !cleaned.Contains(":"))
            {
                var parts = cleaned.Split('.');
                if (parts.Length == 4)
                    return parts[0] + "." + parts[1] + ".*.*";
                return "*.*.*.*";
            }

            if (cleaned.Contains(":"))
            {
                var parts = cleaned.Split(':');
                if (parts.Length >= 2)
                    return parts[0] + ":" + parts[1] + ":****";
                return "****";
            }

            return "****";
        }

        public string SanitizeDeviceId(string deviceId, DeviceIdDisplayMode mode)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return null;

            var trimmed = deviceId.Trim();

            switch (mode)
            {
                case DeviceIdDisplayMode.Hidden:
                    return null;
                case DeviceIdDisplayMode.Full:
                    return trimmed;
                default:
                    if (trimmed.Length <= 4)
                        return "****";
                    return "****" + trimmed.Substring(trimmed.Length - 4);
            }
        }

        public string SanitizeException(string message, string webhookUrl)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            // 先移除 Webhook / Token
            var result = WebhookMasker.Sanitize(message, webhookUrl);
            // 再移除可能出现的绝对路径
            result = StripAbsolutePaths(result);
            return result;
        }

        public string SanitizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            try
            {
                var trimmed = path.Trim();
                // 仅保留文件名或最后一段目录名，避免暴露服务器目录结构
                var normalized = trimmed.Replace('\\', '/');
                var idx = normalized.TrimEnd('/').LastIndexOf('/');
                if (idx >= 0 && idx < normalized.Length - 1)
                {
                    return "…/" + normalized.Substring(idx + 1);
                }
                return trimmed;
            }
            catch
            {
                return "(路径已隐藏)";
            }
        }

        /// <summary>
        /// 粗粒度移除文本中的 Windows/Unix 绝对路径片段，保留末段。
        /// </summary>
        private static string StripAbsolutePaths(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            try
            {
                // Windows 盘符路径 C:\a\b\c -> …\c
                var winPattern = new System.Text.RegularExpressions.Regex(@"[A-Za-z]:\\[^\s""'<>|]+");
                text = winPattern.Replace(text, m =>
                {
                    var idx = m.Value.LastIndexOf('\\');
                    return idx >= 0 ? "…\\" + m.Value.Substring(idx + 1) : m.Value;
                });

                // Unix 绝对路径 /a/b/c -> …/c （至少两级，避免误伤普通 URL 路径片段）
                var unixPattern = new System.Text.RegularExpressions.Regex(@"(?<![A-Za-z0-9._~:/?#\[\]@!$&'()*+,;=-])/(?:[^/\s""'<>|]+/){2,}[^/\s""'<>|]+");
                text = unixPattern.Replace(text, m =>
                {
                    var idx = m.Value.LastIndexOf('/');
                    return idx >= 0 ? "…/" + m.Value.Substring(idx + 1) : m.Value;
                });
            }
            catch
            {
                // 正则异常时返回原文（已至少经过 Webhook 脱敏）
            }

            return text;
        }
    }
}
