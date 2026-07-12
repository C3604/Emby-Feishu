using System;
using System.Collections.Concurrent;
using System.Linq;
using EmbyFeishu.Models;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 用户媒体数据事件源。UserDataSaved 会因播放进度频繁触发，
    /// 因此缓存旧状态，仅当 IsFavorite / Played / UserRating 真正改变时才通知；
    /// 普通播放进度保存与播放完成触发的“已看”不产生额外通知。
    /// </summary>
    public class UserDataEventSource : IEventSource
    {
        private class Snapshot
        {
            public bool IsFavorite;
            public bool Played;
            public double? Rating;
            public DateTime LastUpdated;
        }

        private const int MaxEntries = 5000;
        private readonly ConcurrentDictionary<string, Snapshot> _cache = new ConcurrentDictionary<string, Snapshot>();
        private readonly IUserDataManager _userDataManager;
        private readonly NotificationContext _ctx;
        private bool _started;

        public string Name => "UserData";

        public UserDataEventSource(IUserDataManager userDataManager, NotificationContext ctx)
        {
            _userDataManager = userDataManager;
            _ctx = ctx;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _userDataManager.UserDataSaved += OnUserDataSaved;
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _userDataManager.UserDataSaved -= OnUserDataSaved;
        }

        public void Dispose() => Stop();

        private void OnUserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null) return;

                var data = e?.UserData;
                var item = e?.Item;
                if (data == null || item == null) return;

                var userId = e.User?.Id.ToString("N") ?? "";
                var itemId = item.Id.ToString("N");
                var key = userId + "|" + itemId;

                var reason = e.SaveReason;

                // 取旧快照；不存在则建立基线（不通知）
                var isNew = !_cache.TryGetValue(key, out var prev);
                var current = new Snapshot
                {
                    IsFavorite = data.IsFavorite,
                    Played = data.Played,
                    Rating = data.Rating,
                    LastUpdated = DateTime.UtcNow
                };

                _cache[key] = current;
                EnforceBound();

                if (isNew)
                    return; // 首次仅建立基线

                // 普通播放进度保存：只更新缓存，不通知
                if (reason == UserDataSaveReason.PlaybackProgress || reason == UserDataSaveReason.PlaybackStart
                    || reason == UserDataSaveReason.Import || reason == UserDataSaveReason.UpdateHideFromResume)
                    return;

                var userName = e.User?.Name;
                var itemName = item.Name;

                // 收藏变化
                if (current.IsFavorite != prev.IsFavorite)
                {
                    if (current.IsFavorite && options.NotifyFavoriteAdded)
                        Publish(NotificationEventType.FavoriteAdded, "添加收藏", "⭐", userName, itemName);
                    else if (!current.IsFavorite && options.NotifyFavoriteRemoved)
                        Publish(NotificationEventType.FavoriteRemoved, "取消收藏", "☆", userName, itemName);
                }

                // 已看/未看变化（播放完成触发的 PlaybackFinished 不重复通知，交由播放完成事件覆盖）
                if (current.Played != prev.Played && reason != UserDataSaveReason.PlaybackFinished)
                {
                    if (current.Played && options.NotifyMarkedPlayed)
                        Publish(NotificationEventType.MarkedPlayed, "标记已看", "✅", userName, itemName);
                    else if (!current.Played && options.NotifyMarkedUnplayed)
                        Publish(NotificationEventType.MarkedUnplayed, "标记未看", "🔄", userName, itemName);
                }

                // 评分变化
                if (!NullableEquals(current.Rating, prev.Rating) && options.NotifyUserRatingChanged)
                {
                    var evt = BuildBase(NotificationEventType.UserRatingChanged, "评分变化", "⭐", userName, itemName);
                    evt.AddField("评分", current.Rating.HasValue ? current.Rating.Value.ToString("0.#") : "已清除");
                    _ctx.Publish(evt);
                }
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理用户数据事件异常: {0}", ex.Message); }
        }

        private void Publish(NotificationEventType type, string title, string emoji, string userName, string itemName)
        {
            _ctx.Publish(BuildBase(type, title, emoji, userName, itemName));
        }

        private NotificationEvent BuildBase(NotificationEventType type, string title, string emoji, string userName, string itemName)
        {
            var evt = new NotificationEvent
            {
                EventType = type,
                Category = NotificationCategory.UserActivity,
                Severity = NotificationSeverity.Information,
                Emoji = emoji,
                Title = title,
                UserName = userName,
                ItemName = itemName
            };
            evt.AddField("用户", userName, MessageDetailLevel.Simple);
            evt.AddField("媒体", itemName, MessageDetailLevel.Simple);
            return evt;
        }

        private static bool NullableEquals(double? a, double? b)
        {
            if (a.HasValue != b.HasValue) return false;
            if (!a.HasValue) return true;
            return Math.Abs(a.Value - b.Value) < 0.0001;
        }

        private void EnforceBound()
        {
            if (_cache.Count <= MaxEntries) return;
            var overflow = _cache.Count - MaxEntries;
            foreach (var kvp in _cache.OrderBy(k => k.Value.LastUpdated).Take(overflow).ToList())
            {
                _cache.TryRemove(kvp.Key, out _);
            }
        }

        /// <summary>清理超过 24 小时未更新的缓存</summary>
        public void CleanupStale()
        {
            var threshold = DateTime.UtcNow.AddHours(-24);
            foreach (var kvp in _cache.Where(k => k.Value.LastUpdated < threshold).ToList())
            {
                _cache.TryRemove(kvp.Key, out _);
            }
        }
    }
}
