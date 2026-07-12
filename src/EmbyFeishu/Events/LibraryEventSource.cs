using System;
using System.Linq;
using EmbyFeishu.Infrastructure;
using EmbyFeishu.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 媒体库事件源。基于 Emby 真实类型（BaseItem.GetClientTypeName）过滤非媒体项，
    /// 支持窗口聚合以避免扫描时的消息风暴。使用 ItemAdded 而非 ItemAdding。
    /// </summary>
    public class LibraryEventSource : IEventSource
    {
        private readonly ILibraryManager _libraryManager;
        private readonly NotificationContext _ctx;
        private readonly LibraryAggregator _aggregator;
        private bool _started;

        public string Name => "Library";

        public LibraryEventSource(ILibraryManager libraryManager, NotificationContext ctx, LibraryAggregator aggregator)
        {
            _libraryManager = libraryManager;
            _ctx = ctx;
            _aggregator = aggregator;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _libraryManager.ItemAdded += OnItemAdded;
            _libraryManager.ItemUpdated += OnItemUpdated;
            _libraryManager.ItemRemoved += OnItemRemoved;
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _libraryManager.ItemAdded -= OnItemAdded;
            _libraryManager.ItemUpdated -= OnItemUpdated;
            _libraryManager.ItemRemoved -= OnItemRemoved;
        }

        public void Dispose() => Stop();

        private void OnItemAdded(object sender, ItemChangeEventArgs e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null) return;

                var rec = BuildRecord(e, NotificationEventType.ItemAdded);
                if (rec == null) return;
                if (!MediaTypeClassifier.IsNotifiableNewItem(rec.Kind, options)) return;

                Dispatch(rec, options);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理媒体新增事件异常: {0}", ex.Message); }
        }

        private void OnItemUpdated(object sender, ItemChangeEventArgs e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !options.NotifyItemsUpdated) return;

                var rec = BuildRecord(e, NotificationEventType.ItemUpdated);
                if (rec == null) return;
                Dispatch(rec, options);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理媒体更新事件异常: {0}", ex.Message); }
        }

        private void OnItemRemoved(object sender, ItemChangeEventArgs e)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !options.NotifyItemsRemoved) return;

                var rec = BuildRecord(e, NotificationEventType.ItemRemoved);
                if (rec == null) return;
                Dispatch(rec, options);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理媒体删除事件异常: {0}", ex.Message); }
        }

        /// <summary>
        /// 聚合开启则进聚合器，否则即时逐条发送。
        /// </summary>
        private void Dispatch(LibraryChangeRecord rec, PluginOptions options)
        {
            if (options.EnableLibraryAggregation)
            {
                _aggregator.Record(rec);
            }
            else
            {
                _ctx.Publish(BuildImmediateEvent(rec));
            }
        }

        /// <summary>
        /// 从事件参数提取不可变记录。过滤文件夹、虚拟项、占位项、人物、流派等非媒体项。
        /// </summary>
        private LibraryChangeRecord BuildRecord(ItemChangeEventArgs e, NotificationEventType op)
        {
            var item = e?.Item;
            if (item == null) return null;

            // 过滤虚拟/占位/主题等内部项
            if (item.IsVirtualItem || item.IsThemeMedia || item.IsPlaceHolder) return null;

            // 基于 Emby 真实类型名分类，过滤文件夹/人物等非媒体项（IsFolder 已弃用，改用类型判断）
            var kind = MediaTypeClassifier.Classify(item.GetClientTypeName());
            if (kind == LibraryItemKind.Folder || kind == LibraryItemKind.Person) return null;

            return new LibraryChangeRecord
            {
                Operation = op,
                Kind = kind,
                ItemId = item.Id.ToString("N"),
                DisplayName = item.Name,
                Year = item.ProductionYear,
                LibraryName = ResolveLibraryName(e)
            };
        }

        private static string ResolveLibraryName(ItemChangeEventArgs e)
        {
            try
            {
                var cf = e.CollectionFolders?.FirstOrDefault();
                if (cf != null && !string.IsNullOrWhiteSpace(cf.Name))
                    return cf.Name;
                return e.Parent?.Name;
            }
            catch { return null; }
        }

        private NotificationEvent BuildImmediateEvent(LibraryChangeRecord rec)
        {
            string title, emoji;
            var severity = NotificationSeverity.Information;
            switch (rec.Operation)
            {
                case NotificationEventType.ItemAdded: title = "新媒体入库"; emoji = "🎬"; severity = NotificationSeverity.Success; break;
                case NotificationEventType.ItemUpdated: title = "媒体已更新"; emoji = "✏️"; break;
                default: title = "媒体已删除"; emoji = "🗑️"; severity = NotificationSeverity.Warning; break;
            }

            var evt = new NotificationEvent
            {
                EventType = rec.Operation,
                Category = NotificationCategory.Library,
                Severity = severity,
                Emoji = emoji,
                Title = title,
                ItemId = rec.ItemId,
                ItemName = rec.DisplayName,
                // 同一 ItemId + 操作短时间去重
                DeduplicationKey = "lib|" + rec.Operation + "|" + (rec.ItemId ?? rec.DisplayName),
                DedupWindowSeconds = 5
            };
            evt.AddField("名称", rec.DisplayName, MessageDetailLevel.Simple);
            evt.AddField("类型", MediaTypeClassifier.DisplayName(rec.Kind));
            evt.AddField("年份", rec.Year?.ToString());
            evt.AddField("媒体库", rec.LibraryName);
            return evt;
        }
    }
}
