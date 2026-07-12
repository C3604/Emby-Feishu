using System;
using System.Security.Cryptography;
using System.Text;

namespace EmbyFeishu.Feishu
{
    /// <summary>
    /// 飞书签名计算组件接口，可注入以支持测试。
    /// </summary>
    public interface IFeishuSignatureProvider
    {
        /// <summary>
        /// 按飞书自定义机器人签名规则计算签名。
        /// stringToSign = timestamp + "\n" + secret
        /// HMAC-SHA256 的 key 为 stringToSign 的 UTF-8 字节，待计算消息为空字节数组。
        /// 返回标准 Base64 编码的签名。
        /// </summary>
        string Sign(long timestamp, string secret);
    }

    /// <summary>
    /// 飞书签名算法实现。线程安全，无状态。
    /// 参考：https://open.feishu.cn/document/client-docs/bot-v3/add-custom-bot
    /// </summary>
    public class FeishuSignatureProvider : IFeishuSignatureProvider
    {
        public string Sign(long timestamp, string secret)
        {
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("签名密钥不能为空", nameof(secret));

            // 飞书签名：timestamp + "\n" + secret → UTF-8 → HMAC-SHA256 key
            // 被签名的消息为空字节数组
            var stringToSign = timestamp + "\n" + secret;
            var keyBytes = Encoding.UTF8.GetBytes(stringToSign);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var digest = hmac.ComputeHash(Array.Empty<byte>());
                return Convert.ToBase64String(digest);
            }
        }
    }
}
