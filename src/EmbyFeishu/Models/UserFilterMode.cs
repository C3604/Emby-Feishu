namespace EmbyFeishu.Models
{
    /// <summary>
    /// 用户过滤模式
    /// </summary>
    public enum UserFilterMode
    {
        /// <summary>所有用户都通知</summary>
        All = 0,
        /// <summary>仅通知指定用户</summary>
        IncludeOnly = 1,
        /// <summary>排除指定用户</summary>
        Exclude = 2
    }
}
