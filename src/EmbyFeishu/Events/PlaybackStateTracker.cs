using System;
using System.Collections.Concurrent;
using System.Linq;
using EmbyFeishu.Models;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 播放状态跟踪器，用于暂停/恢复去重
    /// </summary>
    public class PlaybackStateTracker
    {
        private readonly ConcurrentDictionary<string, PlaybackSessionState> _states
            = new ConcurrentDictionary<string, PlaybackSessionState>();

        /// <summary>过期清理阈值（小时）</summary>
        private const int StaleHours = 24;

        /// <summary>
        /// 获取会话键
        /// </summary>
        public static string GetSessionKey(string playSessionId, string sessionId, string itemId, string deviceId)
        {
            if (!string.IsNullOrWhiteSpace(playSessionId))
                return playSessionId;
            return (sessionId ?? "") + "|" + (itemId ?? "") + "|" + (deviceId ?? "");
        }

        /// <summary>
        /// 记录播放开始
        /// </summary>
        public void OnPlaybackStarted(string key)
        {
            var now = DateTime.UtcNow;
            _states[key] = new PlaybackSessionState
            {
                IsPaused = false,
                CreatedAt = now,
                LastUpdatedAt = now
            };
        }

        /// <summary>
        /// 处理播放进度事件，返回应产生的事件类型（Paused/Resumed），无变化返回 null
        /// </summary>
        public PlaybackEventType? OnPlaybackProgress(string key, bool isPaused)
        {
            if (!_states.TryGetValue(key, out var state))
            {
                var now = DateTime.UtcNow;
                _states[key] = new PlaybackSessionState
                {
                    IsPaused = isPaused,
                    CreatedAt = now,
                    LastUpdatedAt = now
                };
                return null;
            }

            if (state.IsPaused == isPaused)
                return null;

            state.IsPaused = isPaused;
            state.LastUpdatedAt = DateTime.UtcNow;

            return isPaused ? PlaybackEventType.Paused : PlaybackEventType.Resumed;
        }

        /// <summary>
        /// 移除播放会话
        /// </summary>
        public void OnPlaybackStopped(string key)
        {
            _states.TryRemove(key, out _);
        }

        /// <summary>
        /// 清理过期会话
        /// </summary>
        public void CleanupStale()
        {
            var threshold = DateTime.UtcNow.AddHours(-StaleHours);
            var staleKeys = _states.Where(kvp => kvp.Value.LastUpdatedAt < threshold)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in staleKeys)
            {
                _states.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// 获取当前跟踪的会话数量（用于测试）
        /// </summary>
        public int Count => _states.Count;
    }
}
