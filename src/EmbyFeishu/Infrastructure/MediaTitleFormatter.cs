namespace EmbyFeishu.Infrastructure
{
    /// <summary>
    /// 媒体标题格式化工具
    /// </summary>
    public static class MediaTitleFormatter
    {
        /// <summary>
        /// 根据媒体信息生成显示标题
        /// </summary>
        public static string Format(string itemName, string seriesName, int? seasonNumber, int? episodeNumber, string episodeName)
        {
            if (!string.IsNullOrWhiteSpace(seriesName))
            {
                var title = seriesName;

                if (seasonNumber.HasValue && episodeNumber.HasValue)
                {
                    title += string.Format(" S{0:D2}E{1:D2}", seasonNumber.Value, episodeNumber.Value);
                }

                if (!string.IsNullOrWhiteSpace(episodeName))
                {
                    title += " - " + episodeName;
                }

                return title;
            }

            if (!string.IsNullOrWhiteSpace(itemName))
                return itemName;

            return "未知媒体";
        }
    }
}
