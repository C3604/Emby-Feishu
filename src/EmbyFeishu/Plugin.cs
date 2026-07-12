using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using EmbyFeishu.Configuration.Groups;
using EmbyFeishu.Feishu;
using EmbyFeishu.Infrastructure;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
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
        /// 防止 PersistTestResult 内部调用 SaveOptions 再次触发 OnOptionsSaved 时重复执行测试推送。
        /// </summary>
        private volatile bool _isPersistingTestResult;

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
        /// 注册控制台侧边栏入口："飞书通知"。
        /// 在 Emby 4.9.5.0 中 BasePluginSimpleUI&lt;T&gt; 提供了 OnCreatePageInfo 虚方法。
        /// </summary>
        protected override void OnCreatePageInfo(PluginPageInfo pageInfo)
        {
            pageInfo.DisplayName = "飞书通知";
            pageInfo.EnableInMainMenu = true;
            pageInfo.EnableInUserMenu = false;
            pageInfo.IsMainConfigPage = true;
            // MenuSection / MenuIcon / FeatureId 不设置，使用默认值保证兼容性
        }

        /// <summary>
        /// 配置保存前处理：将分组对象的 UI 输入同步到旧扁平字段。
        /// 测试推送移至 OnOptionsSaved 中执行（此时框架已完成序列化，使用最新配置）。
        /// </summary>
        protected override bool OnOptionsSaving(PluginOptions options)
        {
            if (options == null) return true;

            options.EnsureGroups();
            // 将分组对象的值回写到旧扁平字段，保证 JSON 序列化时两组数据一致
            options.SyncFromGroups();

            return true;
        }

        /// <summary>
        /// 配置保存后：执行测试推送并记录结果。
        /// 此时新配置已持久化，可安全读取到刚保存的 Webhook、关键词和签名密钥。
        /// </summary>
        protected override void OnOptionsSaved(PluginOptions options)
        {
            if (options == null) return;

            // PersistTestResult 会调用官方 SaveOptions()，从而再次触发本回调；
            // 此时测试推送已完成、结果已写入，直接返回避免重复推送与递归保存。
            if (_isPersistingTestResult) return;

            options.EnsureGroups();

            var maskedUrl = WebhookMasker.Mask(options.FeishuConnection.WebhookUrl);
            _logger.Info("[EmbyFeishu] 配置已保存 — 启用={0}, Webhook={1}", options.FeishuConnection.Enabled, maskedUrl);

            // 在配置已保存之后再执行测试推送
            if (options.AdvancedAndDiagnostics.SendTestNotification)
            {
                // 立即取消勾选，防止下次保存重复发送
                options.AdvancedAndDiagnostics.SendTestNotification = false;
                options.SendTestNotification = false;

                // 执行测试推送
                ExecuteTestPush(options);
            }
        }

        /// <summary>
        /// 执行测试推送。使用当前配置的 Webhook、消息格式、关键词和签名。
        /// 测试结果写回配置并重新持久化。
        /// </summary>
        private void ExecuteTestPush(PluginOptions options)
        {
            options.EnsureGroups();
            var webhookUrl = options.FeishuConnection.WebhookUrl;
            var maskedUrl = WebhookMasker.Mask(webhookUrl);

            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                var msg = "❌ 测试失败：Webhook 地址为空";
                options.AdvancedAndDiagnostics.LastTestResult = FormatTestResult(msg);
                _logger.Warn("[EmbyFeishu] 测试推送失败：Webhook 地址为空");
                PersistTestResult(options);
                return;
            }

            try
            {
                _logger.Info("[EmbyFeishu] 正在发送测试通知到 {0} ...", maskedUrl);

                var httpClient = _applicationHost.Resolve<IHttpClient>();
                var jsonSerializer = _applicationHost.Resolve<IJsonSerializer>();

                var nowStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var testMessage = "✅ Emby 飞书通知测试成功\n\n"
                    + "这是一条由 EmbyFeishu 插件发送的测试消息。\n"
                    + "服务器时间：" + nowStr;

                // 应用关键词安全装饰
                var securityDecorator = new FeishuMessageSecurityDecorator(
                    new FeishuSignatureProvider(),
                    new SystemUnixTimeProvider());

                // 按当前格式选择构造请求体
                var useCard = options.MessageDisplay.MessageFormat == Models.MessageFormat.FeishuCard;
                object requestBody;

                if (useCard)
                {
                    // 卡片格式
                    var card = new Dictionary<string, object>
                    {
                        ["config"] = new Dictionary<string, object> { ["wide_screen_mode"] = true },
                        ["header"] = new Dictionary<string, object>
                        {
                            ["template"] = "green",
                            ["title"] = new Dictionary<string, object>
                            {
                                ["tag"] = "plain_text",
                                ["content"] = "✅ Emby 飞书通知测试"
                            }
                        },
                        ["elements"] = new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["tag"] = "div",
                                ["text"] = new Dictionary<string, object>
                                {
                                    ["tag"] = "lark_md",
                                    ["content"] = "这是一条由 EmbyFeishu 插件发送的测试消息。\n服务器时间：" + nowStr
                                }
                            }
                        }
                    };
                    requestBody = new Dictionary<string, object>
                    {
                        ["msg_type"] = "interactive",
                        ["card"] = card
                    };
                    // 注入关键词
                    requestBody = securityDecorator.DecorateCard(requestBody, options);
                }
                else
                {
                    // 文本格式
                    var text = securityDecorator.DecorateText(testMessage, options);
                    requestBody = new Dictionary<string, object>
                    {
                        ["msg_type"] = "text",
                        ["content"] = new Dictionary<string, object> { ["text"] = text }
                    };
                }

                // 应用签名
                if (options.EnableSignatureVerification && !string.IsNullOrWhiteSpace(options.SignatureSecret))
                {
                    requestBody = securityDecorator.DecorateRequest(requestBody, options);
                }

                var jsonMemory = jsonSerializer.SerializeToString(requestBody);
                var jsonBody = jsonMemory.ToString();

                var timeoutMs = options.FeishuConnection.RequestTimeoutSeconds * 1000;
                var httpRequest = new HttpRequestOptions
                {
                    Url = webhookUrl,
                    RequestContentType = "application/json",
                    RequestContent = jsonBody.AsMemory(),
                    TimeoutMs = timeoutMs,
                    CancellationToken = CancellationToken.None,
                    LogErrors = false,
                    LogRequest = false,
                    LogResponse = false,
                    BufferContent = false
                };

                // 同步阻塞说明：OnOptionsSaved 是 Emby Simple UI 框架的同步回调，无法 await，
                // 也不能改为 async void（会脱离框架保存流程、异常无法捕获）。因此在此处以
                // GetAwaiter().GetResult() 同步等待。阻塞时间由 HttpRequestOptions.TimeoutMs 严格限定，
                // 该值来自已被夹取到 3~60 秒的 RequestTimeoutSeconds，绝不会无限阻塞保存页面。
                using (var response = httpClient.Post(httpRequest).GetAwaiter().GetResult())
                {
                    if (response == null)
                    {
                        var failMsg = "❌ 测试失败：未收到 HTTP 响应";
                        options.AdvancedAndDiagnostics.LastTestResult = FormatTestResult(failMsg);
                        _logger.Warn("[EmbyFeishu] 测试推送失败：未收到响应 ({0})", maskedUrl);
                        PersistTestResult(options);
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

                    if (string.IsNullOrWhiteSpace(responseBody))
                    {
                        // 空响应视为成功（飞书有时返回空体 + HTTP 200）
                        var okMsg = "✅ 测试成功：飞书机器人已接受消息。";
                        options.AdvancedAndDiagnostics.LastTestResult = FormatTestResult(okMsg);
                        _logger.Info("[EmbyFeishu] 测试推送成功（空响应）({0})", maskedUrl);
                        PersistTestResult(options);
                        return;
                    }

                    try
                    {
                        var feishuResponse = jsonSerializer.DeserializeFromString<FeishuWebhookResponse>(responseBody);
                        if (feishuResponse != null && !feishuResponse.IsSuccess)
                        {
                            var errDetail = !string.IsNullOrWhiteSpace(feishuResponse.msg)
                                ? feishuResponse.msg
                                : "未知错误";
                            var failMsg = "❌ 测试失败：飞书返回 code=" + feishuResponse.code + "，" + errDetail;
                            options.AdvancedAndDiagnostics.LastTestResult = FormatTestResult(failMsg);
                            _logger.Warn("[EmbyFeishu] 测试推送失败：飞书错误 code={0} msg={1} ({2})",
                                feishuResponse.code, feishuResponse.msg ?? "", maskedUrl);
                            PersistTestResult(options);
                            return;
                        }
                    }
                    catch
                    {
                        // 无法解析响应但请求已完成，视为成功
                    }

                    var successMsg = "✅ 测试成功：飞书机器人已接受消息。";
                    options.AdvancedAndDiagnostics.LastTestResult = FormatTestResult(successMsg);
                    _logger.Info("[EmbyFeishu] 测试推送成功 ({0})", maskedUrl);
                    PersistTestResult(options);
                }
            }
            catch (OperationCanceledException)
            {
                var cancelMsg = "❌ 测试失败：请求已取消";
                options.AdvancedAndDiagnostics.LastTestResult = FormatTestResult(cancelMsg);
                _logger.Warn("[EmbyFeishu] 测试推送取消 ({0})", maskedUrl);
                PersistTestResult(options);
            }
            catch (Exception ex)
            {
                var rawMsg = ex.InnerException?.Message ?? ex.Message;
                var errMsg = WebhookMasker.Sanitize(rawMsg, webhookUrl);
                var failMsg = "❌ 测试失败：" + errMsg;
                options.AdvancedAndDiagnostics.LastTestResult = FormatTestResult(failMsg);
                _logger.Warn("[EmbyFeishu] 测试推送异常：{0} ({1})", errMsg, maskedUrl);
                PersistTestResult(options);
            }
        }

        /// <summary>
        /// 格式化测试结果字符串，追加当前时间。
        /// </summary>
        private static string FormatTestResult(string message)
        {
            return message + "\n时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 将测试结果持久化到配置文件，确保保存后刷新页面可以显示。
        /// 使用 Emby 4.9.5.0 BasePluginSimpleUI&lt;T&gt; 的官方 protected 方法 SaveOptions(T)，
        /// 不再依赖反射。SaveOptions 会再次触发 OnOptionsSaved，由 _isPersistingTestResult 防止重入。
        /// </summary>
        private void PersistTestResult(PluginOptions options)
        {
            try
            {
                options.SyncFromGroups();
                _isPersistingTestResult = true;
                SaveOptions(options);
            }
            catch (Exception ex)
            {
                _logger.Error("[EmbyFeishu] 持久化测试结果失败: {0}", ex.Message);
            }
            finally
            {
                _isPersistingTestResult = false;
            }
        }
    }
}
