using System.ComponentModel;
using System.IO;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using EmbyFeishu.Configuration.Groups;
using EmbyFeishu.Models;
using MediaBrowser.Model.Attributes;
using MediaBrowser.Model.GenericEdit;
using MediaBrowser.Model.Serialization;

namespace EmbyFeishu
{
    /// <summary>
    /// 插件配置选项，由 Emby Simple UI 自动生成设置页面。
    /// 根页面组织为八个分组，每组继承 EditableOptionsBase。
    /// 所有旧扁平字段保留（标记 Browsable(false)），保证旧配置文件向后兼容。
    /// </summary>
    public class PluginOptions : EditableOptionsBase
    {
        public override string EditorTitle => "Emby 飞书通知设置";

        public override string EditorDescription
            => "配置飞书 Webhook 地址与各类事件通知。保存后立即生效，无需重启。";

        /// <summary>配置架构版本，用于平滑迁移。0/缺失=旧版，1=v1.x，2=分组版。</summary>
        [Browsable(false)]
        public int ConfigSchemaVersion { get; set; } = 0;

        // ===== 分组配置对象（UI 中通过 EditorGroup 展示） =====

        public FeishuConnectionGroup FeishuConnection { get; set; } = new FeishuConnectionGroup();
        public BotSecurityGroup BotSecurity { get; set; } = new BotSecurityGroup();
        public MessageDisplayGroup MessageDisplay { get; set; } = new MessageDisplayGroup();
        public PlaybackNotificationGroup PlaybackNotification { get; set; } = new PlaybackNotificationGroup();
        public LoginAndUserGroup LoginAndUser { get; set; } = new LoginAndUserGroup();
        public LibraryAndUserBehaviorGroup LibraryAndUserBehavior { get; set; } = new LibraryAndUserBehaviorGroup();
        public TaskAndLiveTvAndServerGroup TaskAndLiveTvAndServer { get; set; } = new TaskAndLiveTvAndServerGroup();
        public AdvancedAndDiagnosticsGroup AdvancedAndDiagnostics { get; set; } = new AdvancedAndDiagnosticsGroup();

        // ===== 旧扁平字段（保留用于反序列化兼容，不在 UI 中显示） =====
        // 每个旧字段上标记 Browsable(false)，使其不显示在配置 UI 中。
        // 序列化时这些字段仍会被 JSON 序列化器读写，保证旧配置兼容。

        [Browsable(false)] public bool Enabled { get; set; } = false;
        [Browsable(false)] public string WebhookUrl { get; set; } = "";
        [Browsable(false)] public int RequestTimeoutSeconds { get; set; } = 10;

        [Browsable(false)] public MessageFormat MessageFormat { get; set; } = MessageFormat.Text;
        [Browsable(false)] public MessageDetailLevel MessageDetailLevel { get; set; } = MessageDetailLevel.Custom;
        [Browsable(false)] public bool FallbackToTextOnCardFailure { get; set; } = true;
        [Browsable(false)] public bool ShowEventTime { get; set; } = true;
        [Browsable(false)] public bool ShowServerName { get; set; } = true;
        [Browsable(false)] public bool ShowSensitiveTechnicalDetails { get; set; } = false;
        [Browsable(false)] public IpAddressDisplayMode IpAddressDisplayMode { get; set; } = IpAddressDisplayMode.Masked;
        [Browsable(false)] public DeviceIdDisplayMode DeviceIdDisplayMode { get; set; } = DeviceIdDisplayMode.Masked;

        [Browsable(false)] public bool NotifyPlaybackStarted { get; set; } = true;
        [Browsable(false)] public bool NotifyPlaybackStopped { get; set; } = true;
        [Browsable(false)] public bool NotifyPlaybackPaused { get; set; } = false;
        [Browsable(false)] public bool NotifyPlaybackResumed { get; set; } = false;
        [Browsable(false)] public bool NotifyPlaybackCompleted { get; set; } = true;
        [Browsable(false)] public bool NotifyPlaybackAbandoned { get; set; } = false;
        [Browsable(false)] public bool NotifyPlaybackMethodChanged { get; set; } = false;
        [Browsable(false)] public bool NotifyPlaybackMilestones { get; set; } = false;
        [Browsable(false)] public string PlaybackMilestones { get; set; } = "25,50,75";
        [Browsable(false)] public int MinimumStopSeconds { get; set; } = 5;
        [Browsable(false)] public int CompletionThresholdPercent { get; set; } = 90;
        [Browsable(false)] public bool OnlyVideo { get; set; } = true;

        [Browsable(false)] public bool NotifyAuthenticationSucceeded { get; set; } = false;
        [Browsable(false)] public bool NotifyAuthenticationFailed { get; set; } = true;
        [Browsable(false)] public bool NotifySessionStarted { get; set; } = false;
        [Browsable(false)] public bool NotifySessionEnded { get; set; } = false;
        [Browsable(false)] public bool NotifyRemoteControlDisconnected { get; set; } = false;
        [Browsable(false)] public bool NotifyPartyJoined { get; set; } = false;
        [Browsable(false)] public bool NotifyPartyLeft { get; set; } = false;

        [Browsable(false)] public bool NotifyUserLockedOut { get; set; } = true;
        [Browsable(false)] public bool NotifyUserPasswordChanged { get; set; } = true;
        [Browsable(false)] public bool NotifyUserCreated { get; set; } = false;
        [Browsable(false)] public bool NotifyUserDeleted { get; set; } = false;
        [Browsable(false)] public bool NotifyUserUpdated { get; set; } = false;
        [Browsable(false)] public bool NotifyUserPolicyUpdated { get; set; } = false;
        [Browsable(false)] public bool NotifyUserConfigurationUpdated { get; set; } = false;

        [Browsable(false)] public bool NotifyNewMovies { get; set; } = false;
        [Browsable(false)] public bool NotifyNewEpisodes { get; set; } = false;
        [Browsable(false)] public bool NotifyNewMusic { get; set; } = false;
        [Browsable(false)] public bool NotifyOtherNewItems { get; set; } = false;
        [Browsable(false)] public bool NotifyItemsRemoved { get; set; } = false;
        [Browsable(false)] public bool NotifyItemsUpdated { get; set; } = false;
        [Browsable(false)] public bool EnableLibraryAggregation { get; set; } = true;
        [Browsable(false)] public int LibraryAggregationWindowSeconds { get; set; } = 60;
        [Browsable(false)] public int MaximumIndividualLibraryMessages { get; set; } = 5;

        [Browsable(false)] public bool NotifyFavoriteAdded { get; set; } = false;
        [Browsable(false)] public bool NotifyFavoriteRemoved { get; set; } = false;
        [Browsable(false)] public bool NotifyMarkedPlayed { get; set; } = false;
        [Browsable(false)] public bool NotifyMarkedUnplayed { get; set; } = false;
        [Browsable(false)] public bool NotifyUserRatingChanged { get; set; } = false;

        [Browsable(false)] public bool NotifyTaskFailed { get; set; } = true;
        [Browsable(false)] public bool NotifyTaskCompleted { get; set; } = false;
        [Browsable(false)] public bool NotifyTaskCancelled { get; set; } = false;
        [Browsable(false)] public bool NotifyLibraryScanStarted { get; set; } = false;
        [Browsable(false)] public bool NotifyLibraryScanCompleted { get; set; } = true;
        [Browsable(false)] public bool NotifyMetadataRefreshCompleted { get; set; } = false;
        [Browsable(false)] public bool NotifyBackupCompleted { get; set; } = false;

        [Browsable(false)] public bool EnableLiveTvNotifications { get; set; } = false;
        [Browsable(false)] public bool NotifyRecordingStarted { get; set; } = false;
        [Browsable(false)] public bool NotifyRecordingEnded { get; set; } = false;
        [Browsable(false)] public bool NotifyTimerCreated { get; set; } = false;
        [Browsable(false)] public bool NotifyTimerUpdated { get; set; } = false;
        [Browsable(false)] public bool NotifyTimerCancelled { get; set; } = false;
        [Browsable(false)] public bool NotifySeriesTimerCreated { get; set; } = false;
        [Browsable(false)] public bool NotifySeriesTimerUpdated { get; set; } = false;
        [Browsable(false)] public bool NotifySeriesTimerCancelled { get; set; } = false;

        [Browsable(false)] public bool NotifyServerStarted { get; set; } = true;
        [Browsable(false)] public bool NotifyServerStopping { get; set; } = false;
        [Browsable(false)] public bool NotifyUpdateAvailable { get; set; } = true;
        [Browsable(false)] public bool NotifyApplicationUpdated { get; set; } = true;
        [Browsable(false)] public bool NotifyRestartRequired { get; set; } = true;
        [Browsable(false)] public bool NotifyMaintenanceModeEntered { get; set; } = false;
        [Browsable(false)] public bool NotifyMaintenanceModeExited { get; set; } = false;

        [Browsable(false)] public UserFilterMode UserFilterMode { get; set; } = UserFilterMode.All;
        [Browsable(false)] public string UserNames { get; set; } = "";

        [Browsable(false)] public int MaximumNotificationsPerMinute { get; set; } = 30;
        [Browsable(false)] public bool SecurityEventsBypassRateLimit { get; set; } = true;
        [Browsable(false)] public bool AggregateWhenRateLimited { get; set; } = true;

        [Browsable(false)] public bool IncludeUserName { get; set; } = true;
        [Browsable(false)] public bool IncludeMediaTitle { get; set; } = true;
        [Browsable(false)] public bool IncludeMediaType { get; set; } = false;
        [Browsable(false)] public bool IncludeSeriesEpisode { get; set; } = true;
        [Browsable(false)] public bool IncludeClientName { get; set; } = true;
        [Browsable(false)] public bool IncludeDeviceName { get; set; } = true;
        [Browsable(false)] public bool IncludePlaybackPosition { get; set; } = false;
        [Browsable(false)] public bool IncludePlayedToCompletion { get; set; } = true;

        [Browsable(false)] public int MaxRetryCount { get; set; } = 1;
        [Browsable(false)] public NotificationPreset ApplyPreset { get; set; } = NotificationPreset.None;
        [Browsable(false)] public string DiagnosticInfo { get; set; } = "插件尚未启动";
        [Browsable(false)] public string WebhookHealthStatus { get; set; } = "未知";

        [Browsable(false)] public bool SendTestNotification { get; set; } = false;
        [Browsable(false)] public string LastTestResult { get; set; } = "";

        // ===== 新增安全字段（仅在根层级存储，不在分组 UI 中显示） =====

        /// <summary>是否启用自定义关键词（旧字段兼容）</summary>
        [Browsable(false)] public bool EnableCustomKeyword { get; set; } = false;

        /// <summary>自定义关键词（旧字段兼容）</summary>
        [Browsable(false)] public string CustomKeyword { get; set; } = "";

        /// <summary>是否启用签名校验（旧字段兼容）</summary>
        [Browsable(false)] public bool EnableSignatureVerification { get; set; } = false;

        /// <summary>签名密钥（存储真实值，不在 UI 中直接暴露，旧字段兼容）</summary>
        [Browsable(false)] public string SignatureSecret { get; set; } = "";

        // ===== UI 输入临时字段（不在序列化中保存到持久配置） =====

        /// <summary>
        /// UI 输入的签名密钥临时字段。不为空时表示用户输入了新密钥。
        /// 此字段仅用于 GenericEdit 界面交互，不应被持久化为第二份密钥。
        /// </summary>
        [Browsable(false)]
        public string SignatureSecretInput { get; set; } = "";

        /// <summary>签名密钥状态</summary>
        [Browsable(false)]
        public string SignatureSecretStatus { get; set; } = "未配置";

        /// <summary>
        /// 配置校验，由 Emby Simple UI 框架在保存时调用。
        /// </summary>
        protected override void Validate(ValidationContext context)
        {
            base.Validate(context);

            // 初始化分组对象
            EnsureGroups();

            // 第一步：将 UI 输入从分组对象同步到旧扁平字段（捕获用户输入）
            SyncFromGroups();

            // 第二步：配置迁移（仅在版本号过低时执行一次性迁移）
            Configuration.ConfigMigrator.Apply(this);

            // 不再调用 SyncToGroups()——那会用旧扁平字段覆盖用户刚输入的分组值

            // 校验 Webhook 格式（仅提示，不阻断）
            if (!string.IsNullOrWhiteSpace(FeishuConnection.WebhookUrl))
            {
                var urlErrors = Configuration.ConfigValidator.ValidateWebhookUrl(FeishuConnection.WebhookUrl);
                foreach (var msg in urlErrors)
                {
                    context.AddValidationError(msg);
                }
            }

            // 关键词校验
            if (BotSecurity.EnableCustomKeyword)
            {
                var kw = (BotSecurity.CustomKeyword ?? "").Trim();
                BotSecurity.CustomKeyword = kw;

                if (string.IsNullOrEmpty(kw))
                {
                    context.AddValidationError("启用自定义关键词时，关键词不能为空。");
                }
                else if (kw.Contains("\n") || kw.Contains("\r") || kw.Contains("\t"))
                {
                    context.AddValidationError("自定义关键词不能包含换行、回车或制表符。");
                }
                else
                {
                    // 检查其他控制字符
                    bool hasControl = false;
                    foreach (var c in kw)
                    {
                        if (char.IsControl(c))
                        {
                            hasControl = true;
                            break;
                        }
                    }
                    if (hasControl)
                    {
                        context.AddValidationError("自定义关键词不能包含控制字符。");
                    }
                }
            }

            // 签名校验
            if (BotSecurity.EnableSignatureVerification)
            {
                // 输入了新密钥：替换已有密钥
                var newInput = BotSecurity.SignatureSecretInput?.Trim() ?? "";
                if (!string.IsNullOrEmpty(newInput))
                {
                    SignatureSecret = newInput;
                    BotSecurity.SignatureSecretInput = "";
                    SignatureSecretStatus = "已配置";
                }
                else
                {
                    // 空输入表示保持原密钥。若原本就没有密钥，报错。
                    if (string.IsNullOrEmpty(SignatureSecret))
                    {
                        context.AddValidationError("启用签名校验时，必须填写签名密钥。");
                    }
                }

                // 密钥格式校验
                if (!string.IsNullOrEmpty(SignatureSecret)
                    && (SignatureSecret.Contains("\n") || SignatureSecret.Contains("\r") || SignatureSecret.Contains("\t")))
                {
                    context.AddValidationError("签名密钥不能包含换行、回车或制表符。");
                }
            }
            else
            {
                // 关闭签名时：若用户输入了新密钥则保存（供未来开启时使用）
                var newInput = BotSecurity.SignatureSecretInput?.Trim() ?? "";
                if (!string.IsNullOrEmpty(newInput))
                {
                    SignatureSecret = newInput;
                    SignatureSecretStatus = "已配置";
                }
                BotSecurity.SignatureSecretInput = "";
            }

            // 更新签名状态
            if (string.IsNullOrEmpty(SignatureSecret))
            {
                SignatureSecretStatus = "未配置";
            }
            else
            {
                SignatureSecretStatus = "已配置";
            }
            BotSecurity.SignatureSecretStatus = SignatureSecretStatus;

            // 一般校验
            var configErrors = Configuration.ConfigValidator.Validate(this);
            foreach (var msg in configErrors)
            {
                context.AddValidationError(msg);
            }

            // 用户名规范化
            LoginAndUser.UserNames = Configuration.ConfigValidator.NormalizeUserNames(LoginAndUser.UserNames);

            if (!context.HasErrors
                && !string.IsNullOrWhiteSpace(FeishuConnection.WebhookUrl)
                && !Configuration.ConfigValidator.IsLikelyFeishuDomain(FeishuConnection.WebhookUrl))
            {
                Plugin.Instance?.LogWarning(
                    "Webhook 域名不是飞书(feishu.cn)或 Lark(larksuite.com)，若为自定义中转地址可忽略此提示。");
            }
        }

        /// <summary>
        /// 初始化所有分组对象，防止 null 引用。
        /// </summary>
        public void EnsureGroups()
        {
            if (FeishuConnection == null) FeishuConnection = new FeishuConnectionGroup();
            if (BotSecurity == null) BotSecurity = new BotSecurityGroup();
            if (MessageDisplay == null) MessageDisplay = new MessageDisplayGroup();
            if (PlaybackNotification == null) PlaybackNotification = new PlaybackNotificationGroup();
            if (LoginAndUser == null) LoginAndUser = new LoginAndUserGroup();
            if (LibraryAndUserBehavior == null) LibraryAndUserBehavior = new LibraryAndUserBehaviorGroup();
            if (TaskAndLiveTvAndServer == null) TaskAndLiveTvAndServer = new TaskAndLiveTvAndServerGroup();
            if (AdvancedAndDiagnostics == null) AdvancedAndDiagnostics = new AdvancedAndDiagnosticsGroup();
        }

        /// <summary>
        /// 将旧扁平字段的值同步到分组对象。
        /// </summary>
        public void SyncToGroups()
        {
            Configuration.ConfigSynchronizer.CopyToGroups(this);
        }

        /// <summary>
        /// 从分组对象回写旧扁平字段，支持降级到旧版本插件。
        /// </summary>
        public void SyncFromGroups()
        {
            Configuration.ConfigSynchronizer.CopyFromGroups(this);
        }

        /// <summary>
        /// JSON 反序列化后立即调用。扁平字段被 JSON 赋值后，
        /// 若 ConfigSchemaVersion 已是最新则只需确保分组对象也存在即可；
        /// 若版本过旧则由 Apply 执行一次性迁移。
        /// 注意：不调用 SyncToGroups()，因为保存时会把分组对象的值回写到扁平字段，
        /// 下次加载时扁平字段已有正确的值。
        /// 关键原则：首次反序列化后，若版本号已是最新（说明之前已保存过），
        /// 则扁平字段和分组字段应该一致（上次保存时 SyncFromGroups 保证了这一点）。
        /// 若仍为空分组，则从扁平字段填充（覆盖空值场景）。
        /// </summary>
        public new MediaBrowser.Model.GenericEdit.IEditableObject DeserializeFromJsonString(string jsonString, IJsonSerializer serializer)
        {
            var result = (MediaBrowser.Model.GenericEdit.IEditableObject)base.DeserializeFromJsonString(jsonString, serializer);
            if (result is PluginOptions options)
            {
                options.EnsureGroups();
                Configuration.ConfigMigrator.Apply(options);
                // 加载后必须确保分组对象反映扁平字段的值（当分组字段为空时从扁平填充）
                options.EnsureGroupsHaveData();
            }
            return result;
        }

        /// <summary>
        /// JSON 流反序列化后立即调用。
        /// </summary>
        public new MediaBrowser.Model.GenericEdit.IEditableObject DeserializeFromJsonStream(Stream jsonStream, IJsonSerializer serializer)
        {
            var result = (MediaBrowser.Model.GenericEdit.IEditableObject)base.DeserializeFromJsonStream(jsonStream, serializer);
            if (result is PluginOptions options)
            {
                options.EnsureGroups();
                Configuration.ConfigMigrator.Apply(options);
                options.EnsureGroupsHaveData();
            }
            return result;
        }

        /// <summary>
        /// 当分组对象的 WebhookUrl 为空但扁平字段有值时，从扁平字段填充分组。
        /// 这覆盖了第一次加载已迁移过的配置文件时分组对象为全新实例但扁平字段有值的场景。
        /// </summary>
        public void EnsureGroupsHaveData()
        {
            if (FeishuConnection != null && string.IsNullOrEmpty(FeishuConnection.WebhookUrl) && !string.IsNullOrEmpty(WebhookUrl))
            {
                SyncToGroups();
            }
        }
    }
}
