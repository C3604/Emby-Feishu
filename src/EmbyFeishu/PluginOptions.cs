using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using EmbyFeishu.Models;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu
{
    /// <summary>
    /// 插件配置选项，由 Emby Simple UI 自动生成设置页面。
    /// 分组顺序：飞书连接 → 消息格式 → 播放 → 登录与安全 → 媒体库 → 用户行为 →
    /// 用户管理 → 计划任务 → Live TV → 服务器 → 高级过滤与限流 → 测试与诊断。
    /// 所有旧字段名称与默认值保持不变，新字段缺失时使用安全默认值，保证旧配置继续加载。
    /// </summary>
    public class PluginOptions : EditableOptionsBase
    {
        public override string EditorTitle => "Emby 飞书通知设置";

        public override string EditorDescription => "配置飞书 Webhook 地址与各类事件通知。保存后立即生效，无需重启。";

        /// <summary>配置架构版本，用于将来平滑迁移。0/缺失表示旧版本或全新安装。</summary>
        [Browsable(false)]
        public int ConfigSchemaVersion { get; set; } = 0;

        // ========== 一、飞书连接 ==========

        [DisplayName("① 启用插件")]
        [Description("插件总开关，关闭后不会发送任何通知")]
        public bool Enabled { get; set; } = false;

        [DisplayName("① 飞书 Webhook 地址")]
        [Description("飞书群机器人的 Webhook 地址，以 https:// 开头")]
        public string WebhookUrl { get; set; } = "";

        [DisplayName("① 请求超时（秒）")]
        [Description("飞书 Webhook 请求超时时间，范围 3～60")]
        [IsAdvanced]
        [MinValue(3)]
        [MaxValue(60)]
        public int RequestTimeoutSeconds { get; set; } = 10;

        // ========== 二、消息格式 ==========

        [DisplayName("② 消息格式")]
        [Description("Text=纯文本（默认，与旧版一致）；FeishuCard=飞书交互卡片")]
        public MessageFormat MessageFormat { get; set; } = MessageFormat.Text;

        [DisplayName("② 详细程度")]
        [Description("Simple=极简；Standard=标准；Detailed=含技术细节；Custom=自定义（播放事件沿用下方字段开关）。默认 Custom 以保持旧版外观")]
        public MessageDetailLevel MessageDetailLevel { get; set; } = MessageDetailLevel.Custom;

        [DisplayName("② 卡片失败时回退文本")]
        [Description("卡片发送失败时尝试改用文本发送一次")]
        [IsAdvanced]
        public bool FallbackToTextOnCardFailure { get; set; } = true;

        [DisplayName("② 显示事件时间")]
        [IsAdvanced]
        public bool ShowEventTime { get; set; } = true;

        [DisplayName("② 显示服务器名称")]
        [IsAdvanced]
        public bool ShowServerName { get; set; } = true;

        [DisplayName("② 显示敏感技术细节")]
        [Description("开启后在 Detailed 模式展示编码、分辨率等技术字段")]
        [IsAdvanced]
        public bool ShowSensitiveTechnicalDetails { get; set; } = false;

        [DisplayName("② IP 显示方式")]
        [Description("Hidden=隐藏；Masked=脱敏（默认）；Full=完整")]
        [IsAdvanced]
        public IpAddressDisplayMode IpAddressDisplayMode { get; set; } = IpAddressDisplayMode.Masked;

        [DisplayName("② 设备 ID 显示方式")]
        [Description("Hidden=隐藏；Masked=脱敏（默认）；Full=完整")]
        [IsAdvanced]
        public DeviceIdDisplayMode DeviceIdDisplayMode { get; set; } = DeviceIdDisplayMode.Masked;

        // ========== 三、播放通知 ==========

        [DisplayName("③ 通知播放开始")]
        public bool NotifyPlaybackStarted { get; set; } = true;

        [DisplayName("③ 通知播放停止")]
        public bool NotifyPlaybackStopped { get; set; } = true;

        [DisplayName("③ 通知播放暂停")]
        [IsAdvanced]
        public bool NotifyPlaybackPaused { get; set; } = false;

        [DisplayName("③ 通知播放恢复")]
        [IsAdvanced]
        public bool NotifyPlaybackResumed { get; set; } = false;

        [DisplayName("③ 通知播放完成")]
        [Description("播放到片尾（PlayedToCompletion）时单独推送“播放完成”")]
        public bool NotifyPlaybackCompleted { get; set; } = true;

        [DisplayName("③ 通知中途放弃")]
        [Description("未播放完成即停止时推送“放弃播放”")]
        [IsAdvanced]
        public bool NotifyPlaybackAbandoned { get; set; } = false;

        [DisplayName("③ 通知播放方式变化")]
        [Description("直放/直接串流/转码状态发生变化时通知")]
        [IsAdvanced]
        public bool NotifyPlaybackMethodChanged { get; set; } = false;

        [DisplayName("③ 通知播放进度里程碑")]
        [IsAdvanced]
        public bool NotifyPlaybackMilestones { get; set; } = false;

        [DisplayName("③ 里程碑阈值（%）")]
        [Description("逗号分隔的百分比，如 25,50,75")]
        [IsAdvanced]
        public string PlaybackMilestones { get; set; } = "25,50,75";

        [DisplayName("③ 最短播放秒数")]
        [Description("播放时长不足此秒数时不发送停止/放弃通知，范围 0～600")]
        [IsAdvanced]
        [MinValue(0)]
        [MaxValue(600)]
        public int MinimumStopSeconds { get; set; } = 5;

        [DisplayName("③ 播放完成阈值（%）")]
        [Description("进度达到此百分比视为播放完成，范围 50～100")]
        [IsAdvanced]
        [MinValue(50)]
        [MaxValue(100)]
        public int CompletionThresholdPercent { get; set; } = 90;

        [DisplayName("③ 仅通知视频播放")]
        [Description("开启后只有视频类型的播放会触发通知，忽略音频等")]
        public bool OnlyVideo { get; set; } = true;

        // ========== 四、登录与安全 ==========

        [DisplayName("④ 通知登录成功")]
        [IsAdvanced]
        public bool NotifyAuthenticationSucceeded { get; set; } = false;

        [DisplayName("④ 通知登录失败")]
        public bool NotifyAuthenticationFailed { get; set; } = true;

        [DisplayName("④ 通知会话开始")]
        [IsAdvanced]
        public bool NotifySessionStarted { get; set; } = false;

        [DisplayName("④ 通知会话结束")]
        [IsAdvanced]
        public bool NotifySessionEnded { get; set; } = false;

        [DisplayName("④ 通知远程控制断开")]
        [IsAdvanced]
        public bool NotifyRemoteControlDisconnected { get; set; } = false;

        [DisplayName("④ 通知加入同步播放")]
        [IsAdvanced]
        public bool NotifyPartyJoined { get; set; } = false;

        [DisplayName("④ 通知离开同步播放")]
        [IsAdvanced]
        public bool NotifyPartyLeft { get; set; } = false;

        // ========== 五、用户管理 ==========

        [DisplayName("⑤ 通知用户被锁定")]
        public bool NotifyUserLockedOut { get; set; } = true;

        [DisplayName("⑤ 通知修改密码")]
        public bool NotifyUserPasswordChanged { get; set; } = true;

        [DisplayName("⑤ 通知创建用户")]
        [IsAdvanced]
        public bool NotifyUserCreated { get; set; } = false;

        [DisplayName("⑤ 通知删除用户")]
        [IsAdvanced]
        public bool NotifyUserDeleted { get; set; } = false;

        [DisplayName("⑤ 通知更新用户")]
        [IsAdvanced]
        public bool NotifyUserUpdated { get; set; } = false;

        [DisplayName("⑤ 通知更新用户策略")]
        [IsAdvanced]
        public bool NotifyUserPolicyUpdated { get; set; } = false;

        [DisplayName("⑤ 通知更新用户配置")]
        [IsAdvanced]
        public bool NotifyUserConfigurationUpdated { get; set; } = false;

        // ========== 六、媒体库 ==========

        [DisplayName("⑥ 通知新增电影")]
        [IsAdvanced]
        public bool NotifyNewMovies { get; set; } = false;

        [DisplayName("⑥ 通知新增剧集")]
        [IsAdvanced]
        public bool NotifyNewEpisodes { get; set; } = false;

        [DisplayName("⑥ 通知新增音乐")]
        [IsAdvanced]
        public bool NotifyNewMusic { get; set; } = false;

        [DisplayName("⑥ 通知其他新增项目")]
        [IsAdvanced]
        public bool NotifyOtherNewItems { get; set; } = false;

        [DisplayName("⑥ 通知项目删除")]
        [IsAdvanced]
        public bool NotifyItemsRemoved { get; set; } = false;

        [DisplayName("⑥ 通知项目更新")]
        [IsAdvanced]
        public bool NotifyItemsUpdated { get; set; } = false;

        [DisplayName("⑥ 启用媒体库聚合")]
        [Description("短时间内大量新增/更新合并为汇总消息，避免消息风暴")]
        [IsAdvanced]
        public bool EnableLibraryAggregation { get; set; } = true;

        [DisplayName("⑥ 聚合窗口（秒）")]
        [Description("范围 10～600")]
        [IsAdvanced]
        [MinValue(10)]
        [MaxValue(600)]
        public int LibraryAggregationWindowSeconds { get; set; } = 60;

        [DisplayName("⑥ 逐条推送上限")]
        [Description("同一聚合窗口内超过此数量则改为汇总")]
        [IsAdvanced]
        [MinValue(0)]
        [MaxValue(50)]
        public int MaximumIndividualLibraryMessages { get; set; } = 5;

        // ========== 七、用户行为（用户媒体数据）==========

        [DisplayName("⑦ 通知添加收藏")]
        [IsAdvanced]
        public bool NotifyFavoriteAdded { get; set; } = false;

        [DisplayName("⑦ 通知取消收藏")]
        [IsAdvanced]
        public bool NotifyFavoriteRemoved { get; set; } = false;

        [DisplayName("⑦ 通知标记已看")]
        [IsAdvanced]
        public bool NotifyMarkedPlayed { get; set; } = false;

        [DisplayName("⑦ 通知标记未看")]
        [IsAdvanced]
        public bool NotifyMarkedUnplayed { get; set; } = false;

        [DisplayName("⑦ 通知评分变化")]
        [IsAdvanced]
        public bool NotifyUserRatingChanged { get; set; } = false;

        // ========== 八、计划任务 ==========

        [DisplayName("⑧ 通知任务失败")]
        public bool NotifyTaskFailed { get; set; } = true;

        [DisplayName("⑧ 通知任务完成")]
        [IsAdvanced]
        public bool NotifyTaskCompleted { get; set; } = false;

        [DisplayName("⑧ 通知任务取消")]
        [IsAdvanced]
        public bool NotifyTaskCancelled { get; set; } = false;

        [DisplayName("⑧ 通知媒体库扫描开始")]
        [IsAdvanced]
        public bool NotifyLibraryScanStarted { get; set; } = false;

        [DisplayName("⑧ 通知媒体库扫描完成")]
        public bool NotifyLibraryScanCompleted { get; set; } = true;

        [DisplayName("⑧ 通知元数据刷新完成")]
        [IsAdvanced]
        public bool NotifyMetadataRefreshCompleted { get; set; } = false;

        [DisplayName("⑧ 通知备份完成")]
        [IsAdvanced]
        public bool NotifyBackupCompleted { get; set; } = false;

        // ========== 九、Live TV ==========

        [DisplayName("⑨ 启用 Live TV 通知")]
        [Description("服务器未启用 Live TV 时本组不生效")]
        [IsAdvanced]
        public bool EnableLiveTvNotifications { get; set; } = false;

        [DisplayName("⑨ 通知开始录制")]
        [IsAdvanced]
        public bool NotifyRecordingStarted { get; set; } = false;

        [DisplayName("⑨ 通知结束录制")]
        [IsAdvanced]
        public bool NotifyRecordingEnded { get; set; } = false;

        [DisplayName("⑨ 通知创建定时")]
        [IsAdvanced]
        public bool NotifyTimerCreated { get; set; } = false;

        [DisplayName("⑨ 通知更新定时")]
        [IsAdvanced]
        public bool NotifyTimerUpdated { get; set; } = false;

        [DisplayName("⑨ 通知取消定时")]
        [IsAdvanced]
        public bool NotifyTimerCancelled { get; set; } = false;

        [DisplayName("⑨ 通知创建连续定时")]
        [IsAdvanced]
        public bool NotifySeriesTimerCreated { get; set; } = false;

        [DisplayName("⑨ 通知更新连续定时")]
        [IsAdvanced]
        public bool NotifySeriesTimerUpdated { get; set; } = false;

        [DisplayName("⑨ 通知取消连续定时")]
        [IsAdvanced]
        public bool NotifySeriesTimerCancelled { get; set; } = false;

        // ========== 十、服务器状态 ==========

        [DisplayName("⑩ 通知服务器启动")]
        public bool NotifyServerStarted { get; set; } = true;

        [DisplayName("⑩ 通知服务器停止")]
        [IsAdvanced]
        public bool NotifyServerStopping { get; set; } = false;

        [DisplayName("⑩ 通知有可用更新")]
        public bool NotifyUpdateAvailable { get; set; } = true;

        [DisplayName("⑩ 通知已应用更新")]
        public bool NotifyApplicationUpdated { get; set; } = true;

        [DisplayName("⑩ 通知需要重启")]
        public bool NotifyRestartRequired { get; set; } = true;

        [DisplayName("⑩ 通知进入维护模式")]
        [IsAdvanced]
        public bool NotifyMaintenanceModeEntered { get; set; } = false;

        [DisplayName("⑩ 通知退出维护模式")]
        [IsAdvanced]
        public bool NotifyMaintenanceModeExited { get; set; } = false;

        // ========== 十一、高级过滤与限流 ==========

        [DisplayName("⑪ 用户过滤模式")]
        [Description("All=所有用户；IncludeOnly=仅通知指定用户；Exclude=排除指定用户。仅作用于播放/用户行为类事件")]
        [IsAdvanced]
        public Models.UserFilterMode UserFilterMode { get; set; } = Models.UserFilterMode.All;

        [DisplayName("⑪ 用户名列表")]
        [Description("逗号、分号或换行分隔的用户名")]
        [IsAdvanced]
        [EditMultiline(3)]
        public string UserNames { get; set; } = "";

        [DisplayName("⑪ 每分钟最大通知数")]
        [Description("超过后普通通知被限流或聚合，范围 1～240")]
        [IsAdvanced]
        [MinValue(1)]
        [MaxValue(240)]
        public int MaximumNotificationsPerMinute { get; set; } = 30;

        [DisplayName("⑪ 安全事件豁免限流")]
        [IsAdvanced]
        public bool SecurityEventsBypassRateLimit { get; set; } = true;

        [DisplayName("⑪ 限流时聚合")]
        [IsAdvanced]
        public bool AggregateWhenRateLimited { get; set; } = true;

        // ---- 自定义模式下的播放字段开关（旧字段，保持不变）----

        [DisplayName("⑪ [自定义]显示用户名")]
        [IsAdvanced]
        public bool IncludeUserName { get; set; } = true;

        [DisplayName("⑪ [自定义]显示媒体标题")]
        [IsAdvanced]
        public bool IncludeMediaTitle { get; set; } = true;

        [DisplayName("⑪ [自定义]显示媒体类型")]
        [IsAdvanced]
        public bool IncludeMediaType { get; set; } = false;

        [DisplayName("⑪ [自定义]显示剧集信息")]
        [IsAdvanced]
        public bool IncludeSeriesEpisode { get; set; } = true;

        [DisplayName("⑪ [自定义]显示客户端名称")]
        [IsAdvanced]
        public bool IncludeClientName { get; set; } = true;

        [DisplayName("⑪ [自定义]显示设备名称")]
        [IsAdvanced]
        public bool IncludeDeviceName { get; set; } = true;

        [DisplayName("⑪ [自定义]显示播放位置")]
        [IsAdvanced]
        public bool IncludePlaybackPosition { get; set; } = false;

        [DisplayName("⑪ [自定义]显示是否播放完成")]
        [IsAdvanced]
        public bool IncludePlayedToCompletion { get; set; } = true;

        // ========== 十二、测试与诊断 ==========

        [DisplayName("⑫ 发送测试通知")]
        [Description("勾选后点击保存，向飞书发送一条测试消息。发送后自动取消勾选")]
        public bool SendTestNotification { get; set; } = false;

        [DisplayName("⑫ 上次测试结果")]
        [Description("显示最近一次测试推送的结果")]
        public string LastTestResult { get; set; } = "";

        /// <summary>
        /// 配置校验，由 Emby Simple UI 框架在保存时调用。
        /// </summary>
        protected override void Validate(ValidationContext context)
        {
            base.Validate(context);

            // 一次性配置迁移与规范化
            Configuration.ConfigMigrator.Apply(this);

            if (SendTestNotification)
            {
                if (string.IsNullOrWhiteSpace(WebhookUrl))
                {
                    context.AddValidationError("发送测试通知前，请先填写飞书 Webhook 地址。");
                    SendTestNotification = false;
                    return;
                }

                var urlErrors = Configuration.ConfigValidator.ValidateWebhookUrl(WebhookUrl);
                foreach (var msg in urlErrors)
                {
                    context.AddValidationError(msg);
                }

                if (context.HasErrors)
                {
                    SendTestNotification = false;
                    return;
                }
            }

            var configErrors = Configuration.ConfigValidator.Validate(this);
            foreach (var msg in configErrors)
            {
                context.AddValidationError(msg);
            }

            UserNames = Configuration.ConfigValidator.NormalizeUserNames(UserNames);

            if (!context.HasErrors
                && !string.IsNullOrWhiteSpace(WebhookUrl)
                && !Configuration.ConfigValidator.IsLikelyFeishuDomain(WebhookUrl))
            {
                Plugin.Instance?.LogWarning(
                    "Webhook 域名不是飞书(feishu.cn)或 Lark(larksuite.com)，若为自定义中转地址可忽略此提示。");
            }
        }
    }
}
