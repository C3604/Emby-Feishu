using System;
using System.Collections.Generic;
using EmbyFeishu.Messaging;

namespace EmbyFeishu.Feishu
{
    /// <summary>
    /// 消息安全装饰器接口：负责关键词注入与签名的统一处理，不与各事件格式化器耦合。
    /// </summary>
    public interface IFeishuMessageSecurityDecorator
    {
        /// <summary>
        /// 对文本消息正文注入关键词（若启用），返回处理后的文本。
        /// </summary>
        string DecorateText(string originalText, PluginOptions options);

        /// <summary>
        /// 对飞书卡片消息底部注入关键词元素（若启用），返回处理后的卡片对象。
        /// </summary>
        object DecorateCard(object cardBody, PluginOptions options);

        /// <summary>
        /// 对已构造的飞书请求体添加签名和时间戳（若启用），返回处理后的请求体。
        /// </summary>
        object DecorateRequest(object requestBody, PluginOptions options);
    }

    /// <summary>
    /// 消息安全装饰器实现。统一处理飞书自定义关键词和签名校验，
    /// 不在各事件格式化器中分散实现。
    /// </summary>
    public class FeishuMessageSecurityDecorator : IFeishuMessageSecurityDecorator
    {
        private readonly IFeishuSignatureProvider _signatureProvider;
        private readonly IUnixTimeProvider _timeProvider;

        public FeishuMessageSecurityDecorator(IFeishuSignatureProvider signatureProvider, IUnixTimeProvider timeProvider)
        {
            _signatureProvider = signatureProvider ?? throw new ArgumentNullException(nameof(signatureProvider));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        /// <summary>
        /// 关键词仅注入一次。若正文已包含完全相同的关键词则不重复追加。
        /// </summary>
        public string DecorateText(string originalText, PluginOptions options)
        {
            if (options == null || !options.EnableCustomKeyword || string.IsNullOrWhiteSpace(options.CustomKeyword))
                return originalText ?? "";

            var keyword = options.CustomKeyword.Trim();
            if (string.IsNullOrEmpty(keyword))
                return originalText ?? "";

            var text = originalText ?? "";

            // 已包含完全相同的关键词则不重复追加
            if (text.Contains(keyword))
                return text;

            return text + "\n\n" + keyword;
        }

        /// <summary>
        /// 在卡片底部 elements 列表末尾追加一个包含关键词的 note 元素。
        /// </summary>
        public object DecorateCard(object cardBody, PluginOptions options)
        {
            if (options == null || !options.EnableCustomKeyword || string.IsNullOrWhiteSpace(options.CustomKeyword))
                return cardBody;

            var keyword = options.CustomKeyword.Trim();
            if (string.IsNullOrEmpty(keyword))
                return cardBody;

            if (!(cardBody is Dictionary<string, object> requestDict))
                return cardBody;

            if (!requestDict.TryGetValue("card", out var cardObj) || !(cardObj is Dictionary<string, object> card))
                return cardBody;

            // 检查卡片内容是否已包含关键词
            var cardJson = SimpleJsonSerialize(card);
            if (cardJson.Contains(keyword))
                return cardBody;

            if (!card.TryGetValue("elements", out var elemsObj) || !(elemsObj is List<object> elements))
                elements = new List<object>();

            // 追加分隔线 + 含关键词的 note 元素
            elements.Add(new Dictionary<string, object> { ["tag"] = "hr" });
            elements.Add(new Dictionary<string, object>
            {
                ["tag"] = "note",
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["tag"] = "plain_text",
                        ["content"] = keyword
                    }
                }
            });

            card["elements"] = elements;
            return requestDict;
        }

        /// <summary>
        /// 对请求体添加 timestamp 和 sign 字段（若启用签名校验）。
        /// 每次调用都重新生成时间戳和签名。
        /// </summary>
        public object DecorateRequest(object requestBody, PluginOptions options)
        {
            if (options == null || !options.EnableSignatureVerification)
                return requestBody;

            if (string.IsNullOrWhiteSpace(options.SignatureSecret))
                return requestBody;

            if (!(requestBody is Dictionary<string, object> requestDict))
                return requestBody;

            var timestamp = _timeProvider.NowSeconds();
            var sign = _signatureProvider.Sign(timestamp, options.SignatureSecret);

            // 飞书签名模式下 timestamp 使用字符串
            requestDict["timestamp"] = timestamp.ToString();
            requestDict["sign"] = sign;

            return requestDict;
        }

        /// <summary>
        /// 将卡片对象简单序列化为 JSON 字符串，用于检查是否已含关键词。
        /// 此非标准 JSON 序列化，仅用于字符串包含检查，不追求格式正确。
        /// </summary>
        private static string SimpleJsonSerialize(object obj)
        {
            if (obj == null) return "";
            if (obj is string s) return s;
            if (obj is Dictionary<string, object> d)
            {
                var parts = new List<string>();
                foreach (var kv in d)
                    parts.Add(kv.Key + ":" + SimpleJsonSerialize(kv.Value));
                return string.Join(",", parts);
            }
            if (obj is List<object> l)
            {
                var parts = new List<string>();
                foreach (var item in l)
                    parts.Add(SimpleJsonSerialize(item));
                return string.Join(",", parts);
            }
            return obj.ToString();
        }
    }
}
