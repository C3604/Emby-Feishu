using System;
using System.Collections.Generic;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 每分钟滑动窗口限流器，线程安全。
    /// </summary>
    public class SlidingWindowRateLimiter
    {
        private readonly object _lock = new object();
        private readonly Queue<DateTime> _timestamps = new Queue<DateTime>();
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 判断在给定每分钟上限下是否允许通过；允许则记录本次时间。
        /// </summary>
        public bool Allow(int maxPerMinute)
        {
            if (maxPerMinute <= 0)
                maxPerMinute = 1;

            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var cutoff = now - Window;
                while (_timestamps.Count > 0 && _timestamps.Peek() < cutoff)
                {
                    _timestamps.Dequeue();
                }

                if (_timestamps.Count >= maxPerMinute)
                    return false;

                _timestamps.Enqueue(now);
                return true;
            }
        }

        public int CurrentCount
        {
            get
            {
                lock (_lock)
                {
                    var cutoff = DateTime.UtcNow - Window;
                    while (_timestamps.Count > 0 && _timestamps.Peek() < cutoff)
                        _timestamps.Dequeue();
                    return _timestamps.Count;
                }
            }
        }
    }
}
