using System;
using System.Collections.Concurrent;
using System.Linq;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 有界、带过期的去重缓存。用于抑制短时间内重复的事件。
    /// </summary>
    public class DeduplicationCache
    {
        private readonly ConcurrentDictionary<string, DateTime> _entries = new ConcurrentDictionary<string, DateTime>();
        private readonly int _maxEntries;

        public DeduplicationCache(int maxEntries = 2000)
        {
            _maxEntries = maxEntries;
        }

        /// <summary>
        /// 尝试标记一个键。若在窗口内已存在则返回 false（判定为重复），否则记录并返回 true。
        /// </summary>
        public bool TryMark(string key, int windowSeconds)
        {
            if (string.IsNullOrEmpty(key) || windowSeconds <= 0)
                return true; // 不去重

            var now = DateTime.UtcNow;
            var expiry = now.AddSeconds(windowSeconds);

            while (true)
            {
                if (_entries.TryGetValue(key, out var existing))
                {
                    if (existing > now)
                        return false; // 未过期，重复
                    // 已过期，尝试续期
                    if (_entries.TryUpdate(key, expiry, existing))
                    {
                        EnforceBound(now);
                        return true;
                    }
                    // 竞争失败，重试
                    continue;
                }

                if (_entries.TryAdd(key, expiry))
                {
                    EnforceBound(now);
                    return true;
                }
                // 竞争失败，重试
            }
        }

        private void EnforceBound(DateTime now)
        {
            if (_entries.Count <= _maxEntries)
                return;

            // 先清过期
            foreach (var kvp in _entries.Where(kvp => kvp.Value <= now).ToList())
            {
                _entries.TryRemove(kvp.Key, out _);
            }

            // 仍超限则移除最早过期的一批
            if (_entries.Count > _maxEntries)
            {
                var overflow = _entries.Count - _maxEntries;
                foreach (var kvp in _entries.OrderBy(k => k.Value).Take(overflow).ToList())
                {
                    _entries.TryRemove(kvp.Key, out _);
                }
            }
        }

        /// <summary>清理已过期条目</summary>
        public void CleanupStale()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _entries.Where(kvp => kvp.Value <= now).ToList())
            {
                _entries.TryRemove(kvp.Key, out _);
            }
        }

        public int Count => _entries.Count;
    }
}
