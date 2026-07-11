using System;

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
    }
}
