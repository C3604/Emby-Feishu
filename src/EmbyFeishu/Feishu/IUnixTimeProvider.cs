using System;

namespace EmbyFeishu.Feishu
{
    /// <summary>
    /// 可替换的 Unix 时间源，方便测试固定时间戳。
    /// </summary>
    public interface IUnixTimeProvider
    {
        /// <summary>返回当前 UTC Unix 时间戳（秒）</summary>
        long NowSeconds();
    }

    /// <summary>
    /// 系统时间实现：使用 DateTime.UtcNow。
    /// </summary>
    public class SystemUnixTimeProvider : IUnixTimeProvider
    {
        public long NowSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
