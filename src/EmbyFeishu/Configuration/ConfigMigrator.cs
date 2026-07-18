namespace EmbyFeishu.Configuration
{
    /// <summary>
    /// 配置兼容层。v2 引入分组配置，旧扁平字段在反序列化后自动迁移到分组对象。
    /// 绝不改动 GUID、Webhook 或已有开关的用户取值。
    /// </summary>
    public static class ConfigMigrator
    {
        /// <summary>当前配置架构版本</summary>
        public const int CurrentSchemaVersion = 2;

        /// <summary>
        /// 对配置对象应用迁移。幂等，可重复调用。
        /// 只在 ConfigSchemaVersion 低于当前版本时执行一次性迁移。
        /// 不再无条件调用 SyncToGroups/SyncFromGroups——由调用方决定同步方向。
        /// 也不重置 LastTestResult——那是用户可见的诊断信息。
        /// </summary>
        public static void Apply(PluginOptions options)
        {
            if (options == null)
                return;

            options.EnsureGroups();

            // v0/v1 → v2：将旧扁平字段迁移到新分组对象（仅执行一次）
            if (options.ConfigSchemaVersion < CurrentSchemaVersion)
            {
                MigrateToGroups(options);
                // 迁移后同步：先确保分组对象有值，再回写扁平字段
                options.SyncFromGroups();
                options.ConfigSchemaVersion = CurrentSchemaVersion;
            }

            ClampRanges(options);
        }

        private static void MigrateToGroups(PluginOptions options)
        {
            ConfigSynchronizer.CopyToGroups(options);
        }

        /// <summary>
        /// 将数值配置夹到合法范围。
        /// </summary>
        private static void ClampRanges(PluginOptions options)
        {
            options.FeishuConnection.RequestTimeoutSeconds = Clamp(options.FeishuConnection.RequestTimeoutSeconds, 3, 60);
            options.PlaybackNotification.MinimumStopSeconds = Clamp(options.PlaybackNotification.MinimumStopSeconds, 0, 600);
            options.PlaybackNotification.CompletionThresholdPercent = Clamp(options.PlaybackNotification.CompletionThresholdPercent, 50, 100);
            options.LibraryAndUserBehavior.LibraryAggregationWindowSeconds = Clamp(options.LibraryAndUserBehavior.LibraryAggregationWindowSeconds, 10, 600);
            options.LibraryAndUserBehavior.MaximumIndividualLibraryMessages = Clamp(options.LibraryAndUserBehavior.MaximumIndividualLibraryMessages, 0, 50);
            options.AdvancedAndDiagnostics.MaximumNotificationsPerMinute = Clamp(options.AdvancedAndDiagnostics.MaximumNotificationsPerMinute, 1, 240);
            options.AdvancedAndDiagnostics.MaxRetryCount = Clamp(options.AdvancedAndDiagnostics.MaxRetryCount, 0, 3);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
