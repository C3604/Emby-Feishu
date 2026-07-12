using System;

namespace EmbyFeishu.Infrastructure
{
    /// <summary>
    /// 媒体项分类。输入取自 Emby 真实 API BaseItem.GetClientTypeName()，
    /// 以 DLL 自身的类型命名为准，不做脆弱的自由字符串猜测。
    /// </summary>
    public enum LibraryItemKind
    {
        Movie,
        Episode,
        Series,
        Audio,
        MusicAlbum,
        MusicArtist,
        Video,
        Trailer,
        Playlist,
        BoxSet,
        Person,
        Folder,
        Other
    }

    public static class MediaTypeClassifier
    {
        /// <summary>
        /// 将 Emby 客户端类型名映射为分类。
        /// </summary>
        public static LibraryItemKind Classify(string clientTypeName)
        {
            if (string.IsNullOrWhiteSpace(clientTypeName))
                return LibraryItemKind.Other;

            switch (clientTypeName.Trim())
            {
                case "Movie": return LibraryItemKind.Movie;
                case "Episode": return LibraryItemKind.Episode;
                case "Series": return LibraryItemKind.Series;
                case "Season": return LibraryItemKind.Series;
                case "Audio": return LibraryItemKind.Audio;
                case "MusicAlbum": return LibraryItemKind.MusicAlbum;
                case "MusicArtist": return LibraryItemKind.MusicArtist;
                case "Trailer": return LibraryItemKind.Trailer;
                case "Video": return LibraryItemKind.Video;
                case "Playlist": return LibraryItemKind.Playlist;
                case "BoxSet": return LibraryItemKind.BoxSet;
                case "Person": return LibraryItemKind.Person;
                case "Folder":
                case "CollectionFolder":
                case "UserRootFolder":
                case "AggregateFolder":
                case "Genre":
                case "MusicGenre":
                case "Studio":
                case "Year":
                case "Chapter":
                    return LibraryItemKind.Folder;
                default:
                    return LibraryItemKind.Other;
            }
        }

        /// <summary>
        /// 判断该分类是否允许触发“新增内容”通知（结合配置开关）。
        /// 过滤 Folder/Person/Genre/Studio 等非媒体项；BoxSet、Trailer、MusicArtist 默认不推送。
        /// </summary>
        public static bool IsNotifiableNewItem(LibraryItemKind kind, PluginOptions options)
        {
            switch (kind)
            {
                case LibraryItemKind.Movie:
                    return options.NotifyNewMovies;
                case LibraryItemKind.Episode:
                case LibraryItemKind.Series:
                    return options.NotifyNewEpisodes;
                case LibraryItemKind.Audio:
                case LibraryItemKind.MusicAlbum:
                    return options.NotifyNewMusic;
                case LibraryItemKind.MusicArtist:
                    // 音乐艺术家默认不推送
                    return false;
                case LibraryItemKind.Video:
                    return options.NotifyOtherNewItems;
                case LibraryItemKind.Trailer:
                    // 预告片默认不推送
                    return false;
                case LibraryItemKind.BoxSet:
                    // 合集默认不推送，需明确开启“其他新增”
                    return options.NotifyOtherNewItems;
                default:
                    return false;
            }
        }

        /// <summary>用于聚合统计的粗分类：电影/剧集/音乐/其他</summary>
        public static string AggregationBucket(LibraryItemKind kind)
        {
            switch (kind)
            {
                case LibraryItemKind.Movie: return "Movie";
                case LibraryItemKind.Episode:
                case LibraryItemKind.Series: return "Episode";
                case LibraryItemKind.Audio:
                case LibraryItemKind.MusicAlbum:
                case LibraryItemKind.MusicArtist: return "Audio";
                default: return "Other";
            }
        }

        public static string DisplayName(LibraryItemKind kind)
        {
            switch (kind)
            {
                case LibraryItemKind.Movie: return "电影";
                case LibraryItemKind.Episode: return "剧集";
                case LibraryItemKind.Series: return "剧集";
                case LibraryItemKind.Audio: return "音乐";
                case LibraryItemKind.MusicAlbum: return "专辑";
                case LibraryItemKind.MusicArtist: return "艺术家";
                case LibraryItemKind.Video: return "视频";
                case LibraryItemKind.Trailer: return "预告片";
                case LibraryItemKind.Playlist: return "播放列表";
                case LibraryItemKind.BoxSet: return "合集";
                default: return "项目";
            }
        }
    }
}
