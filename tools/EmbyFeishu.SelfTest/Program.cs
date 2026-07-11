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
            TestUserFilter();
            TestConfigValidation();
            TestNotificationFormatting();
            TestPlaybackStateTracker();
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
            if (condition)
            {
                _passed++;
                Console.WriteLine($"  ✓ {testName}");
            }
            else
            {
                _failed++;
                _failures.Add(testName);
                Console.WriteLine($"  ✗ {testName}");
            }
        }

        static void AssertEqual(string expected, string actual, string testName)
        {
            Assert(expected == actual, $"{testName} (期望=\"{expected}\", 实际=\"{actual}\")");
        }

        // ===== 媒体标题格式化 =====
        static void TestMediaTitleFormatting()
        {
            Console.WriteLine("\n【媒体标题格式化】");

            AssertEqual("星际穿越",
                MediaTitleFormatter.Format("星际穿越", null, null, null, null),
                "电影标题");

            AssertEqual("权力的游戏 S01E02 - 国王大道",
                MediaTitleFormatter.Format("国王大道", "权力的游戏", 1, 2, "国王大道"),
                "剧集标题（含季集号）");

            AssertEqual("权力的游戏 - 特别篇",
                MediaTitleFormatter.Format("特别篇", "权力的游戏", null, null, "特别篇"),
                "剧集标题（无季集号）");

            AssertEqual("未知媒体",
                MediaTitleFormatter.Format(null, null, null, null, null),
                "全部为空时降级显示");

            AssertEqual("未知媒体",
                MediaTitleFormatter.Format("", null, null, null, null),
                "空字符串降级显示");

            AssertEqual("权力的游戏 S01E02",
                MediaTitleFormatter.Format("", "权力的游戏", 1, 2, null),
                "剧集无单集名称");
        }

        // ===== 播放时间格式化 =====
        static void TestTimeFormatting()
        {
            Console.WriteLine("\n【播放时间格式化】");

            AssertEqual("01:13:25",
                TimeFormatter.FormatTicks(TimeSpan.FromSeconds(4405).Ticks),
                "超过1小时的时间");

            AssertEqual("05:30",
                TimeFormatter.FormatTicks(TimeSpan.FromSeconds(330).Ticks),
                "不足1小时的时间");

            Assert(TimeFormatter.FormatTicks(null) == null, "null ticks 返回 null");
            Assert(TimeFormatter.FormatTicks(0) == null, "0 ticks 返回 null");
            Assert(TimeFormatter.FormatTicks(-1) == null, "负数 ticks 返回 null");
        }

        // ===== Webhook 脱敏 =====
        static void TestWebhookMasking()
        {
            Console.WriteLine("\n【Webhook 脱敏】");

            var masked1 = WebhookMasker.Mask("https://open.feishu.cn/open-apis/bot/v2/hook/abcdef123456");
            Assert(!masked1.Contains("abcdef123456"), "脱敏后不包含完整 token");
            Assert(masked1.Contains("open.feishu.cn"), "脱敏后保留域名");
            Assert(masked1.Contains("3456"), "脱敏后保留末尾四位");

            AssertEqual("(空)", WebhookMasker.Mask(null), "null URL 脱敏");
            AssertEqual("(空)", WebhookMasker.Mask(""), "空字符串脱敏");
        }

        // ===== 用户过滤 =====
        static void TestUserFilter()
        {
            Console.WriteLine("\n【用户过滤】");

            Assert(UserFilter.ShouldNotify("张三", UserFilterMode.All, ""),
                "All 模式 - 任何用户通知");

            Assert(UserFilter.ShouldNotify("张三", UserFilterMode.IncludeOnly, "张三, 李四"),
                "IncludeOnly - 在列表中的用户通知");

            Assert(!UserFilter.ShouldNotify("王五", UserFilterMode.IncludeOnly, "张三, 李四"),
                "IncludeOnly - 不在列表中的用户不通知");

            Assert(!UserFilter.ShouldNotify(null, UserFilterMode.IncludeOnly, "张三"),
                "IncludeOnly - 未知用户不通知");

            Assert(!UserFilter.ShouldNotify("张三", UserFilterMode.Exclude, "张三, 李四"),
                "Exclude - 在列表中的用户不通知");

            Assert(UserFilter.ShouldNotify("王五", UserFilterMode.Exclude, "张三, 李四"),
                "Exclude - 不在列表中的用户通知");

            Assert(UserFilter.ShouldNotify(null, UserFilterMode.Exclude, "张三"),
                "Exclude - 未知用户默认通知");

            Assert(UserFilter.ShouldNotify("zhangsan", UserFilterMode.IncludeOnly, "ZhangSan"),
                "IncludeOnly - 忽略大小写");
        }

        // ===== 配置校验 =====
        static void TestConfigValidation()
        {
            Console.WriteLine("\n【配置校验】");

            var opts1 = new PluginOptions { Enabled = true, WebhookUrl = "" };
            var errs1 = ConfigValidator.Validate(opts1);
            Assert(errs1.Count > 0, "启用但 Webhook 为空时报错");

            var opts2 = new PluginOptions { Enabled = true, WebhookUrl = "http://example.com" };
            var errs2 = ConfigValidator.Validate(opts2);
            Assert(errs2.Count > 0, "非 HTTPS 地址报错");

            var opts3 = new PluginOptions { Enabled = true, WebhookUrl = "https://open.feishu.cn/open-apis/bot/v2/hook/test" };
            var errs3 = ConfigValidator.Validate(opts3);
            Assert(errs3.Count == 0, "合法飞书地址通过");

            var opts4 = new PluginOptions { Enabled = true, WebhookUrl = "https://example.com/webhook" };
            var errs4 = ConfigValidator.Validate(opts4);
            Assert(errs4.Count > 0, "非飞书域名给出警告");

            var opts5 = new PluginOptions { Enabled = false, WebhookUrl = "" };
            var errs5 = ConfigValidator.Validate(opts5);
            Assert(errs5.Count == 0, "未启用时 Webhook 可为空");

            var opts6 = new PluginOptions { Enabled = true, WebhookUrl = "https://open.feishu.cn/test", RequestTimeoutSeconds = 1 };
            var errs6 = ConfigValidator.Validate(opts6);
            Assert(errs6.Count > 0, "超时值过小报错");

            AssertEqual("张三, 李四",
                ConfigValidator.NormalizeUserNames("张三, 李四, 张三, "),
                "用户名去重和规范化");
        }

        // ===== 消息格式化 =====
        static void TestNotificationFormatting()
        {
            Console.WriteLine("\n【消息格式化】");

            var formatter = new FeishuTextNotificationFormatter();
            var options = new PluginOptions();

            var startEvt = new PlaybackNotificationEvent
            {
                EventType = PlaybackEventType.Started,
                OccurredAt = new DateTime(2026, 7, 11, 22, 30, 15),
                UserName = "张三",
                SeriesName = "示例剧集",
                SeasonNumber = 1,
                EpisodeNumber = 2,
                EpisodeName = "第二集",
                ClientName = "Emby Web",
                DeviceName = "Chrome"
            };

            var startMsg = formatter.Format(startEvt, options);
            Assert(startMsg.Contains("▶️") && startMsg.Contains("开始播放"), "开始播放消息包含标题");
            Assert(startMsg.Contains("张三"), "开始播放消息包含用户名");
            Assert(startMsg.Contains("示例剧集 S01E02 - 第二集"), "开始播放消息包含剧集信息");
            Assert(startMsg.Contains("Emby Web"), "开始播放消息包含客户端");
            Assert(startMsg.Contains("Chrome"), "开始播放消息包含设备");
            Assert(startMsg.Contains("2026-07-11 22:30:15"), "开始播放消息包含时间");

            var stopEvt = new PlaybackNotificationEvent
            {
                EventType = PlaybackEventType.Stopped,
                OccurredAt = new DateTime(2026, 7, 11, 23, 43, 40),
                UserName = "张三",
                ItemName = "示例电影",
                PlaybackPositionTicks = TimeSpan.FromSeconds(4405).Ticks,
                RuntimeTicks = TimeSpan.FromSeconds(7500).Ticks,
                PlayedToCompletion = false,
                ClientName = "Emby Theater",
                DeviceName = "Living Room PC"
            };

            var stopOptions = new PluginOptions
            {
                IncludePlaybackPosition = true,
                IncludePlayedToCompletion = true
            };
            var stopMsg = formatter.Format(stopEvt, stopOptions);
            Assert(stopMsg.Contains("⏹️") && stopMsg.Contains("停止播放"), "停止播放消息包含标题");
            Assert(stopMsg.Contains("示例电影"), "停止播放消息包含电影名");
            Assert(stopMsg.Contains("01:13:25"), "停止播放消息包含播放位置");
            Assert(stopMsg.Contains("播放完成：否"), "停止播放消息包含完成状态");

            // 测试字段缺失时省略
            var minEvt = new PlaybackNotificationEvent
            {
                EventType = PlaybackEventType.Started,
                OccurredAt = DateTime.Now
            };
            var minMsg = formatter.Format(minEvt, options);
            Assert(!minMsg.Contains("用户："), "用户名为空时省略该字段");
            Assert(minMsg.Contains("未知媒体"), "媒体标题为空时显示未知媒体");
        }

        // ===== 播放状态去重 =====
        static void TestPlaybackStateTracker()
        {
            Console.WriteLine("\n【播放状态去重】");

            var tracker = new PlaybackStateTracker();
            var key = "test-session-1";

            tracker.OnPlaybackStarted(key);
            Assert(tracker.Count == 1, "播放开始后跟踪数量为 1");

            var r1 = tracker.OnPlaybackProgress(key, false);
            Assert(r1 == null, "Playing→Playing 不产生事件");

            var r2 = tracker.OnPlaybackProgress(key, true);
            Assert(r2 == PlaybackEventType.Paused, "Playing→Paused 产生暂停事件");

            var r3 = tracker.OnPlaybackProgress(key, true);
            Assert(r3 == null, "Paused→Paused 不产生事件（去重）");

            var r4 = tracker.OnPlaybackProgress(key, false);
            Assert(r4 == PlaybackEventType.Resumed, "Paused→Playing 产生恢复事件");

            var r5 = tracker.OnPlaybackProgress(key, false);
            Assert(r5 == null, "Playing→Playing 再次不产生事件");

            tracker.OnPlaybackStopped(key);
            Assert(tracker.Count == 0, "播放停止后跟踪数量为 0");
        }

        // ===== 测试推送配置 =====
        static void TestSendTestNotificationFlag()
        {
            Console.WriteLine("\n【测试推送配置】");

            var opts1 = new PluginOptions { SendTestNotification = false };
            Assert(opts1.SendTestNotification == false, "默认不发送测试通知");

            var opts2 = new PluginOptions { SendTestNotification = true, WebhookUrl = "" };
            Assert(opts2.SendTestNotification == true, "可以设置发送测试通知标志");

            var opts3 = new PluginOptions { LastTestResult = "✅ 测试成功！" };
            Assert(opts3.LastTestResult.Contains("测试成功"), "可以记录测试结果");

            var opts4 = new PluginOptions { LastTestResult = "" };
            Assert(opts4.LastTestResult == "", "测试结果默认为空");

            // Webhook URL 校验方法
            var urlErrors1 = ConfigValidator.ValidateWebhookUrl("https://open.feishu.cn/test");
            Assert(urlErrors1.Count == 0, "ValidateWebhookUrl 合法 HTTPS 通过");

            var urlErrors2 = ConfigValidator.ValidateWebhookUrl("http://example.com");
            Assert(urlErrors2.Count > 0, "ValidateWebhookUrl 非 HTTPS 报错");

            var urlErrors3 = ConfigValidator.ValidateWebhookUrl("not-a-url");
            Assert(urlErrors3.Count > 0, "ValidateWebhookUrl 无效 URL 报错");

            var urlErrors4 = ConfigValidator.ValidateWebhookUrl("");
            Assert(urlErrors4.Count == 0, "ValidateWebhookUrl 空字符串不报错");
        }
    }
}
