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
        /// 检查是否跨越了新的进度里程碑。快进跨越多个阈值时只返回当前最高阈值一次。
        /// 无新里程碑返回 null。
        /// </summary>
        public int? CheckMilestone(string key, int percent, System.Collections.Generic.IEnumerable<int> milestones)
        {
            if (milestones == null)
                return null;

            var state = _states.GetOrAdd(key, _ => new Models.PlaybackSessionState
            {
                IsPaused = false,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            });

            int? highest = null;
            lock (state.MilestonesSent)
            {
                foreach (var m in milestones)
                {
                    if (percent >= m && !state.MilestonesSent.Contains(m))
                    {
                        state.MilestonesSent.Add(m);
                        if (highest == null || m > highest.Value)
                            highest = m;
                    }
                }
            }

            if (highest != null)
                state.LastUpdatedAt = DateTime.UtcNow;

            return highest;
        }

        /// <summary>
        /// 检查播放方式是否发生变化。首次观测建立基线返回 false（不通知），
        /// 之后仅在方式真正改变时返回 true。
        /// </summary>
        public bool CheckPlayMethodChanged(string key, string playMethod)
        {
            if (string.IsNullOrWhiteSpace(playMethod))
                return false;

            var state = _states.GetOrAdd(key, _ => new Models.PlaybackSessionState
            {
                IsPaused = false,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            });

            if (state.LastPlayMethod == null)
            {
                state.LastPlayMethod = playMethod;
                return false;
            }

            if (!string.Equals(state.LastPlayMethod, playMethod, StringComparison.OrdinalIgnoreCase))
            {
                state.LastPlayMethod = playMethod;
                state.LastUpdatedAt = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        /// <summary>标记本次播放已发送“播放完成”，返回之前是否已标记</summary>
        public bool MarkCompleted(string key)
        {
            if (_states.TryGetValue(key, out var state))
            {
                var prev = state.CompletedNotified;
                state.CompletedNotified = true;
                return prev;
            }
            return false;
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
