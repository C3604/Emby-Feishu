using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu.Configuration.Groups
{
    /// <summary>
    /// 第六组：媒体库与用户行为 — 新增项目、更新删除、聚合、收藏、已看未看、评分。
    /// </summary>
    public class LibraryAndUserBehaviorGroup : EditableOptionsBase
    {
        public override string EditorTitle => "媒体库与用户行为";

        // 媒体库新增
        [DisplayName("通知新增电影")]
        [IsAdvanced]
        public bool NotifyNewMovies { get; set; } = false;

        [DisplayName("通知新增剧集")]
        [IsAdvanced]
        public bool NotifyNewEpisodes { get; set; } = false;

        [DisplayName("通知新增音乐")]
        [IsAdvanced]
        public bool NotifyNewMusic { get; set; } = false;

        [DisplayName("通知其他新增项目")]
        [IsAdvanced]
        public bool NotifyOtherNewItems { get; set; } = false;

        [DisplayName("通知项目删除")]
        [IsAdvanced]
        public bool NotifyItemsRemoved { get; set; } = false;

        [DisplayName("通知项目更新")]
        [IsAdvanced]
        public bool NotifyItemsUpdated { get; set; } = false;

        // 聚合
        [DisplayName("启用媒体库聚合")]
        [Description("短时间内大量新增/更新合并为汇总消息，避免消息风暴")]
        [IsAdvanced]
        public bool EnableLibraryAggregation { get; set; } = true;

        [DisplayName("聚合窗口（秒）")]
        [Description("范围 10～600")]
        [IsAdvanced]
        [MinValue(10)]
        [MaxValue(600)]
        [VisibleCondition(nameof(EnableLibraryAggregation), SimpleCondition.IsTrue)]
        public int LibraryAggregationWindowSeconds { get; set; } = 60;

        [DisplayName("逐条推送上限")]
        [Description("同一聚合窗口内超过此数量则改为汇总")]
        [IsAdvanced]
        [MinValue(0)]
        [MaxValue(50)]
        [VisibleCondition(nameof(EnableLibraryAggregation), SimpleCondition.IsTrue)]
        public int MaximumIndividualLibraryMessages { get; set; } = 5;

        // 用户行为
        [DisplayName("通知添加收藏")]
        [IsAdvanced]
        public bool NotifyFavoriteAdded { get; set; } = false;

        [DisplayName("通知取消收藏")]
        [IsAdvanced]
        public bool NotifyFavoriteRemoved { get; set; } = false;

        [DisplayName("通知标记已看")]
        [IsAdvanced]
        public bool NotifyMarkedPlayed { get; set; } = false;

        [DisplayName("通知标记未看")]
        [IsAdvanced]
        public bool NotifyMarkedUnplayed { get; set; } = false;

        [DisplayName("通知评分变化")]
        [IsAdvanced]
        public bool NotifyUserRatingChanged { get; set; } = false;
    }
}
