using System;
using System.IO;
using System.Text;
using System.Threading;
using EmbyFeishu.Feishu;
using EmbyFeishu.Infrastructure;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace EmbyFeishu
{
    /// <summary>
    /// Emby 飞书通知插件主类
    /// </summary>
    public class Plugin : BasePluginSimpleUI<PluginOptions>, IHasThumbImage
    {
        /// <summary>
        /// 插件唯一标识符，固定不变
        /// </summary>
        public static readonly Guid PluginGuid = new Guid("d3a7f1b2-8c4e-4f5a-9b6d-2e1c0a3f5d7b");

        private readonly ILogger _logger;
        private readonly IApplicationHost _applicationHost;

        /// <summary>
        /// 插件单例实例
        /// </summary>
        public static Plugin Instance { get; private set; }

        public Plugin(IApplicationHost applicationHost, ILogManager logManager) : base(applicationHost)
        {
            _applicationHost = applicationHost;
            _logger = logManager.GetLogger("EmbyFeishu");
            Instance = this;
            _logger.Info("[EmbyFeishu] 插件已实例化，版本 {0}", Version);
        }

        public override string Name => "Emby 飞书通知";

        public override string Description => "将 Emby 播放事件推送到飞书群机器人 Webhook";

        public override Guid Id => PluginGuid;

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        /// <summary>
        /// 返回嵌入在程序集中的插件图标，供 Emby 后台显示。
        /// </summary>
        public Stream GetThumbImage()
        {
            var assembly = GetType().Assembly;
            return assembly.GetManifestResourceStream("EmbyFeishu.thumb.png");
        }

        /// <summary>
        /// 获取当前插件选项
        /// </summary>
        public PluginOptions GetPluginOptions()
        {
            return GetOptions();
        }

        /// <summary>
        /// 供配置类等外部组件记录非阻断性警告日志
        /// </summary>
        public void LogWarning(string message)
        {
            _logger.Warn("[EmbyFeishu] {0}", message);
        }

        /// <summary>
        /// 配置保存前处理：执行测试推送
        /// </summary>
        protected override bool OnOptionsSaving(PluginOptions options)
        {
            if (options.SendTestNotification)
            {
                options.SendTestNotification = false;
                ExecuteTestPush(options);
            }

            return true;
        }

        /// <summary>
        /// 配置保存后记录日志（脱敏）
        /// </summary>
        protected override void OnOptionsSaved(PluginOptions options)
        {
            var maskedUrl = WebhookMasker.Mask(options.WebhookUrl);
            _logger.Info("[EmbyFeishu] 配置已保存 — 启用={0}, Webhook={1}", options.Enabled, maskedUrl);
        }

        /// <summary>
        /// 执行测试推送
        /// </summary>
        private void ExecuteTestPush(PluginOptions options)
        {
            var maskedUrl = WebhookMasker.Mask(options.WebhookUrl);

            if (string.IsNullOrWhiteSpace(options.WebhookUrl))
            {
                options.LastTestResult = "❌ 测试失败：Webhook 地址为空";
                _logger.Warn("[EmbyFeishu] 测试推送失败：Webhook 地址为空");
                return;
            }

            try
            {
                _logger.Info("[EmbyFeishu] 正在发送测试通知到 {0} ...", maskedUrl);

                var httpClient = _applicationHost.Resolve<IHttpClient>();
                var jsonSerializer = _applicationHost.Resolve<IJsonSerializer>();

                var testMessage = string.Format(
                    "✅ Emby 飞书通知插件测试\n\n"
                    + "这是一条测试消息，说明插件配置正确。\n"
                    + "插件版本：{0}\n"
                    + "测试时间：{1}",
                    Version,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                var request = new FeishuWebhookRequest
                {
                    msg_type = "text",
                    content = new FeishuTextContent { text = testMessage }
                };

                var jsonMemory = jsonSerializer.SerializeToString(request);
                var jsonBody = jsonMemory.ToString();

                var httpRequest = new HttpRequestOptions
                {
                    Url = options.WebhookUrl,
                    RequestContentType = "application/json",
                    RequestContent = jsonBody.AsMemory(),
                    TimeoutMs = options.RequestTimeoutSeconds * 1000,
                    CancellationToken = CancellationToken.None,
                    // 关闭 Emby HttpClient 自带日志，避免完整 Webhook 地址写入 Emby 日志
                    LogErrors = false,
                    LogRequest = false,
                    LogResponse = false,
                    BufferContent = false
                };

                using (var response = httpClient.Post(httpRequest).GetAwaiter().GetResult())
                {
                    if (response == null)
                    {
                        options.LastTestResult = "❌ 测试失败：未收到响应 (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                        _logger.Warn("[EmbyFeishu] 测试推送失败：未收到响应");
                        return;
                    }

                    string responseBody = null;
                    using (var stream = response.Content)
                    {
                        if (stream != null)
                        {
                            using (var reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                responseBody = reader.ReadToEnd();
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        try
                        {
                            var feishuResponse = jsonSerializer.DeserializeFromString<FeishuWebhookResponse>(responseBody);
                            if (feishuResponse != null && !feishuResponse.IsSuccess)
                            {
                                var errMsg = string.Format("飞书返回错误 code={0}: {1}", feishuResponse.code, feishuResponse.msg ?? "未知");
                                options.LastTestResult = "❌ " + errMsg + " (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                                _logger.Warn("[EmbyFeishu] 测试推送失败：{0}", errMsg);
                                return;
                            }
                        }
                        catch
                        {
                            // 无法解析响应但请求已完成，视为成功
                        }
                    }

                    options.LastTestResult = "✅ 测试成功！请检查飞书群是否收到消息 (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                    _logger.Info("[EmbyFeishu] 测试推送成功 ({0})", maskedUrl);
                }
            }
            catch (Exception ex)
            {
                // 异常消息可能包含完整 Webhook 地址，脱敏后再写入界面/日志（LastTestResult 会被持久化到配置）
                var rawMsg = ex.InnerException?.Message ?? ex.Message;
                var errMsg = WebhookMasker.Sanitize(rawMsg, options.WebhookUrl);
                options.LastTestResult = "❌ 测试失败：" + errMsg + " (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                _logger.Warn("[EmbyFeishu] 测试推送异常：{0} ({1})", errMsg, maskedUrl);
            }
        }
    }
}
