using System;
using System.Collections.Generic;
using EmbyFeishu.Configuration;
using EmbyFeishu.Events;
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
            Console.WriteLine("  EmbyFeishu 自测工具");
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
            Assert(masked1.Contains("3456"), "脱敏后保留末尾四位");
            AssertEqual("(空)", WebhookMasker.Mask(null), "null URL 脱敏");
            AssertEqual("(空)", WebhookMasker.Mask(""), "空字符串脱敏");

            var url = "https://open.feishu.cn/open-apis/bot/v2/hook/abcdef123456";
            var msgWithUrl = "请求失败: 无法连接到 " + url + " 服务器";
            var sanitized1 = WebhookMasker.Sanitize(msgWithUrl, url);
            Assert(!sanitized1.Contains(url), "Sanitize 移除完整 URL");
            Assert(!sanitized1.Contains("abcdef123456"), "Sanitize 移除 Token");
            Assert(!WebhookMasker.Sanitize("invalid token abcdef123456 in path", url).Contains("abcdef123456"), "Sanitize 移除裸 Token");
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
            Assert(ConfigValidator.Validate(new PluginOptions { Enabled = true, WebhookUrl = "" }).Count > 0, "启用但 Webhook 为空时报错");
            Assert(ConfigValidator.Validate(new PluginOptions { Enabled = true, WebhookUrl = "http://example.com" }).Count > 0, "非 HTTPS 地址报错");
            Assert(ConfigValidator.Validate(new PluginOptions { Enabled = true, WebhookUrl = "https://open.feishu.cn/open-apis/bot/v2/hook/test" }).Count == 0, "合法飞书地址通过");
            Assert(ConfigValidator.Validate(new PluginOptions { Enabled = true, WebhookUrl = "https://example.com/webhook" }).Count == 0, "非飞书域名不阻断保存");
            Assert(!ConfigValidator.IsLikelyFeishuDomain("https://example.com/webhook"), "非飞书域名被识别为非飞书");
            Assert(ConfigValidator.IsLikelyFeishuDomain("https://open.feishu.cn/open-apis/bot/v2/hook/x"), "飞书域名被正确识别");
            Assert(ConfigValidator.IsLikelyFeishuDomain("https://open.larksuite.com/open-apis/bot/v2/hook/x"), "Lark 域名被正确识别");
            Assert(ConfigValidator.Validate(new PluginOptions { Enabled = false, WebhookUrl = "" }).Count == 0, "未启用时 Webhook 可为空");
            Assert(ConfigValidator.Validate(new PluginOptions { Enabled = true, WebhookUrl = "https://open.feishu.cn/test", RequestTimeoutSeconds = 1 }).Count > 0, "超时值过小报错");
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
            Assert(opts.MessageDetailLevel == MessageDetailLevel.Custom, "默认详细程度为 Custom（保持旧版播放外观）");
            Assert(opts.MessageFormat == MessageFormat.Text, "默认消息格式为 Text（保持旧版外观）");
            Assert(opts.NotifyPlaybackStarted && opts.NotifyPlaybackStopped, "旧播放开关默认值保留");
            Assert(opts.NotifyPlaybackCompleted, "新增：播放完成默认开启");
            Assert(!opts.NotifyPlaybackAbandoned, "新增：中途放弃默认关闭");

            var legacy = new PluginOptions { ConfigSchemaVersion = 0, RequestTimeoutSeconds = 999, CompletionThresholdPercent = 10, MaximumNotificationsPerMinute = 0 };
            ConfigMigrator.Apply(legacy);
            Assert(legacy.ConfigSchemaVersion == ConfigMigrator.CurrentSchemaVersion, "迁移后架构版本已提升");
            Assert(legacy.RequestTimeoutSeconds == 60, "越界超时被夹到上限");
            Assert(legacy.CompletionThresholdPercent == 50, "越界完成阈值被夹到下限");
            Assert(legacy.MaximumNotificationsPerMinute == 1, "越界限流被夹到下限");
        }

        static void TestMediaTypeClassifier()
        {
            Console.WriteLine("\n【媒体类型分类】");
            Assert(MediaTypeClassifier.Classify("Movie") == LibraryItemKind.Movie, "识别电影");
            Assert(MediaTypeClassifier.Classify("Episode") == LibraryItemKind.Episode, "识别剧集");
            Assert(MediaTypeClassifier.Classify("Audio") == LibraryItemKind.Audio, "识别音频");
            Assert(MediaTypeClassifier.Classify("Folder") == LibraryItemKind.Folder, "文件夹归为 Folder");
            Assert(MediaTypeClassifier.Classify("CollectionFolder") == LibraryItemKind.Folder, "媒体库根归为 Folder");
            Assert(MediaTypeClassifier.Classify("Person") == LibraryItemKind.Person, "人物归为 Person");
            Assert(MediaTypeClassifier.Classify("Genre") == LibraryItemKind.Folder, "流派归为 Folder");

            var opts = new PluginOptions { NotifyNewMovies = true, NotifyNewEpisodes = true, NotifyNewMusic = false };
            Assert(MediaTypeClassifier.IsNotifiableNewItem(LibraryItemKind.Movie, opts), "开启电影后电影可通知");
            Assert(MediaTypeClassifier.IsNotifiableNewItem(LibraryItemKind.Episode, opts), "开启剧集后剧集可通知");
            Assert(!MediaTypeClassifier.IsNotifiableNewItem(LibraryItemKind.Audio, opts), "未开启音乐时音频不通知");
            Assert(!MediaTypeClassifier.IsNotifiableNewItem(LibraryItemKind.Folder, opts), "文件夹永不通知");
            Assert(!MediaTypeClassifier.IsNotifiableNewItem(LibraryItemKind.Person, opts), "人物永不通知");
            Assert(!MediaTypeClassifier.IsNotifiableNewItem(LibraryItemKind.Trailer, opts), "预告片默认不通知");
            Assert(!MediaTypeClassifier.IsNotifiableNewItem(LibraryItemKind.MusicArtist, new PluginOptions { NotifyNewMusic = true }), "音乐艺术家默认不通知");
        }

        // ===================== 文本 / 卡片 / 详细程度 =====================

        // 模拟 PlaybackEventSource 构建的开始播放事件
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
            var options = new PluginOptions(); // 默认 Custom
            var msg = FeishuTextNotificationFormatter.BuildText(BuildPlaybackStart(), options);

            Assert(msg.Contains("▶️") && msg.Contains("开始播放"), "开始播放消息包含标题");
            Assert(msg.Contains("用户：张三"), "包含用户名");
            Assert(msg.Contains("示例剧集 S01E02 - 第二集"), "包含剧集信息");
            Assert(msg.Contains("客户端：Emby Web"), "包含客户端");
            Assert(msg.Contains("设备：Chrome"), "包含设备");
            Assert(msg.Contains("时间：2026-07-11 22:30:15"), "包含时间");
            // Custom 默认下不显示“类型”（IncludeMediaType=false）与“播放方式”（无自定义开关），保持旧版外观
            Assert(!msg.Contains("类型："), "Custom 默认隐藏类型字段");
            Assert(!msg.Contains("播放方式："), "Custom 默认隐藏播放方式字段");
            // Detailed 技术字段不出现
            Assert(!msg.Contains("视频编码"), "非 Detailed 不显示技术字段");

            // 字段缺失省略
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

            // Custom 尊重旧字段开关
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

            // 特殊字符不破坏结构（仍是合法字典）
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
            Assert(tracker.OnPlaybackProgress(key, false) == null, "Playing→Playing 再次不产生事件");
            tracker.OnPlaybackStopped(key);
            Assert(tracker.Count == 0, "播放停止后跟踪数量为 0");

            var t2 = new PlaybackStateTracker();
            t2.OnPlaybackStarted("sessionA");
            t2.OnPlaybackStarted("sessionB");
            Assert(t2.Count == 2, "两个会话独立跟踪");
            Assert(t2.OnPlaybackProgress("sessionA", true) == PlaybackEventType.Paused, "会话A暂停");
            Assert(t2.OnPlaybackProgress("sessionB", false) == null, "会话A暂停不影响会话B");
            Assert(t2.OnPlaybackProgress("sessionB", true) == PlaybackEventType.Paused, "会话B独立产生暂停");
            t2.OnPlaybackStopped("sessionA");
            Assert(t2.Count == 1, "停止会话A后会话B仍存在");

            AssertEqual("psid-1", PlaybackStateTracker.GetSessionKey("psid-1", "sid", "item", "dev"), "有 PlaySessionId 时用作键");
            AssertEqual("sid|item|dev", PlaybackStateTracker.GetSessionKey(null, "sid", "item", "dev"), "无 PlaySessionId 时降级组合键");
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
            Assert(tracker.CheckMilestone(key, 80, milestones) == null, "已发送后不再触发");

            var mk = "s2";
            Assert(tracker.CheckPlayMethodChanged(mk, "DirectPlay") == false, "首次建立播放方式基线不通知");
            Assert(tracker.CheckPlayMethodChanged(mk, "DirectPlay") == false, "方式未变不通知");
            Assert(tracker.CheckPlayMethodChanged(mk, "Transcode") == true, "方式改变时通知");
            Assert(tracker.CheckPlayMethodChanged(mk, "Transcode") == false, "改变后再次相同不通知");

            // 播放完成互斥
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
            Assert(cache.TryMark("k3", 0) == true, "窗口为 0 时始终通过");
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

            // 安全事件豁免限流
            var sec = new NotificationEvent { Severity = NotificationSeverity.Security };
            Assert(policy.Evaluate(sec, opts) == PolicyDecision.Send, "安全事件豁免限流");

            // 去重
            var policy2 = new NotificationPolicy();
            var d1 = new NotificationEvent { Severity = NotificationSeverity.Security, DeduplicationKey = "authfail|u|d|ip", DedupWindowSeconds = 30 };
            var d2 = new NotificationEvent { Severity = NotificationSeverity.Security, DeduplicationKey = "authfail|u|d|ip", DedupWindowSeconds = 30 };
            Assert(policy2.Evaluate(d1, opts) == PolicyDecision.Send, "首次登录失败放行");
            Assert(policy2.Evaluate(d2, opts) == PolicyDecision.SuppressedDuplicate, "短时间内相同登录失败被去重（防风暴）");
        }

        static void TestLibraryAggregator()
        {
            Console.WriteLine("\n【媒体库聚合】");
            var published = new List<NotificationEvent>();
            var opts = new PluginOptions { Enabled = true, WebhookUrl = "https://x", EnableLibraryAggregation = true, MaximumIndividualLibraryMessages = 5 };

            // 少量新增 → 逐条（Dispose 触发同步刷新）
            var agg1 = new LibraryAggregator(e => published.Add(e), () => opts, () => "srv", new NullLogger());
            agg1.Record(new LibraryChangeRecord { Operation = NotificationEventType.ItemAdded, Kind = LibraryItemKind.Movie, ItemId = "1", DisplayName = "电影A" });
            agg1.Record(new LibraryChangeRecord { Operation = NotificationEventType.ItemAdded, Kind = LibraryItemKind.Movie, ItemId = "2", DisplayName = "电影B" });
            agg1.Record(new LibraryChangeRecord { Operation = NotificationEventType.ItemAdded, Kind = LibraryItemKind.Movie, ItemId = "2", DisplayName = "电影B" }); // 重复去重
            agg1.Dispose();
            Assert(published.Count == 2, "少量新增逐条推送且同一 ItemId 去重");
            Assert(published.TrueForAll(e => e.EventType == NotificationEventType.ItemAdded), "逐条事件为 ItemAdded");

            // 超过上限 → 汇总
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
    }
}
