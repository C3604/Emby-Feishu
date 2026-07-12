using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EmbyFeishu.Infrastructure;
using EmbyFeishu.Models;
using MediaBrowser.Model.Logging;

namespace EmbyFeishu.Events
{
    /// <summary>媒体库聚合中的单条记录（不含任何 Emby 原始对象）</summary>
    public class LibraryChangeRecord
    {
        public NotificationEventType Operation { get; set; } // ItemAdded / ItemUpdated / ItemRemoved
        public LibraryItemKind Kind { get; set; }
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public int? Year { get; set; }
        public string LibraryName { get; set; }
    }

    /// <summary>
    /// 媒体库变更聚合器。窗口内累积新增/更新/删除，到期后逐条或汇总推送。
    /// 插件停止时安全刷新未发送数据。线程安全。
    /// </summary>
    public class LibraryAggregator : IDisposable
    {
        private readonly object _lock = new object();
        private readonly List<LibraryChangeRecord> _records = new List<LibraryChangeRecord>();
        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Action<NotificationEvent> _publish;
        private readonly Func<PluginOptions> _getOptions;
        private readonly Func<string> _getServerName;
        private readonly ILogger _logger;
        private Timer _flushTimer;
        private bool _windowOpen;
        private bool _disposed;

        private const int MaxRecords = 500;

        public LibraryAggregator(
            Action<NotificationEvent> publish,
            Func<PluginOptions> getOptions,
            Func<string> getServerName,
            ILogger logger)
        {
            _publish = publish;
            _getOptions = getOptions;
            _getServerName = getServerName;
            _logger = logger;
        }

        /// <summary>记录一条媒体库变更，加入当前聚合窗口</summary>
        public void Record(LibraryChangeRecord record)
        {
            if (_disposed || record == null)
                return;

            var options = _getOptions();
            if (options == null)
                return;

            lock (_lock)
            {
                // 同一 ItemId + 同一操作在窗口内去重
                var dedupKey = record.Operation + "|" + (record.ItemId ?? record.DisplayName ?? Guid.NewGuid().ToString("N"));
                if (!_seen.Add(dedupKey))
                    return;

                if (_records.Count >= MaxRecords)
                {
                    // 防止无限增长：立即刷新一次
                    FlushLocked();
                }

                _records.Add(record);

                if (!_windowOpen)
                {
                    _windowOpen = true;
                    var windowMs = Math.Max(10, options.LibraryAggregationWindowSeconds) * 1000;
                    _flushTimer?.Dispose();
                    _flushTimer = new Timer(_ => SafeFlush(), null, windowMs, Timeout.Infinite);
                }
            }
        }

        private void SafeFlush()
        {
            try
            {
                lock (_lock)
                {
                    FlushLocked();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("[EmbyFeishu] 媒体库聚合刷新异常: {0}", ex.Message);
            }
        }

        /// <summary>立即刷新（调用者需持有锁）</summary>
        private void FlushLocked()
        {
            _windowOpen = false;
            _flushTimer?.Dispose();
            _flushTimer = null;

            if (_records.Count == 0)
            {
                _seen.Clear();
                return;
            }

            var options = _getOptions();
            var batch = _records.ToList();
            _records.Clear();
            _seen.Clear();

            if (options == null)
                return;

            var added = batch.Where(r => r.Operation == NotificationEventType.ItemAdded).ToList();
            var updated = batch.Count(r => r.Operation == NotificationEventType.ItemUpdated);
            var removed = batch.Count(r => r.Operation == NotificationEventType.ItemRemoved);

            var max = options.MaximumIndividualLibraryMessages;

            // 少量且仅新增：逐条推送；否则汇总
            if (added.Count <= max && updated == 0 && removed == 0)
            {
                foreach (var rec in added)
                {
                    _publish(BuildIndividual(rec));
                }
            }
            else
            {
                _publish(BuildSummary(added, updated, removed));
            }
        }

        private NotificationEvent BuildIndividual(LibraryChangeRecord rec)
        {
            var emoji = EmojiFor(rec.Kind);
            var evt = new NotificationEvent
            {
                EventType = NotificationEventType.ItemAdded,
                Category = NotificationCategory.Library,
                Severity = NotificationSeverity.Success,
                Emoji = emoji,
                Title = "新媒体入库",
                ItemName = rec.DisplayName,
                ItemType = MediaTypeClassifier.DisplayName(rec.Kind),
                ServerName = _getServerName()
            };
            evt.AddField("名称", rec.DisplayName, MessageDetailLevel.Simple);
            evt.AddField("类型", MediaTypeClassifier.DisplayName(rec.Kind));
            evt.AddField("年份", rec.Year?.ToString());
            evt.AddField("媒体库", rec.LibraryName);
            return evt;
        }

        private NotificationEvent BuildSummary(List<LibraryChangeRecord> added, int updated, int removed)
        {
            int movies = added.Count(r => MediaTypeClassifier.AggregationBucket(r.Kind) == "Movie");
            int episodes = added.Count(r => MediaTypeClassifier.AggregationBucket(r.Kind) == "Episode");
            int music = added.Count(r => MediaTypeClassifier.AggregationBucket(r.Kind) == "Audio");
            int others = added.Count - movies - episodes - music;

            var evt = new NotificationEvent
            {
                EventType = NotificationEventType.LibraryAggregated,
                Category = NotificationCategory.Library,
                Severity = NotificationSeverity.Success,
                Emoji = "📚",
                Title = "媒体库更新",
                ServerName = _getServerName()
            };

            evt.AddField("新增电影", movies > 0 ? movies.ToString() : null, MessageDetailLevel.Simple);
            evt.AddField("新增剧集", episodes > 0 ? episodes.ToString() : null, MessageDetailLevel.Simple);
            evt.AddField("新增音乐", music > 0 ? music.ToString() : null);
            evt.AddField("其他新增", others > 0 ? others.ToString() : null);
            evt.AddField("更新项目", updated > 0 ? updated.ToString() : null);
            evt.AddField("删除项目", removed > 0 ? removed.ToString() : null);

            // 列出前若干项目名称
            var names = added.Where(r => !string.IsNullOrWhiteSpace(r.DisplayName))
                .Select(r => r.DisplayName).Take(5).ToList();
            if (names.Count > 0)
            {
                var suffix = added.Count > names.Count ? string.Format(" 等 {0} 项", added.Count) : "";
                evt.AddField("部分内容", string.Join("、", names) + suffix, MessageDetailLevel.Detailed);
            }

            return evt;
        }

        private static string EmojiFor(LibraryItemKind kind)
        {
            switch (kind)
            {
                case LibraryItemKind.Movie: return "🎬";
                case LibraryItemKind.Episode:
                case LibraryItemKind.Series: return "📺";
                case LibraryItemKind.Audio:
                case LibraryItemKind.MusicAlbum:
                case LibraryItemKind.MusicArtist: return "🎵";
                default: return "🆕";
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                lock (_lock)
                {
                    FlushLocked();
                }
            }
            catch (Exception ex)
            {
                _logger.Debug("[EmbyFeishu] 媒体库聚合器释放时刷新异常: {0}", ex.Message);
            }
            _flushTimer?.Dispose();
        }
    }
}
