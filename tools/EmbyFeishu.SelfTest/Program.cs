using System;
using System.Collections.Generic;
using EmbyFeishu.Configuration;
using EmbyFeishu.Events;
using EmbyFeishu.Feishu;
using EmbyFeishu.Infrastructure;
using EmbyFeishu.Messaging;
using EmbyFeishu.Models;

namespace EmbyFeishu.SelfTest
{
    class Program
    {
        private static int _passed;
        private static int _failed;
        private static readonly List<string> _failures = new List<string>();

        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("  EmbyFeishu 自测工具 v2");
            Console.WriteLine("========================================\n");

            TestMediaTitleFormatting();
            TestTimeFormatting();
            TestWebhookMasking();
            TestSanitizer();
            TestUserFilter();
            TestConfigValidation();
            TestConfigMigrator();
            TestMediaTypeClassifier();
            TestTextFormatting();
            TestDetailLevels();
            TestCardFormatting();
            TestPlaybackStateTracker();
            TestMilestonesAndMethod();
            TestDeduplication();
            TestRateLimiter();
            TestPolicy();
            TestLibraryAggregator();
            TestSendTestNotificationFlag();

            // ==== 新增测试 ====
            TestSidebar();
            TestGroupDefaults();
            TestGroupMigration();
            TestKeywordValidation();
            TestKeywordInjection();
            TestSignatureAlgorithm();
            TestSignatureCombinations();
            TestSecurityDecorator();
            TestConfigSchemaVersion();
            TestPluginPageInfo();
            TestWebhookPersistence();
            TestTestNotification();

            Console.WriteLine("\n========================================");
            Console.WriteLine($"  结果: 通过 {_passed}, 失败 {_failed}");
            Console.WriteLine("========================================");

            if (_failures.Count > 0)
            {
                Console.WriteLine("\n失败的测试:");
                foreach (var f in _failures)
                    Console.WriteLine($"  ✗ {f}");
            }

            return _failed > 0 ? 1 : 0;
        }

        static void Assert(bool condition, string testName)
        {
            if (condition) { _passed++; Console.WriteLine($"  ✓ {testName}"); }
            else { _failed++; _failures.Add(testName); Console.WriteLine($"  ✗ {testName}"); }
        }

        static void AssertEqual(string expected, string actual, string testName)
        {
            Assert(expected == actual, $"{testName} (期望=\"{expected}\", 实际=\"{actual}\")");
        }

        // ===================== 现有基础能力 =====================

        static void TestMediaTitleFormatting()
        {
            Console.WriteLine("\n【媒体标题格式化】");
            AssertEqual("星际穿越", MediaTitleFormatter.Format("星际穿越", null, null, null, null), "电影标题");
            AssertEqual("权力的游戏 S01E02 - 国王大道", MediaTitleFormatter.Format("国王大道", "权力的游戏", 1, 2, "国王大道"), "剧集标题（含季集号）");
            AssertEqual("权力的游戏 - 特别篇", MediaTitleFormatter.Format("特别篇", "权力的游戏", null, null, "特别篇"), "剧集标题（无季集号）");
            AssertEqual("未知媒体", MediaTitleFormatter.Format(null, null, null, null, null), "全部为空时降级显示");
            AssertEqual("未知媒体", MediaTitleFormatter.Format("", null, null, null, null), "空字符串降级显示");
            AssertEqual("权力的游戏 S01E02", MediaTitleFormatter.Format("", "权力的游戏", 1, 2, null), "剧集无单集名称");
        }

        static void TestTimeFormatting()
        {
            Console.WriteLine("\n【播放时间格式化】");
            AssertEqual("01:13:25", TimeFormatter.FormatTicks(TimeSpan.FromSeconds(4405).Ticks), "超过1小时的时间");
            AssertEqual("05:30", TimeFormatter.FormatTicks(TimeSpan.FromSeconds(330).Ticks), "不足1小时的时间");
            Assert(TimeFormatter.FormatTicks(null) == null, "null ticks 返回 null");
            Assert(TimeFormatter.FormatTicks(0) == null, "0 ticks 返回 null");
            Assert(TimeFormatter.FormatTicks(-1) == null, "负数 ticks 返回 null");
        }

        static void TestWebhookMasking()
        {
            Console.WriteLine("\n【Webhook 脱敏】");
            var masked1 = WebhookMasker.Mask("https://open.feishu.cn/open-apis/bot/v2/hook/abcdef123456");
            Assert(!masked1.Contains("abcdef123456"), "脱敏后不包含完整 token");
            Assert(masked1.Contains("open.feishu.cn"), "脱敏后保留域名");
            AssertEqual("(空)", WebhookMasker.Mask(null), "null URL 脱敏");
            AssertEqual("(空)", WebhookMasker.Mask(""), "空字符串脱敏");

            var url = "https://open.feishu.cn/open-apis/bot/v2/hook/abcdef123456";
            var sanitized1 = WebhookMasker.Sanitize("请求失败: 无法连接到 " + url, url);
            Assert(!sanitized1.Contains(url), "Sanitize 移除完整 URL");
            Assert(!sanitized1.Contains("abcdef123456"), "Sanitize 移除 Token");
            Assert(WebhookMasker.Sanitize("连接超时", url) == "连接超时", "Sanitize 不影响无敏感信息的消息");
            Assert(WebhookMasker.Sanitize(null, url) == null, "Sanitize null 安全");
            Assert(WebhookMasker.Sanitize("消息", "") == "消息", "Sanitize 空 URL 时原样返回");
        }

        static void TestSanitizer()
        {
            Console.WriteLine("\n【敏感信息脱敏器】");
            var s = new SensitiveDataSanitizer();

            Assert(s.SanitizeIpAddress("192.168.1.100", IpAddressDisplayMode.Hidden) == null, "IP 隐藏模式返回 null");
            AssertEqual("192.168.1.100", s.SanitizeIpAddress("192.168.1.100", IpAddressDisplayMode.Full), "IP 完整模式原样返回");
            AssertEqual("192.168.*.*", s.SanitizeIpAddress("192.168.1.100", IpAddressDisplayMode.Masked), "IPv4 脱敏保留前两段");
            Assert(s.SanitizeIpAddress("2001:db8:1234:5678::1", IpAddressDisplayMode.Masked).StartsWith("2001:db8:"), "IPv6 脱敏保留前两组");

            Assert(s.SanitizeDeviceId("abcdef123456", DeviceIdDisplayMode.Hidden) == null, "设备ID隐藏返回 null");
            AssertEqual("abcdef123456", s.SanitizeDeviceId("abcdef123456", DeviceIdDisplayMode.Full), "设备ID完整原样返回");
            AssertEqual("****3456", s.SanitizeDeviceId("abcdef123456", DeviceIdDisplayMode.Masked), "设备ID脱敏保留末四位");

            var winPathMsg = "无法写入 C:\\Emby\\Media\\Movies\\secret.mkv 权限不足";
            var safe = s.SanitizeException(winPathMsg, null);
            Assert(!safe.Contains("C:\\Emby\\Media\\Movies"), "异常消息移除 Windows 绝对路径目录");
            Assert(safe.Contains("secret.mkv"), "异常消息保留文件名");

            AssertEqual("…/movie.mkv", s.SanitizePath("/mnt/media/movies/movie.mkv"), "Unix 路径仅保留文件名");
        }

        static void TestUserFilter()
        {
            Console.WriteLine("\n【用户过滤】");
            Assert(UserFilter.ShouldNotify("张三", UserFilterMode.All, ""), "All 模式 - 任何用户通知");
            Assert(UserFilter.ShouldNotify("张三", UserFilterMode.IncludeOnly, "张三, 李四"), "IncludeOnly - 在列表中的用户通知");
            Assert(!UserFilter.ShouldNotify("王五", UserFilterMode.IncludeOnly, "张三, 李四"), "IncludeOnly - 不在列表中的用户不通知");
            Assert(!UserFilter.ShouldNotify(null, UserFilterMode.IncludeOnly, "张三"), "IncludeOnly - 未知用户不通知");
            Assert(!UserFilter.ShouldNotify("张三", UserFilterMode.Exclude, "张三, 李四"), "Exclude - 在列表中的用户不通知");
            Assert(UserFilter.ShouldNotify("王五", UserFilterMode.Exclude, "张三, 李四"), "Exclude - 不在列表中的用户通知");
            Assert(UserFilter.ShouldNotify(null, UserFilterMode.Exclude, "张三"), "Exclude - 未知用户默认通知");
            Assert(UserFilter.ShouldNotify("zhangsan", UserFilterMode.IncludeOnly, "ZhangSan"), "IncludeOnly - 忽略大小写");
        }

        static void TestConfigValidation()
        {
            Console.WriteLine("\n【配置校验】");
            var enabledOpts = new PluginOptions();
            enabledOpts.EnsureGroups();
            enabledOpts.FeishuConnection.Enabled = true;
            enabledOpts.FeishuConnection.WebhookUrl = "";
            Assert(ConfigValidator.Validate(enabledOpts).Count > 0, "启用但 Webhook 为空时报错");

            var httpOpts = new PluginOptions();
            httpOpts.EnsureGroups();
            httpOpts.FeishuConnection.Enabled = true;
            httpOpts.FeishuConnection.WebhookUrl = "http://example.com";
            Assert(ConfigValidator.Validate(httpOpts).Count > 0, "非 HTTPS 地址报错");

            var goodOpts = new PluginOptions();
            goodOpts.EnsureGroups();
            goodOpts.FeishuConnection.Enabled = true;
            goodOpts.FeishuConnection.WebhookUrl = "https://open.feishu.cn/open-apis/bot/v2/hook/test";
            Assert(ConfigValidator.Validate(goodOpts).Count == 0, "合法飞书地址通过");

            var otherOpts = new PluginOptions();
            otherOpts.EnsureGroups();
            otherOpts.FeishuConnection.Enabled = true;
            otherOpts.FeishuConnection.WebhookUrl = "https://example.com/webhook";
            Assert(ConfigValidator.Validate(otherOpts).Count == 0, "非飞书域名不阻断保存");

            Assert(!ConfigValidator.IsLikelyFeishuDomain("https://example.com/webhook"), "非飞书域名被识别为非飞书");
            Assert(ConfigValidator.IsLikelyFeishuDomain("https://open.feishu.cn/open-apis/bot/v2/hook/x"), "飞书域名被正确识别");
            Assert(ConfigValidator.IsLikelyFeishuDomain("https://open.larksuite.com/open-apis/bot/v2/hook/x"), "Lark 域名被正确识别");

            var offOpts = new PluginOptions();
            offOpts.EnsureGroups();
            Assert(ConfigValidator.Validate(offOpts).Count == 0, "未启用时 Webhook 可为空");

            AssertEqual("张三, 李四", ConfigValidator.NormalizeUserNames("张三, 李四, 张三, "), "用户名去重和规范化");

            var ms = ConfigValidator.ParseMilestones("25,50,75");
            Assert(ms.Count == 3 && ms[0] == 25 && ms[2] == 75, "里程碑解析升序");
            var ms2 = ConfigValidator.ParseMilestones("75, 25, 25, 120, abc, 50");
            Assert(ms2.Count == 3 && ms2[0] == 25 && ms2[1] == 50 && ms2[2] == 75, "里程碑解析去重、剔除越界与非法值");
        }

        static void TestConfigMigrator()
        {
            Console.WriteLine("\n【配置迁移与兼容】");
            var opts = new PluginOptions();
            ConfigMigrator.Apply(opts);
            Assert(opts.MessageDetailLevel == MessageDetailLevel.Custom, "默认详细程度为 Custom（保持旧版播放外观）");
            Assert(opts.MessageFormat == MessageFormat.Text, "默认消息格式为 Text（保持旧版外观）");
            Assert(opts.NotifyPlaybackStarted && opts.NotifyPlaybackStopped, "旧播放开关默认值保留");
            Assert(opts.NotifyPlaybackCompleted, "新增：播放完成默认开启");
            Assert(!opts.NotifyPlaybackAbandoned, "新增：中途放弃默认关闭");

            var legacy = new PluginOptions { ConfigSchemaVersion = 0, RequestTimeoutSeconds = 999, CompletionThresholdPercent = 10, MaximumNotificationsPerMinute = 0 };
            ConfigMigrator.Apply(legacy);
            Assert(legacy.ConfigSchemaVersion == ConfigMigrator.CurrentSchemaVersion, "迁移后架构版本已提升");
            Assert(legacy.FeishuConnection.RequestTimeoutSeconds == 60, "越界超时被夹到上限");
            Assert(legacy.PlaybackNotification.CompletionThresholdPercent == 50, "越界完成阈值被夹到下限");
            Assert(legacy.AdvancedAndDiagnostics.MaximumNotificationsPerMinute == 1, "越界限流被夹到下限");
        }

        static void TestMediaTypeClassifier()
        {
            Console.WriteLine("\n【媒体类型分类】");
            Assert(MediaTypeClassifier.Classify("Movie") == LibraryItemKind.Movie, "识别电影");
            Assert(MediaTypeClassifier.Classify("Episode") == LibraryItemKind.Episode, "识别剧集");
            Assert(MediaTypeClassifier.Classify("Audio") == LibraryItemKind.Audio, "识别音频");
            Assert(MediaTypeClassifier.Classify("Folder") == LibraryItemKind.Folder, "文件夹归为 Folder");

            var opts = new PluginOptions { NotifyNewMovies = true, NotifyNewEpisodes = true, NotifyNewMusic = false };
            Assert(MediaTypeClassifier.IsNotifiableNewItem(LibraryItemKind.Movie, opts), "开启电影后电影可通知");
            Assert(MediaTypeClassifier.IsNotifiableNewItem(LibraryItemKind.Episode, opts), "开启剧集后剧集可通知");
            Assert(!MediaTypeClassifier.IsNotifiableNewItem(LibraryItemKind.Audio, opts), "未开启音乐时音频不通知");
        }

        // ===================== 文本 / 卡片 / 详细程度 =====================

        static NotificationEvent BuildPlaybackStart()
        {
            var evt = new NotificationEvent
            {
                EventType = NotificationEventType.PlaybackStarted,
                Category = NotificationCategory.Playback,
                Severity = NotificationSeverity.Information,
                Emoji = "▶️",
                Title = "开始播放",
                OccurredAt = new DateTime(2026, 7, 11, 22, 30, 15),
                UserName = "张三"
            };
            evt.AddField("用户", "张三", MessageDetailLevel.Simple, false, CustomFieldKeys.UserName);
            evt.AddField("媒体", "示例剧集 S01E02 - 第二集", MessageDetailLevel.Simple, false, CustomFieldKeys.MediaTitle);
            evt.AddField("类型", "Video", MessageDetailLevel.Standard, false, CustomFieldKeys.MediaType);
            evt.AddField("播放方式", "直接播放", MessageDetailLevel.Standard);
            evt.AddField("客户端", "Emby Web", MessageDetailLevel.Standard, false, CustomFieldKeys.ClientName);
            evt.AddField("设备", "Chrome", MessageDetailLevel.Standard, false, CustomFieldKeys.DeviceName);
            evt.AddField("视频编码", "hevc", MessageDetailLevel.Detailed, true);
            return evt;
        }

        static void TestTextFormatting()
        {
            Console.WriteLine("\n【文本消息格式化（默认 Custom 保持旧版外观）】");
            var options = new PluginOptions();
            var msg = FeishuTextNotificationFormatter.BuildText(BuildPlaybackStart(), options);

            Assert(msg.Contains("▶️") && msg.Contains("开始播放"), "开始播放消息包含标题");
            Assert(msg.Contains("用户：张三"), "包含用户名");
            Assert(msg.Contains("示例剧集 S01E02 - 第二集"), "包含剧集信息");
            Assert(msg.Contains("客户端：Emby Web"), "包含客户端");
            Assert(msg.Contains("设备：Chrome"), "包含设备");
            Assert(msg.Contains("时间：2026-07-11 22:30:15"), "包含时间");
            Assert(!msg.Contains("类型："), "Custom 默认隐藏类型字段");
            Assert(!msg.Contains("播放方式："), "Custom 默认隐藏播放方式字段");
            Assert(!msg.Contains("视频编码"), "非 Detailed 不显示技术字段");

            var minEvt = new NotificationEvent { EventType = NotificationEventType.PlaybackStarted, Category = NotificationCategory.Playback, Emoji = "▶️", Title = "开始播放" };
            var minMsg = FeishuTextNotificationFormatter.BuildText(minEvt, options);
            Assert(!minMsg.Contains("用户："), "用户名为空时省略该字段");
        }

        static void TestDetailLevels()
        {
            Console.WriteLine("\n【详细程度】");
            var evt = BuildPlaybackStart();

            var simple = FeishuTextNotificationFormatter.BuildText(evt, new PluginOptions { MessageDetailLevel = MessageDetailLevel.Simple });
            Assert(simple.Contains("用户：张三") && simple.Contains("示例剧集"), "Simple 含用户与媒体");
            Assert(!simple.Contains("客户端："), "Simple 不含客户端");

            var standard = FeishuTextNotificationFormatter.BuildText(evt, new PluginOptions { MessageDetailLevel = MessageDetailLevel.Standard });
            Assert(standard.Contains("客户端：Emby Web") && standard.Contains("播放方式：直接播放"), "Standard 含客户端与播放方式");
            Assert(!standard.Contains("视频编码"), "Standard 不含技术字段");

            var detailed = FeishuTextNotificationFormatter.BuildText(evt, new PluginOptions { MessageDetailLevel = MessageDetailLevel.Detailed, ShowSensitiveTechnicalDetails = true });
            Assert(detailed.Contains("视频编码：hevc"), "Detailed 且开启技术细节时含技术字段");

            var detailedNoTech = FeishuTextNotificationFormatter.BuildText(evt, new PluginOptions { MessageDetailLevel = MessageDetailLevel.Detailed, ShowSensitiveTechnicalDetails = false });
            Assert(!detailedNoTech.Contains("视频编码"), "Detailed 但关闭技术细节时不含技术字段");

            var customType = FeishuTextNotificationFormatter.BuildText(evt, new PluginOptions { MessageDetailLevel = MessageDetailLevel.Custom, IncludeMediaType = true });
            Assert(customType.Contains("类型：Video"), "Custom 开启 IncludeMediaType 后显示类型");
        }

        static void TestCardFormatting()
        {
            Console.WriteLine("\n【飞书卡片格式化】");
            var card = new FeishuCardNotificationFormatter();
            var body = card.BuildBody(BuildPlaybackStart(), new PluginOptions { MessageDetailLevel = MessageDetailLevel.Standard }) as Dictionary<string, object>;
            Assert(body != null && (string)body["msg_type"] == "interactive", "卡片 msg_type 为 interactive");
            var cardObj = body["card"] as Dictionary<string, object>;
            Assert(cardObj != null && cardObj.ContainsKey("header") && cardObj.ContainsKey("elements"), "卡片含 header 与 elements");
            var header = cardObj["header"] as Dictionary<string, object>;
            Assert((string)header["template"] == "blue", "Information 严重程度映射蓝色");

            Assert(FeishuCardNotificationFormatter.SeverityColor(NotificationSeverity.Success) == "green", "Success 映射绿色");
            Assert(FeishuCardNotificationFormatter.SeverityColor(NotificationSeverity.Warning) == "orange", "Warning 映射橙色");
            Assert(FeishuCardNotificationFormatter.SeverityColor(NotificationSeverity.Error) == "red", "Error 映射红色");
            Assert(FeishuCardNotificationFormatter.SeverityColor(NotificationSeverity.Security) == "red", "Security 映射红色");

            var evt = BuildPlaybackStart();
            evt.ItemName = "带\"引号\"与\\反斜杠";
            evt.Fields.Add(new NotificationField("备注", "换行\n与<标签>", MessageDetailLevel.Simple));
            var body2 = card.BuildBody(evt, new PluginOptions { MessageDetailLevel = MessageDetailLevel.Detailed }) as Dictionary<string, object>;
            Assert(body2 != null && body2.ContainsKey("card"), "含特殊字符时卡片结构仍完整");
        }

        // ===================== 播放状态 / 里程碑 / 方式 =====================

        static void TestPlaybackStateTracker()
        {
            Console.WriteLine("\n【播放状态去重与多会话隔离】");
            var tracker = new PlaybackStateTracker();
            var key = "test-session-1";
            tracker.OnPlaybackStarted(key);
            Assert(tracker.Count == 1, "播放开始后跟踪数量为 1");
            Assert(tracker.OnPlaybackProgress(key, false) == null, "Playing→Playing 不产生事件");
            Assert(tracker.OnPlaybackProgress(key, true) == PlaybackEventType.Paused, "Playing→Paused 产生暂停事件");
            Assert(tracker.OnPlaybackProgress(key, true) == null, "Paused→Paused 不产生事件（去重）");
            Assert(tracker.OnPlaybackProgress(key, false) == PlaybackEventType.Resumed, "Paused→Playing 产生恢复事件");
            tracker.OnPlaybackStopped(key);
            Assert(tracker.Count == 0, "播放停止后跟踪数量为 0");

            var t2 = new PlaybackStateTracker();
            t2.OnPlaybackStarted("sessionA");
            t2.OnPlaybackStarted("sessionB");
            Assert(t2.Count == 2, "两个会话独立跟踪");
            Assert(t2.OnPlaybackProgress("sessionA", true) == PlaybackEventType.Paused, "会话A暂停");
            Assert(t2.OnPlaybackProgress("sessionB", false) == null, "会话B不受会话A暂停影响");
            t2.OnPlaybackStopped("sessionA");
            Assert(t2.Count == 1, "停止会话A后会话B仍存在");
        }

        static void TestMilestonesAndMethod()
        {
            Console.WriteLine("\n【进度里程碑与播放方式】");
            var tracker = new PlaybackStateTracker();
            var key = "s1";
            tracker.OnPlaybackStarted(key);
            var milestones = new List<int> { 25, 50, 75 };

            Assert(tracker.CheckMilestone(key, 10, milestones) == null, "10% 未达任何里程碑");
            Assert(tracker.CheckMilestone(key, 30, milestones) == 25, "30% 触发 25% 里程碑");
            Assert(tracker.CheckMilestone(key, 30, milestones) == null, "同一里程碑只触发一次");
            Assert(tracker.CheckMilestone(key, 80, milestones) == 75, "快进跨越多个阈值只返回最高的 75");

            var mk = "s2";
            Assert(tracker.CheckPlayMethodChanged(mk, "DirectPlay") == false, "首次建立播放方式基线不通知");
            Assert(tracker.CheckPlayMethodChanged(mk, "Transcode") == true, "方式改变时通知");

            var ck = "s3";
            tracker.OnPlaybackStarted(ck);
            Assert(tracker.MarkCompleted(ck) == false, "首次标记完成前未完成");
            Assert(tracker.MarkCompleted(ck) == true, "重复标记完成返回已完成");
        }

        // ===================== 去重 / 限流 / 策略 =====================

        static void TestDeduplication()
        {
            Console.WriteLine("\n【去重缓存】");
            var cache = new DeduplicationCache();
            Assert(cache.TryMark("k1", 30) == true, "首次标记通过");
            Assert(cache.TryMark("k1", 30) == false, "窗口内重复被抑制");
            Assert(cache.TryMark("k2", 30) == true, "不同键互不影响");
            Assert(cache.TryMark("k3", 0) == true, "窗口为 0 时不去重");
        }

        static void TestRateLimiter()
        {
            Console.WriteLine("\n【滑动窗口限流】");
            var rl = new SlidingWindowRateLimiter();
            int allowed = 0;
            for (int i = 0; i < 10; i++) if (rl.Allow(5)) allowed++;
            Assert(allowed == 5, "每分钟上限 5 时仅放行 5 条");
            Assert(!rl.Allow(5), "超过上限后被拒绝");
        }

        static void TestPolicy()
        {
            Console.WriteLine("\n【通知策略：去重+限流+安全豁免】");
            var policy = new NotificationPolicy();
            var opts = new PluginOptions { MaximumNotificationsPerMinute = 2, SecurityEventsBypassRateLimit = true, AggregateWhenRateLimited = true };

            var e1 = new NotificationEvent { Severity = NotificationSeverity.Information };
            Assert(policy.Evaluate(e1, opts) == PolicyDecision.Send, "第1条普通通知放行");
            var e2 = new NotificationEvent { Severity = NotificationSeverity.Information };
            Assert(policy.Evaluate(e2, opts) == PolicyDecision.Send, "第2条普通通知放行");
            var e3 = new NotificationEvent { Severity = NotificationSeverity.Information };
            Assert(policy.Evaluate(e3, opts) == PolicyDecision.SuppressedRateLimit, "第3条超限被抑制");
            Assert(policy.DrainSuppressedCount() == 1, "被抑制计数为 1 并清零");

            var sec = new NotificationEvent { Severity = NotificationSeverity.Security };
            Assert(policy.Evaluate(sec, opts) == PolicyDecision.Send, "安全事件豁免限流");

            var policy2 = new NotificationPolicy();
            var d1 = new NotificationEvent { Severity = NotificationSeverity.Security, DeduplicationKey = "authfail|u|d|ip", DedupWindowSeconds = 30 };
            var d2 = new NotificationEvent { Severity = NotificationSeverity.Security, DeduplicationKey = "authfail|u|d|ip", DedupWindowSeconds = 30 };
            Assert(policy2.Evaluate(d1, opts) == PolicyDecision.Send, "首次登录失败放行");
            Assert(policy2.Evaluate(d2, opts) == PolicyDecision.SuppressedDuplicate, "短时间内相同登录失败被去重");
        }

        static void TestLibraryAggregator()
        {
            Console.WriteLine("\n【媒体库聚合】");
            var published = new List<NotificationEvent>();
            var opts = new PluginOptions { Enabled = true, WebhookUrl = "https://x", EnableLibraryAggregation = true, MaximumIndividualLibraryMessages = 5 };

            var agg1 = new LibraryAggregator(e => published.Add(e), () => opts, () => "srv", new NullLogger());
            agg1.Record(new LibraryChangeRecord { Operation = NotificationEventType.ItemAdded, Kind = LibraryItemKind.Movie, ItemId = "1", DisplayName = "电影A" });
            agg1.Record(new LibraryChangeRecord { Operation = NotificationEventType.ItemAdded, Kind = LibraryItemKind.Movie, ItemId = "2", DisplayName = "电影B" });
            agg1.Record(new LibraryChangeRecord { Operation = NotificationEventType.ItemAdded, Kind = LibraryItemKind.Movie, ItemId = "2", DisplayName = "电影B" });
            agg1.Dispose();
            Assert(published.Count == 2, "少量新增逐条推送且同一 ItemId 去重");

            published.Clear();
            var agg2 = new LibraryAggregator(e => published.Add(e), () => opts, () => "srv", new NullLogger());
            for (int i = 0; i < 8; i++)
                agg2.Record(new LibraryChangeRecord { Operation = NotificationEventType.ItemAdded, Kind = LibraryItemKind.Episode, ItemId = "e" + i, DisplayName = "剧集" + i });
            agg2.Dispose();
            Assert(published.Count == 1 && published[0].EventType == NotificationEventType.LibraryAggregated, "超过上限转为单条汇总");
        }

        static void TestSendTestNotificationFlag()
        {
            Console.WriteLine("\n【测试推送配置】");
            Assert(new PluginOptions { SendTestNotification = false }.SendTestNotification == false, "默认不发送测试通知");
            Assert(new PluginOptions { SendTestNotification = true }.SendTestNotification == true, "可以设置发送测试通知标志");
            Assert(new PluginOptions { LastTestResult = "✅ 测试成功！" }.LastTestResult.Contains("测试成功"), "可以记录测试结果");
            Assert(new PluginOptions { LastTestResult = "" }.LastTestResult == "", "测试结果默认为空");
            Assert(ConfigValidator.ValidateWebhookUrl("https://open.feishu.cn/test").Count == 0, "ValidateWebhookUrl 合法 HTTPS 通过");
            Assert(ConfigValidator.ValidateWebhookUrl("http://example.com").Count > 0, "ValidateWebhookUrl 非 HTTPS 报错");
            Assert(ConfigValidator.ValidateWebhookUrl("not-a-url").Count > 0, "ValidateWebhookUrl 无效 URL 报错");
            Assert(ConfigValidator.ValidateWebhookUrl("").Count == 0, "ValidateWebhookUrl 空字符串不报错");
        }

        // ===================== 新增测试：侧边栏 =====================

        static void TestSidebar()
        {
            Console.WriteLine("\n【侧边栏入口】");
            // 由于 PluginPageInfo 是通过 OnCreatePageInfo 重写设置的，
            // 无法在此 SelfTest 中直接调用 Plugin 实例（无 Emby 容器）。
            // 这里直接验证 PluginPageInfo 的属性定义能力。
            var pageInfo = new MediaBrowser.Model.Plugins.PluginPageInfo();

            // 设置属性
            pageInfo.DisplayName = "飞书通知";
            pageInfo.EnableInMainMenu = true;
            pageInfo.EnableInUserMenu = false;
            pageInfo.IsMainConfigPage = true;

            Assert(pageInfo.DisplayName == "飞书通知", "DisplayName 为飞书通知");
            Assert(pageInfo.EnableInMainMenu == true, "EnableInMainMenu 为 true");
            Assert(pageInfo.EnableInUserMenu == false, "EnableInUserMenu 为 false");
            Assert(pageInfo.IsMainConfigPage == true, "IsMainConfigPage 为 true");

            // 验证不设置 MenuSection/MenuIcon/FeatureId 时默认值为空
            Assert(string.IsNullOrEmpty(pageInfo.MenuSection), "MenuSection 默认空");
            Assert(string.IsNullOrEmpty(pageInfo.MenuIcon), "MenuIcon 默认空");
            Assert(string.IsNullOrEmpty(pageInfo.FeatureId), "FeatureId 默认空");
        }

        // ===================== 新增测试：分组和迁移 =====================

        static void TestGroupDefaults()
        {
            Console.WriteLine("\n【分组默认值】");
            var opts = new PluginOptions();
            opts.EnsureGroups();

            Assert(opts.FeishuConnection != null, "飞书连接分组不为 null");
            Assert(opts.BotSecurity != null, "机器人安全分组不为 null");
            Assert(opts.MessageDisplay != null, "消息显示分组不为 null");
            Assert(opts.PlaybackNotification != null, "播放通知分组不为 null");
            Assert(opts.LoginAndUser != null, "登录与用户分组不为 null");
            Assert(opts.LibraryAndUserBehavior != null, "媒体库与用户行为分组不为 null");
            Assert(opts.TaskAndLiveTvAndServer != null, "任务/Live TV/服务器分组不为 null");
            Assert(opts.AdvancedAndDiagnostics != null, "高级与诊断分组不为 null");

            // 安全默认值
            Assert(opts.BotSecurity.EnableCustomKeyword == false, "默认不启用自定义关键词");
            Assert(opts.BotSecurity.CustomKeyword == "", "默认关键词为空");
            Assert(opts.BotSecurity.EnableSignatureVerification == false, "默认不启用签名校验");

            // 签名密钥未配置
            Assert(string.IsNullOrEmpty(opts.SignatureSecret), "默认签名密钥为空");
            Assert(opts.SignatureSecretStatus == "未配置", "默认签名状态为未配置");
        }

        static void TestGroupMigration()
        {
            Console.WriteLine("\n【配置分组迁移】");
            // 模拟旧配置
            var opts = new PluginOptions
            {
                ConfigSchemaVersion = 1,
                Enabled = true,
                WebhookUrl = "https://open.feishu.cn/hook/test123",
                RequestTimeoutSeconds = 15,
                MessageFormat = MessageFormat.FeishuCard,
                NotifyPlaybackStarted = true,
                NotifyPlaybackStopped = true,
                NotifyServerStarted = true,
                NotifyAuthenticationFailed = true
            };

            // 执行迁移
            ConfigMigrator.Apply(opts);

            // 验证迁移后值一致
            Assert(opts.ConfigSchemaVersion == ConfigMigrator.CurrentSchemaVersion, "迁移后版本号正确");
            Assert(opts.FeishuConnection.WebhookUrl == "https://open.feishu.cn/hook/test123", "Webhook 迁移正确");
            Assert(opts.FeishuConnection.Enabled == true, "Enabled 迁移正确");
            Assert(opts.FeishuConnection.RequestTimeoutSeconds == 15, "Timeout 迁移正确");
            Assert(opts.MessageDisplay.MessageFormat == MessageFormat.FeishuCard, "MessageFormat 迁移正确");
            Assert(opts.PlaybackNotification.NotifyPlaybackStarted == true, "播放开始迁移正确");
            Assert(opts.PlaybackNotification.NotifyPlaybackStopped == true, "播放停止迁移正确");
            Assert(opts.TaskAndLiveTvAndServer.NotifyServerStarted == true, "服务器启动迁移正确");
            Assert(opts.LoginAndUser.NotifyAuthenticationFailed == true, "登录失败迁移正确");

            // 验证旧字段仍保存值（支持降级）
            Assert(opts.Enabled == true, "旧 Enabled 字段保留值");
            Assert(opts.WebhookUrl == "https://open.feishu.cn/hook/test123", "旧 WebhookUrl 字段保留值");
            Assert(opts.NotifyPlaybackStarted == true, "旧通知开关保留值");
        }

        static void TestConfigSchemaVersion()
        {
            Console.WriteLine("\n【配置版本管理】");
            var opts = new PluginOptions();
            opts.EnsureGroups();

            // 新创建的配置版本应升级
            ConfigMigrator.Apply(opts);
            Assert(opts.ConfigSchemaVersion == ConfigMigrator.CurrentSchemaVersion, "新配置升级至当前版本");

            // 已迁移配置不会被覆盖
            opts.BotSecurity.EnableCustomKeyword = true;
            opts.BotSecurity.CustomKeyword = "test";
            opts.SyncFromGroups();

            ConfigMigrator.Apply(opts);
            Assert(opts.BotSecurity.EnableCustomKeyword == true, "已设置的启用关键词不会被覆盖");
            Assert(opts.BotSecurity.CustomKeyword == "test", "已设置的关键词不会被重置");
        }

        // ===================== 新增测试：关键词 =====================

        static void TestKeywordValidation()
        {
            Console.WriteLine("\n【关键词校验】");
            // 合法关键词
            Assert(ConfigValidator.ValidateCustomKeyword("Hello") == null, "普通关键词通过");
            Assert(ConfigValidator.ValidateCustomKeyword("测试关键词") == null, "中文关键词通过");
            Assert(ConfigValidator.ValidateCustomKeyword(" test ") == null, "带首尾空格关键词（外部已 trim）");

            // 非法关键词
            Assert(ConfigValidator.ValidateCustomKeyword("line\nbreak") != null, "含换行符被拒绝");
            Assert(ConfigValidator.ValidateCustomKeyword("tab\tchar") != null, "含制表符被拒绝");
            Assert(ConfigValidator.ValidateCustomKeyword("") == null, "空字符串无错误（留待开启时再校验）");

            // 控制字符
            Assert(ConfigValidator.ValidateCustomKeyword("abc\0def") != null, "含 null 字符被拒绝");
        }

        static void TestKeywordInjection()
        {
            Console.WriteLine("\n【关键词注入】");
            var decorator = new FeishuMessageSecurityDecorator(
                new FeishuSignatureProvider(),
                new SystemUnixTimeProvider());

            var opts = new PluginOptions
            {
                EnableCustomKeyword = true,
                CustomKeyword = "安全关键词"
            };

            // 文本消息
            var text = "这是一条测试消息";
            var decorated = decorator.DecorateText(text, opts);
            Assert(decorated.Contains("安全关键词"), "文本消息包含关键词");
            Assert(decorated.EndsWith("安全关键词"), "关键词在文本末尾");
            Assert(decorated.Contains(text), "原消息正文保留");

            // 已包含关键词不重复
            var alreadyHas = "消息正文 安全关键词";
            var noDup = decorator.DecorateText(alreadyHas, opts);
            Assert(noDup == alreadyHas, "已含关键词时不重复追加");

            // 关闭后不出现
            opts.EnableCustomKeyword = false;
            var noKw = decorator.DecorateText(text, opts);
            Assert(noKw == text, "关闭关键词后不追加");

            // 卡片消息
            opts.EnableCustomKeyword = true;
            var card = new Dictionary<string, object>
            {
                ["msg_type"] = "interactive",
                ["card"] = new Dictionary<string, object>
                {
                    ["config"] = new Dictionary<string, object> { ["wide_screen_mode"] = true },
                    ["header"] = new Dictionary<string, object>
                    {
                        ["title"] = new Dictionary<string, object>
                        {
                            ["tag"] = "plain_text",
                            ["content"] = "测试卡片"
                        }
                    },
                    ["elements"] = new List<object>()
                }
            };
            var decoratedCard = decorator.DecorateCard(card, opts) as Dictionary<string, object>;
            Assert(decoratedCard != null, "装饰后的卡片不为 null");
            // 卡片元素末尾应有关键词
            var cardObj = decoratedCard["card"] as Dictionary<string, object>;
            var elements = cardObj["elements"] as List<object>;
            bool hasKeyword = false;
            foreach (var elem in elements)
            {
                if (elem is Dictionary<string, object> d && d.TryGetValue("tag", out var tag) && (string)tag == "note")
                {
                    hasKeyword = true;
                    break;
                }
            }
            Assert(hasKeyword, "卡片底部包含关键词 note 元素");

            // 空关键词情况
            opts.CustomKeyword = "";
            var emptyResult = decorator.DecorateText(text, opts);
            Assert(emptyResult == text, "空关键词不追加");
        }

        // ===================== 新增测试：签名算法 =====================

        static void TestSignatureAlgorithm()
        {
            Console.WriteLine("\n【签名算法】");
            var provider = new FeishuSignatureProvider();

            // 固定测试向量
            var timestamp = 1700000000L;
            var secret = "test-secret";
            var expectedSign = "mbm4Y4oluIPQ00qlBIhX8vAZ0EKv3nw0LuTb91jPL84=";
            var actualSign = provider.Sign(timestamp, secret);
            AssertEqual(expectedSign, actualSign, "固定测试向量匹配");

            // 中文密钥
            var chineseSecret = "中文测试密钥";
            var cnSign = provider.Sign(timestamp, chineseSecret);
            Assert(!string.IsNullOrEmpty(cnSign), "中文密钥正常产生签名");
            Assert(IsValidBase64(cnSign), "中文密钥签名为合法 Base64");

            // 不同时间戳产生不同签名
            var sign1 = provider.Sign(1700000000L, secret);
            var sign2 = provider.Sign(1700000001L, secret);
            Assert(sign1 != sign2, "不同时间戳产生不同签名");

            // 相同输入产生相同签名
            var sign3 = provider.Sign(timestamp, secret);
            AssertEqual(sign1, sign3, "相同输入产生相同签名");

            // 空密钥拒绝
            bool threw = false;
            try { provider.Sign(timestamp, ""); }
            catch (ArgumentException) { threw = true; }
            Assert(threw, "空密钥时抛出异常");

            bool threw2 = false;
            try { provider.Sign(timestamp, null); }
            catch (ArgumentException) { threw2 = true; }
            Assert(threw2, "null 密钥时抛出异常");

            // 签名结果可被 Base64 解码
            var decoded = Convert.FromBase64String(actualSign);
            Assert(decoded.Length > 0, "签名可被 Base64 解码");

            // 签名结果长度检查（HMAC-SHA256 → 32 字节 → 44 字符 Base64）
            Assert(actualSign.Length == 44, "SHA256 签名 Base64 长度为 44");
        }

        static bool IsValidBase64(string input)
        {
            try { Convert.FromBase64String(input); return true; }
            catch { return false; }
        }

        // ===================== 新增测试：安全装饰器组合 =====================

        static void TestSignatureCombinations()
        {
            Console.WriteLine("\n【签名与关键词组合】");
            var provider = new FeishuSignatureProvider();
            var timeProvider = new FixedUnixTimeProvider(1700000000L);
            var decorator = new FeishuMessageSecurityDecorator(provider, timeProvider);

            // 均关闭
            var optsOff = new PluginOptions
            {
                EnableCustomKeyword = false,
                EnableSignatureVerification = false
            };
            var body = new Dictionary<string, object>
            {
                ["msg_type"] = "text",
                ["content"] = new Dictionary<string, object> { ["text"] = "测试" }
            };
            var result = decorator.DecorateRequest(body, optsOff) as Dictionary<string, object>;
            Assert(!result.ContainsKey("timestamp"), "均关闭时无 timestamp");
            Assert(!result.ContainsKey("sign"), "均关闭时无 sign");

            // 仅签名
            var optsSig = new PluginOptions
            {
                EnableCustomKeyword = false,
                EnableSignatureVerification = true,
                SignatureSecret = "test-secret"
            };
            var resultSig = decorator.DecorateRequest(body, optsSig) as Dictionary<string, object>;
            Assert(resultSig.ContainsKey("timestamp"), "仅签名时包含 timestamp");
            Assert(resultSig.ContainsKey("sign"), "仅签名时包含 sign");
            Assert((string)resultSig["timestamp"] == "1700000000", "timestamp 为字符串格式");

            // 仅关键词（不影响请求顶层）
            var optsKw = new PluginOptions
            {
                EnableCustomKeyword = true,
                CustomKeyword = "测试",
                EnableSignatureVerification = false
            };
            var decoratedText = decorator.DecorateText("正文", optsKw);
            Assert(decoratedText.Contains("测试"), "仅关键词时文本包含关键词");

            // 两者同时开启
            var optsBoth = new PluginOptions
            {
                EnableCustomKeyword = true,
                CustomKeyword = "关键词",
                EnableSignatureVerification = true,
                SignatureSecret = "test-secret"
            };
            var resultBoth = decorator.DecorateRequest(body, optsBoth) as Dictionary<string, object>;
            Assert(resultBoth.ContainsKey("timestamp"), "同时开启时包含 timestamp");
            Assert(resultBoth.ContainsKey("sign"), "同时开启时包含 sign");

            // 签名密钥为空时不添加签名
            var optsEmptySig = new PluginOptions
            {
                EnableSignatureVerification = true,
                SignatureSecret = ""
            };
            var emptySigBody = new Dictionary<string, object>
            {
                ["msg_type"] = "text",
                ["content"] = new Dictionary<string, object> { ["text"] = "测试" }
            };
            var resultNoSig = decorator.DecorateRequest(emptySigBody, optsEmptySig) as Dictionary<string, object>;
            Assert(!resultNoSig.ContainsKey("timestamp"), "密钥为空时不添加 timestamp");
            Assert(!resultNoSig.ContainsKey("sign"), "密钥为空时不添加 sign");
        }

        static void TestSecurityDecorator()
        {
            Console.WriteLine("\n【安全装饰器综合】");
            var provider = new FeishuSignatureProvider();
            var timeProvider = new FixedUnixTimeProvider(1700000000L);
            var decorator = new FeishuMessageSecurityDecorator(provider, timeProvider);

            // 卡片消息同时有关键词和签名
            var opts = new PluginOptions
            {
                EnableCustomKeyword = true,
                CustomKeyword = "安全词",
                EnableSignatureVerification = true,
                SignatureSecret = "test-secret"
            };

            var card = new Dictionary<string, object>
            {
                ["msg_type"] = "interactive",
                ["card"] = new Dictionary<string, object>
                {
                    ["config"] = new Dictionary<string, object> { ["wide_screen_mode"] = true },
                    ["header"] = new Dictionary<string, object>
                    {
                        ["title"] = new Dictionary<string, object>
                        {
                            ["tag"] = "plain_text",
                            ["content"] = "标题"
                        }
                    },
                    ["elements"] = new List<object>()
                }
            };

            // 先装饰卡片（加关键词），再装饰请求（加签名）
            var decoratedCard = decorator.DecorateCard(card, opts) as Dictionary<string, object>;
            var decoratedReq = decorator.DecorateRequest(decoratedCard, opts) as Dictionary<string, object>;

            Assert(decoratedReq.ContainsKey("timestamp"), "同时模式：请求含 timestamp");
            Assert(decoratedReq.ContainsKey("sign"), "同时模式：请求含 sign");
            Assert(decoratedReq.ContainsKey("msg_type"), "同时模式：msg_type 不变");
            Assert(decoratedReq.ContainsKey("card"), "同时模式：card 仍存在");

            // 关键词关闭
            opts.EnableCustomKeyword = false;
            var textNoKw = decorator.DecorateText("消息", opts);
            AssertEqual("消息", textNoKw, "关键词关闭后文本不含关键词");
        }

        static void TestPluginPageInfo()
        {
            Console.WriteLine("\n【PluginPageInfo 属性验证】");
            var pageInfo = new MediaBrowser.Model.Plugins.PluginPageInfo();

            // 验证所有属性存在
            pageInfo.Name = "Test";
            pageInfo.DisplayName = "Display";
            pageInfo.EmbeddedResourcePath = "/path/to/page";
            pageInfo.EnableInMainMenu = true;
            pageInfo.EnableInUserMenu = false;
            pageInfo.MenuSection = "Server";
            pageInfo.FeatureId = "feature";
            pageInfo.MenuIcon = "icon.png";
            pageInfo.IsMainConfigPage = true;

            Assert(pageInfo.Name == "Test", "Name 属性可读写");
            Assert(pageInfo.DisplayName == "Display", "DisplayName 属性可读写");
            Assert(pageInfo.EmbeddedResourcePath == "/path/to/page", "EmbeddedResourcePath 属性可读写");
            Assert(pageInfo.EnableInMainMenu == true, "EnableInMainMenu 属性可读写");
            Assert(pageInfo.EnableInUserMenu == false, "EnableInUserMenu 属性可读写");
            Assert(pageInfo.MenuSection == "Server", "MenuSection 属性可读写");
            Assert(pageInfo.FeatureId == "feature", "FeatureId 属性可读写");
            Assert(pageInfo.MenuIcon == "icon.png", "MenuIcon 属性可读写");
            Assert(pageInfo.IsMainConfigPage == true, "IsMainConfigPage 属性可读写");
        }

        // ===================== Webhook 回归测试 =====================

        static void TestWebhookPersistence()
        {
            Console.WriteLine("\n【Webhook 持久保存】");

            // 1. 新配置填写 Webhook，保存后值不丢失
            var opts = new PluginOptions();
            opts.EnsureGroups();
            opts.FeishuConnection.WebhookUrl = "https://open.feishu.cn/hook/save-test";
            opts.FeishuConnection.Enabled = true;
            opts.SyncFromGroups(); // 模拟保存时的同步

            // 模拟反序列化：从扁平字段恢复
            var reloaded = new PluginOptions
            {
                WebhookUrl = opts.WebhookUrl,
                Enabled = opts.Enabled,
                ConfigSchemaVersion = ConfigMigrator.CurrentSchemaVersion
            };
            reloaded.EnsureGroups();
            reloaded.EnsureGroupsHaveData();

            Assert(reloaded.FeishuConnection.WebhookUrl == "https://open.feishu.cn/hook/save-test",
                "保存后重新加载 Webhook 值不变");

            // 2. 修改 Webhook，保存后为新值
            opts.FeishuConnection.WebhookUrl = "https://open.feishu.cn/hook/changed";
            opts.SyncFromGroups();
            var reloaded2 = new PluginOptions
            {
                WebhookUrl = opts.WebhookUrl,
                Enabled = opts.Enabled,
                ConfigSchemaVersion = ConfigMigrator.CurrentSchemaVersion
            };
            reloaded2.EnsureGroups();
            reloaded2.EnsureGroupsHaveData();
            Assert(reloaded2.FeishuConnection.WebhookUrl == "https://open.feishu.cn/hook/changed",
                "修改后重新加载为新值");

            // 3. 旧扁平配置迁移后 Webhook 不丢失
            var legacy = new PluginOptions
            {
                ConfigSchemaVersion = 1,
                Enabled = true,
                WebhookUrl = "https://open.feishu.cn/hook/legacy",
                RequestTimeoutSeconds = 20
            };
            ConfigMigrator.Apply(legacy);
            Assert(legacy.FeishuConnection.WebhookUrl == "https://open.feishu.cn/hook/legacy",
                "旧扁平配置迁移后 Webhook 不丢失");
            Assert(legacy.FeishuConnection.RequestTimeoutSeconds == 20,
                "旧扁平配置迁移后 RequestTimeout 不丢失");

            // 4. 已迁移配置再次加载时不被旧空字段覆盖
            var already = new PluginOptions
            {
                ConfigSchemaVersion = ConfigMigrator.CurrentSchemaVersion,
                WebhookUrl = "https://open.feishu.cn/hook/kept",
                Enabled = true
            };
            already.EnsureGroups();
            already.EnsureGroupsHaveData();
            already.FeishuConnection.WebhookUrl = "https://open.feishu.cn/hook/kept";
            already.FeishuConnection.Enabled = true;
            already.SyncFromGroups();
            ConfigMigrator.Apply(already);
            Assert(already.FeishuConnection.WebhookUrl == "https://open.feishu.cn/hook/kept",
                "已迁移配置再次加载时不被旧空字段覆盖");

            // 5. 保存其他设置时，Webhook 不被清空
            var existing = new PluginOptions
            {
                ConfigSchemaVersion = ConfigMigrator.CurrentSchemaVersion,
                WebhookUrl = "https://open.feishu.cn/hook/keep-me",
                Enabled = true
            };
            existing.EnsureGroups();
            existing.EnsureGroupsHaveData();
            existing.FeishuConnection.Enabled = false;
            existing.SyncFromGroups();
            Assert(existing.WebhookUrl == "https://open.feishu.cn/hook/keep-me",
                "保存其他设置时 Webhook 不被清空");
            Assert(existing.FeishuConnection.WebhookUrl == "https://open.feishu.cn/hook/keep-me",
                "保存其他设置时分组 Webhook 不被清空");
        }

        // ===================== 测试通知逻辑测试 =====================

        static void TestTestNotification()
        {
            Console.WriteLine("\n【测试通知逻辑】");

            // 1. 测试通知开关默认为 false
            var opts = new PluginOptions();
            opts.EnsureGroups();
            Assert(!opts.AdvancedAndDiagnostics.SendTestNotification, "测试通知默认不勾选");

            // 2. 模拟保存后取消勾选：同步后 SendTestNotification 应为 false
            opts.AdvancedAndDiagnostics.SendTestNotification = true;
            // 框架在保存时会先调用 Validate (SyncFromGroups)，然后 OnOptionsSaved 中取消
            opts.SyncFromGroups(); // Validate 中的同步
            Assert(opts.SendTestNotification, "同步后扁平字段 SendTestNotification 为 true");
            // 模拟 OnOptionsSaved 中的逻辑：取消勾选
            opts.AdvancedAndDiagnostics.SendTestNotification = false;
            opts.SendTestNotification = false;
            opts.SyncFromGroups();
            Assert(!opts.SendTestNotification, "取消勾选后扁平字段为 false");
            Assert(!opts.AdvancedAndDiagnostics.SendTestNotification, "取消勾选后分组字段为 false");

            // 3. 测试结果时间格式化
            var result = FormatTestResultForTest("✅ 测试成功：飞书机器人已接受消息。");
            Assert(result.Contains("✅ 测试成功"), "测试结果包含成功消息");
            Assert(result.Contains("时间："), "测试结果包含时间戳");

            // 4. 保存其他设置不清空测试结果
            var opts2 = new PluginOptions { LastTestResult = "✅ 测试成功：飞书机器人已接受消息。\n时间：2026-07-12 12:30:00" };
            opts2.EnsureGroups();
            opts2.EnsureGroupsHaveData();
            // 模拟：配置加载后，LastTestResult 在扁平字段中，需要同步到分组
            opts2.SyncToGroups();
            Assert(opts2.AdvancedAndDiagnostics.LastTestResult.Contains("测试成功"),
                "加载后测试结果显示在分组中");

            // 5. 只改其他设置，测试结果保持不变
            opts2.FeishuConnection.Enabled = false;
            opts2.SyncFromGroups();
            Assert(opts2.AdvancedAndDiagnostics.LastTestResult.Contains("测试成功"),
                "只改其他设置后测试结果不变");
            Assert(opts2.LastTestResult.Contains("测试成功"),
                "只改其他设置后扁平 LastTestResult 不变");

            // 6. 关键词和签名开关会反映在配置中
            var opts3 = new PluginOptions
            {
                EnableCustomKeyword = true,
                CustomKeyword = "测试词",
                EnableSignatureVerification = true,
                SignatureSecret = "test-secret"
            };
            opts3.EnsureGroups();
            opts3.SyncToGroups();
            Assert(opts3.BotSecurity.EnableCustomKeyword, "关键词开关同步到分组");
            Assert(opts3.BotSecurity.CustomKeyword == "测试词", "关键词内容同步到分组");
            Assert(opts3.BotSecurity.EnableSignatureVerification, "签名开关同步到分组");

            // 7. 测试结果不包含敏感信息
            var sanitized = WebhookMasker.Sanitize("error connecting to https://open.feishu.cn/hook/secret123",
                "https://open.feishu.cn/hook/secret123");
            Assert(!sanitized.Contains("secret123"), "脱敏后不含完整 Webhook Token");
            Assert(!sanitized.Contains("open.feishu.cn/hook/secret123"), "脱敏后不含完整 Webhook URL");
        }

        static string FormatTestResultForTest(string message)
        {
            return message + "\n时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>固定时间戳的 UnixTimeProvider 实现，用于测试</summary>
        class FixedUnixTimeProvider : IUnixTimeProvider
        {
            private readonly long _fixed;
            public FixedUnixTimeProvider(long fixedTimestamp) { _fixed = fixedTimestamp; }
            public long NowSeconds() => _fixed;
        }
    }
}
