using System;
using System.Linq;

namespace EmbyFeishu.Infrastructure
{
    /// <summary>
    /// Webhook 地址脱敏工具
    /// </summary>
    public static class WebhookMasker
    {
        /// <summary>
        /// 对 Webhook URL 进行脱敏，只保留域名和末尾四位
        /// </summary>
        public static string Mask(string webhookUrl)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                return "(空)";

            try
            {
                var uri = new Uri(webhookUrl.Trim());
                var path = uri.AbsolutePath + (uri.Query ?? "");
                if (path.Length <= 4)
                    return uri.Host + "/****";

                var lastFour = path.Substring(path.Length - 4);
                return uri.Host + "/****" + lastFour;
            }
            catch
            {
                if (webhookUrl.Length <= 8)
                    return "****";
                return webhookUrl.Substring(0, 4) + "****" + webhookUrl.Substring(webhookUrl.Length - 4);
            }
        }

        /// <summary>
        /// 从任意文本（如异常消息）中移除可能包含的完整 Webhook 地址或 Token，替换为脱敏形式，
        /// 避免完整地址通过日志、异常或界面泄露。
        /// </summary>
        public static string Sanitize(string message, string webhookUrl)
        {
            if (string.IsNullOrEmpty(message))
                return message;
            if (string.IsNullOrWhiteSpace(webhookUrl))
                return message;

            var trimmed = webhookUrl.Trim();
            var result = message.Replace(trimmed, Mask(trimmed));

            // 同时移除仅出现 Token（路径最后一段）的情况
            try
            {
                var uri = new Uri(trimmed);
                var token = uri.AbsolutePath
                    .Split('/')
                    .LastOrDefault(s => !string.IsNullOrEmpty(s));
                if (!string.IsNullOrEmpty(token) && token.Length >= 8)
                {
                    result = result.Replace(token, "****");
                }
            }
            catch
            {
                // URL 无法解析时忽略 Token 处理
            }

            return result;
        }
    }
}
