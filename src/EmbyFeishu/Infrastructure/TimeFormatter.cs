using System;

namespace EmbyFeishu.Infrastructure
{
    /// <summary>
    /// 时间格式化工具
    /// </summary>
    public static class TimeFormatter
    {
        /// <summary>
        /// 将 Emby Ticks 转为 HH:mm:ss 格式
        /// </summary>
        public static string FormatTicks(long? ticks)
        {
            if (ticks == null || ticks <= 0)
                return null;

            var ts = TimeSpan.FromTicks(ticks.Value);
            if (ts.TotalHours >= 1)
                return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            return string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
        }

        /// <summary>
        /// 格式化当前时间
        /// </summary>
        public static string FormatDateTime(DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
