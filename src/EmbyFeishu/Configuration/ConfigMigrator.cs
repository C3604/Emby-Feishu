namespace EmbyFeishu.Configuration
{
    /// <summary>
    /// 配置兼容层。旧配置文件反序列化后，缺失的新字段由属性初始值提供安全默认，
    /// 这里只做跨版本的一次性规范化与推导，绝不改动 GUID、Webhook 或已有开关的用户取值。
    /// </summary>
    public static class ConfigMigrator
    {
        /// <summary>当前配置架构版本</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>
        /// 对配置对象应用迁移。幂等，可重复调用。
        /// </summary>
        public static void Apply(PluginOptions options)
        {
            if (options == null)
                return;

            // 版本号提升（未来若需要按版本分支迁移，可在此判断 options.ConfigSchemaVersion）
            if (options.ConfigSchemaVersion < CurrentSchemaVersion)
            {
                // v0 -> v1：无破坏性字段重命名；新字段全部由初始值兜底。
                // MessageDetailLevel 默认 Custom，播放事件沿用旧的 Include* 开关，
                // 因此旧用户升级后播放通知外观保持不变。
                options.ConfigSchemaVersion = CurrentSchemaVersion;
            }

            ClampRanges(options);
        }

        /// <summary>
        /// 将数值配置夹到合法范围，避免旧配置或异常输入越界。
        /// </summary>
        private static void ClampRanges(PluginOptions options)
        {
            options.RequestTimeoutSeconds = Clamp(options.RequestTimeoutSeconds, 3, 60);
            options.MinimumStopSeconds = Clamp(options.MinimumStopSeconds, 0, 600);
            options.CompletionThresholdPercent = Clamp(options.CompletionThresholdPercent, 50, 100);
            options.LibraryAggregationWindowSeconds = Clamp(options.LibraryAggregationWindowSeconds, 10, 600);
            options.MaximumIndividualLibraryMessages = Clamp(options.MaximumIndividualLibraryMessages, 0, 50);
            options.MaximumNotificationsPerMinute = Clamp(options.MaximumNotificationsPerMinute, 1, 240);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
