using EmbyFeishu.Models;

namespace EmbyFeishu.Infrastructure
{
    /// <summary>
    /// 敏感信息脱敏器。所有安全相关字段进入日志和飞书消息前必须经过它。
    /// </summary>
    public interface ISensitiveDataSanitizer
    {
        /// <summary>脱敏 Webhook 地址，仅保留域名和末尾四位</summary>
        string SanitizeWebhook(string webhookUrl);

        /// <summary>按显示模式脱敏 IP 地址</summary>
        string SanitizeIpAddress(string ip, IpAddressDisplayMode mode);

        /// <summary>按显示模式脱敏设备 ID</summary>
        string SanitizeDeviceId(string deviceId, DeviceIdDisplayMode mode);

        /// <summary>脱敏异常消息：移除 Webhook、绝对路径等敏感片段</summary>
        string SanitizeException(string message, string webhookUrl);

        /// <summary>脱敏文件系统绝对路径，仅保留文件名</summary>
        string SanitizePath(string path);
    }
}
